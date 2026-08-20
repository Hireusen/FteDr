/// <summary>
/// 플레이어가 잠수함 트리거 안/밖으로 들어가고 나갈 때 발행합니다.
/// HUD/레이더 등, "잠수함을 벗어나기 전까지는 숨겨야 하는" UI들이 이 이벤트로 표시 여부를 결정합니다.
/// </summary>
public readonly struct OnPlayerSubmarineAreaChanged
{
    public readonly bool isInsideSubmarine;

    public OnPlayerSubmarineAreaChanged(bool isInsideSubmarine)
    {
        this.isInsideSubmarine = isInsideSubmarine;
    }

    /// <param name="isInsideSubmarine">true면 잠수함 트리거 안, false면 벗어남</param>
    public static void Publish(bool isInsideSubmarine)
    {
        CEventBus<OnPlayerSubmarineAreaChanged>.Publish(new OnPlayerSubmarineAreaChanged(isInsideSubmarine));
    }
}
