using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CAimShow : AFrameable, IUpdateFrameable
{
    [SerializeField] private Image _aimImg;
    [SerializeField] private Transform _cam;
    [SerializeField] private Transform _armTransform;
    [SerializeField] private LayerMask _collectibleLayout;
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

    // 잡을 수 있는 상태면 초록불, 아니면 빨간불, 물건을 조준하고 있지 않은 상태면 파란불
    public void AimColorChange(float maxDistance,float distance)
    {
    }

    //카메라의 일정범위에 있는 물건 에임에 오면 아웃라인표시
    public void ShowOutLineInDistance()
    {
        RaycastHit hit;
        if(Physics.Raycast(_cam.transform.position,_cam.forward,out hit, 8, _collectibleLayout))
        {
            CCollectible temp=hit.collider.gameObject.GetComponent<CCollectible>();
            if (temp != _currentAimObject)
            {
                _currentAimObject=hit.collider.GetComponent<CCollectible>();
                _currentAimObject.ShowOutline();
                print("outlineshow");
            }
        }
        else
        {
            if (_currentAimObject != null)
            {
                _currentAimObject.HideOutline();
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

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    
    #endregion
}
