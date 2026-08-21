using UnityEngine;

/// <summary>
/// 해상도가 변경될 경우 발행합니다.
/// </summary>
public readonly struct OnOptionResolutionChanged
{
    public readonly int width;
    public readonly int height;
    public readonly FullScreenMode fullScreenMode;

    public OnOptionResolutionChanged(int width, int height, FullScreenMode fullScreenMode)
    {
        this.width = width;
        this.height = height;
        this.fullScreenMode = fullScreenMode;
    }

    /// <param name="width">화면 가로 길이</param>
    /// <param name="height">화면 세로 길이</param>
    /// <param name="fullScreenMode">전체화면 모드</param>
    public static void Publish(int width, int height, FullScreenMode fullScreenMode)
    {
        CEventBus<OnOptionResolutionChanged>.Publish(
            new OnOptionResolutionChanged(width, height, fullScreenMode));
    }
}
