using UnityEngine;
using Cinemachine;

/// <summary>
/// 플레이어 상태(수영 / 지상)에 따라 CinemachineFreeLook 의 리그(Top/Middle/Bottom)
/// 높이·반경을 부드럽게 보간하여 카메라 상하 회전 범위를 상태별로 다르게 만드는 스크립트입니다.
/// </summary>
public class CFreeLookStateRig : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private CinemachineFreeLook _freeLook;
    [SerializeField] private CPlayerController _player;

    [Header("지상 리그 (0:Top, 1:Middle, 2:Bottom)")]
    [SerializeField]
    private CinemachineFreeLook.Orbit[] _groundOrbits = new CinemachineFreeLook.Orbit[3]
    {
        new CinemachineFreeLook.Orbit(3f, 4f),
        new CinemachineFreeLook.Orbit(1.5f, 5f),
        new CinemachineFreeLook.Orbit(0.5f, 4f),
    };

    [Header("수영 리그 (0:Top, 1:Middle, 2:Bottom)")]
    [SerializeField]
    private CinemachineFreeLook.Orbit[] _swimOrbits = new CinemachineFreeLook.Orbit[3]
    {
        new CinemachineFreeLook.Orbit(6f, 3f),
        new CinemachineFreeLook.Orbit(1.5f, 5f),
        new CinemachineFreeLook.Orbit(-4f, 3f),
    };

    [Header("전환 부드러움")]
    [SerializeField] private float _blendSharpness = 6f;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Update()
    {
        if (_freeLook == null || _player == null) return;

        CinemachineFreeLook.Orbit[] target =
            _player.CurrentState == EPlayerState.Swimming ? _swimOrbits : _groundOrbits;

        float t = 1f - Mathf.Exp(-_blendSharpness * Time.deltaTime);

        for (int i = 0; i < 3; i++)
        {
            _freeLook.m_Orbits[i].m_Height = Mathf.Lerp(_freeLook.m_Orbits[i].m_Height, target[i].m_Height, t);
            _freeLook.m_Orbits[i].m_Radius = Mathf.Lerp(_freeLook.m_Orbits[i].m_Radius, target[i].m_Radius, t);
        }
    }
    #endregion
}
