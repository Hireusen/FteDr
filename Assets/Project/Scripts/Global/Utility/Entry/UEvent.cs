using System;
using UnityEngine.SceneManagement;

/// <summary>
/// CGameManager의 씬 로드 진입점 역할을 하는 퍼사드 클래스입니다.
/// </summary>
public static class UScene
{
    private static CGameManager Manager => CGameManager.Ins;

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
}
