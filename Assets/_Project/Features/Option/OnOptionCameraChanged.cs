/// <summary>
/// 그림자 캐스팅 모드가 변경될 경우 발행합니다.
/// </summary>
public readonly struct OnOptionCameraChanged
{
    public readonly float fov;
    public readonly float clipPlane;

    public OnOptionCameraChanged(float fov, float clipPlane) {
        this.fov = fov;
        this.clipPlane = clipPlane;
    }

    public static void Publish(float fov, float clipPlane)
    {
        CEventBus<OnOptionCameraChanged>.Publish(new OnOptionCameraChanged(fov, clipPlane));
    }
}
