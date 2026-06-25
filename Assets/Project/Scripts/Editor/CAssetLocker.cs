using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 유니티 실행 또는 컴파일 시 대용량 에셋 폴더에 있는 에셋을 자동 잠금합니다.
/// </summary>
[InitializeOnLoad]
public class CAssetLocker : AssetPostprocessor
{
    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 정적 생성자 : 유니티 엔진 시작 시 실행
    static CAssetLocker()
    {
        EditorApplication.delayCall += ScanAndLockLargeAssets;
    }

    // 수동 실행
    [MenuItem("Tools/대용량 에셋 읽기 전용 변경")]
    public static void ShowWindow()
    {
        ScanAndLockLargeAssets();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 특정 경로의 에셋 스캔 및 잠금
    private static void ScanAndLockLargeAssets()
    {
        string path = Path.Combine(Application.dataPath, K.LARGE_ASSETS_FOLDER_NAME); // 절대 경로 반환
        if (!Directory.Exists(path)) return; // 폴더 없을 시 종료

        int lockedCount = LockDirectory(path);
        if(lockedCount > 0)
        {
            UDebug.Print($"{lockedCount}개의 에셋을 읽기 전용 상태로 변경했습니다.");
        }
    }

    // 해당 경로의 모든 파일 잠금
    private static int LockDirectory(string path)
    {
        int count = 0;
        string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

        int length = files.Length;
        for (int i = 0; i < length; ++i)
        {
            string filePath = files[i];
            // 메타 파일을 제외한 모든 파일 잠금
            if (filePath.EndsWith(".meta")) continue;
            if (ApplyReadOnly(filePath))
            {
                count++;
            }
        }

        return count;
    }

    // OS 읽기 전용 속성 부여
    private static bool ApplyReadOnly(string filePath)
    {
        FileAttributes attributes = File.GetAttributes(filePath); // 파일의 속성 가져오기

        // 이미 읽기 전용 속성일 경우 생략
        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) return false;

        // 읽기 전용 속성으로 변경
        File.SetAttributes(filePath, attributes | FileAttributes.ReadOnly);
        return true;
    }
    #endregion
}
