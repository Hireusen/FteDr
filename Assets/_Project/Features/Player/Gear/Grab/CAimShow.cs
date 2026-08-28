using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CAimShow : AFrameable, IUpdateFrameable
{
    [SerializeField] private CNewGrab _grabScript;
    [SerializeField] private Image _aimImg;
    [SerializeField] private Transform _cam;
    [SerializeField] private Transform _armTransform;
    [SerializeField] private LayerMask _collectibleLayout;
    [SerializeField] private AimInfo _aimInfo;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CCollectible _currentAimObject;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public enum EAimStatus
    {
        Normal,
        UnReached,
        Reached
    }

    //카메라의 일정범위에 있는 물건 에임에 오면 아웃라인표시
    public void ShowOutLineInDistance()
    {
        RaycastHit hit;
        if(Physics.Raycast(_cam.transform.position,_cam.forward,out hit, 4, _collectibleLayout))
        {
            //아웃라인용+ 조준모드 툴팁표시
            CCollectible temp=hit.transform.root.gameObject.GetComponent<CCollectible>();
            if (temp != _currentAimObject)
            {
                if (_currentAimObject != null)
                {
                    _currentAimObject.HideOutline();
                    _aimInfo.HideTooltip();
                }
                _currentAimObject=temp;

                _aimInfo.ShowTooltip(_currentAimObject);
                _currentAimObject.ShowOutline();
                print("outlineshow");
                
            }

            //에임용
            if (_grabScript.grabStatus != CNewGrab.EGrabStatus.ReadyShoot) return;
            if ((_armTransform.position - hit.point).magnitude < _grabScript.GetMaxDistance())
            {
                //이거 원래 그랩쪽에서 담당했어야할거 같은데, 기능변경전에는 이게 맞음..
                _grabScript.ReachGrab = (_armTransform.position - hit.point).magnitude;
                ChangeState(EAimStatus.Reached);
            }
            else
            {
                _grabScript.ReachGrab = _grabScript.GetMaxDistance();
                ChangeState(EAimStatus.UnReached);
            }
        }
        else
        {
            if (_grabScript.grabStatus != CNewGrab.EGrabStatus.ReadyShoot) return;
            _grabScript.ReachGrab = _grabScript.GetMaxDistance();
            ChangeState(EAimStatus.Normal);
            if (_currentAimObject != null)
            {
                _currentAimObject.HideOutline();
                _aimInfo.HideTooltip();
                _currentAimObject = null;
            }
        }
    }
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        
        ShowOutLineInDistance();
        
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void ChangeState(EAimStatus status)
    {
        EAimStatus nextStatus = status;
        switch (nextStatus)
        {
            case EAimStatus.Normal:
                _aimImg.color = Color.white;
                break;
            case EAimStatus.UnReached:
                _aimImg.color = Color.red;
                break;
            case EAimStatus.Reached:
                _aimImg.color = Color.green;
                break;
        }
    }
    private void SetReference()
    {
        var player = CGameManager.Player;
        var comp=player.GetComponent<CDiverToAim>();
        (_grabScript, _armTransform, _cam) = comp.GetReference(this.gameObject);
        this.gameObject.SetActive(false);

    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        SetReference();
    }
    #endregion
}
