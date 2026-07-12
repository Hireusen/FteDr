/// <summary>
/// 그랩 키를 눌렀을 때 1회 발행합니다.
/// </summary>
public readonly struct OnInputGrab
{
    public static void Publish()
    {
        CEventBus<OnInputGrab>.Publish(new OnInputGrab());
    }
}
