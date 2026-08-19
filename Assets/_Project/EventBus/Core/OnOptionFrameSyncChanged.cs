/// <summary>
/// 프레임 제한 및 수직동기화 옵션이 변경될 경우 발행합니다.
/// </summary>
public readonly struct OnOptionFrameSyncChanged
{
    public readonly int targetFrameRate;
    public readonly bool vSync;

    public OnOptionFrameSyncChanged(int targetFrameRate, bool vSync)
    {
        this.targetFrameRate = targetFrameRate;
        this.vSync = vSync;
    }

    /// <param name="targetFrameRate">목표 프레임(-1은 제한 없음)</param>
    /// <param name="vSync">수직동기화 켬/끔 여부</param>
    public static void Publish(int targetFrameRate, bool vSync)
    {
        CEventBus<OnOptionFrameSyncChanged>.Publish(new OnOptionFrameSyncChanged(targetFrameRate, vSync));
    }
}
