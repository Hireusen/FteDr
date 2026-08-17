using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class DependencyCopier : EditorWindow
{
    private Object targetAsset;
    private DefaultAsset destinationFolder;

    private bool ignoreScripts = true;
    private bool ignoreShaders = true;
    private bool ignoreFonts = true;
    private bool ignoreTMP = true;

    // 리포트 UI 출력을 위한 상태 변수들
    private bool showReport = false;
    private int copiedCount = 0;
    private List<string> skippedScripts = new List<string>();
    private List<string> skippedOthers = new List<string>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/에셋 통째로 복사기 (Dependency Copier)")]
    public static void ShowWindow()
    {
        // 창 크기를 리포트 보기에 넉넉하게 조금 키웁니다.
        GetWindow<DependencyCopier>("통째로 복사기").minSize = new Vector2(400, 500);
    }

    void OnGUI()
    {
        GUILayout.Label("프리팹 및 연결된 에셋 복사", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetAsset = EditorGUILayout.ObjectField("복사할 원본 (프리팹)", targetAsset, typeof(Object), false);
        destinationFolder = (DefaultAsset)EditorGUILayout.ObjectField("복사될 도착 폴더", destinationFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space();

        GUILayout.Label("필터링 설정 (자동 복사에서 제외할 항목)", EditorStyles.boldLabel);
        ignoreScripts = EditorGUILayout.Toggle("C# 스크립트 제외 (.cs)", ignoreScripts);
        ignoreShaders = EditorGUILayout.Toggle("셰이더 제외 (.shader, .cginc)", ignoreShaders);
        ignoreFonts = EditorGUILayout.Toggle("일반 폰트 제외 (.ttf, .otf)", ignoreFonts);
        ignoreTMP = EditorGUILayout.Toggle("TextMeshPro 폰트/에셋 제외 (SDF 포함)", ignoreTMP);

        EditorGUILayout.Space();

        if (GUILayout.Button("🚀 스마트 복사 실행", GUILayout.Height(40)))
        {
            ExecuteCopy();
        }

        // === 실행 완료 후 리포트 UI 표시 영역 ===
        if (showReport)
        {
            EditorGUILayout.Space();
            DrawLine();
            EditorGUILayout.Space();

            GUILayout.Label("📋 복사 결과 상세 리포트", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"성공적으로 {copiedCount}개의 에셋(텍스처, 메테리얼 등)을 복사하고 엮어주었습니다.", MessageType.Info);

            if (skippedScripts.Count == 0 && skippedOthers.Count == 0)
            {
                EditorGUILayout.HelpBox("제외(필터링)된 에셋이 없습니다.", MessageType.None);
            }
            else
            {
                // 스크롤 가능한 박스 영역 시작
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box", GUILayout.ExpandHeight(true));

                if (skippedScripts.Count > 0)
                {
                    GUILayout.Label($"[제외된 C# 스크립트: {skippedScripts.Count}개]", EditorStyles.boldLabel);
                    foreach (var path in skippedScripts)
                    {
                        EditorGUILayout.SelectableLabel(path, GUILayout.Height(16));
                    }
                    EditorGUILayout.Space();
                }

                if (skippedOthers.Count > 0)
                {
                    GUILayout.Label($"[제외된 셰이더/폰트/TMP 등: {skippedOthers.Count}개]", EditorStyles.boldLabel);
                    foreach (var path in skippedOthers)
                    {
                        EditorGUILayout.SelectableLabel(path, GUILayout.Height(16));
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void ExecuteCopy()
    {
        if (targetAsset == null || destinationFolder == null)
        {
            Debug.LogError("원본 에셋과 도착 폴더를 모두 지정해주세요.");
            return;
        }

        // 이전 리포트 데이터 초기화
        skippedScripts.Clear();
        skippedOthers.Clear();
        copiedCount = 0;
        showReport = false;

        string targetPath = AssetDatabase.GetAssetPath(targetAsset);
        string destFolderPath = AssetDatabase.GetAssetPath(destinationFolder);

        string[] allDependencies = AssetDatabase.GetDependencies(targetPath, true);
        List<string> validDependencies = new List<string>();

        foreach (string path in allDependencies)
        {
            if (path.StartsWith("Packages/") || path.StartsWith("Resources/unity_builtin_extra"))
                continue;

            bool skip = false;

            // 파일명 대신 path(전체 경로)를 저장합니다.
            if (ignoreScripts && (path.EndsWith(".cs") || path.EndsWith(".dll")))
            {
                skippedScripts.Add(path);
                skip = true;
            }
            else if (ignoreShaders && (path.EndsWith(".shader") || path.EndsWith(".cginc") || path.EndsWith(".compute")))
            {
                skippedOthers.Add(path);
                skip = true;
            }
            else if (ignoreFonts && (path.EndsWith(".ttf") || path.EndsWith(".otf")))
            {
                skippedOthers.Add(path);
                skip = true;
            }
            else if (ignoreTMP && (path.Contains("TextMesh Pro") || path.Contains("TMP_") || path.Contains("SDF")))
            {
                skippedOthers.Add(path);
                skip = true;
            }

            if (!skip)
            {
                validDependencies.Add(path);
            }
        }

        Dictionary<string, string> guidMap = new Dictionary<string, string>();
        List<string> newAssetPaths = new List<string>();

        try
        {
            for (int i = 0; i < validDependencies.Count; i++)
            {
                string oldPath = validDependencies[i];
                EditorUtility.DisplayProgressBar("에셋 복사 중", oldPath, (float)i / validDependencies.Count);

                string fileName = Path.GetFileName(oldPath);
                string newPath = AssetDatabase.GenerateUniqueAssetPath($"{destFolderPath}/{fileName}");

                if (AssetDatabase.CopyAsset(oldPath, newPath))
                {
                    string oldGUID = AssetDatabase.AssetPathToGUID(oldPath);
                    string newGUID = AssetDatabase.AssetPathToGUID(newPath);
                    guidMap[oldGUID] = newGUID;
                    newAssetPaths.Add(newPath);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        try
        {
            for (int i = 0; i < newAssetPaths.Count; i++)
            {
                string path = newAssetPaths[i];
                EditorUtility.DisplayProgressBar("레퍼런스 연결 중", path, (float)i / newAssetPaths.Count);

                if (path.EndsWith(".prefab") || path.EndsWith(".mat") || path.EndsWith(".asset") || path.EndsWith(".controller") || path.EndsWith(".anim"))
                {
                    string fullPath = Path.GetFullPath(path);
                    string content = File.ReadAllText(fullPath);
                    bool isModified = false;

                    foreach (var kvp in guidMap)
                    {
                        if (content.Contains(kvp.Key))
                        {
                            content = content.Replace(kvp.Key, kvp.Value);
                            isModified = true;
                        }
                    }

                    if (isModified)
                    {
                        File.WriteAllText(fullPath, content);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 복사 완료 후 리포트 UI 활성화
        copiedCount = validDependencies.Count;
        showReport = true;

        // 창 다시 그리기 (리포트를 띄우기 위함)
        Repaint();
    }

    // UI 구분선 그리기 함수
    private void DrawLine()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
    }
}
