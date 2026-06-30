/// <summary>
/// 가방 소지품이 변경될 때 발행합니다.
/// </summary>
public readonly struct OnPlayerBagChanged
{
    public readonly int count;
    public readonly int capacity;

    public OnPlayerBagChanged(int count, int capacity)
    {
        this.count = count;
        this.capacity = capacity;
    }

    /// <param name="count">현재 가방에 담긴 개수</param>
    /// <param name="capacity">현재 가방 최대 슬롯 수</param>
    public static void Publish(int count, int capacity)
    {
        CEventBus<OnPlayerBagChanged>.Publish(new OnPlayerBagChanged(count, capacity));
    }
}
