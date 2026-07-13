/// <summary>
/// 하강 키를 눌렀을 때나 뗏을 때 1회 발행합니다.
/// </summary>
public readonly struct OnInputDescent
{
    public readonly bool descentPressed;

    public OnInputDescent(bool descentPressed)
    {
        this.descentPressed = descentPressed;
    }

    /// <param name="descentPressed">점프 입력 여부</param>
    public static void Publish(bool descentPressed)
    {
        CEventBus<OnInputDescent>.Publish(new OnInputDescent(descentPressed));
    }
}
