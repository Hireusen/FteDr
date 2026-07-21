using UnityEngine;
using TMPro;

/// <summary>
/// 상점 오른쪽의 상세 패널입니다. 로우에 마우스를 올리면 그 장비의 이름/현재/다음 레벨 정보를 보여주고,
/// 골드 표시도 실시간으로 갱신합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CShopDetailPanel : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _currentText;
    [SerializeField] private TMP_Text _nextText;
    [Tooltip("보유 골드 표시 (선택)")]
    [SerializeField] private TMP_Text _moneyText;

    [Header("표시 형식")]
    [SerializeField] private string _currentFormat = "현재 레벨 : {0}\n{1}";
    [SerializeField] private string _nextFormat = "다음 레벨 : {0}\n{1}";
    [SerializeField] private string _maxLevelText = "최대 레벨입니다";
    [SerializeField] private string _moneyFormat = "{0} G";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    // 구매 등으로 레벨이 바뀌었을 때, 지금 보여주고 있는 장비라면 같이 갱신하기 위해 기억해둔다.
    private EDataType _shownGearType = EDataType.None;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnRequestShowGearDetail>.Subscribe(ShowHandler);
        CEventBus<OnGearUpgraded>.Subscribe(GearUpgradedHandler);
        CEventBus<OnMoneyChanged>.Subscribe(MoneyChangedHandler);

        RefreshMoney(UPlayer.Money);
    }

    private void OnDisable()
    {
        CEventBus<OnRequestShowGearDetail>.Unsubscribe(ShowHandler);
        CEventBus<OnGearUpgraded>.Unsubscribe(GearUpgradedHandler);
        CEventBus<OnMoneyChanged>.Unsubscribe(MoneyChangedHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void ShowHandler(OnRequestShowGearDetail ctx)
    {
        _shownGearType = ctx.gear.Type;
        Bind(ctx.displayName, ctx.gear, ctx.currentLevel);
    }

    // 지금 보여주고 있는 장비가 업그레이드됐다면, 바뀐 레벨 기준으로 다시 갱신한다.
    private void GearUpgradedHandler(OnGearUpgraded ctx)
    {
        if (ctx.gearType != _shownGearType) return;

        AGearSO gear = UShopGearData.ResolveGear(ctx.gearType);
        if (gear == null) return;

        Bind(null, gear, ctx.newLevel);
    }

    private void MoneyChangedHandler(OnMoneyChanged ctx)
    {
        RefreshMoney(ctx.money);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // displayName이 null이면 이름 텍스트는 갱신하지 않는다 (레벨업으로 인한 재갱신 시 이름은 그대로 유지).
    private void Bind(string displayName, AGearSO gear, int currentLevel)
    {
        if (displayName != null && _nameText != null)
        {
            _nameText.text = displayName;
        }

        if (_currentText != null)
        {
            _currentText.text = string.Format(_currentFormat, currentLevel, UShopGearData.GetStatSummary(gear, currentLevel));
        }

        int maxLevel = gear.MaxLevel;
        if (_nextText != null)
        {
            if (currentLevel >= maxLevel)
            {
                _nextText.text = _maxLevelText;
            }
            else
            {
                int nextLevel = currentLevel + 1;
                _nextText.text = string.Format(_nextFormat, nextLevel, UShopGearData.GetStatSummary(gear, nextLevel));
            }
        }
    }

    private void RefreshMoney(int money)
    {
        if (_moneyText != null)
        {
            _moneyText.text = string.Format(_moneyFormat, money);
        }
    }
    #endregion
}
