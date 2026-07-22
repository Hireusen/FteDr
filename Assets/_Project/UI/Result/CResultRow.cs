using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 판매 결과 목록의 로우 하나입니다. (아이템 아이콘, 이름, 개수, 판매 합산 금액)
/// </summary>
[DisallowMultipleComponent]
public sealed class CResultRow : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private TMP_Text _priceText;

    [Header("표시 형식")]
    [SerializeField] private string _countFormat = "X {0}";
    [SerializeField] private string _priceFormat = "{0} G";
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이 로우에 판매 결과 한 건을 바인딩합니다.</summary>
    /// <param name="entry">판매 결과 데이터</param>
    public void Setup(SoldItemEntry entry)
    {
        // 프리팹 저장 시점에 컴포넌트의 enabled 체크박스가 꺼진 채로 남아있는 경우를 방어한다.
        // (텍스트/배경은 항상 보여야 하므로 Setup이 호출되면 무조건 켠다)
        if (_backgroundImage != null) _backgroundImage.enabled = true;
        if (_nameText != null) _nameText.enabled = true;
        if (_countText != null) _countText.enabled = true;
        if (_priceText != null) _priceText.enabled = true;

        CCollectibleSO data = UData.Collectible(entry.collectibleId);

        if (_iconImage != null)
        {
            _iconImage.sprite = data != null ? data.Icon : null;
            _iconImage.enabled = data != null && data.Icon != null;
        }

        if (_nameText != null)
        {
            _nameText.text = data != null ? data.Name : entry.collectibleId;
        }

        if (_countText != null)
        {
            _countText.text = string.Format(_countFormat, entry.count);
        }

        if (_priceText != null)
        {
            _priceText.text = string.Format(_priceFormat, entry.subtotal);
        }
    }
    #endregion
}
