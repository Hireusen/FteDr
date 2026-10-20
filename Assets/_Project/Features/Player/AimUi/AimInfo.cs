using UnityEngine;

/// <summary>
/// 조준 모드에서 아이템 툴팁을 띄우기 위함
/// </summary>
public class AimInfo : AFrameable
{
    [SerializeField] private GameObject _tooltip;
    private Vector3 _pos;

    public void ActiveTooltip()
    {
        _tooltip.SetActive(true);
    }
    public void InActiveTooltip()
    {
        _tooltip.SetActive(false);
    }

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public void ShowTooltip(CCollectible currentCollectible)
    {
        OnRequestShowTooltip.Publish(currentCollectible.Data, _pos);
    }
    public void HideTooltip()
    {
        OnRequestHideTooltip.Publish();
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _pos = _tooltip.transform.position;
    }
    #endregion
}
