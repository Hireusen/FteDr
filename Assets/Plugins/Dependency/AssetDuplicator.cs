using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;

public class AssetDeduplicator : EditorWindow
{
    private string _targetDirectory = "Assets";
    private string _targetExtension = "*.png;*.jpg;*.jpeg;*.tga;*.fbx;*.obj;*.mat";
    private bool _includeSubFolders = true;

    // 안전장치 옵션
    private bool _safeMode = true;

    [MenuItem("Tools/Asset Deduplicator (해시 기반 중복 제거)")]
    public static void ShowWindow()
    {
        GetWindow<AssetDeduplicator>("Asset Deduplicator");
    }

    private void OnGUI()
    {
        GUILayout.Label("중복 에셋 검색 및 병합 도구", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _targetDirectory = EditorGUILayout.TextField("검색 폴더 (상대 경로)", _targetDirectory);
        _targetExtension = EditorGUILayout.TextField("검색 확장자 (;로 구분)", _targetExtension);
        _includeSubFolders = EditorGUILayout.Toggle("하위 폴더 포함", _includeSubFolders);

        EditorGUILayout.Space();
        // 🔥 안전 모드 토글 UI
        _safeMode = EditorGUILayout.Toggle(new GUIContent("안전 모드 (이름 체크)", "해시가 같아도 파일 이름이 다르면(예: Aged vs Fine) 병합하지 않고 스킵합니다."), _safeMode);

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
        string[] extensions = _targetExtension.Split(';');
        List<string> allFilePaths = new List<string>();

        SearchOption searchOption = _includeSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string dataPath = Application.dataPath;
        string searchDir = Path.Combine(dataPath.Substring(0, dataPath.Length - 6), _targetDirectory);

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

        Dictionary<string, List<string>> hashMap = new Dictionary<string, List<string>>();
        int totalFiles = allFilePaths.Count;

        for (int i = 0; i < totalFiles; i++)
        {
            string filePath = allFilePaths[i];
            if (filePath.EndsWith(".meta")) continue;

            if (i % 50 == 0)
            {
                EditorUtility.DisplayProgressBar("해시 계산 중", $"진행 중: {i} / {totalFiles}", (float)i / totalFiles);
            }

            string hash = CalculateMD5(filePath);
            if (!string.IsNullOrEmpty(hash))
            {
                if (!hashMap.ContainsKey(hash)) hashMap[hash] = new List<string>();
                string assetPath = "Assets" + filePath.Substring(dataPath.Length).Replace("\\", "/");
                hashMap[hash].Add(assetPath);
            }
        }
        EditorUtility.ClearProgressBar();

        int duplicateGroupsCount = 0;
        int filesToDeleteCount = 0;

        foreach (var kvp in hashMap)
        {
            List<string> duplicates = kvp.Value;
            if (duplicates.Count > 1)
            {
                string originalAssetPath = duplicates[0];
                string originalGUID = AssetDatabase.AssetPathToGUID(originalAssetPath);
                string originalNameBase = GetCleanFileName(originalAssetPath);

                bool groupHasLogged = false;

                for (int i = 1; i < duplicates.Count; i++)
                {
                    string duplicatePath = duplicates[i];
                    string duplicateGUID = AssetDatabase.AssetPathToGUID(duplicatePath);
                    string duplicateNameBase = GetCleanFileName(duplicatePath);

                    // 🔥 안전 모드: 이름 기반 필터링
                    if (_safeMode && !IsNameSimilar(originalNameBase, duplicateNameBase))
                    {
                        Debug.LogWarning($"<color=yellow>[안전 모드 스킵]</color> 내용(해시)은 같지만 이름이 달라 병합을 스킵합니다.\n" +
                                         $"원본: {originalAssetPath}\n대상: {duplicatePath}");
                        continue;
                    }

                    if (!groupHasLogged)
                    {
                        Debug.Log($"<color=green>[중복 그룹 발견 - 해시: {kvp.Key}]</color>\n" +
                                  $"▶ 유지할 원본: {originalAssetPath} (GUID: {originalGUID})");
                        groupHasLogged = true;
                        duplicateGroupsCount++;
                    }

                    filesToDeleteCount++;
                    Debug.Log($"   삭제 대상 복사본: {duplicatePath} (GUID: {duplicateGUID})");

                    if (executeMerge)
                    {
                        ReplaceGUIDInAllAssets(duplicateGUID, originalGUID);
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
            Debug.Log($"<color=cyan>[검색 완료]</color> 총 {duplicateGroupsCount}개의 중복 그룹, 제거 가능한 잉여 파일 {filesToDeleteCount}개 발견.");
        }
    }

    private string CalculateMD5(string filename)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filename))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    // 파일 이름에서 확장자, 공백, 숫자 등을 어느 정도 제거하여 핵심 단어만 추출
    private string GetCleanFileName(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        // 유니티가 복사본을 만들 때 붙이는 " 1", "_copy" 등 제거
        name = name.Replace(" 1", "").Replace(" 2", "").Replace("_copy", "").Replace("(", "").Replace(")", "").Trim();
        return name;
    }

    // 두 이름이 "거의 같은지" 비교 (Aged vs Fine 처럼 다르면 false 반환)
    private bool IsNameSimilar(string name1, string name2)
    {
        // 1. 이름이 완전히 똑같으면 통과
        if (name1 == name2) return true;

        // 2. 한쪽이 다른 쪽을 포함하고 있는지 확인 (예: Wood vs Wood_Dark)
        // 이 부분은 프로젝트 네이밍 컨벤션에 따라 좀 더 타이트하게 잡으려면 false로 바꾸셔도 됩니다.
        if (name1.Contains(name2) || name2.Contains(name1)) return true;

        return false;
    }

    private void ReplaceGUIDInAllAssets(string oldGUID, string newGUID)
    {
        if (string.IsNullOrEmpty(oldGUID) || string.IsNullOrEmpty(newGUID) || oldGUID == newGUID) return;

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
            catch (Exception e)
            {
                Debug.LogError($"[파일 읽기/쓰기 오류] {assetPath}: {e.Message}");
            }
        }
    }
}
