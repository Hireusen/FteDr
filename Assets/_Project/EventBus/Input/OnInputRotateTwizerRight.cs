/// <summary>
/// 집게 손 오른쪽 회전 키를 눌렀을 때나 뗏을 때 1회 발행합니다.
/// </summary>
public readonly struct OnInputRotateTwizerRight
{
    public readonly bool rightPressed;

    public OnInputRotateTwizerRight(bool rightPressed)
    {
        this.rightPressed = rightPressed;
    }

    /// <param name="rightPressed">키 누르기 여부</param>
    public static void Publish(bool rightPressed)
    {
        CEventBus<OnInputRotateTwizerRight>.Publish(new OnInputRotateTwizerRight(rightPressed));
    }
}
