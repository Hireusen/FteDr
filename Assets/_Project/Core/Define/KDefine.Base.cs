using UnityEngine;

/// <summary>
/// 프로젝트의 기본적인 상수를 정의하는 매크로 클래스입니다.
/// </summary>
public static partial class K
{
    // 프로젝트 표준 비율
    public const int SCREEN_WIDTH = 1920;
    public const int SCREEN_HEIGHT = 1080;

    // 그래픽 기본 설정
    public const FullScreenMode DEFAULT_SCREEN_MODE = FullScreenMode.FullScreenWindow;
    public const int DEFAULT_TARGET_FRAME_RATE = 60;
    public const bool DEFAULT_VSYNC = false;

    // 조작 기본 설정 (UI에는 100을 곱해 백분율로 표시된다)
    public const float DEFAULT_CAMERA_SENSITIVITY = 0.5f;
    public const float MIN_CAMERA_SENSITIVITY = 0.05f; // 0이면 카메라가 아예 돌아가지 않으므로 하한을 둔다
    public const float MAX_CAMERA_SENSITIVITY = 1.0f;

    // 상수
    public const float SMALL_DISTANCE = 0.001f;
}
