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

    // 조작 기본 설정
    // 감도 단계(0~1, UI에는 100을 곱해 백분율로 표시)는 실제 회전 배율로 지수 변환된다.
    // 사람은 회전 속도를 "몇 배인지"로 느끼므로, 단계가 같은 만큼 오를 때 배율이 같은 비율로 늘어야 체감이 균일하다.
    //   단계 0.0 → BASE / RANGE, 0.5 → BASE, 1.0 → BASE * RANGE  (RANGE=4면 25%마다 2배)
    public const float DEFAULT_CAMERA_SENSITIVITY_LEVEL = 0.5f;
    public const float CAMERA_SENSITIVITY_BASE = 0.5f;  // 슬라이더 중앙(50%)일 때의 회전 배율
    public const float CAMERA_SENSITIVITY_RANGE = 4.0f; // 양 끝에서 BASE의 몇 배 / 몇 분의 1까지 갈지

    // 상수
    public const float SMALL_DISTANCE = 0.001f;
}
