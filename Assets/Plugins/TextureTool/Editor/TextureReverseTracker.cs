using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TextureReverseTracker : EditorWindow
{
    private Dictionary<Texture2D, List<string>> safeRefs = new Dictionary<Texture2D, List<string>>();
    private Dictionary<Texture2D, List<string>> warningRefs = new Dictionary<Texture2D, List<string>>();
    private Vector2 scrollPos;
    private bool hasSearched = false;

    // 향후 프리팹 관련 에디터들을 따로 분류해두실 계획에 맞춰 메뉴 경로를 설정했습니다.
    [MenuItem("Tools/Prefab Editor/텍스처 역추적 (폴더 의존성 검사)")]
    public static void ShowWindow()
    {
        GetWindow<TextureReverseTracker>("텍스처 역추적 검사기").Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("프로젝트 창에서 검사할 텍스처(들)를 선택한 후 실행하세요.", EditorStyles.helpBox);

        if (GUILayout.Button("선택한 텍스처 역추적 실행", GUILayout.Height(30)))
        {
            RunTracking();
        }

        GUILayout.Space(10);

        if (hasSearched)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (safeRefs.Count == 0 && warningRefs.Count == 0)
            {
                GUILayout.Label("참조하고 있는 프리팹이 없습니다.", EditorStyles.boldLabel);
            }

            // 외부 폴더 참조 (경고) 출력
            foreach (var kvp in warningRefs)
            {
                if (kvp.Value.Count > 0)
                {
                    GUI.color = new Color(1f, 0.7f, 0.7f); // 옅은 빨간색
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = Color.white;

                    GUILayout.Label($"[외부 참조 주의] {kvp.Key.name}", EditorStyles.boldLabel);
                    string texDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(kvp.Key));
                    GUILayout.Label($"텍스처 위치: {texDir}");

                    GUILayout.Space(5);
                    foreach (string prefabPath in kvp.Value)
                    {
                        GUILayout.Label($" ➔ {prefabPath}");
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }

            // 동일 폴더 참조 (안전) 출력
            foreach (var kvp in safeRefs)
            {
                if (kvp.Value.Count > 0)
                {
                    GUI.color = new Color(0.7f, 1f, 0.7f); // 옅은 초록색
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUI.color = Color.white;

                    GUILayout.Label($"[안전] {kvp.Key.name}", EditorStyles.boldLabel);
                    foreach (string prefabPath in kvp.Value)
                    {
                        GUILayout.Label($" ➔ {prefabPath}");
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void RunTracking()
    {
        safeRefs.Clear();
        warningRefs.Clear();
        hasSearched = true;

        // 1. 선택된 텍스처들 가져오기
        Object[] selectedObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("선택된 텍스처가 없습니다.");
            return;
        }

        // 2. 프로젝트 내 모든 프리팹 경로 수집
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        List<string> allPrefabPaths = new List<string>();
        foreach (string guid in prefabGuids)
        {
            allPrefabPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
        }

        // 진행률 바 표시 (프리팹이 많으면 시간이 걸릴 수 있음)
        int totalPrefabs = allPrefabPaths.Count;

        foreach (Texture2D tex in selectedObjects)
        {
            safeRefs[tex] = new List<string>();
            warningRefs[tex] = new List<string>();
            string texPath = AssetDatabase.GetAssetPath(tex);
            string texDir = Path.GetDirectoryName(texPath).Replace("\\\\", "/");

            for (int i = 0; i < totalPrefabs; i++)
            {
                string prefabPath = allPrefabPaths[i];

                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("역추적 중...", $"{tex.name} 검사 중 ({i}/{totalPrefabs})", (float)i / totalPrefabs);
                }

                // 해당 프리팹의 의존성에 이 텍스처가 포함되어 있는지 확인
                string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);
                if (dependencies.Contains(texPath))
                {
                    string prefabDir = Path.GetDirectoryName(prefabPath).Replace("\\\\", "/");

                    // 폴더 비교: 프리팹이 텍스처와 같은 폴더(혹은 하위 폴더)에 있는지 확인
                    if (prefabDir.StartsWith(texDir))
                    {
                        safeRefs[tex].Add(prefabPath);
                    }
                    else
                    {
                        warningRefs[tex].Add(prefabPath);
                    }
                }
            }
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("텍스처 역추적이 완료되었습니다.");
    }
}
