using Codice.Client.BaseCommands;
using TMPro;
using Unity.VisualScripting.YamlDotNet.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CAimShow : AFrameable, IUpdateFrameable
{
    
    [Header("플레이어")]
    [SerializeField] private CNewGrab _grabScript;
    [SerializeField] private Transform _cam;
    [SerializeField] private Transform _armTransform;
    [Header("수집품 정보")]
    [SerializeField] private LayerMask _collectibleLayout;
    [SerializeField] private AimInfo _aimInfo;
    [Header("기본에임 이미지")]
    [SerializeField] private Image _aimImg;
    [Header("조준모드 이미지")]
    [SerializeField] private Image _bigReachedAimImg;
    [SerializeField] private Image _bigNotReachedAimImg;
    [SerializeField] private Image _bigNormalAimImg;
    [SerializeField] private Image _background;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CCollectible _currentAimObject;
    private struct AimModeImgset
    {
        public Image reached;
        public Image notReached;
        public Image normal;
    }
    private AimModeImgset _aimModeImgs;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────

    public enum EAimStatus
    {
        Normal,
        UnReached,
        Reached,
        Transition
    }
    public struct TransitionInfo
    {
        public EAimStatus current;
        public EAimStatus next;
    }
    public TransitionInfo transitionInfo;
    public EAimStatus currentStatus;
    public bool IsAimMode { get; private set; } = false;
    public void AimModeOn()
    {
        _background.gameObject.SetActive(true);
        _aimImg.gameObject.SetActive(false);
        IsAimMode = true;
    }
    public void WaitModeOn()
    {
        _background.gameObject.SetActive(false);
        _aimImg.gameObject.SetActive(true);
        IsAimMode = false;
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
        switch (currentStatus)
        {
            case EAimStatus.Transition:
                ChangeState(transitionInfo.next);
                break;
        }
        
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    
    private void Transition(EAimStatus status)
    {
        transitionInfo.current = status;
        transitionInfo.next = status;
        currentStatus = EAimStatus.Transition;

    }
    private void ChangeState(EAimStatus status)
    {
        currentStatus = status;
        switch (currentStatus)
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
        (_grabScript, _armTransform, _cam) = comp.GetReference(this);
        WaitModeOn();

    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        SetReference();
        _aimModeImgs.reached = _bigReachedAimImg;
        _aimModeImgs.notReached = _bigNotReachedAimImg;
        _aimModeImgs.normal = _bigNormalAimImg;
    }
    #endregion
}
