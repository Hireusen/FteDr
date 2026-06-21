using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 페이드 효과를 전담하는 정적 유틸리티 클래스입니다.
/// </summary>
public static class UFade
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private static bool _isInitialize;
    private static Image _fadeImage;
    private static CanvasGroup _canvasGroup;
    private static MonoBehaviour _coroutineRunner; // 코루틴 실행자
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 현재 페이드 연출 진행 여부를 반환합니다.
    /// </summary>
    public static bool IsFading { get; private set; }

    /// <summary>
    /// 페이드 아웃 효과를 실행합니다.
    /// </summary>
    /// <param name="duration">연출 시간</param>
    public static void FadeOut
        (float duration, bool blockRaycasts = false, float startAlpha = 0f, float endAlpha = 1f)
    {
        if (!_isInitialize)
        {
            Initialize();
        }
        ExecuteFade(startAlpha, endAlpha, duration, blockRaycasts);
    }

    /// <summary>
    /// 페이드 인 효과를 실행합니다.
    /// </summary>
    /// <param name="duration">연출 시간</param>
    public static void FadeIn
        (float duration, bool blockRaycasts = false, float startAlpha = 1f, float endAlpha = 0f)
    {
        if (!_isInitialize)
        {
            Initialize();
        }
        ExecuteFade(startAlpha, endAlpha, duration, blockRaycasts);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private static void ExecuteFade(float startAlpha, float targetAlpha, float duration, bool blockRaycasts)
    {
        _coroutineRunner.StopAllCoroutines(); // 기존 페이드 효과 종료
        _canvasGroup.blocksRaycasts = blockRaycasts; // 레이캐스트 차단 설정

        // 지속시간 0 방어
        if (duration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            IsFading = false;
            return;
        }

        // 페이드 코루틴 시작
        _coroutineRunner.StartCoroutine(DoFade(startAlpha, targetAlpha, duration));
    }

    private static IEnumerator DoFade(float startAlpha, float targetAlpha, float duration)
    {
        IsFading = true; // 대기 플래그 ON
        float time = 0f;

        // 투명도 보간
        while (time < duration)
        {
            time += Time.unscaledDeltaTime; // 일시정지 상태 무관
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }

        // 페이드 완료
        _canvasGroup.alpha = targetAlpha;
        _canvasGroup.blocksRaycasts = false;
        IsFading = false; // 대기 플래그 OFF
    }

    private static void Initialize()
    {
        if (_isInitialize) return;

        // 캔버스 생성 및 파괴 방지
        GameObject root = new GameObject("FadeCanvas");
        Object.DontDestroyOnLoad(root);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 덮기
        canvas.sortingOrder = 9999; // 최상단 노출

        // 캔버스에 컴포넌트 추가
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();

        // 페이드 이미지 생성
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(root.transform, false);
        _fadeImage = imageObj.AddComponent<Image>();

        // 페이드 이미지 컴포넌트 설정
        _fadeImage.color = Color.black;
        RectTransform rect = _fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // 투명도 및 터치 블로킹
        _canvasGroup = imageObj.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        // 정적 클래스이므로 코루틴을 돌리기 위한 컴포넌트 추가
        _coroutineRunner = root.AddComponent<FadeRunner>();

        _isInitialize = true;
    }

    // 플레이 모드가 시작될 때 자동으로 로그 기록을 초기화합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetVariable()
    {
        _isInitialize = false;
        _fadeImage = null;
        _canvasGroup = null;
        _coroutineRunner = null;
    }

    // 코루틴 실행용 은닉 클래스
    private class FadeRunner : MonoBehaviour { }
    #endregion
}
