using System;
using UnityEngine.SceneManagement;

/// <summary>
/// CGameManager의 씬 로드 진입점 역할을 하는 퍼사드 클래스입니다.
/// </summary>
public static class UScene
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private static CGameManager Manager => CGameManager.Ins;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 씬을 반환합니다.</summary>
    public static EScene Current => Manager.Scene;

    /// <summary>씬 로드 진행 중 여부를 반환합니다.</summary>
    public static bool IsLoading => Manager.IsSceneLoading;

    /// <summary>
    /// 해당 씬을 비동기 로드합니다.
    /// </summary>
    /// <param name="scene">로드할 씬</param>
    /// <param name="onComplete">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="delay">씬 로드 시작 전에 대기할 시간(초)</param>
    /// <param name="mode">씬 로드 모드</param>
    public static void Load(
        EScene scene,
        Action onComplete = null,
        Action<float> onProgress = null,
        float delay = 0f,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        Manager.LoadSceneAsync((int)scene, onComplete, onProgress, delay, mode);
    }

    /// <summary>
    /// 다음 빌드 인덱스의 씬을 비동기 로드합니다.
    /// </summary>
    /// <returns>다음 씬이 빌드 세팅 범위 내에 있어 로드를 시작했다면 True</returns>
    public static bool NextLoad(
        Action onComplete = null,
        Action<float> onProgress = null,
        float delay = 0f,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        int next = (int)Current + 1;
        if (!IsValidBuildIndex(next)) return false;

        Manager.LoadSceneAsync(next, onComplete, onProgress, delay, mode);
        return true;
    }

    /// <summary>
    /// 이전 빌드 인덱스의 씬을 비동기 로드합니다.
    /// </summary>
    /// <returns>다음 씬이 빌드 세팅 범위 내에 있어 로드를 시작했다면 True</returns>
    public static bool PrevLoad(
        Action onComplete = null,
        Action<float> onProgress = null,
        float delay = 0f,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        int prev = (int)Current - 1;
        if (!IsValidBuildIndex(prev)) return false;

        Manager.LoadSceneAsync(prev, onComplete, onProgress, delay, mode);
        return true;
    }

    /// <summary>
    /// 해당 씬을 페이드 효과와 함께 비동기 로드합니다.
    /// </summary>
    /// <param name="scene">로드할 씬</param>
    /// <param name="delay">씬 로드 시작 전에 대기할 시간(초)</param>
    /// <param name="fadeOut">페이드 아웃 시간(초)</param>
    /// <param name="fadeIn">페이드 인 시간(초)</param>
    /// <param name="onComplete">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="mode">씬 로드 모드</param>
    public static void LoadWithFade(
        EScene scene,
        float delay = 0f,
        float fadeOut = 0.45f,
        float fadeIn = 0.45f,
        Action onComplete = null,
        Action<float> onProgress = null,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        Manager.LoadSceneAsyncWithFade((int)scene, delay, fadeOut, fadeIn, onComplete, onProgress, mode);
    }

    /// <summary>
    /// 다음 빌드 인덱스의 씬을 페이드 효과와 함께 비동기 로드합니다.
    /// </summary>
    /// <returns>다음 씬이 빌드 세팅 범위 내에 있어 로드를 시작했다면 True</returns>
    public static bool NextLoadWithFade(
        float delay = 0f,
        float fadeOut = 0.45f,
        float fadeIn = 0.45f,
        Action onComplete = null,
        Action<float> onProgress = null,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        int next = (int)Current + 1;
        if (!IsValidBuildIndex(next)) return false;

        Manager.LoadSceneAsyncWithFade(next, delay, fadeOut, fadeIn, onComplete, onProgress, mode);
        return true;
    }

    /// <summary>
    /// 이전 빌드 인덱스의 씬을 페이드 효과와 함께 비동기 로드합니다.
    /// </summary>
    /// <returns>다음 씬이 빌드 세팅 범위 내에 있어 로드를 시작했다면 True</returns>
    public static bool PrevLoadWithFade(
        float delay = 0f,
        float fadeOut = 0.45f,
        float fadeIn = 0.45f,
        Action onComplete = null,
        Action<float> onProgress = null,
        LoadSceneMode mode = LoadSceneMode.Single)
    {
        int prev = (int)Current - 1;
        if (!IsValidBuildIndex(prev)) return false;

        Manager.LoadSceneAsyncWithFade(prev, delay, fadeOut, fadeIn, onComplete, onProgress, mode);
        return true;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 빌드 세팅에 등록된 씬 인덱스 범위인지 검사합니다.
    private static bool IsValidBuildIndex(int index)
    {
        return index >= 0 && index < SceneManager.sceneCountInBuildSettings;
    }
    #endregion
}
