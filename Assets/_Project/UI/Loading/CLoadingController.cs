using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Loading_Canvas를 담당합니다. 씬 전환 시작/종료 이벤트로 스스로 열고 닫히며,
/// 진행률은 OnSceneLoadProgress를 구독해서 슬라이더에 반영합니다.
///
/// 주의: 진행률이 실제로 들어오려면, 씬을 로드하는 쪽(UScene.Load/LoadWithFade 호출부)이
/// onProgress 콜백 안에서 OnSceneLoadProgress.Publish(progress)를 직접 호출해줘야 합니다.
/// (CGameManager는 progress를 이벤트로 자동 방송하지 않고, 호출한 쪽에만 콜백으로 돌려줍니다)
/// </summary>
public sealed class CLoadingController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private string _progressFormat = "{0:0}%";
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
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void StartHandler(OnSceneLoadStart ctx)
    {
        RefreshProgress(0f);
        OnRequestOpenUI.Publish(EUI.LoadingWindow);
    }

    private void EndHandler(OnSceneLoadEnd ctx)
    {
        RefreshProgress(1f);
        OnRequestCloseUI.Publish(EUI.LoadingWindow);
    }

    private void ProgressHandler(OnSceneLoadProgress ctx)
    {
        RefreshProgress(ctx.progress);
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
