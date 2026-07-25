using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// 선택한 CCollectibleSO들의 식별 정보를 CSV로 추출하는 에디터 유틸리티입니다.
/// </summary>
public class CCollectibleNameExporter : EditorWindow
{
    #region ─────────────────────────▶ 설정 값 ◀─────────────────────────
    private bool _onlyEmpty = false; // 이름 또는 설명이 비어있는 것만 추출
    private string _lastCsv = "";    // 미리보기용 캐시
    private Vector2 _scroll;
    #endregion

    #region ─────────────────────────▶ 윈도우 ◀─────────────────────────
    [MenuItem("Tools/IO/수집품 이름 추출기")]
    public static void ShowWindow()
    {
        CCollectibleNameExporter window = GetWindow<CCollectibleNameExporter>("수집품 이름 추출기");
        window.minSize = new Vector2(420f, 360f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "프로젝트 창에서 CCollectibleSO들을 선택한 뒤 실행하세요.\n" +
            "id, name, description을 CSV로 뽑습니다. (이름/설명 채우기용 목록)",
            MessageType.Info);
        EditorGUILayout.Space();

        _onlyEmpty = EditorGUILayout.Toggle("이름/설명이 빈 것만 추출", _onlyEmpty);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("클립보드로 복사", GUILayout.Height(28)))
        {
            ExportToClipboard();
        }
        if (GUILayout.Button("CSV 파일로 저장", GUILayout.Height(28)))
        {
            ExportToFile();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_lastCsv))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200f));
            EditorGUILayout.TextArea(_lastCsv);
            EditorGUILayout.EndScrollView();
        }
    }
    #endregion

    #region ─────────────────────────▶ 실행 ◀─────────────────────────
    private void ExportToClipboard()
    {
        string csv = BuildCsv(out int count);
        if (count == 0) return;

        _lastCsv = csv;
        EditorGUIUtility.systemCopyBuffer = csv;
        UDebug.Print($"{count}개 수집품 정보를 클립보드에 복사했습니다.");
    }

    private void ExportToFile()
    {
        string csv = BuildCsv(out int count);
        if (count == 0) return;

        string path = EditorUtility.SaveFilePanel("CSV 저장", "", "collectible_names.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        _lastCsv = csv;
        System.IO.File.WriteAllText(path, csv, new UTF8Encoding(true)); // BOM 포함(엑셀 한글 호환)
        UDebug.Print($"{count}개 수집품 정보를 저장했습니다: {path}");
    }
    #endregion

    #region ─────────────────────────▶ CSV 생성 ◀─────────────────────────
    // 선택된 CCollectibleSO를 순회하며 CSV 문자열을 만든다. count에 실제 추출 수를 반환.
    private string BuildCsv(out int count)
    {
        count = 0;
        Object[] selected = Selection.GetFiltered(typeof(CCollectibleSO), SelectionMode.Assets);

        if (selected.Length == 0)
        {
            UDebug.Print("선택된 CCollectibleSO가 없습니다.", LogType.Warning);
            return "";
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("id,name,description"); // 헤더

        for (int i = 0; i < selected.Length; ++i)
        {
            CCollectibleSO so = selected[i] as CCollectibleSO;
            if (so == null) continue;

            // AUnitSO 계열(_name/_description)이 아닐 수 있으므로 SerializedObject로 안전 접근
            SerializedObject sObj = new SerializedObject(so);
            SerializedProperty idProp = sObj.FindProperty("_id");
            SerializedProperty nameProp = sObj.FindProperty("_name");
            SerializedProperty descProp = sObj.FindProperty("_description");

            string id = idProp != null ? idProp.stringValue : so.name;
            string name = nameProp != null ? nameProp.stringValue : "";
            string desc = descProp != null ? descProp.stringValue : "";

            if (_onlyEmpty && !IsBlank(name) && !IsBlank(desc)) continue;

            sb.Append(Escape(id)).Append(',')
              .Append(Escape(name)).Append(',')
              .Append(Escape(desc)).Append('\n');
            ++count;
        }

        if (count == 0)
        {
            UDebug.Print("추출 대상이 없습니다. (필터 조건 확인)", LogType.Warning);
            return "";
        }
        return sb.ToString();
    }

    // 기본값("이름"/"설명")이나 실제 빈 문자열을 비어있음으로 간주.
    private bool IsBlank(string s)
    {
        return string.IsNullOrEmpty(s) || s == "이름" || s == "설명";
    }

    // CSV 필드 escape (RFC4180): 콤마/따옴표/줄바꿈 포함 시 따옴표로 감싸고 내부 따옴표는 2배.
    private string Escape(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";

        bool needQuote = field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");
        if (!needQuote) return field;

        string escaped = field.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
    #endregion
}
