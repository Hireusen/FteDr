using UnityEngine;

/// <summary>
/// 각 씬에 배치하는 BGM 컴포넌트입니다.
/// </summary>
public class CBGMChange : AMono
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    [SerializeField] private bool _playing = true;

    private readonly string[] _playlist =
    {
        null, // Boot
        Id.BGM_サカナだった頃, // Title
        Id.BGM_Cleyton_RX_Underwater, // Stage 1
        Id.BGM_Underwater_Theme_II,
        Id.BGM_海の中の旋律, // Id.BGM_珊瑚礁,
        Id.BGM_Blue_Water,
        Id.BGM_Observing_The_Star,
        null, // Stage 6
        null, // Ending
    };
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public bool IsPlaying => _playing;
    public void SetPlay(bool playing)
    {
        _playing = playing;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (!_playing) return;

        int sceneIndex = (int)UScene.Current;
        USound.PlayBgm(_playlist[sceneIndex]);
    }
    #endregion
}
