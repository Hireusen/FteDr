using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FindUsedTexturesSelected : EditorWindow
{
    private bool excludeFonts = true;
    private bool excludePackages = true; // 읽기 전용 패키지 제외 옵션

    [MenuItem("Tools/텍스처/프리팹의 텍스처 선택")]
    public static void ShowWindow()
    {
        GetWindow<FindUsedTexturesSelected>("텍스처 추출 설정").Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("추출 옵션", EditorStyles.boldLabel);

        excludeFonts = EditorGUILayout.Toggle("폰트(SDF, TextMesh 등) 제외", excludeFonts);
        excludePackages = EditorGUILayout.Toggle("Packages 폴더(읽기전용) 제외", excludePackages);

        GUILayout.Space(15);

        if (GUILayout.Button("선택한 프리팹 텍스처 추출 실행", GUILayout.Height(30)))
        {
            SelectUsedTexturesForSelectedPrefabs(excludeFonts, excludePackages);
        }
    }

    private static void SelectUsedTexturesForSelectedPrefabs(bool excludeFonts, bool excludePackages)
    {
        GameObject[] selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Assets);

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("선택된 프리팹이 없습니다. 프로젝트 창에서 검사할 UI 프리팹들을 먼저 선택해주세요.");
            return;
        }

        List<string> selectedPaths = new List<string>();
        foreach (GameObject obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
            {
                selectedPaths.Add(path);
            }
        }

        if (selectedPaths.Count == 0)
        {
            Debug.LogWarning("선택된 에셋 중 프리팹(.prefab)이 없습니다.");
            return;
        }

        string[] dependencies = AssetDatabase.GetDependencies(selectedPaths.ToArray(), true);
        List<Object> usedTextures = new List<Object>();

        foreach (string path in dependencies)
        {
            // 1. Packages 폴더(읽기 전용) 원천 차단
            if (excludePackages && path.StartsWith("Packages/"))
            {
                continue;
            }

            string lowerPath = path.ToLower();

            // 2. 폰트 관련 파일 차단
            if (excludeFonts)
            {
                if (lowerPath.Contains("font") || lowerPath.Contains("sdf") || lowerPath.Contains("textmesh"))
                {
                    continue;
                }
            }

            // 3. 텍스처만 추출
            if (lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") ||
                lowerPath.EndsWith(".jpeg") || lowerPath.EndsWith(".tga") ||
                lowerPath.EndsWith(".psd"))
            {
                Object tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    usedTextures.Add(tex);
                }
            }
        }

        Selection.objects = usedTextures.ToArray();
        Debug.Log($"선택한 프리팹 {selectedPaths.Count}개에서 총 {usedTextures.Count}개의 텍스처를 찾아 선택했습니다.");
    }
}
