/// <summary>
/// 장비가 업그레이드되어 레벨이 오를 때 발행합니다.
/// </summary>
public readonly struct OnGearUpgraded
{
    public readonly EDataType gearType;
    public readonly int newLevel;

    public OnGearUpgraded(EDataType gearType, int newLevel)
    {
        this.gearType = gearType;
        this.newLevel = newLevel;
    }

    /// <param name="gearType">업그레이드된 장비 타입</param>
    /// <param name="newLevel">변경 후 레벨</param>
    public static void Publish(EDataType gearType, int newLevel)
    {
        CEventBus<OnGearUpgraded>.Publish(new OnGearUpgraded(gearType, newLevel));
    }
}
