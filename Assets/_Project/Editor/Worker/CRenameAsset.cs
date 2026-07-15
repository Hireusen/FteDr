using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// 선택된 에셋들의 이름을 일괄 변형하는 에디터 유틸리티입니다.
/// 접두사/접미사 적용, 공백↔언더바 전환, 단어별 첫 문자 대소문자 변환을 지원합니다.
/// </summary>
public class CRenameAsset : EditorWindow
{
    #region ─────────────────────────▶ 설정 값 ◀─────────────────────────
    private string _prefix = "M_";
    private string _suffix = "";
    private ESeparatorMode _separatorMode = ESeparatorMode.SpaceToUnderscore;
    private ECapitalMode _capitalMode = ECapitalMode.UpperFirst;
    #endregion

    #region ─────────────────────────▶ 윈도우 ◀─────────────────────────
    [MenuItem("Tools/Worker/파일 이름 도구")]
    public static void ShowWindow()
    {
        CRenameAsset window = GetWindow<CRenameAsset>("이름 일괄 변경");
        window.minSize = new Vector2(300f, 220f);
        window.maxSize = new Vector2(300f, 220f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("접두사 / 접미사", EditorStyles.boldLabel);
        _prefix = EditorGUILayout.TextField("접두사", _prefix);
        _suffix = EditorGUILayout.TextField("접미사", _suffix);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("변환 옵션", EditorStyles.boldLabel);
        _separatorMode = (ESeparatorMode)EditorGUILayout.EnumPopup("구분자 변환", _separatorMode);
        _capitalMode = (ECapitalMode)EditorGUILayout.EnumPopup("단어 첫 문자", _capitalMode);
        EditorGUILayout.Space();

        if (GUILayout.Button("선택한 에셋의 이름 변경", GUILayout.Height(30)))
        {
            ExecuteRename();
        }
    }
    #endregion

    #region ─────────────────────────▶ 실행 ◀─────────────────────────
    private void ExecuteRename()
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
            string newName = BuildNewName(oldName);

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
        UDebug.Print($"총 {successCount}개의 에셋 이름을 변경했습니다.");
    }
    #endregion

    #region ─────────────────────────▶ 이름 변형 로직 ◀─────────────────────────
    // 원본 이름에 모든 옵션을 적용해 새 이름을 만든다.
    private string BuildNewName(string original)
    {
        // 1) 공백/언더바를 경계로 단어 분리 (빈 단어 제거)
        string[] words = original.Split(new[] { ' ', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);

        // 2) 각 단어 첫 문자 대소문자 변환
        if (_capitalMode != ECapitalMode.None)
        {
            for (int i = 0; i < words.Length; ++i)
            {
                words[i] = ApplyCapital(words[i]);
            }
        }

        // 3) 구분자 결정 후 재결합
        string separator = _separatorMode == ESeparatorMode.SpaceToUnderscore ? "_" : " ";
        string core = string.Join(separator, words);

        // 4) 접두사/접미사 적용 (중복 부착 방지)
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(_prefix) && !core.StartsWith(_prefix))
        {
            sb.Append(_prefix);
        }
        sb.Append(core);
        if (!string.IsNullOrEmpty(_suffix) && !core.EndsWith(_suffix))
        {
            sb.Append(_suffix);
        }

        return sb.ToString();
    }

    // 단어 하나의 첫 문자만 대소문자 변환한다. (나머지 글자는 유지)
    private string ApplyCapital(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        char first = _capitalMode == ECapitalMode.UpperFirst
            ? char.ToUpper(word[0])
            : char.ToLower(word[0]);

        if (word.Length == 1)
        {
            return first.ToString();
        }
        return first + word.Substring(1);
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    // 공백/언더바 구분자 변환 방식
    private enum ESeparatorMode
    {
        None = 0,
        SpaceToUnderscore,
        UnderscoreToSpace,
    }

    // 단어 첫 문자 대소문자 변환 방식
    private enum ECapitalMode
    {
        None = 0,
        UpperFirst,
        LowerFirst,
    }
    #endregion
}
