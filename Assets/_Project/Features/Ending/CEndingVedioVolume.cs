using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// 영상 볼륨 조절하기
/// </summary>
public class CEndingVedioVolume : AMono
{
    #region ─────────────────────────▷ 인스펙터 ◁─────────────────────────
    [Header("영상 연결")]
    [SerializeField] private VideoPlayer _vedioRef;

    [Header("소리 가중치")]
    [SerializeField] private float _soundMultifiler = 0.5f;

    #endregion

    #region ─────────────────────────▷ 내부 메서드 ◁─────────────────────────
    private void HandleVolumeChanged(OnOptionVolumeChanged ctx)
    {
        float volume = ctx.master * ctx.bgm * _soundMultifiler;
        _vedioRef.SetDirectAudioVolume(0, volume);
    }
    #endregion

    #region ─────────────────────────▷ 메시지 함수 ◁─────────────────────────
    private void Awake()
    {
        if (UDebug.IsNull(_vedioRef))
        {
            enabled = false;
            return;
        }

        var option = CLocalOptionManager.Ins.Option;
        HandleVolumeChanged(new OnOptionVolumeChanged(option.masterVolume, option.sfxVolume, option.bgmVolume, option.ambienceVolume));
    }
    private void OnEnable()
    {
        CEventBus<OnOptionVolumeChanged>.Subscribe(HandleVolumeChanged);
    }
    private void OnDisable()
    {
        CEventBus<OnOptionVolumeChanged>.Unsubscribe(HandleVolumeChanged);
    }
    #endregion

}
