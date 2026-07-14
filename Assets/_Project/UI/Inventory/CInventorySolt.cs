using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯 하나를 표현하는 컴포넌트입니다. 아이콘 표시 + 마우스 호버 시 툴팁/선택 강조를 담당합니다.
/// Grid Layout Group 안에 고정 개수로 미리 배치되며, 데이터가 있을 때만 아이콘/호버가 동작합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CInventorySlot : AMono, IPointerEnterHandler, IPointerExitHandler
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("아이콘")]
    [Tooltip("수집품 아이콘이 표시될 이미지")]
    [SerializeField] private Image _iconImage;
    [Tooltip("아이콘 원본 비율을 유지할지 여부 (스프라이트마다 크기/비율이 달라도 셀 안에 맞춰짐)")]
    [SerializeField] private bool _preserveIconAspect = true;

    [Header("호버 강조 표시")]
    [Tooltip("아이템이 있는 슬롯에 마우스를 올렸을 때 켜질 강조 오브젝트 (테두리/하이라이트 이미지 등). 평소엔 비활성 상태로 둡니다.")]
    [SerializeField] private GameObject _hoverHighlight;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CCollectibleSO _data; // 이 슬롯이 표시하고 있는 수집품 데이터. null이면 빈 칸.
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이 슬롯에 아이템이 채워져 있는지 여부입니다.</summary>
    public bool HasItem => _data != null;

    /// <summary>
    /// 슬롯에 표시할 데이터를 바인딩합니다. collectibleId가 비어있으면 빈 칸으로 비웁니다.
    /// </summary>
    /// <param name="collectibleId">수집품 ID (null/빈 문자열이면 빈 칸)</param>
    public void Setup(string collectibleId)
    {
        if (collectibleId.IsBlank())
        {
            Clear();
            return;
        }

        _data = UData.Collectible(collectibleId);

        if (_data == null)
        {
            UDebug.Print($"CInventorySlot: 수집품 ID({collectibleId})에 해당하는 SO를 찾을 수 없습니다.", LogType.Error, gameObject);
            Clear();
            return;
        }

        if (_iconImage != null)
        {
            _iconImage.sprite = _data.Icon;
            _iconImage.enabled = _data.Icon != null;
            _iconImage.preserveAspect = _preserveIconAspect; // 스프라이트 원본 비율 유지
        }
    }

    /// <summary>슬롯을 빈 칸 상태로 비웁니다.</summary>
    public void Clear()
    {
        _data = null;

        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }

        SetHighlight(false); // 아이템이 사라지면 강조도 함께 끈다 (호버 중 제거되는 예외 상황 방어)
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!HasItem) return;

        SetHighlight(true);
        OnRequestShowTooltip.Publish(_data, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);

        if (!HasItem) return;

        OnRequestHideTooltip.Publish();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void SetHighlight(bool active)
    {
        if (_hoverHighlight != null)
        {
            _hoverHighlight.SetActive(active);
        }
    }
    #endregion
}
