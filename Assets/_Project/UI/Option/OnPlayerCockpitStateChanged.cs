/// <summary>
/// 플레이어가 잠수함 조종석에 앉거나 내릴 때 발행합니다.
/// </summary>
public readonly struct OnPlayerCockpitStateChanged
{
    public readonly bool isSitting;

    public OnPlayerCockpitStateChanged(bool isSitting)
    {
        this.isSitting = isSitting;
    }

    /// <param name="isSitting">true면 조종석에 앉음, false면 내림</param>
    public static void Publish(bool isSitting)
    {
        CEventBus<OnPlayerCockpitStateChanged>.Publish(new OnPlayerCockpitStateChanged(isSitting));
    }
}
