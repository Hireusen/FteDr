/// <summary>
/// 치트 키를 눌렀을 때 1회 발행합니다.
/// </summary>
public readonly struct OnInputCheat
{
    public static void Publish()
    {
        CEventBus<OnInputCheat>.Publish(new OnInputCheat());
    }
}
