using UnityEngine;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class AimInfo : AFrameable, IUpdateFrameable
{
    [SerializeField] private GameObject _tooltip;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Vector3 _pos;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public void ShowTooltip(CCollectible currentCollectible)
    {
        OnRequestShowTooltip.Publish(currentCollectible.Data, _pos);
    }
    public void HideTooltip()
    {
        OnRequestHideTooltip.Publish();
    }

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _pos= _tooltip.transform.position;
    }
    #endregion
}
