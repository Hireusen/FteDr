/// <summary>
/// 그물 키를 눌렀을 때 1회 발행합니다.
/// </summary>
public readonly struct OnInputInventory
{
    public static void Publish()
    {
        CEventBus<OnInputInventory>.Publish(new OnInputInventory());
    }
}
