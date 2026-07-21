/// <summary>
/// 점프 키를 눌렀을 때나 뗏을 때 1회 발행합니다.
/// </summary>
public readonly struct OnInputRotateTwizerLeft
{
    public readonly bool jumpPressed;

    public OnInputRotateTwizerLeft(bool jumpPressed)
    {
        this.jumpPressed = jumpPressed;
    }

    /// <param name="jumpPressed">점프 입력 여부</param>
    public static void Publish(bool jumpPressed)
    {
        CEventBus<OnInputJump>.Publish(new OnInputJump(jumpPressed));
    }
}
