/// <summary>
/// 산소량이 변경될 때 발행합니다.
/// </summary>
public readonly struct OnPlayerOxygenChanged
{
    public readonly float current;
    public readonly float max;

    public OnPlayerOxygenChanged(float current, float max)
    {
        this.current = current;
        this.max = max;
    }

    /// <param name="current">현재 산소량</param>
    /// <param name="max">현재 최대 산소량 (패널티 반영 후)</param>
    public static void Publish(float current, float max)
    {
        CEventBus<OnPlayerOxygenChanged>.Publish(new OnPlayerOxygenChanged(current, max));
    }
}
