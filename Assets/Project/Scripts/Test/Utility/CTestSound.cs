using UnityEngine;

/// <summary>
/// USound의 각 기능을 테스트하는 스크립트입니다.
/// </summary>
public class CTestSound : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("테스트 트리거")]
    [SerializeField] private bool _testSfxCamera;
    [SerializeField] private bool _testSfxPosition;
    [SerializeField] private bool _testSfxFollow;
    [SerializeField] private bool _testStopAllSfx;
    [SerializeField] private bool _testStopAllSfxFade;
    [SerializeField] private bool _testBgmPlay;
    [SerializeField] private bool _testBgmStop;
    [SerializeField] private bool _testAmbiencePlay;
    [SerializeField] private bool _testAmbienceStop;
    [SerializeField] private bool _testUnderwaterOn;
    [SerializeField] private bool _testUnderwaterOff;
    [SerializeField] private bool _testRefreshVolume;

    [Header("재생 파라미터")]
    [SerializeField] private Transform _followTarget;
    [SerializeField] private Vector3 _sfxPosition = new Vector3(0f, 0f, 5f);
    [SerializeField] private float _fadeOutTime = 0.4f;
    [SerializeField] private float _underwaterCutoff = 750f;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    public void ExecuteUpdateFrame()
    {
        // SFX
        if (_testSfxCamera)
        {
            _testSfxCamera = false;
            USound.PlaySfx(Id.SFX_Sonar_Ping);
            UDebug.Print("카메라 SFX 재생");
        }
        if (_testSfxPosition)
        {
            _testSfxPosition = false;
            USound.PlaySfx(Id.SFX_Swim_Submerged_02, _sfxPosition);
            UDebug.Print($"좌표 SFX 재생 @ {_sfxPosition}");
        }
        if (_testSfxFollow)
        {
            _testSfxFollow = false;
            if (_followTarget != null)
            {
                USound.PlaySfx(Id.SFX_Jump_03, _followTarget);
                UDebug.Print("추종 SFX 재생");
            }
            else
            {
                UDebug.Print("추종 대상(_followTarget)이 비어있습니다.", LogType.Warning);
            }
        }
        if (_testStopAllSfx)
        {
            _testStopAllSfx = false;
            USound.StopAllSfx();
            UDebug.Print("모든 SFX 즉시 중단");
        }
        if (_testStopAllSfxFade)
        {
            _testStopAllSfxFade = false;
            USound.StopAllSfx(_fadeOutTime);
            UDebug.Print($"모든 SFX 페이드 중단 ({_fadeOutTime}초)");
        }

        // BGM
        if (_testBgmPlay)
        {
            _testBgmPlay = false;
            USound.PlayBgm(Id.BGM_Ocean_2_Loop);
            UDebug.Print("BGM 재생");
        }
        if (_testBgmStop)
        {
            _testBgmStop = false;
            USound.StopBgm();
            UDebug.Print("BGM 중단");
        }

        // Ambience
        if (_testAmbiencePlay)
        {
            _testAmbiencePlay = false;
            USound.PlayAmbience(Id.BGM_Observing_The_Star);
            UDebug.Print("환경음 재생");
        }
        if (_testAmbienceStop)
        {
            _testAmbienceStop = false;
            USound.StopAmbience();
            UDebug.Print("환경음 중단");
        }

        // 기타
        if (_testUnderwaterOn)
        {
            _testUnderwaterOn = false;
            USound.SetUnderwater(true, _underwaterCutoff);
            UDebug.Print($"수중 효과 ON (cutoff {_underwaterCutoff}Hz)");
        }
        if (_testUnderwaterOff)
        {
            _testUnderwaterOff = false;
            USound.SetUnderwater(false);
            UDebug.Print("수중 효과 OFF");
        }
        if (_testRefreshVolume)
        {
            _testRefreshVolume = false;
            USound.RefreshVolume();
            UDebug.Print("볼륨 갱신 호출");
        }
    }
    #endregion
}
