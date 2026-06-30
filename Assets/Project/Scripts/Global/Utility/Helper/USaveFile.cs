using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 직렬화 가능한 데이터를 Json으로 저장하고 불러오는 유틸리티 클래스입니다.
/// </summary>
public static class USaveFile
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const string EXTENSION = ".json"; // 저장 파일 확장자
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 지정한 파일명으로 데이터를 JSON 직렬화하여 저장합니다.
    /// </summary>
    /// <typeparam name="T">직렬화 가능한 데이터 타입([Serializable])</typeparam>
    /// <param name="fileName">확장자를 제외한 파일명</param>
    /// <param name="data">저장할 데이터</param>
    /// <param name="prettyPrint">사람이 읽기 좋은 형태로 저장할지 여부</param>
    /// <returns>저장 성공 여부</returns>
    public static bool Save<T>(string fileName, T data, bool prettyPrint = true)
    {
        if (fileName.IsBlank())
        {
            UDebug.Print("저장 파일명이 비어 있어 저장을 중단합니다.", LogType.Error);
            return false;
        }

        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint);
            string path = GetPath(fileName);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception e)
        {
            UDebug.Print($"파일({fileName}) 저장에 실패했습니다. {e.Message}", LogType.Error);
            return false;
        }
    }

    /// <summary>
    /// 지정한 파일에서 데이터를 불러옵니다.
    /// 파일이 없거나 손상된 경우 fallback 값을 반환합니다.
    /// </summary>
    /// <typeparam name="T">역직렬화할 데이터 타입([Serializable])</typeparam>
    /// <param name="fileName">확장자를 제외한 파일명</param>
    /// <param name="fallback">불러오기 실패 시 반환할 기본값</param>
    /// <returns>불러온 데이터 또는 fallback</returns>
    public static T Load<T>(string fileName, T fallback)
    {
        if (fileName.IsBlank())
        {
            UDebug.Print("불러올 파일명이 비어 있어 기본값을 반환합니다.", LogType.Warning);
            return fallback;
        }

        string path = GetPath(fileName);
        if (!File.Exists(path))
        {
            // 최초 실행 등 파일이 없는 정상 케이스
            return fallback;
        }

        try
        {
            string json = File.ReadAllText(path);
            T data = JsonUtility.FromJson<T>(json);
            // FromJson은 빈/잘못된 문자열에 대해 default를 반환할 수 있음
            if (data == null) return fallback;

            return data;
        }
        catch (Exception e)
        {
            UDebug.Print($"파일({fileName}) 불러오기에 실패했습니다. 기본값을 반환합니다. {e.Message}", LogType.Error);
            return fallback;
        }
    }

    /// <summary>
    /// 지정한 저장 파일이 존재하는지 확인합니다.
    /// </summary>
    /// <param name="fileName">확장자를 제외한 파일명</param>
    public static bool Exists(string fileName)
    {
        if (fileName.IsBlank()) return false;

        return File.Exists(GetPath(fileName));
    }

    /// <summary>
    /// 지정한 저장 파일을 삭제합니다.
    /// </summary>
    /// <param name="fileName">확장자를 제외한 파일명</param>
    /// <returns>삭제 성공 여부(파일이 없으면 false)</returns>
    public static bool Delete(string fileName)
    {
        if (!Exists(fileName)) return false;

        try
        {
            File.Delete(GetPath(fileName));
            return true;
        }
        catch (Exception e)
        {
            UDebug.Print($"파일({fileName}) 삭제에 실패했습니다. {e.Message}", LogType.Error);
            return false;
        }
    }

    /// <summary>
    /// 모든 저장 파일(*.json)을 삭제합니다.
    /// </summary>
    /// <returns>삭제한 파일 개수</returns>
    public static int DeleteAll()
    {
        string dir = Application.persistentDataPath;
        if (!Directory.Exists(dir)) return 0;

        int deleted = 0;
        try
        {
            string[] files = Directory.GetFiles(dir, "*" + EXTENSION);
            int length = files.Length;
            for (int i = 0; i < length; ++i)
            {
                try
                {
                    File.Delete(files[i]);
                    ++deleted;
                }
                catch (Exception e)
                {
                    UDebug.Print($"파일({files[i]}) 삭제에 실패했습니다. {e.Message}", LogType.Error);
                }
            }
        }
        catch (Exception e)
        {
            UDebug.Print($"저장 파일 전체 삭제에 실패했습니다. {e.Message}", LogType.Error);
        }

        return deleted;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 파일명을 persistentDataPath 기준 전체 경로로 변환
    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName + EXTENSION);
    }
    #endregion
}
