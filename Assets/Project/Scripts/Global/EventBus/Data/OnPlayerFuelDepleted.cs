/// <summary>
/// 연료가 0으로 고갈되는 순간 1회 발행합니다.
/// </summary>
public readonly struct OnPlayerFuelDepleted
{
    public static void Publish()
    {
        CEventBus<OnPlayerFuelDepleted>.Publish(new OnPlayerFuelDepleted());
    }
}
