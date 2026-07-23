/// <summary>
/// 상점 UI 전용 유틸리티입니다. AGearSO/각 서브클래스를 타입별로 어떻게 표시할지를 이 파일에 몰아넣었습니다.
/// 새 장비 타입이 추가되면 이 파일의 메서드들(ResolveGear/GetCurrentLevel/ApplyPurchase/GetStatSummary)에
/// 케이스만 추가하면 됩니다.
/// 잠수함(Submarine)은 UData에 대응 항목이 없고 '레벨' 대신 '해금된 스테이지'를 쓰는 예외 케이스라
/// GetCurrentLevel/ApplyPurchase에서만 특별 처리합니다.
/// </summary>
public static class UShopGearData
{
    /// <summary>
    /// EDataType으로 해당 장비 SO를 조회합니다. (이미 있는 UData.FuelTank() 등을 그대로 호출만 함)
    /// 잠수함(Submarine)처럼 UData에 대응 항목이 없는 타입은 UData를 건드리지 않기 위해,
    /// 호출부(로우)가 직접 들고 있는 SO를 <paramref name="directOverride"/>로 넘겨받아 그대로 반환합니다.
    /// </summary>
    /// <param name="gearType">장비 타입</param>
    /// <param name="directOverride">UData로 조회할 수 없는 타입(잠수함 등)을 위해 호출부가 직접 연결한 SO</param>
    public static AGearSO ResolveGear(EDataType gearType, AGearSO directOverride = null)
    {
        switch (gearType)
        {
            case EDataType.FuelTank: return UData.FuelTank();
            case EDataType.Thruster: return UData.Thruster();
            case EDataType.Radar: return UData.Radar();
            case EDataType.GrabTool: return UData.GrabTool();
            case EDataType.Bag: return UData.Bag();
            case EDataType.Submarine: return directOverride;
            default: return null;
        }
    }

    /// <summary>
    /// 상점 로우가 표시할 '현재 레벨'을 반환합니다.
    /// 일반 장비는 UPlayer.GetGearLevel(1부터 시작)을 그대로 쓰고,
    /// 잠수함은 UPlayer.UnlockedStage(0부터 시작, "기본 스테이지 외에 추가로 해금한 스테이지 수")에 +1을 더해서
    /// AGearSO의 1-based 레벨 규칙(UpgradeCost/MaxLevel이 기대하는 인덱스)에 맞춥니다.
    /// (예: UnlockedStage == 0 → level == 1 → _upgradeCosts[0]이 다음 해금 비용)
    /// </summary>
    /// <param name="gearType">장비 타입</param>
    public static int GetCurrentLevel(EDataType gearType)
    {
        return gearType == EDataType.Submarine
            ? UPlayer.UnlockedStage + 1
            : UPlayer.GetGearLevel(gearType);
    }

    /// <summary>
    /// 골드 차감 이후, 실제 업그레이드/해금 효과를 적용합니다.
    /// 일반 장비는 UPlayer.UpgradeGear를, 잠수함은 UPlayer.UnlockNextStage를 호출합니다.
    /// (UnlockNextStage는 실패 케이스가 없으므로 잠수함은 항상 true를 반환. 이미 최대 스테이지인 경우는
    ///  호출 이전에 UpgradeCost(level)가 -1을 반환해서 구매 자체가 막히므로 여기까지 오지 않음)
    /// </summary>
    /// <param name="gearType">장비 타입</param>
    public static bool ApplyPurchase(EDataType gearType)
    {
        if (gearType == EDataType.Submarine)
        {
            UPlayer.UnlockNextStage();
            return true;
        }

        return UPlayer.UpgradeGear(gearType);
    }

    /// <summary>
    /// 상점 상세 패널에 표시할, 해당 레벨의 대표 스탯 요약 문자열을 반환합니다.
    /// 장비 SO 종류에 따라 이미 공개되어 있는 메서드(MaxFuel, Capacity 등)만 호출합니다.
    /// </summary>
    /// <param name="gear">대상 장비 SO</param>
    /// <param name="level">조회할 레벨</param>
    public static string GetStatSummary(AGearSO gear, int level)
    {
        switch (gear)
        {
            case CFuelTankSO fuelTank:
                return $"최대 산소량: {fuelTank.MaxFuel(level):0}";
            case CBagSO bag:
                return $"칸 수: {bag.Capacity(level)} / 최대 무게: {bag.MaxWeight(level):0}KG";
            case CThrusterSO thruster:
                return $"최고 속도: {thruster.MaxSpeed(level):0.#}";
            case CRadarSO radar:
                return $"감지 거리: {radar.MaxDetectDistance(level):0}M";
            case CGrabToolSO grabTool:
                return $"사거리: {grabTool.ReachDistance(level):0.#}M";
            case CSubmarineSO:
                return $"스테이지 {level}까지 해금되었습니다. → 구매 시 스테이지 {level + 1}까지 해금됩니다.";
            default:
                return string.Empty;
        }
    }
}
