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

    /// <param name="sensitivity">카메라가 입력에 곱해 쓰는 실제 회전 배율 (CLocalOptionManager.CameraSensitivity와 같음)</param>
    public static void Publish(float sensitivity)
    {
        CEventBus<OnOptionCameraSensitivityChanged>.Publish(new OnOptionCameraSensitivityChanged(sensitivity));
    }
}
