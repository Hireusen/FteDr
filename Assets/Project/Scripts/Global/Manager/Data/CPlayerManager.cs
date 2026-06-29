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

    #region ─────────────────────────▶ 산소 ◀─────────────────────────
    /// <summary>현재 산소량입니다.</summary>
    public float CurrentOxygen => _runtime.currentOxygen;

    /// <summary>패널티가 적용된 현재 최대 산소량입니다.</summary>
    public float MaxOxygen
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.OxygenTank);
            float baseMax = UData.OxygenTank().MaxOxygen(level);
            return Mathf.Max(0f, baseMax - _runtime.oxygenPenalty);
        }
    }

    /// <summary>현재 산소가 경고 임계값 미만인지 여부입니다. (5% 미만 화면 이펙트 등)</summary>
    public bool IsOxygenLow
    {
        get
        {
            int level = CProgressManager.Ins.GetGearLevel(EDataType.OxygenTank);
            float threshold = UData.OxygenTank().WarningThreshold(level); // 0~1 비율
            float max = MaxOxygen;
            if (max <= 0f) return true;

            return (_runtime.currentOxygen / max) < threshold;
        }
    }

    /// <summary>새 잠수를 시작합니다. 산소를 최대로 채우고 페널티/소지품을 리셋합니다.</summary>
    public void ResetForNew()
    {
        _runtime.ResetForNew();
        _runtime.currentOxygen = MaxOxygen;
        PublishOxygen();
        PublishBag();
    }

    /// <summary>산소를 소모합니다.</summary>
    /// <param name="amount">소모량(양수)</param>
    public void ConsumeOxygen(float amount)
    {
        _runtime.currentOxygen = Mathf.Max(0f, _runtime.currentOxygen - amount);
        PublishOxygen();
    }

    /// <summary>산소를 회복합니다.</summary>
    /// <param name="amount">회복량(양수)</param>
    public void RecoverOxygen(float amount)
    {
        _runtime.currentOxygen = Mathf.Min(MaxOxygen, _runtime.currentOxygen + amount);
        PublishOxygen();
    }

    /// <summary>최대 산소량을 깎습니다.</summary>
    /// <param name="amount">감소량(양수)</param>
    public void ApplyOxygenPenalty(float amount)
    {
        _runtime.oxygenPenalty += Mathf.Max(0f, amount);
        _runtime.currentOxygen = Mathf.Min(_runtime.currentOxygen, MaxOxygen);
        PublishOxygen();
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

    private void PublishOxygen()
    {
        OnPlayerOxygenChanged.Publish(_runtime.currentOxygen, MaxOxygen);
    }

    private void PublishBag()
    {
        OnPlayerBagChanged.Publish(_runtime.bagItems.Count, BagCapacity);
    }
    #endregion
}
