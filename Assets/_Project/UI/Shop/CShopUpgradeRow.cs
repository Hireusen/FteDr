using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상점 목록의 장비 로우 하나를 표현하는 컴포넌트입니다. (최대 산소량 증가, 가방, 추진기 등 5종 전부 공용)
/// EDataType만 다르게 지정해서 5번 배치하면 됩니다. 타입별 분기는 전부 UShopGearData가 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CShopUpgradeRow : AMono, IPointerEnterHandler
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("데이터")]
    [Tooltip("이 로우가 나타내는 장비 타입")]
    [SerializeField] private EDataType _gearType;
    [Tooltip("화면에 표시할 이름 (예: 최대 산소량 증가)")]
    [SerializeField] private string _displayName;

    [Header("필수 연결")]
    [SerializeField] private TMP_Text _nameLevelText;
    [Tooltip("현재 레벨 / 최대 레벨 비율을 보여줄 이미지 (Image Type: Filled)")]
    [SerializeField] private Image _levelFillImage;
    [SerializeField] private TMP_Text _priceText;
    [Tooltip("가격 버튼. 누르면 즉시 구매/업그레이드된다.")]
    [SerializeField] private Button _priceButton;

    [Header("표시 형식")]
    [SerializeField] private string _nameLevelFormat = "{0} LV {1}";
    [SerializeField] private string _priceFormat = "{0} G";
    [SerializeField] private string _maxLevelLabel = "MAX";
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (_priceButton != null)
        {
            _priceButton.onClick.AddListener(OnClickPurchase);
        }
    }

    private void OnEnable()
    {
        CEventBus<OnGearUpgraded>.Subscribe(GearUpgradedHandler);
        CEventBus<OnMoneyChanged>.Subscribe(MoneyChangedHandler);

        Refresh();
    }

    private void OnDisable()
    {
        CEventBus<OnGearUpgraded>.Unsubscribe(GearUpgradedHandler);
        CEventBus<OnMoneyChanged>.Unsubscribe(MoneyChangedHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        AGearSO gear = UShopGearData.ResolveGear(_gearType);
        if (gear == null) return;

        OnRequestShowGearDetail.Publish(_displayName, gear, UPlayer.GetGearLevel(_gearType));
    }

    // 내 장비 타입이 업그레이드됐을 때만 반응한다 (다른 로우의 구매에는 반응하지 않음).
    private void GearUpgradedHandler(OnGearUpgraded ctx)
    {
        if (ctx.gearType != _gearType) return;
        Refresh();
    }

    // 골드는 모든 로우의 구매 가능 여부에 영향을 주므로, 어떤 로우든 상관없이 갱신한다.
    private void MoneyChangedHandler(OnMoneyChanged ctx)
    {
        Refresh();
    }

    // 비용 확인 → 차감 → 업그레이드를 UPlayer에 이미 있는 멤버들만 조합해서 처리한다. (UPlayer에 새 메서드 추가 없음)
    private void OnClickPurchase()
    {
        AGearSO gear = UShopGearData.ResolveGear(_gearType);
        if (gear == null) return;

        int level = UPlayer.GetGearLevel(_gearType);
        int cost = gear.UpgradeCost(level);

        if (cost < 0)
        {
            UDebug.Print($"[상점] '{_displayName}'은(는) 이미 최대 레벨입니다.", LogType.Warning, gameObject);
            return;
        }

        if (!UPlayer.TrySpendMoney(cost))
        {
            OnRequestNotice.Publish("골드가 부족합니다.");
            UDebug.Print($"[상점] '{_displayName}' 구매 실패 (골드 부족)", LogType.Warning, gameObject);
            return;
        }

        if (!UPlayer.UpgradeGear(_gearType))
        {
            UPlayer.AddMoney(cost); // 방어: 업그레이드 실패 시 차감한 골드를 되돌린다
            UDebug.Print($"[상점] '{_displayName}' 업그레이드 실패 (골드는 환불됨)", LogType.Warning, gameObject);
        }
        // 성공 시의 화면 갱신은 OnGearUpgraded/OnMoneyChanged 구독으로 자동 처리된다.
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void Refresh()
    {
        AGearSO gear = UShopGearData.ResolveGear(_gearType);
        if (gear == null)
        {
            UDebug.Print($"CShopUpgradeRow: EDataType '{_gearType}'에 해당하는 장비 SO를 찾을 수 없습니다.", LogType.Error, gameObject);
            return;
        }

        int level = UPlayer.GetGearLevel(_gearType);
        int maxLevel = gear.MaxLevel;
        int cost = gear.UpgradeCost(level);

        if (_nameLevelText != null)
        {
            _nameLevelText.text = string.Format(_nameLevelFormat, _displayName, level);
        }

        if (_levelFillImage != null)
        {
            _levelFillImage.fillAmount = maxLevel > 0 ? Mathf.Clamp01((float)level / maxLevel) : 0f;
        }

        bool isMaxLevel = cost < 0;
        if (_priceText != null)
        {
            _priceText.text = isMaxLevel ? _maxLevelLabel : string.Format(_priceFormat, cost);
        }

        if (_priceButton != null)
        {
            _priceButton.interactable = !isMaxLevel && UPlayer.Money >= cost;
        }
    }
    #endregion
}
