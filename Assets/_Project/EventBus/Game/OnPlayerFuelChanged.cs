/// <summary>
/// 연료량이 변경될 때 발행합니다.
/// </summary>
public readonly struct OnPlayerFuelChanged
{
    public readonly float current;
    public readonly float max;

    public OnPlayerFuelChanged(float current, float max)
    {
        this.current = current;
        this.max = max;
    }

    /// <param name="current">현재 연료량</param>
    /// <param name="max">현재 최대 연료량 (패널티 반영 후)</param>
    public static void Publish(float current, float max)
    {
        CEventBus<OnPlayerFuelChanged>.Publish(new OnPlayerFuelChanged(current, max));
    }
}
