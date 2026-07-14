using UnityEngine;
using UnityEditor;

/// <summary>
/// CFolderCustomizerSO의 커스텀 인스펙터입니다.
/// 프로젝트 창에서 선택한 폴더를 GUID와 함께 목록에 바로 추가할 수 있게 하고,
/// 값이 변경되면 드로어 캐시를 비우고 프로젝트 창을 즉시 다시 그립니다.
/// </summary>
[CustomEditor(typeof(CFolderCustomizerSO))]
public class CFolderCustomizerSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            ((CFolderCustomizerSO)target).InvalidateLookup();
            CFolderCustomizerDrawer.Refresh();
        }

        // 슬라이더 드래그처럼 마우스를 누른 채 값이 연속으로 바뀌는 동안에는
        // 프로젝트 창이 실시간으로 따라오지 않는다. hotControl이 0이 아니면(어떤 컨트롤을
        // 드래그/조작 중이면) 매 프레임 프로젝트 창을 다시 그려 실시간 반영한다.
        // 조작이 끝나면 hotControl이 0으로 돌아가 리페인트가 멈추므로 상시 부하가 없다.
        if (GUIUtility.hotControl != 0)
        {
            serializedObject.ApplyModifiedProperties();
            ((CFolderCustomizerSO)target).InvalidateLookup();
            CFolderCustomizerDrawer.RefreshLive();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("도구", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("선택한 폴더 추가"))
            {
                AddSelectedFolders();
            }
            if (GUILayout.Button("경로 캐시 갱신"))
            {
                RefreshCachedPaths();
            }
        }

        EditorGUILayout.HelpBox(
            "GUID로 폴더를 식별하므로 폴더를 옮기거나 이름을 바꿔도 설정이 유지됩니다.\n" +
            "색의 알파(A)를 0으로 두면 부모 폴더의 색을 상속합니다.\n" +
            "배지 위치가 마음에 안 들면 위쪽 '배지 위치/크기'의 오프셋을 조절하세요.",
            MessageType.Info);
    }

    // 선택된 폴더들을 GUID로 직접 읽어 항목에 추가. (오브젝트 타입 해석에 흔들리지 않음)
    private void AddSelectedFolders()
    {
        CFolderCustomizerSO setting = (CFolderCustomizerSO)target;
        SerializedProperty entries = serializedObject.FindProperty("_entries");

        int added = 0;
        foreach (string guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) continue;
            if (setting.FindDirect(guid) != null) continue;

            entries.arraySize++;
            SerializedProperty element = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            element.FindPropertyRelative("guid").stringValue = guid;
            element.FindPropertyRelative("cachedPath").stringValue = path;
            element.FindPropertyRelative("color").colorValue = new Color(0.4f, 0.7f, 1f, 1f);
            element.FindPropertyRelative("customIcon").objectReferenceValue = null;
            element.FindPropertyRelative("builtinIconName").stringValue = null;
            added++;
        }

        if (added == 0)
        {
            UnityEngine.Debug.LogWarning("[FolderCustomizer] 추가할 폴더가 없습니다. 프로젝트 창에서 폴더를 선택했는지, 이미 등록된 폴더인지 확인하세요.");
            return;
        }

        serializedObject.ApplyModifiedProperties();
        setting.InvalidateLookup();
        CFolderCustomizerDrawer.Refresh();
    }

    private void RefreshCachedPaths()
    {
        SerializedProperty entries = serializedObject.FindProperty("_entries");
        for (int i = 0; i < entries.arraySize; ++i)
        {
            SerializedProperty element = entries.GetArrayElementAtIndex(i);
            string guid = element.FindPropertyRelative("guid").stringValue;
            if (string.IsNullOrEmpty(guid)) continue;
            element.FindPropertyRelative("cachedPath").stringValue = AssetDatabase.GUIDToAssetPath(guid);
        }
        serializedObject.ApplyModifiedProperties();
        CFolderCustomizerDrawer.Refresh();
    }
}
