using UnityEngine;
using TMPro;

/// <summary>
/// Result_Canvas를 담당합니다. OnItemsSold를 받아 판매 결과 목록을 그리고, 총 판매 금액을 표기합니다.
/// (닫기 버튼은 CUIWindow가 전담하므로 여기서는 다루지 않음)
/// </summary>
[DisallowMultipleComponent]
public sealed class CResultController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private Transform _rowContainer;
    [SerializeField] private CResultRow _rowPrefab;
    [Tooltip("닫기 버튼 위에 표시할 총 판매 금액 텍스트")]
    [SerializeField] private TMP_Text _totalGoldText;

    [Header("표시 형식")]
    [SerializeField] private string _totalGoldFormat = "{0} G";
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnItemsSold>.Subscribe(ItemsSoldHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnItemsSold>.Unsubscribe(ItemsSoldHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void ItemsSoldHandler(OnItemsSold ctx)
    {
        RefreshList(ctx.entries);

        if (_totalGoldText != null)
        {
            _totalGoldText.text = string.Format(_totalGoldFormat, ctx.totalGold);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void RefreshList(System.Collections.Generic.IReadOnlyList<SoldItemEntry> entries)
    {
        if (_rowContainer == null || _rowPrefab == null)
        {
            UDebug.Print("CResultController: 로우 컨테이너/프리팹이 비어있습니다.", LogType.Error, gameObject);
            return;
        }

        UObject.DestroyChildren(_rowContainer); // Result는 자주 열리는 창이 아니라 매번 새로 그려도 무방

        int count = entries.Count;
        for (int i = 0; i < count; ++i)
        {
            CResultRow row = Instantiate(_rowPrefab, _rowContainer);
            row.Setup(entries[i]);
        }
    }
    #endregion
}
