using UnityEngine;

/// <summary>
/// 가방 소지품 변경에 맞춰 슬롯 목록 전체를 다시 그리는 인벤토리 뷰입니다.
/// (Grid Layout Group 하위에 Slot_Image 프리팹을 붙였다 뗐다 관리)
/// </summary>
[DisallowMultipleComponent]
public sealed class CInventoryView : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("인벤토리 설정")]
    [Tooltip("Grid Layout Group이 붙어있는 슬롯들의 부모")]
    [SerializeField] private Transform _slotContainer;
    [Tooltip("슬롯 하나를 표현하는 프리펩")]
    [SerializeField] private CInventorySlot _slotPrefab;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnPlayerBagChanged>.Subscribe(BagChangedHandler);
        RefreshSlots(); // 창을 여는 시점에 현재 소지품 상태를 즉시 반영
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerBagChanged>.Unsubscribe(BagChangedHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void BagChangedHandler(OnPlayerBagChanged data)
    {
        RefreshSlots();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 현재 UPlayer.BagItems 기준으로 슬롯을 전부 새로 그립니다.
    private void RefreshSlots()
    {
        if (_slotContainer == null || _slotPrefab == null)
        {
            UDebug.Print("CInventoryView: 슬롯 컨테이너/프리팹이 비어있습니다.", LogType.Error, gameObject);
            return;
        }

        UObject.DestroyChildren(_slotContainer);

        var bagItems = UPlayer.BagItems;
        int count = bagItems.Count;
        for (int i = 0; i < count; ++i)
        {
            CInventorySlot slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Setup(bagItems[i]);
        }
    }
    #endregion
}
