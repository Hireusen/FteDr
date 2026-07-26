using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Loading_Canvas를 담당합니다. 씬 전환 시작/종료 이벤트로 스스로 열고 닫힙니다.
///
/// 실제 진행률(OnSceneLoadProgress, 순식간에 0→1로 뛸 수 있음)과 "화면에 보여줄 값"을 분리합니다.
/// 화면 값은 _fillSpeed로 정해진 속도로만 차오르고, 실제 로드가 끝나 목표가 1이 되어도
/// 화면 값이 실제로 1까지 다 채워진 뒤에야 닫힙니다. 그래서 로드가 아무리 빨라도
/// 게이지가 자연스럽게 0→100까지 차오르는 걸 보여준 다음 페이드아웃돼요.
///
/// 주의: 진행률이 실제로 들어오려면, 씬을 로드하는 쪽(UScene.Load/LoadWithFade 호출부)이
/// onProgress 콜백 안에서 OnSceneLoadProgress.Publish(progress)를 직접 호출해줘야 합니다.
/// </summary>
public sealed class CLoadingController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private string _progressFormat = "{0:0}%";

    [Header("게이지 채워지는 속도")]
    [Tooltip("초당 이만큼(0~1 기준) 차오릅니다. 1.0이면 최소 1초, 2.0이면 최소 0.5초 걸려서 꽉 참.")]
    [SerializeField] private float _fillSpeed = 1.2f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _targetProgress;   // 실제 로드 진행률 (순식간에 뛸 수 있음)
    private float _displayedProgress; // 화면에 실제로 보여주는 값 (정해진 속도로만 따라감)
    private bool _isLoading;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnSceneLoadStart>.Subscribe(StartHandler);
        CEventBus<OnSceneLoadEnd>.Subscribe(EndHandler);
        CEventBus<OnSceneLoadProgress>.Subscribe(ProgressHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnSceneLoadStart>.Unsubscribe(StartHandler);
        CEventBus<OnSceneLoadEnd>.Unsubscribe(EndHandler);
        CEventBus<OnSceneLoadProgress>.Unsubscribe(ProgressHandler);
    }

    private void Update()
    {
        if (!_isLoading) return;

        _displayedProgress = Mathf.MoveTowards(_displayedProgress, _targetProgress, _fillSpeed * Time.unscaledDeltaTime);
        RefreshProgress(_displayedProgress);

        // 실제 로드가 끝났고(목표=1), 화면 게이지도 다 채워졌을 때만 닫는다.
        if (_targetProgress >= 1f && _displayedProgress >= 1f - 0.001f)
        {
            _isLoading = false;
            OnRequestCloseUI.Publish(EUI.LoadingWindow);
        }
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void StartHandler(OnSceneLoadStart ctx)
    {
        _isLoading = true;
        _targetProgress = 0f;
        _displayedProgress = 0f;
        RefreshProgress(0f);
        OnRequestOpenUI.Publish(EUI.LoadingWindow);
    }

    private void EndHandler(OnSceneLoadEnd ctx)
    {
        // CGameManager가 부트 완료 직후 발행하는 "가짜" 이벤트(prevScene==nextScene==Boot)는
        // 실제 씬 전환이 아니므로 무시한다. 이걸 안 걸러내면 CMoveScene의 진짜 전환과 타이밍이 겹쳐
        // 로딩창이 열리자마자 바로 닫혀버리는 문제가 생긴다.
        if (ctx.prevScene == ctx.nextScene) return;

        // 여기서 바로 안 닫는다 — Update()가 화면 게이지를 마저 채운 뒤 스스로 닫는다.
        _targetProgress = 1f;
    }

    private void ProgressHandler(OnSceneLoadProgress ctx)
    {
        // 역행 방지 (진행률이 순간적으로 흔들려도 게이지가 뒤로 가지 않도록)
        _targetProgress = Mathf.Max(_targetProgress, ctx.progress);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void RefreshProgress(float t)
    {
        if (_progressSlider != null) _progressSlider.value = Mathf.Clamp01(t);
        if (_progressText != null) _progressText.text = string.Format(_progressFormat, Mathf.Clamp01(t) * 100f);
    }
    #endregion
}
