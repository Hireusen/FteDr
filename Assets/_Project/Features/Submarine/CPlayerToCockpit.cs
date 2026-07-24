using Cinemachine;
using System.Collections;
using UnityEngine;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CPlayerToCockpit : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private CSubMarineUpDown _cSubMarineUpDown;
    [SerializeField] private CinemachineVirtualCamera _cockpitCam;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private int _cockpitoriginPriority;
    private Coroutine _camToCutCoroutine;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public int ToCockpitPriority { get;private set; } = 600;
    public bool SitCockpit { get; private set; } = false;
    public CinemachineBrain CineBrain { get; private set; } 
    public void MoveToCockpit()
    {
        SitCockpit = true;
        //여기서 조작 불가능하게 만들어야 함
        OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, true);
        CineBrain =Camera.main.GetComponent<CinemachineBrain>();
        CineBrain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 1f);
        _cockpitoriginPriority = _cockpitCam.Priority;
        _cockpitCam.Priority = ToCockpitPriority;

        //_lastSitTime = Time.time;
        print("movetocockpit");
    }
    public void CockpitToPlayer()
    {
        if (_camToCutCoroutine != null) return;

        _cockpitCam.Priority = _cockpitoriginPriority;
        _camToCutCoroutine = StartCoroutine(CamToCut());
    }

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv3;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (SitCockpit==false) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SitCockpit = false;
            _cSubMarineUpDown.StartCutScene(false);
            OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, false);
            UDebug.Print("벗어남 사유 : Q 입력");
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SitCockpit = false;
            OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, false);
            _cSubMarineUpDown.StartCutScene(true);
            UDebug.Print("벗어남 사유 : E 입력");
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private IEnumerator CamToCut()
    {
        yield return UCoroutine.GetWait(1f);
        CineBrain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut,0f);
        SitCockpit = false;
        // 여기서 조작 가능하게 만들어야 함.
        OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, false);
        UDebug.Print("벗어남 사유 : 캠투컷");
        _camToCutCoroutine = null;
    }
    #endregion
}
