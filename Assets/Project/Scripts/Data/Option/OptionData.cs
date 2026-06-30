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

    // 전체화면
    public FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
    #endregion
}
