using UnityEngine;
using UnityEditor;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class AssetDeduplicator : EditorWindow
{
    private string targetDirectory = "Assets"; // 검색을 시작할 기본 폴더
    private string targetExtension = "*.png;*.jpg;*.jpeg;*.tga"; // 검색할 확장자 (주로 텍스처)
    private bool includeSubFolders = true;

    [MenuItem("Tools/Asset Deduplicator (해시 기반 중복 제거)")]
    public static void ShowWindow()
    {
        GetWindow<AssetDeduplicator>("Asset Deduplicator");
    }

    void OnGUI()
    {
        GUILayout.Label("중복 에셋 검색 및 병합 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetDirectory = EditorGUILayout.TextField("검색 폴더 (상대 경로)", targetDirectory);
        targetExtension = EditorGUILayout.TextField("검색 확장자 (;로 구분)", targetExtension);
        includeSubFolders = EditorGUILayout.Toggle("하위 폴더 포함", includeSubFolders);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "주의: 이 작업은 메타데이터(GUID)를 수정합니다.\n" +
            "반드시 Git 브랜치를 새로 생성하고 커밋한 상태에서 진행하세요!",
            MessageType.Warning);

        if (GUILayout.Button("1. 중복 파일 검색 및 콘솔 출력", GUILayout.Height(40)))
        {
            FindDuplicates(false);
        }

        if (GUILayout.Button("2. ⚠️ 중복 파일 통합 실행 (삭제 포함)", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("경고",
                "정말로 중복된 파일들을 삭제하고 레퍼런스를 하나로 덮어씌우시겠습니까?\n복구할 수 없습니다!",
                "실행", "취소"))
            {
                FindDuplicates(true);
            }
        }
    }

    private void FindDuplicates(bool executeMerge)
    {
        // 1. 타겟 확장자 파싱
        string[] extensions = targetExtension.Split(';');
        List<string> allFilePaths = new List<string>();

        // 2. 파일 목록 수집
        SearchOption searchOption = includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string dataPath = Application.dataPath;
        string searchDir = Path.Combine(dataPath.Substring(0, dataPath.Length - 6), targetDirectory); // Assets 폴더 경로 조합

        if (!Directory.Exists(searchDir))
        {
            Debug.LogError($"[Deduplicator] 경로를 찾을 수 없습니다: {searchDir}");
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("파일 수집 중", "지정된 폴더에서 파일을 찾는 중입니다...", 0.1f);
            foreach (string ext in extensions)
            {
                allFilePaths.AddRange(Directory.GetFiles(searchDir, ext.Trim(), searchOption));
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 3. 해시 계산 및 그룹화
        Dictionary<string, List<string>> hashMap = new Dictionary<string, List<string>>();
        int totalFiles = allFilePaths.Count;

        for (int i = 0; i < totalFiles; i++)
        {
            string filePath = allFilePaths[i];

            // 메타 파일은 제외
            if (filePath.EndsWith(".meta")) continue;

            if (i % 50 == 0) // UI 응답성 유지
            {
                EditorUtility.DisplayProgressBar("해시 계산 중", $"진행 중: {i} / {totalFiles}", (float)i / totalFiles);
            }

            string hash = CalculateMD5(filePath);
            if (!string.IsNullOrEmpty(hash))
            {
                if (!hashMap.ContainsKey(hash))
                {
                    hashMap[hash] = new List<string>();
                }
                // Unity 에셋 경로 포맷으로 변환 (Assets/...)
                string assetPath = "Assets" + filePath.Substring(dataPath.Length).Replace("\\", "/");
                hashMap[hash].Add(assetPath);
            }
        }
        EditorUtility.ClearProgressBar();

        // 4. 중복 결과 처리
        int duplicateGroupsCount = 0;
        int filesToDeleteCount = 0;

        foreach (var kvp in hashMap)
        {
            List<string> duplicates = kvp.Value;
            if (duplicates.Count > 1)
            {
                duplicateGroupsCount++;

                // 원본을 하나 결정 (일단 리스트의 첫 번째를 원본으로 취급)
                string originalAssetPath = duplicates[0];
                string originalGUID = AssetDatabase.AssetPathToGUID(originalAssetPath);

                Debug.Log($"<color=green>[중복 그룹 발견 - 해시: {kvp.Key}]</color>\n" +
                          $"▶ 유지할 원본: {originalAssetPath} (GUID: {originalGUID})");

                for (int i = 1; i < duplicates.Count; i++)
                {
                    string duplicatePath = duplicates[i];
                    string duplicateGUID = AssetDatabase.AssetPathToGUID(duplicatePath);
                    filesToDeleteCount++;

                    Debug.Log($"   삭제 대상 복사본: {duplicatePath} (GUID: {duplicateGUID})");

                    if (executeMerge)
                    {
                        // 1단계: 레퍼런스 강제 교체 (꼼수)
                        // 삭제할 에셋을 바라보던 레퍼런스들이 원본을 바라보도록, 프로젝트 내의 텍스트 기반 에셋(.prefab, .mat, .asset 등) 내부의 GUID 텍스트를 강제로 치환하는 방법도 있지만, 
                        // 너무 위험하고 느리기 때문에, 가장 안전한 방법은 AssetDatabase 자체의 기능을 이용하는 것입니다.

                        // 하지만 순수 API만으로는 A를 B로 가리키게 하는 완벽한 방법은 "A를 삭제하고, A가 있던 자리에 B를 복사/이동" 하는 것이 아닙니다. 
                        // 참조하는 쪽(Material, Prefab 등)의 meta 데이터를 뜯어 고쳐야 합니다.
                        // 이 스크립트는 10만개 스케일을 고려하여 가장 확실한 "Text Replace" 방식을 사용합니다. 

                        ReplaceGUIDInAllAssets(duplicateGUID, originalGUID);

                        // 2단계: 복사본 삭제
                        AssetDatabase.DeleteAsset(duplicatePath);
                    }
                }
            }
        }

        if (executeMerge)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=cyan>[완료]</color> {duplicateGroupsCount}개의 중복 그룹 처리 완료. 총 {filesToDeleteCount}개의 중복 파일이 삭제되고 병합되었습니다.");
        }
        else
        {
            Debug.Log($"<color=yellow>[검색 완료]</color> 총 {duplicateGroupsCount}개의 중복 그룹, 제거 가능한 잉여 파일 {filesToDeleteCount}개 발견.");
        }
    }

    // 파일의 MD5 해시값 계산
    private string CalculateMD5(string filename)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filename))
            {
                var hash = md5.ComputeHash(stream);
                return System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    // 🔥 핵심: 프로젝트 내의 모든 시리얼라이즈된 파일(.prefab, .mat 등) 텍스트를 까서 구 GUID를 새 GUID로 강제 치환
    private void ReplaceGUIDInAllAssets(string oldGUID, string newGUID)
    {
        if (string.IsNullOrEmpty(oldGUID) || string.IsNullOrEmpty(newGUID) || oldGUID == newGUID) return;

        // Force Text Serialization이 켜져 있어야 작동합니다. (Project Settings -> Editor -> Asset Serialization = Force Text)
        string[] allAssets = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".prefab") || s.EndsWith(".mat") || s.EndsWith(".asset") || s.EndsWith(".unity") || s.EndsWith(".controller") || s.EndsWith(".anim"))
            .ToArray();

        foreach (string assetPath in allAssets)
        {
            try
            {
                string text = File.ReadAllText(assetPath);
                if (text.Contains(oldGUID))
                {
                    text = text.Replace(oldGUID, newGUID);
                    File.WriteAllText(assetPath, text);
                    Debug.Log($"[레퍼런스 수정됨] {Path.GetFileName(assetPath)} 내부의 {oldGUID}를 {newGUID}로 교체했습니다.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[파일 읽기/쓰기 오류] {assetPath}: {e.Message}");
            }
        }
    }
}
