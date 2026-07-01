using UnityEngine;

/// <summary>
/// 플레이어의 런타임 데이터를 보유하고 스탯을 계산하는 매니저입니다.
/// </summary>
public sealed class CPlayerManager : ASingleton<CPlayerManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly PlayerRuntimeData _runtime = new();
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override bool IsGlobal => true;

    /// <summary>플레이어 런타임 데이터를 읽습니다.</summary>
    public PlayerRuntimeData Runtime => _runtime;
    #endregion

    #region ─────────────────────────▶ 연료 ◀─────────────────────────
    /// <summary>현재 연료량입니다.</summary>
    public float CurrentFuel => _runtime.currentFuel;

    /// <summary>패널티가 적용된 현재 최대 연료량입니다.</summary>
    public float MaxFuel
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.FuelTank);
            float baseMax = UData.FuelTank().MaxFuel(level);
            return Mathf.Max(0f, baseMax - _runtime.fuelPenalty);
        }
    }

    /// <summary>현재 연료가 경고 임계값 미만인지 여부입니다. (5% 미만 화면 이펙트 등)</summary>
    public bool IsFuelLow
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.FuelTank);
            float threshold = UData.FuelTank().WarningThreshold(level); // 0~1 비율
            float max = MaxFuel;
            if (max <= 0f) return true;

            return (_runtime.currentFuel / max) < threshold;
        }
    }

    /// <summary>새 잠수를 시작합니다. 연료를 최대로 채우고 페널티/소지품을 리셋합니다.</summary>
    public void ResetForNew()
    {
        _runtime.ResetForNew();
        _runtime.currentFuel = MaxFuel;
        PublishFuel();
        PublishBag();
    }

    /// <summary>연료를 소모합니다.</summary>
    /// <param name="amount">소모량(양수)</param>
    public void ConsumeFuel(float amount)
    {
        _runtime.currentFuel = Mathf.Max(0f, _runtime.currentFuel - amount);
        PublishFuel();
    }

    /// <summary>연료를 회복합니다.</summary>
    /// <param name="amount">회복량(양수)</param>
    public void RecoverFuel(float amount)
    {
        _runtime.currentFuel = Mathf.Min(MaxFuel, _runtime.currentFuel + amount);
        PublishFuel();
    }

    /// <summary>최대 연료량을 깎습니다.</summary>
    /// <param name="amount">감소량(양수)</param>
    public void ApplyFuelPenalty(float amount)
    {
        _runtime.fuelPenalty += Mathf.Max(0f, amount);
        _runtime.currentFuel = Mathf.Min(_runtime.currentFuel, MaxFuel);
        PublishFuel();
    }
    #endregion

    #region ─────────────────────────▶ 가방 / 소지품 ◀─────────────────────────
    /// <summary>현재 가방 최대 슬롯 수입니다.</summary>
    public int BagCapacity
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.Bag);
            return UData.Bag().Capacity(level);
        }
    }

    /// <summary>가방에 빈 슬롯이 있는지 여부입니다.</summary>
    public bool HasBagSpace => _runtime.bagItems.Count < BagCapacity;

    /// <summary>일반 수집품을 가방에 담습니다. 공간이 없으면 false.</summary>
    /// <param name="collectibleId">수집품 ID</param>
    public bool TryAddToBag(string collectibleId)
    {
        if (!HasBagSpace) return false;

        _runtime.bagItems.Add(collectibleId);
        PublishBag();
        return true;
    }

    /// <summary>특수 수집품을 손에 듭니다. 이미 들고 있으면 False</summary>
    /// <param name="specialId">특수 수집품 ID</param>
    public bool TryHoldSpecial(string specialId)
    {
        if (!_runtime.heldSpecialItem.IsBlank()) return false;

        _runtime.heldSpecialItem = specialId;
        return true;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 부모 클래스가 최초 1회 호출합니다.
    protected override void Initialize()
    {

    }

    private void PublishFuel()
    {
        OnPlayerFuelChanged.Publish(_runtime.currentFuel, MaxFuel);
    }

    private void PublishBag()
    {
        OnPlayerBagChanged.Publish(_runtime.bagItems.Count, BagCapacity);
    }
    #endregion
}
