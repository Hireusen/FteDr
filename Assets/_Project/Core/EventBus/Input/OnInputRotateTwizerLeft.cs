/// <summary>
/// 집게 손 왼쪽 회전 키를 눌렀을 때나 뗏을 때 1회 발행합니다.
/// </summary>
public readonly struct OnInputRotateTwizerLeft
{
    public readonly bool leftPressed;

    public OnInputRotateTwizerLeft(bool leftPressed)
    {
        this.leftPressed = leftPressed;
    }

    /// <param name="leftPressed">키 누르기 여부</param>
    public static void Publish(bool leftPressed)
    {
        CEventBus<OnInputRotateTwizerLeft>.Publish(new OnInputRotateTwizerLeft(leftPressed));
    }
}
