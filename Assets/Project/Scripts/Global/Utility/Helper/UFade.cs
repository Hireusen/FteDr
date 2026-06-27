using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 페이드 효과를 전담하는 정적 유틸리티 클래스입니다.
/// 색상 및 스프라이트(이미지) 페이드를 지원합니다.
/// </summary>
public static class UFade
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private static bool _isInitialize;
    private static Image _fadeImage;
    private static CanvasGroup _canvasGroup;
    private static MonoBehaviour _coroutineRunner;

    private static readonly Color _defaultColor = Color.black;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 페이드 연출 진행 여부를 반환합니다.</summary>
    public static bool IsFading { get; private set; }

    /// <summary>
    /// 페이드 아웃(화면 덮기) 효과를 실행합니다.
    /// </summary>
    /// <param name="duration">연출 시간</param>
    /// <param name="blockRaycasts">페이드 진행 중 입력 차단 여부</param>
    /// <param name="onComplete">페이드 완료 콜백</param>
    public static void FadeOut(
        float duration,
        bool blockRaycasts = false,
        float startAlpha = 0f,
        float endAlpha = 1f,
        Action onComplete = null)
    {
        EnsureInitialized();
        ExecuteFade(startAlpha, endAlpha, duration, blockRaycasts, onComplete);
    }

    /// <summary>
    /// 페이드 인(화면 드러내기) 효과를 실행합니다.
    /// </summary>
    public static void FadeIn(
        float duration,
        bool blockRaycasts = false,
        float startAlpha = 1f,
        float endAlpha = 0f,
        Action onComplete = null)
    {
        EnsureInitialized();
        ExecuteFade(startAlpha, endAlpha, duration, blockRaycasts, onComplete);
    }

    /// <summary>
    /// 페이드에 사용할 색상을 설정합니다.
    /// </summary>
    public static void SetColor(Color color)
    {
        EnsureInitialized();
        _fadeImage.sprite = null;
        _fadeImage.color = new Color(color.r, color.g, color.b, 1f);
    }

    /// <summary>
    /// 페이드에 사용할 스프라이트(이미지)를 설정합니다.
    /// </summary>
    /// <param name="sprite">표시할 스프라이트</param>
    /// <param name="tint">스프라이트에 곱해질 색상 틴트 (기본 흰색 = 원본 색)</param>
    /// <param name="preserveAspect">종횡비 유지 여부</param>
    public static void SetSprite(Sprite sprite, Color? tint = null, bool preserveAspect = false)
    {
        EnsureInitialized();
        _fadeImage.sprite = sprite;
        _fadeImage.color = tint ?? Color.white;
        _fadeImage.preserveAspect = preserveAspect;
    }

    /// <summary>
    /// 페이드 색상과 스프라이트를 초기화합니다.
    /// </summary>
    public static void ResetVisual()
    {
        EnsureInitialized();
        _fadeImage.sprite = null;
        _fadeImage.preserveAspect = false;
        _fadeImage.color = _defaultColor;
    }

    /// <summary>
    /// 현재 진행 중인 페이드를 즉시 중단합니다.
    /// </summary>
    public static void StopFade()
    {
        if (!_isInitialize) return;
        _coroutineRunner.StopAllCoroutines();
        ResetVisual();
        IsFading = false;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private static void ExecuteFade(
        float startAlpha, float targetAlpha, float duration, bool blockRaycasts, Action onComplete)
    {
        _coroutineRunner.StopAllCoroutines();
        _canvasGroup.blocksRaycasts = blockRaycasts;

        // 지속시간 0 방어
        if (duration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.blocksRaycasts = false;
            IsFading = false;
            onComplete?.Invoke();
            return;
        }

        // 플리커 방지: 시작 alpha를 먼저 적용
        _canvasGroup.alpha = startAlpha;
        IsFading = true; // 호출 직후 동기적으로 true 보장

        _coroutineRunner.StartCoroutine(
            DoFade(startAlpha, targetAlpha, duration, onComplete));
    }

    private static IEnumerator DoFade(
        float startAlpha, float targetAlpha, float duration, Action onComplete)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // 일시정지 상태 무관
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        // 페이드 완료
        _canvasGroup.alpha = targetAlpha;
        _canvasGroup.blocksRaycasts = false;
        IsFading = false;
        onComplete?.Invoke();
    }

    private static void EnsureInitialized()
    {
        if (_isInitialize) return;

        // 캔버스 생성 및 파괴 방지
        GameObject root = new GameObject("FadeCanvas");
        UnityEngine.Object.DontDestroyOnLoad(root);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();

        // 페이드 이미지 생성
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(root.transform, false);
        _fadeImage = imageObj.AddComponent<Image>();
        _fadeImage.color = _defaultColor;

        RectTransform rect = _fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // 투명도 및 터치 블로킹
        _canvasGroup = imageObj.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        _coroutineRunner = root.AddComponent<FadeRunner>();

        _isInitialize = true;
    }

    // 플레이 모드 시작 시(도메인 리로드 비활성화 대응) 정적 상태를 초기화합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _isInitialize = false;
        _fadeImage = null;
        _canvasGroup = null;
        _coroutineRunner = null;
        IsFading = false;
    }

    private class FadeRunner : MonoBehaviour { }
    #endregion
}
