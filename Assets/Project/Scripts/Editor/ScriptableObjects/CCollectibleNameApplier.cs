using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// CSV를 읽어 각 CCollectibleSO의 이름/설명 필드에 일괄 기록하는 에디터입니다.
/// </summary>
public class CCollectibleNameApplier : EditorWindow
{
    #region ─────────────────────────▶ 설정 값 ◀─────────────────────────
    private TextAsset _csvAsset;            // 프로젝트 내 CSV 에셋(선택)
    private bool _overwriteExisting = true; // 이미 값이 있어도 덮어쓸지
    private string _report = "";
    private Vector2 _scroll;
    #endregion

    #region ─────────────────────────▶ 윈도우 ◀─────────────────────────
    [MenuItem("Tools/수집품 이름 적용기")]
    public static void ShowWindow()
    {
        CCollectibleNameApplier window = GetWindow<CCollectibleNameApplier>("수집품 이름 적용기");
        window.minSize = new Vector2(420f, 360f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "id,name,description 형식의 CSV를 SO에 기록합니다.\n" +
            "CSV 에셋을 지정하거나, 프로젝트 밖 파일은 아래 버튼으로 선택하세요.",
            MessageType.Info);
        EditorGUILayout.Space();

        _csvAsset = (TextAsset)EditorGUILayout.ObjectField("CSV 에셋", _csvAsset, typeof(TextAsset), false);
        _overwriteExisting = EditorGUILayout.Toggle("기존 값 덮어쓰기", _overwriteExisting);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("지정한 CSV 에셋으로 적용", GUILayout.Height(28)))
        {
            if (_csvAsset != null)
            {
                ApplyFromText(_csvAsset.text);
            }
            else
            {
                UDebug.Print("CSV 에셋을 지정하세요.", LogType.Warning);
            }
        }
        if (GUILayout.Button("파일 선택해서 적용", GUILayout.Height(28)))
        {
            ApplyFromFileDialog();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_report))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("결과", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(180f));
            EditorGUILayout.TextArea(_report);
            EditorGUILayout.EndScrollView();
        }
    }
    #endregion

    #region ─────────────────────────▶ 실행 ◀─────────────────────────
    private void ApplyFromFileDialog()
    {
        string path = EditorUtility.OpenFilePanel("CSV 선택", "", "csv");
        if (string.IsNullOrEmpty(path)) return;

        string text = System.IO.File.ReadAllText(path);
        ApplyFromText(text);
    }

    private void ApplyFromText(string csvText)
    {
        // 1) CSV 파싱 → id -> (name, description)
        Dictionary<string, (string name, string desc)> map = ParseCsv(csvText);
        if (map.Count == 0)
        {
            UDebug.Print("CSV에서 유효한 행을 찾지 못했습니다.", LogType.Warning);
            return;
        }

        // 2) 프로젝트의 모든 CCollectibleSO를 id로 색인
        Dictionary<string, CCollectibleSO> soById = BuildSoIndex();

        // 3) 매칭하여 기록
        int applied = 0;
        int skippedExisting = 0;
        int missingField = 0;
        List<string> notFound = new List<string>();

        foreach (KeyValuePair<string, (string name, string desc)> kv in map)
        {
            if (!soById.TryGetValue(kv.Key, out CCollectibleSO so))
            {
                notFound.Add(kv.Key);
                continue;
            }

            int result = WriteToSo(so, kv.Value.name, kv.Value.desc);
            if (result == 1) ++applied;
            else if (result == 0) ++skippedExisting;
            else ++missingField;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        BuildReport(map.Count, applied, skippedExisting, missingField, notFound);
        UDebug.Print($"이름/설명 적용 완료: 성공 {applied}, 건너뜀 {skippedExisting}, 필드없음 {missingField}, 미발견 {notFound.Count}.");
    }

    // SO 하나에 기록. 반환: 1=적용, 0=기존값이라 건너뜀, -1=필드 없음
    private int WriteToSo(CCollectibleSO so, string name, string desc)
    {
        SerializedObject sObj = new SerializedObject(so);
        SerializedProperty nameProp = sObj.FindProperty("_name");
        SerializedProperty descProp = sObj.FindProperty("_description");

        // _name/_description이 없는 상속 구조면 기록 불가
        if (nameProp == null || descProp == null)
        {
            return -1;
        }

        // 덮어쓰기 옵션이 꺼져 있고 이미 실제 값이 있으면 건너뜀
        if (!_overwriteExisting && !IsBlank(nameProp.stringValue) && !IsBlank(descProp.stringValue))
        {
            return 0;
        }

        nameProp.stringValue = name;
        descProp.stringValue = desc;
        sObj.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(so);
        return 1;
    }
    #endregion

    #region ─────────────────────────▶ SO 색인 ◀─────────────────────────
    // 프로젝트의 모든 CCollectibleSO를 _id(없으면 파일명)로 색인한다.
    private Dictionary<string, CCollectibleSO> BuildSoIndex()
    {
        Dictionary<string, CCollectibleSO> dict = new Dictionary<string, CCollectibleSO>();
        string[] guids = AssetDatabase.FindAssets("t:CCollectibleSO");

        for (int i = 0; i < guids.Length; ++i)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            CCollectibleSO so = AssetDatabase.LoadAssetAtPath<CCollectibleSO>(path);
            if (so == null) continue;

            SerializedObject sObj = new SerializedObject(so);
            SerializedProperty idProp = sObj.FindProperty("_id");
            string key = (idProp != null && !string.IsNullOrEmpty(idProp.stringValue)) ? idProp.stringValue : so.name;

            if (!dict.ContainsKey(key))
            {
                dict.Add(key, so);
            }
        }
        return dict;
    }
    #endregion

    #region ─────────────────────────▶ CSV 파싱 ◀─────────────────────────
    // RFC4180 방식 CSV 파싱. 따옴표로 감싼 필드 내부의 콤마/줄바꿈/이스케이프 따옴표 처리.
    private Dictionary<string, (string, string)> ParseCsv(string text)
    {
        Dictionary<string, (string, string)> map = new Dictionary<string, (string, string)>();
        List<string[]> rows = SplitRows(text);

        // 헤더가 있으면 건너뛰기 (첫 칼럼이 "id"면 헤더로 간주)
        int start = 0;
        if (rows.Count > 0 && rows[0].Length > 0 && rows[0][0].Trim().ToLower() == "id")
        {
            start = 1;
        }

        for (int i = start; i < rows.Count; ++i)
        {
            string[] cols = rows[i];
            if (cols.Length < 1 || string.IsNullOrEmpty(cols[0])) continue;

            string id = cols[0];
            string name = cols.Length > 1 ? cols[1] : "";
            string desc = cols.Length > 2 ? cols[2] : "";
            map[id] = (name, desc);
        }
        return map;
    }

    // 전체 텍스트를 행 → 필드 배열로 분해 (따옴표 상태를 추적)
    private List<string[]> SplitRows(string text)
    {
        List<string[]> rows = new List<string[]>();
        List<string> fields = new List<string>();
        StringBuilder cur = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; ++i)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // 다음 문자도 따옴표면 이스케이프된 따옴표
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cur.Append('"');
                        ++i;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    cur.Append(c);
                }
                continue;
            }

            if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(cur.ToString());
                cur.Clear();
            }
            else if (c == '\n' || c == '\r')
            {
                // 줄 끝: CRLF의 \r\n 중복 처리
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    ++i;
                }
                fields.Add(cur.ToString());
                cur.Clear();
                if (fields.Count > 0)
                {
                    rows.Add(fields.ToArray());
                }
                fields = new List<string>();
            }
            else
            {
                cur.Append(c);
            }
        }

        // 마지막 필드/행 마무리 (파일 끝에 개행 없을 때)
        if (cur.Length > 0 || fields.Count > 0)
        {
            fields.Add(cur.ToString());
            rows.Add(fields.ToArray());
        }
        return rows;
    }
    #endregion

    #region ─────────────────────────▶ 유틸 ◀─────────────────────────
    private bool IsBlank(string s)
    {
        return string.IsNullOrEmpty(s) || s == "이름" || s == "설명";
    }

    private void BuildReport(int total, int applied, int skipped, int missingField, List<string> notFound)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"CSV 행: {total}");
        sb.AppendLine($"적용: {applied}");
        sb.AppendLine($"기존값 유지(건너뜀): {skipped}");
        sb.AppendLine($"_name/_description 없음: {missingField}");
        sb.AppendLine($"매칭 SO 미발견: {notFound.Count}");

        if (missingField > 0)
        {
            sb.AppendLine();
            sb.AppendLine("※ 필드 없음이 있다면 CCollectibleSO가 AUnitSO를 상속하는지 확인하세요.");
        }
        if (notFound.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[미발견 id]");
            int show = Mathf.Min(notFound.Count, 30);
            for (int i = 0; i < show; ++i)
            {
                sb.AppendLine("  " + notFound[i]);
            }
            if (notFound.Count > show)
            {
                sb.AppendLine($"  ... 외 {notFound.Count - show}개");
            }
        }
        _report = sb.ToString();
    }
    #endregion
}
