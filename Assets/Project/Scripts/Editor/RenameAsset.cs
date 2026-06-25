using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 선택된 에셋들에 특정 접두사를 일괄 적용하는 에디터 유틸리티입니다.
/// </summary>
public class CRenameAsset : EditorWindow
{
    private string _prefix = "M_";
    private bool _replaceBlank = true;

    [MenuItem("Tools/파일 이름 도구")]
    public static void ShowWindow()
    {
        CRenameAsset window = GetWindow<CRenameAsset>("접두사 일괄 적용");
        window.minSize = new Vector2(240f, 90f);
        window.maxSize = new Vector2(240f, 90f);
        window.Show();
    }

    private void ExecuteRename()
    {
        // 프로젝트 창에서 선택된 객체 추출
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
            if (AssetDatabase.IsValidFolder(path)) continue;

            string oldName = obj.name;
            string newName = oldName;
            newName = char.ToUpper(newName[0]) + newName.Substring(1); // 첫 문자 대문자
            // 공백 변환
            if (_replaceBlank)
            {
                newName = newName.Replace(" ", "_");
            }

            // 접두사 적용
            if (!newName.StartsWith(_prefix))
            {
                newName = _prefix + newName;
            }

            if (oldName == newName) continue;
            // 새로운 이름 적용
            string errorMsg = AssetDatabase.RenameAsset(path, newName);
            if (string.IsNullOrEmpty(errorMsg))
            {
                successCount++;
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

    private void OnGUI()
    {
        _prefix = EditorGUILayout.TextField("적용할 접두사", _prefix);
        _replaceBlank = EditorGUILayout.Toggle("공백을 언더바로 변환합니다.", _replaceBlank);
        EditorGUILayout.Space();

        if (GUILayout.Button("선택한 에셋의 이름 변경", GUILayout.Height(30)))
        {
            ExecuteRename();
        }
    }
}
