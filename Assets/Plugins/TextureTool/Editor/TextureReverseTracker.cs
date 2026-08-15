using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class TextureReverseTracker : EditorWindow
{
    private Dictionary<Texture2D, List<string>> safeRefs = new Dictionary<Texture2D, List<string>>();
    private Dictionary<Texture2D, List<string>> warningRefs = new Dictionary<Texture2D, List<string>>();
    private Vector2 scrollPos;
    private bool hasSearched = false;

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

        // [최적화 1] 선택된 텍스처 정보를 딕셔너리로 캐싱 (O(1) 고속 검색용)
        Dictionary<string, Texture2D> targetTexDict = new Dictionary<string, Texture2D>();
        Dictionary<string, string> targetTexDirs = new Dictionary<string, string>(); // 미리 폴더 경로도 계산

        foreach (Texture2D tex in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            targetTexDict[path] = tex;
            targetTexDirs[path] = Path.GetDirectoryName(path).Replace("\\", "/");

            safeRefs[tex] = new List<string>();
            warningRefs[tex] = new List<string>();
        }

        // 2. 프로젝트 내 모든 프리팹 경로 수집
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int totalPrefabs = prefabGuids.Length;

        // [최적화 2] 텍스처 기준이 아닌 '프리팹 기준'으로 단 한 번만 순회
        for (int i = 0; i < totalPrefabs; i++)
        {
            // 진행률 바 업데이트 (취소 기능 추가)
            if (i % 50 == 0)
            {
                bool isCanceled = EditorUtility.DisplayCancelableProgressBar(
                    "역추적 중...",
                    $"프리팹 의존성 검사 중 ({i}/{totalPrefabs})",
                    (float)i / totalPrefabs);

                if (isCanceled)
                {
                    Debug.LogWarning("텍스처 역추적 작업이 사용자에 의해 취소되었습니다.");
                    break;
                }
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

            // 이 프리팹이 참조하는 모든 에셋을 가져옴 (재귀적)
            string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);
            string prefabDir = null; // 지연 평가를 위해 null로 초기화

            foreach (string dep in dependencies)
            {
                // [최적화 3] Contains 배열 검색 대신 Dictionary를 통해 즉시 매칭 확인
                if (targetTexDict.TryGetValue(dep, out Texture2D matchedTex))
                {
                    // 매칭되었을 때만 프리팹 경로 계산 (문자열 연산 최소화)
                    if (prefabDir == null)
                        prefabDir = Path.GetDirectoryName(prefabPath).Replace("\\", "/");

                    string texDir = targetTexDirs[dep];

                    if (prefabDir.StartsWith(texDir))
                    {
                        safeRefs[matchedTex].Add(prefabPath);
                    }
                    else
                    {
                        warningRefs[matchedTex].Add(prefabPath);
                    }
                }
            }
        }

        EditorUtility.ClearProgressBar();
        Debug.Log("텍스처 역추적이 완료되었습니다.");
    }
}
