/// <summary>
/// 상점 UI 전용 유틸리티입니다. AGearSO/각 서브클래스를 타입별로 어떻게 표시할지를 이 파일에 몰아넣었습니다.
/// 새 장비 타입이 추가되면 이 파일의 두 메서드에만 케이스를 추가하면 됩니다.
/// </summary>
public static class UShopGearData
{
    /// <summary>
    /// EDataType으로 해당 장비 SO를 조회합니다. (이미 있는 UData.FuelTank() 등을 그대로 호출만 함)
    /// </summary>
    /// <param name="gearType">장비 타입</param>
    public static AGearSO ResolveGear(EDataType gearType)
    {
        switch (gearType)
        {
            case EDataType.FuelTank: return UData.FuelTank();
            case EDataType.Thruster: return UData.Thruster();
            case EDataType.Radar: return UData.Radar();
            case EDataType.GrabTool: return UData.GrabTool();
            case EDataType.Bag: return UData.Bag();
            default: return null;
        }
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
            default:
                return string.Empty;
        }
    }
}
