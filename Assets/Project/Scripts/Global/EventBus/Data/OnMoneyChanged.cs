/// <summary>
/// 보유 골드가 변경될 때 발행합니다.
/// </summary>
public readonly struct OnMoneyChanged
{
    public readonly int money;

    public OnMoneyChanged(int money)
    {
        this.money = money;
    }

    /// <param name="money">변경 후 보유 골드</param>
    public static void Publish(int money)
    {
        CEventBus<OnMoneyChanged>.Publish(new OnMoneyChanged(money));
    }
}
