using UnityEngine;
using UnityEditor;

/// <summary>
/// 선택된 에셋들의 이름에서 특정 문자열을 찾아 다른 문자열로 일괄 치환하는 에디터 유틸리티입니다.
/// 예) 이름에 포함된 "Red"를 모두 "Blue"로 변경.
/// </summary>
public class CReplaceAssetName : EditorWindow
{
    #region ─────────────────────────▶ 설정 값 ◀─────────────────────────
    private string _find = "Red";
    private string _replace = "Blue";
    private bool _caseSensitive = true;
    #endregion

    #region ─────────────────────────▶ 윈도우 ◀─────────────────────────
    [MenuItem("Tools/Worker/이름 문자열 치환")]
    public static void ShowWindow()
    {
        CReplaceAssetName window = GetWindow<CReplaceAssetName>("이름 문자열 치환");
        window.minSize = new Vector2(320f, 180f);
        window.maxSize = new Vector2(320f, 180f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("치환할 문자열", EditorStyles.boldLabel);
        _find = EditorGUILayout.TextField("찾을 단어", _find);
        _replace = EditorGUILayout.TextField("바꿀 단어", _replace);
        EditorGUILayout.Space();

        _caseSensitive = EditorGUILayout.Toggle("대소문자 구분", _caseSensitive);
        EditorGUILayout.Space();

        // 찾을 단어가 비어 있으면 실행 버튼 비활성화
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_find)))
        {
            if (GUILayout.Button("선택한 에셋의 이름 치환", GUILayout.Height(30)))
            {
                ExecuteReplace();
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 실행 ◀─────────────────────────
    private void ExecuteReplace()
    {
        // 프로젝트 창에서 선택된 에셋 추출
        Object[] selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

        if (selectedAssets.Length == 0)
        {
            UDebug.Print("선택한 에셋이 없습니다.", LogType.Warning);
            return;
        }

        int successCount = 0;
        int length = selectedAssets.Length;
        for (int i = 0; i < length; ++i)
        {
            Object obj = selectedAssets[i];
            string path = AssetDatabase.GetAssetPath(obj);

            // 폴더 제외
            if (AssetDatabase.IsValidFolder(path))
            {
                continue;
            }

            string oldName = obj.name;
            string newName = ReplaceName(oldName);

            // 변경점이 없으면 건너뜀
            if (oldName == newName)
            {
                continue;
            }

            string errorMsg = AssetDatabase.RenameAsset(path, newName);
            if (string.IsNullOrEmpty(errorMsg))
            {
                ++successCount;
            }
            else
            {
                UDebug.Print($"{oldName}의 이름을 변경하지 못했습니다.\n{errorMsg}", LogType.Error, obj);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UDebug.Print($"총 {successCount}개의 에셋 이름을 치환했습니다.");
    }
    #endregion

    #region ─────────────────────────▶ 치환 로직 ◀─────────────────────────
    // 원본 이름에서 찾을 단어를 바꿀 단어로 치환한다.
    private string ReplaceName(string original)
    {
        if (_caseSensitive)
        {
            return original.Replace(_find, _replace);
        }

        // 대소문자 무시 치환: 원본의 대소문자와 무관하게 _find와 일치하는 구간을 찾아 교체
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int cursor = 0;
        while (cursor < original.Length)
        {
            int found = original.IndexOf(_find, cursor, System.StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                sb.Append(original, cursor, original.Length - cursor);
                break;
            }
            sb.Append(original, cursor, found - cursor);
            sb.Append(_replace);
            cursor = found + _find.Length;
        }
        return sb.ToString();
    }
    #endregion
}
