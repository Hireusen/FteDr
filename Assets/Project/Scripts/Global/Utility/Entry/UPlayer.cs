/// <summary>
/// 플레이어 데이터 접근의 진입점 역할을 하는 퍼사드 클래스입니다.
/// </summary>
public static class UPlayer
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private static CPlayerManager Player => CPlayerManager.Ins;
    private static CProgressManager Progress => CProgressManager.Ins;
    #endregion

    #region ─────────────────────────▶ 연료 (휘발성) ◀─────────────────────────
    /// <summary>현재 연료량입니다.</summary>
    public static float CurrentFuel => Player.CurrentFuel;

    /// <summary>패널티가 적용된 현재 최대 연료량입니다.</summary>
    public static float MaxFuel => Player.MaxFuel;

    /// <summary>현재 연료가 경고 임계값 미만인지 여부입니다.</summary>
    public static bool IsFuelLow => Player.IsFuelLow;

    /// <summary>새 잠수를 시작합니다. 연료를 최대로 채우고 소지품을 리셋합니다.</summary>
    public static void ResetForNew() => Player.ResetForNew();

    /// <summary>연료를 소모합니다.</summary>
    /// <param name="amount">소모량(양수)</param>
    public static void ConsumeFuel(float amount) => Player.ConsumeFuel(amount);

    /// <summary>연료를 회복합니다. (공깃방울 등)</summary>
    /// <param name="amount">회복량(양수)</param>
    public static void RecoverFuel(float amount) => Player.RecoverFuel(amount);

    /// <summary>적 공격 등으로 최대 연료량을 깎습니다. (사망 시 회복)</summary>
    /// <param name="amount">감소량(양수)</param>
    public static void ApplyFuelPenalty(float amount) => Player.ApplyFuelPenalty(amount);
    #endregion

    #region ─────────────────────────▶ 가방 / 소지품 (휘발성) ◀─────────────────────────
    /// <summary>현재 가방 최대 슬롯 수입니다.</summary>
    public static int BagCapacity => Player.BagCapacity;

    /// <summary>가방에 빈 슬롯이 있는지 여부입니다.</summary>
    public static bool HasBagSpace => Player.HasBagSpace;

    /// <summary>일반 수집품을 가방에 담습니다. 공간이 없으면 False</summary>
    /// <param name="collectibleId">수집품 ID</param>
    public static bool TryAddToBag(string collectibleId) => Player.TryAddToBag(collectibleId);

    /// <summary>특수 수집품을 손에 듭니다. 이미 들고 있으면 False</summary>
    /// <param name="specialId">특수 수집품 ID</param>
    public static bool TryHoldSpecial(string specialId) => Player.TryHoldSpecial(specialId);
    #endregion

    #region ─────────────────────────▶ 재화 (영속) ◀─────────────────────────
    /// <summary>현재 보유 골드입니다.</summary>
    public static int Money => Progress.Money;

    /// <summary>골드를 추가합니다.</summary>
    /// <param name="amount">추가할 양</param>
    public static void AddMoney(int amount) => Progress.AddMoney(amount);

    /// <summary>골드가 충분하면 차감하고 True를 반환합니다.</summary>
    /// <param name="cost">차감할 비용</param>
    public static bool TrySpendMoney(int cost) => Progress.TrySpendMoney(cost);
    #endregion

    #region ─────────────────────────▶ 업그레이드 (영속) ◀─────────────────────────
    /// <summary>지정한 장비의 현재 레벨을 반환합니다.</summary>
    /// <param name="gearType">장비 타입</param>
    public static int GetGearLevel(EDataType gearType) => Progress.GetGearLevel(gearType);

    /// <summary>지정한 장비의 레벨을 1 올립니다. (최대 레벨이면 실패)</summary>
    /// <param name="gearType">장비 타입</param>
    public static bool UpgradeGear(EDataType gearType) => Progress.UpgradeGear(gearType);
    #endregion

    #region ─────────────────────────▶ 진행 상황 (영속) ◀─────────────────────────
    /// <summary>돈으로 해금한 최대 스테이지입니다. (이동 가능 범위의 상한)</summary>
    public static int UnlockedStage => Progress.UnlockedStage;

    /// <summary>현재 위치한 스테이지입니다.</summary>
    public static int CurrentStage => Progress.CurrentStage;

    /// <summary>해당 스테이지가 이미 해금되어 자유 이동 가능한지 여부입니다.</summary>
    /// <param name="stage">대상 스테이지</param>
    public static bool IsStageUnlocked(int stage) => Progress.IsStageUnlocked(stage);

    /// <summary>현재 위치를 지정 스테이지로 설정합니다. (해금 범위 내 상승/하강)</summary>
    /// <param name="stage">이동할 스테이지</param>
    public static void SetCurrentStage(int stage) => Progress.SetCurrentStage(stage);

    /// <summary>다음 스테이지를 해금합니다. (해금 비용 차감은 호출부 책임)</summary>
    public static void UnlockNextStage() => Progress.UnlockNextStage();
    #endregion
}
