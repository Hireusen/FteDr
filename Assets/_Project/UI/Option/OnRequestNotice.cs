/// <summary>
/// 화면에 짧게 떴다 사라지는 안내 토스트를 요청할 때 발행합니다.
/// 특정 창에 종속되지 않는 범용 알림이라, CUIWindow 스택과는 무관하게 동작합니다.
/// </summary>
public readonly struct OnRequestNotice
{
    public readonly string message;

    public OnRequestNotice(string message)
    {
        this.message = message;
    }

    /// <param name="message">표시할 메시지</param>
    public static void Publish(string message)
    {
        CEventBus<OnRequestNotice>.Publish(new OnRequestNotice(message));
    }
}
