using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// SO 에셋을 스캔하여, 내부의 문자열 ID를 자동 수집하고 매크로 파일을 생성합니다.
/// </summary>
public class CStringIdGenerator : EditorWindow
{
    // 리플렉션으로 읽어올 SO 내부의 대상 ID 필드명
    private const string TARGET_FIELD_NAME = "_id";

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    [MenuItem("Tools/문자열 ID 생성기")]
    public static void ShowWindow()
    {
        GetWindow<CStringIdGenerator>("String ID Generator");
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 진입점
    private static void EntryIdGenerator(Type soType)
    {
        // 해당 타입의 모든 에셋 GUID 수집
        string[] guids = AssetDatabase.FindAssets($"t:{soType.Name}");
        if (guids == null || guids.Length == 0)
        {
            UDebug.Print($"타입({soType})에 해당하는 에셋이 존재하지 않습니다.", LogType.Warning);
            return;
        }
        UDebug.Print($"타입({soType})의 GUID를 {guids.Length}개 수집했습니다.");

        // GUID를 기반으로 SO 인스턴스 리스트 작성
        var soList = BuildSoList(soType, guids);

        // SO 리스트를 바탕으로 C# 스크립트 생성
        StringBuilder sb = BuildScript(soType, soList);
        if (sb == null)
        {
            UDebug.Print($"문자열 생성을 실패했습니다. (Target Field Name: {TARGET_FIELD_NAME} 확인 필요)", LogType.Assert);
            return;
        }

        // 완성된 문자열을 실제 cs 파일로 디스크에 출력
        CreateScriptFile(sb, soType);
    }

    // GUID 배열을 순회하여 SO 인스턴스 로드
    private static List<ScriptableObject> BuildSoList(Type soType, string[] guids)
    {
        List<ScriptableObject> soList = new();
        int length = guids.Length;

        for (int i = 0; i < length; ++i)
        {
            // GUID를 실제 프로젝트 내 파일 경로로 변환
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            // 경로에 있는 에셋 로드
            ScriptableObject so = AssetDatabase.LoadAssetAtPath(path, soType) as ScriptableObject;

            if (so == null)
            {
                UDebug.Print($"알 수 없는 오류 : 경로({path})에서 SO를 로드했지만 비어있습니다.", LogType.Assert);
                continue;
            }
            soList.Add(so);
        }
        return soList;
    }

    // 리플렉션으로 ID 필드 읽고 문자열 작성
    private static StringBuilder BuildScript(Type soType, List<ScriptableObject> soList)
    {
        StringBuilder sb = new();
        // IDE 명명 규칙 경고(IDE1006) 무시 전처리
        sb.AppendLine("#pragma warning disable IDE1006");
        sb.AppendLine("");
        sb.AppendLine("public static partial class Id");
        sb.AppendLine("{");

        // 리플렉션으로 ID 변수 수집
        FieldInfo idField = soType.GetField(TARGET_FIELD_NAME, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (idField == null) return null;

        int length = soList.Count;
        for (int i = 0; i < length; ++i)
        {
            // 실제 인스턴스의 ID 값 추출
            object id = idField.GetValue(soList[i]);
            if (id == null) continue;

            // 공백 + 하이픈 → 언더스코어
            string safeId = id.ToString();
            safeId = safeId.Replace(" ", "_").Replace("-", "_");
            sb.AppendLine($"    public const string {safeId} = \"{id}\";");
        }
        sb.AppendLine("}");
        return sb;
    }

    // 문자열을 받아서 스크립트 파일을 작성합니다.
    private static void CreateScriptFile(StringBuilder sb, Type soType)
    {
        string name = soType.Name;

        // 폴더 자동 생성
        if (!Directory.Exists(K.STRING_ID_EXPORT_PATH))
        {
            Directory.CreateDirectory(K.STRING_ID_EXPORT_PATH);
        }

        string path = $"{K.STRING_ID_EXPORT_PATH}/IdDefine.{name}.cs";
        string newCode = sb.ToString();
        // 변경점이 있는지 확인
        if (File.Exists(path))
        {
            string oldCode = File.ReadAllText(path);
            if (newCode == oldCode)
            {
                UDebug.Print($"{name}.cs의 내용이 동일하므로 작업을 생략했습니다.");
                return;
            }
        }

        // UTF-8 인코딩으로 파일 쓰기 수행
        using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
        {
            sw.Write(newCode);
            AssetDatabase.Refresh();
            UDebug.Print($"경로({K.STRING_ID_EXPORT_PATH})에 {name}을 작성했습니다.");
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnGUI()
    {
        Type soType = null;
        GUILayout.Label("▷ 변환할 SO 선택 ◁", EditorStyles.boldLabel);

        // 버튼 입력
        if (GUILayout.Button("수집품 ID")) soType = typeof(CCollectibleSO);
        if (GUILayout.Button("에너미 ID")) soType = typeof(CEnemySO);
        if (GUILayout.Button("그랩 도구 ID")) soType = typeof(CGrabToolSO);
        if (GUILayout.Button("연료통 ID")) soType = typeof(CFuelTankSO);
        if (GUILayout.Button("플레이어 ID")) soType = typeof(CPlayerSO);
        if (GUILayout.Button("가방 ID")) soType = typeof(CBagSO);
        if (GUILayout.Button("레이더 ID")) soType = typeof(CRadarSO);
        if (GUILayout.Button("추진기 ID")) soType = typeof(CThrusterSO);
        if (GUILayout.Button("사운드 ID")) soType = typeof(CSoundSO);

        if (soType == null) return;

        // 타입 안정성 검사
        if (soType.IsSubclassOf(typeof(ScriptableObject)) || soType == typeof(ScriptableObject))
        {
            UDebug.Print($"문자열 Id 스크립트 생성을 시작합니다. (대상 : {soType.Name})");
            EntryIdGenerator(soType);
        }
        else
        {
            UDebug.Print($"등록한 클래스({soType.Name})가 스크립터블 오브젝트를 상속받지 않습니다.", LogType.Assert);
        }
    }
    #endregion
}
