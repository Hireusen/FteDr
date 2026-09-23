using UnityEngine;
using System;

/// <summary>
/// 로컬에 저장되는 사용자 옵션 데이터입니다.
/// </summary>
[Serializable]
public class OptionData
{
    #region ─────────────────────────▶ 볼륨 ◀─────────────────────────
    // 0~1 범위의 정규화된 볼륨 값
    public float masterVolume = 0.75f;
    public float bgmVolume = 0.6f;
    public float sfxVolume = 0.8f;
    public float ambienceVolume = 0.7f;
    #endregion

    #region ─────────────────────────▶ 화면 ◀─────────────────────────
    // 해상도
    public int resolutionWidth = K.SCREEN_WIDTH;
    public int resolutionHeight = K.SCREEN_HEIGHT;

    // 화면 모드
    public FullScreenMode screenMode = K.DEFAULT_SCREEN_MODE;

    // 프레임 및 수직동기화
    public int targetFrameRate = K.DEFAULT_TARGET_FRAME_RATE;
    public bool vSync = K.DEFAULT_VSYNC;
    #endregion

    #region ─────────────────────────▷ 그래픽 ◁─────────────────────────
    public bool useShadow = true;
    public int textureQualityLimit = 0; // 0 : 원본 / 1 : 절반 해상도 / 2 : 절절반 해상도
    public float verticalFOV = 60f;
    public float farClipPlane = 95f;
    #endregion

    #region ─────────────────────────▶ 조작 ◀─────────────────────────
    // 카메라 감도 단계 (0~1, 슬라이더 값 그대로. UI에는 100을 곱해 백분율로 표시)
    // 실제 회전 배율은 CLocalOptionManager.CameraSensitivity로 변환해서 쓴다.
    public float cameraSensitivityLevel = K.DEFAULT_CAMERA_SENSITIVITY_LEVEL;
    #endregion
}
