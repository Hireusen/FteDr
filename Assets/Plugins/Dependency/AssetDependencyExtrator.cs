using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class AssetDependencyExtractor : EditorWindow
{
    private Object targetFeature; // 분석할 대상 (프리팹 또는 폴더)
    private string thirdPartyRoot = "Assets/ThirdPartyIgnored"; // 뽑아낼 타겟 폴더
    private string destinationFolder = "Assets/_Project/Features/MyFeature/Extracted"; // 옮길 목적지
    private bool ignoreScripts = true; // C# 스크립트는 이동 금지 (컴파일 에러 방지)

    [MenuItem("Tools/Asset Dependency Extractor (서드파티 종속성 추출기)")]
    public static void ShowWindow()
    {
        GetWindow<AssetDependencyExtractor>("Dependency Extractor");
    }

    void OnGUI()
    {
        GUILayout.Label("특정 프리팹/폴더가 참조하는 서드파티 에셋 분리 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "1. 분석할 기능 폴더나 프리팹을 타겟에 넣습니다.\n" +
            "2. 그 타겟이 참조하는 에셋 중 '서드파티 폴더'에 있는 것만 찾아냅니다.\n" +
            "3. 찾아낸 에셋들을 '목적지 폴더'로 안전하게 이동시킵니다.", MessageType.Info);

        EditorGUILayout.Space();

        targetFeature = EditorGUILayout.ObjectField("분석할 타겟 (프리팹 or 폴더)", targetFeature, typeof(Object), false);
        thirdPartyRoot = EditorGUILayout.TextField("서드파티 루트 폴더", thirdPartyRoot);
        destinationFolder = EditorGUILayout.TextField("이동시킬 목적지 폴더", destinationFolder);
        ignoreScripts = EditorGUILayout.Toggle("C# 스크립트 이동 제외 (권장)", ignoreScripts);

        EditorGUILayout.Space();

        if (GUILayout.Button("1. 추출 대상 에셋 검색 및 확인 (로그 출력)", GUILayout.Height(40)))
        {
            ExtractAssets(false);
        }

        if (GUILayout.Button("2. 🚀 목적지로 실제 이동 실행", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("확인",
                $"검색된 모든 서드파티 에셋을\n{destinationFolder}\n경로로 이동시킵니다. 진행하시겠습니까?",
                "이동", "취소"))
            {
                ExtractAssets(true);
            }
        }
    }

    private void ExtractAssets(bool executeMove)
    {
        if (targetFeature == null)
        {
            Debug.LogError("분석할 타겟을 지정해주세요!");
            return;
        }

        string targetPath = AssetDatabase.GetAssetPath(targetFeature);
        string[] rootPaths = { targetPath };

        // 만약 타겟이 폴더면, 폴더 내의 모든 에셋을 포함시켜야 함
        if (AssetDatabase.IsValidFolder(targetPath))
        {
            rootPaths = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories)
                .Where(p => !p.EndsWith(".meta"))
                .ToArray();
        }

        // 1. 타겟이 의존하는(사용하는) 모든 에셋 경로 추출
        string[] allDependencies = AssetDatabase.GetDependencies(rootPaths, true);

        // 2. 그 중 '서드파티 폴더' 안에 있는 것만 필터링
        List<string> assetsToMove = new List<string>();
        foreach (string dep in allDependencies)
        {
            // 폴더 자체는 제외
            if (AssetDatabase.IsValidFolder(dep)) continue;

            // 서드파티 폴더 하위에 있는지 확인
            if (dep.Replace("\\", "/").StartsWith(thirdPartyRoot.Replace("\\", "/")))
            {
                // 스크립트 제외 옵션
                if (ignoreScripts && dep.EndsWith(".cs")) continue;

                assetsToMove.Add(dep);
            }
        }

        if (assetsToMove.Count == 0)
        {
            Debug.Log("<color=cyan>[검색 완료]</color> 타겟이 참조하는 서드파티 에셋이 없습니다.");
            return;
        }

        Debug.Log($"<color=yellow>[분석 완료]</color> 총 {assetsToMove.Count}개의 추출 대상 에셋을 찾았습니다.");

        if (!executeMove)
        {
            foreach (string p in assetsToMove) Debug.Log($"이동 대기: {p}");
            return;
        }

        // 3. 실제 폴더 이동 로직
        int moveCount = 0;
        try
        {
            for (int i = 0; i < assetsToMove.Count; i++)
            {
                string oldPath = assetsToMove[i];
                string fileName = Path.GetFileName(oldPath);

                // 서드파티 내부의 하위 폴더 구조를 무시하고 
                // 목적지 폴더에 평탄화(Flatten)해서 전부 모아버립니다. (가장 관리가 편함)
                string newPath = $"{destinationFolder}/{fileName}";

                if (EditorUtility.DisplayCancelableProgressBar("에셋 이동 중", $"{fileName} 이동 중...", (float)i / assetsToMove.Count))
                {
                    break;
                }

                // 목적지 폴더가 없으면 생성
                CreateFolderRecursively(destinationFolder);

                // 이름 충돌 방지 (목적지에 이미 같은 이름의 파일이 있으면 _1, _2 등을 붙임)
                newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                string result = AssetDatabase.MoveAsset(oldPath, newPath);
                if (string.IsNullOrEmpty(result))
                {
                    Debug.Log($"<color=green>[이동 완료]</color> {oldPath} -> {newPath}");
                    moveCount++;
                }
                else
                {
                    Debug.LogError($"[이동 실패] {oldPath}: {result}");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[작업 완료]</color> 총 {moveCount}개의 에셋을 목적지로 이동시켰습니다.");
        }
    }

    // 폴더가 계층적으로 없을 경우 쪼개서 생성해주는 유틸리티 함수
    private void CreateFolderRecursively(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] folders = folderPath.Split('/');
        string currentPath = folders[0]; // 보통 "Assets"

        for (int i = 1; i < folders.Length; i++)
        {
            string nextPath = currentPath + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }
            currentPath = nextPath;
        }
    }
}
