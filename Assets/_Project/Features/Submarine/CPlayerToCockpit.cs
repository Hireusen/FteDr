using Cinemachine;
using System.Collections;
using UnityEngine;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CPlayerToCockpit : AFrameable, IUpdateFrameable
{
    [SerializeField] private CSubMarineUpDown _cSubMarineUpDown;
    [SerializeField] private CinemachineVirtualCamera _cockpitCam;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private int _cockpitoriginPriority;
    
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
        print("movetocockpit");
    }
    public void CockpitToPlayer()
    {
        _cockpitCam.Priority = _cockpitoriginPriority;
        StartCoroutine(CamToCut());
        
    }

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if(SitCockpit==false) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            _cSubMarineUpDown.StartCutScene(false);
            OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, false);
            SitCockpit = false;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SitCockpit = false;
            OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, false);
            _cSubMarineUpDown.StartCutScene(true);
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

    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    
    #endregion
}
