using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯 하나를 표현하는 컴포넌트입니다. 아이콘 표시 + 마우스 호버 시 툴팁 요청을 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CInventorySlot : AMono, IPointerEnterHandler, IPointerExitHandler
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("수집품 아이콘이 표시될 이미지")]
    [SerializeField] private Image _iconImage;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CCollectibleSO _data; // 이 슬롯이 표시하고 있는 수집품 데이터
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 슬롯에 표시할 데이터를 바인딩합니다. (UI_InventoryView가 생성 직후 호출)
    /// </summary>
    /// <param name="collectibleId">수집품 ID</param>
    public void Setup(string collectibleId)
    {
        _data = UData.Collectible(collectibleId);

        if (_data == null)
        {
            UDebug.Print($"UI_InventorySlot: 수집품 ID({collectibleId})에 해당하는 SO를 찾을 수 없습니다.", LogType.Error, gameObject);
            return;
        }

        if (_iconImage != null)
        {
            _iconImage.sprite = _data.Icon;
            _iconImage.enabled = _data.Icon != null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data == null) return;

        OnRequestShowTooltip.Publish(_data, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnRequestHideTooltip.Publish();
    }
    #endregion
}
