using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerUI_Canvas(HUD)를 담당하는 컨트롤러입니다. 여닫는 "창"이 아니라 상시 표시되는 HUD라서
/// CUIWindow는 붙지 않고, CUIManager가 계산해서 발행하는 OnRequestHudVisibility를 직접 구독합니다.
/// </summary>
public sealed class CPlayerHudController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("HUD 표시 제어")]
    [Tooltip("Shop/Inventory 등 HidesHud 창이 열리면 이 캔버스 그룹을 꺼서 숨깁니다.")]
    [SerializeField] private CanvasGroup _hudCanvasGroup;

    [Header("산소(연료) 게이지")]
    [SerializeField] private Image _oxygenFillImage;
    [SerializeField] private TMP_Text _oxygenCurText;
    [SerializeField] private TMP_Text _oxygenMaxText;
    [SerializeField] private string _oxygenFormat = "{0:0} / {1:0}";

    [Header("버튼")]
    [SerializeField] private Button _btnBag;
    [SerializeField] private Button _btnNet;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_btnBag != null)
        {
            _btnBag.onClick.AddListener(() => OnRequestOpenUI.Publish(EUI.InventoryWindow));
        }

        if (_btnNet != null)
        {
            // 그물 버튼은 UI를 열지 않고 즉시 게임플레이 액션으로 이어진다. 키보드와 동일한 이벤트를 재사용.
            _btnNet.onClick.AddListener(() => OnInputNet.Publish());
        }
    }

    private void OnEnable()
    {
        CEventBus<OnPlayerFuelChanged>.Subscribe(FuelHandler);
        CEventBus<OnRequestHudVisibility>.Subscribe(HudVisibilityHandler);

        RefreshOxygen(UPlayer.CurrentFuel, UPlayer.MaxFuel); // 진입 시 현재값 즉시 반영
        HudVisibilityHandler(new OnRequestHudVisibility(true)); // Game Scene 진입 시 HUD는 기본적으로 켜져있어야 한다
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerFuelChanged>.Unsubscribe(FuelHandler);
        CEventBus<OnRequestHudVisibility>.Unsubscribe(HudVisibilityHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void FuelHandler(OnPlayerFuelChanged ctx)
    {
        RefreshOxygen(ctx.current, ctx.max);
    }

    private void HudVisibilityHandler(OnRequestHudVisibility ctx)
    {
        if (_hudCanvasGroup == null) return;

        _hudCanvasGroup.alpha = ctx.visible ? 1f : 0f;
        _hudCanvasGroup.blocksRaycasts = ctx.visible;
        _hudCanvasGroup.interactable = ctx.visible;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void RefreshOxygen(float current, float max)
    {
        if (_oxygenFillImage != null)
        {
            _oxygenFillImage.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        if (_oxygenCurText != null && _oxygenMaxText != null)
        {
            _oxygenCurText.text = string.Format("{0:0}", current);
            _oxygenMaxText.text = string.Format("{0:0}", max);
        }
    }
    #endregion
}
