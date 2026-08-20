/// <summary>
/// 정적 이벤트 버스의 구조체입니다.
/// </summary>
public readonly struct OnPlayerFuelStateChanged
{
    public readonly EFuelState state;
    public readonly EFuelState previous;

    public OnPlayerFuelStateChanged(EFuelState state, EFuelState previous)
    {
        this.state = state;
        this.previous = previous;
    }

    public static void Publish(EFuelState state, EFuelState previous)
    {
        CEventBus<OnPlayerFuelStateChanged>.Publish(new OnPlayerFuelStateChanged(state, previous));
    }
}
