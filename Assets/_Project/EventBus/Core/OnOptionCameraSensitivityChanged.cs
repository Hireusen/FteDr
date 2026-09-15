/// <summary>
/// 카메라 회전 감도를 변경할 경우 발행합니다.
/// </summary>
public readonly struct OnOptionCameraSensitivityChanged
{
    public readonly float sensitivity;

    public OnOptionCameraSensitivityChanged(float sensitivity)
    {
        this.sensitivity = sensitivity;
    }

    /// <param name="sensitivity">카메라 회전 감도 (K.MIN_CAMERA_SENSITIVITY ~ K.MAX_CAMERA_SENSITIVITY)</param>
    public static void Publish(float sensitivity)
    {
        CEventBus<OnOptionCameraSensitivityChanged>.Publish(new OnOptionCameraSensitivityChanged(sensitivity));
    }
}
