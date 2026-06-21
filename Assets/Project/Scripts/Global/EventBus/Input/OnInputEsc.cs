/// <summary>
/// Esc 키를 눌렀을 때 1회 발행
/// </summary>
public readonly struct OnInputEsc
{
    public static void Publish()
    {
        CEventBus<OnInputEsc>.Publish(new OnInputEsc());
    }
}
