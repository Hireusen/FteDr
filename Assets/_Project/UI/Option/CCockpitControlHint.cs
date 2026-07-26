using UnityEngine;
using DG.Tweening;

/// <summary>
/// 조종석에 앉아있는 동안만 "Q/E로 조작 가능" 안내를 보여주는 상시 HUD 요소입니다.
/// CUIWindow(스택형 창)를 쓰지 않습니다 — 여닫는 창이 아니라 상태에 따라 자동으로 나타나는 힌트라서
/// CPlayerHudController와 같은 성격입니다.
/// </summary>
public sealed class CCockpitControlHint : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.2f;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        // 시작할 땐 조종석에 앉아있지 않으므로 즉시 숨김 (연출 없이)
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    private void OnEnable()
    {
        CEventBus<OnPlayerCockpitStateChanged>.Subscribe(CockpitStateHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerCockpitStateChanged>.Unsubscribe(CockpitStateHandler);
        _canvasGroup?.DOKill();
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void CockpitStateHandler(OnPlayerCockpitStateChanged ctx)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.DOKill();
        _canvasGroup.DOFade(ctx.isSitting ? 1f : 0f, _fadeDuration).SetUpdate(true);
        _canvasGroup.blocksRaycasts = false; // 이 힌트는 항상 클릭을 막지 않는다 (안내 전용)
        _canvasGroup.interactable = false;
    }
    #endregion
}
