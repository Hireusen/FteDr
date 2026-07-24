using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// PlayerUI_Canvas(HUD)를 담당하는 컨트롤러입니다. 여닫는 "창"이 아니라 상시 표시되는 HUD라서
/// CUIWindow는 붙지 않고, CUIManager가 계산해서 발행하는 OnRequestHudVisibility를 직접 구독합니다.
/// </summary>
public sealed class CPlayerHubController : AMono
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

    [Header("산소 게이지 세로 길이 (레벨업 시 증가)")]
    [Tooltip("같이 늘어나야 하는 RectTransform들 (예: Background, Fill Area). 전부 동일한 목표 높이로 동시에 커집니다.")]
    [SerializeField] private RectTransform[] _oxygenHeightTargets;
    [SerializeField] private float _baseSliderHeight = 200f;
    [SerializeField] private float _heightPerLevel = 20f;
    [SerializeField] private float _heightGrowDuration = 0.4f;

    [Header("버튼")]
    [SerializeField] private Button _btnBag;
    [SerializeField] private Button _btnNet;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    // 최종 표시 여부 = 잠수함 밖 && UI 창들이 HUD를 허용할 때. 시작 시 잠수함 안이라고 가정해 기본 숨김.
    // (CSubmarineAreaSensor가 씬에 붙어있어야 "밖으로 나감" 이벤트가 와서 실제로 보이게 됨)
    private bool _isInsideSubmarine = true;
    private bool _windowsAllowHud = true;
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
        CEventBus<OnGearUpgraded>.Subscribe(GearUpgradedHandler);
        CEventBus<OnPlayerSubmarineAreaChanged>.Subscribe(SubmarineAreaHandler);

        RefreshOxygen(UPlayer.CurrentFuel, UPlayer.MaxFuel); // 진입 시 현재값 즉시 반영
        ApplySliderHeight(UPlayer.GetGearLevel(EDataType.FuelTank), instant: true); // 진입 시점 레벨 기준으로 즉시 반영 (연출 없음)
        RefreshHudVisibility(); // 시작 시 잠수함 안이라고 가정하고 있으므로 기본은 숨김 상태로 시작
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerFuelChanged>.Unsubscribe(FuelHandler);
        CEventBus<OnRequestHudVisibility>.Unsubscribe(HudVisibilityHandler);
        CEventBus<OnGearUpgraded>.Unsubscribe(GearUpgradedHandler);
        CEventBus<OnPlayerSubmarineAreaChanged>.Unsubscribe(SubmarineAreaHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void FuelHandler(OnPlayerFuelChanged ctx)
    {
        RefreshOxygen(ctx.current, ctx.max);
    }

    private void HudVisibilityHandler(OnRequestHudVisibility ctx)
    {
        _windowsAllowHud = ctx.visible;
        RefreshHudVisibility();
    }

    // 잠수함 트리거를 벗어나기 전까지는 다른 조건과 무관하게 HUD를 숨긴다.
    private void SubmarineAreaHandler(OnPlayerSubmarineAreaChanged ctx)
    {
        _isInsideSubmarine = ctx.isInsideSubmarine;
        RefreshHudVisibility();
    }

    // 연료탱크가 업그레이드되면 슬라이더 세로 길이를 부드럽게 늘린다.
    private void GearUpgradedHandler(OnGearUpgraded ctx)
    {
        if (ctx.gearType != EDataType.FuelTank) return;
        ApplySliderHeight(ctx.newLevel, instant: false);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 최종 표시 여부 = 잠수함 밖 && UI 창들이 HUD를 허용할 때
    private void RefreshHudVisibility()
    {
        if (_hudCanvasGroup == null) return;

        bool visible = !_isInsideSubmarine && _windowsAllowHud;
        _hudCanvasGroup.alpha = visible ? 1f : 0f;
        _hudCanvasGroup.blocksRaycasts = visible;
        _hudCanvasGroup.interactable = visible;
    }

    private void ApplySliderHeight(int fuelTankLevel, bool instant)
    {
        if (_oxygenHeightTargets == null || _oxygenHeightTargets.Length == 0) return;

        float targetHeight = _baseSliderHeight + (fuelTankLevel - 1) * _heightPerLevel;

        int count = _oxygenHeightTargets.Length;
        for (int i = 0; i < count; ++i)
        {
            RectTransform rect = _oxygenHeightTargets[i];
            if (rect == null) continue;

            rect.DOKill();
            Vector2 size = rect.sizeDelta;

            if (instant || _heightGrowDuration <= 0f)
            {
                rect.sizeDelta = new Vector2(size.x, targetHeight);
            }
            else
            {
                rect.DOSizeDelta(new Vector2(size.x, targetHeight), _heightGrowDuration).SetUpdate(true);
            }
        }
    }

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
