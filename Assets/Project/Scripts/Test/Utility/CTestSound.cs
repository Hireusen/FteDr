using UnityEngine;

/// <summary>
/// USound의 각 기능을 키 입력으로 테스트하는 스크립트입니다.
/// </summary>
public class CTestSound : MonoBehaviour
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("재생 대상 (추종 테스트용)")]
    [SerializeField] private Transform _followTarget;

    [Header("SFX 키")]
    [SerializeField] private KeyCode _keySfxCamera = KeyCode.Q;   // 카메라 SFX
    [SerializeField] private KeyCode _keySfxPosition = KeyCode.W; // 좌표 SFX
    [SerializeField] private KeyCode _keySfxFollow = KeyCode.E;   // 추종 SFX
    [SerializeField] private KeyCode _keyStopAll = KeyCode.R;     // 전체 즉시 중단
    [SerializeField] private KeyCode _keyStopAllFade = KeyCode.T; // 전체 페이드 중단

    [Header("BGM 키")]
    [SerializeField] private KeyCode _keyBgmPlay = KeyCode.A;     // BGM 재생
    [SerializeField] private KeyCode _keyBgmStop = KeyCode.S;     // BGM 중단

    [Header("Ambience 키")]
    [SerializeField] private KeyCode _keyAmbiencePlay = KeyCode.D; // 환경음 재생
    [SerializeField] private KeyCode _keyAmbienceStop = KeyCode.F; // 환경음 중단

    [Header("기타 키")]
    [SerializeField] private KeyCode _keyUnderwaterOn = KeyCode.Z;  // 수중 ON
    [SerializeField] private KeyCode _keyUnderwaterOff = KeyCode.X; // 수중 OFF
    [SerializeField] private KeyCode _keyRefreshVolume = KeyCode.C; // 볼륨 갱신

    [Header("재생 파라미터")]
    [SerializeField] private Vector3 _sfxPosition = new Vector3(0f, 0f, 5f);
    [SerializeField] private float _fadeOutTime = 1f;
    [SerializeField] private float _underwaterCutoff = 1500f;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Update()
    {
        // SFX
        if (Input.GetKeyDown(_keySfxCamera))
        {
            USound.PlaySfx(Id.SFX_Sonar_Ping);
            UDebug.Print("카메라 SFX 재생");
        }
        if (Input.GetKeyDown(_keySfxPosition))
        {
            USound.PlaySfx(Id.SFX_Swim_Submerged_02, _sfxPosition);
            UDebug.Print($"좌표 SFX 재생 @ {_sfxPosition}");
        }
        if (Input.GetKeyDown(_keySfxFollow))
        {
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
        if (Input.GetKeyDown(_keyStopAll))
        {
            USound.StopAllSfx();
            UDebug.Print("모든 SFX 즉시 중단");
        }
        if (Input.GetKeyDown(_keyStopAllFade))
        {
            USound.StopAllSfx(_fadeOutTime);
            UDebug.Print($"모든 SFX 페이드 중단 ({_fadeOutTime}초)");
        }

        // BGM
        if (Input.GetKeyDown(_keyBgmPlay))
        {
            USound.PlayBgm(Id.BGM_Ocean_2_Loop);
            UDebug.Print("BGM 재생");
        }
        if (Input.GetKeyDown(_keyBgmStop))
        {
            USound.StopBgm();
            UDebug.Print("BGM 중단");
        }

        // Ambience
        if (Input.GetKeyDown(_keyAmbiencePlay))
        {
            USound.PlayAmbience(Id.BGM_Observing_The_Star);
            UDebug.Print("환경음 재생");
        }
        if (Input.GetKeyDown(_keyAmbienceStop))
        {
            USound.StopAmbience();
            UDebug.Print("환경음 중단");
        }

        // 기타
        if (Input.GetKeyDown(_keyUnderwaterOn))
        {
            USound.SetUnderwater(true, _underwaterCutoff);
            UDebug.Print($"수중 효과 ON (cutoff {_underwaterCutoff}Hz)");
        }
        if (Input.GetKeyDown(_keyUnderwaterOff))
        {
            USound.SetUnderwater(false);
            UDebug.Print("수중 효과 OFF");
        }
        if (Input.GetKeyDown(_keyRefreshVolume))
        {
            USound.RefreshVolume();
            UDebug.Print("볼륨 갱신 호출");
        }
    }
    #endregion
}
