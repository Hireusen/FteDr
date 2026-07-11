/// <summary>
/// 회수 키를 눌렀을 때 1회 발행합니다.
/// </summary>
public readonly struct OnInputCollect
{
    public static void Publish()
    {
        CEventBus<OnInputCollect>.Publish(new OnInputCollect());
    }
}
