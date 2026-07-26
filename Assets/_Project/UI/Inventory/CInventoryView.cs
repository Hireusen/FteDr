using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가로 7 * 세로 3 = 21칸의 고정 슬롯을 미리 생성해두고, 가방 소지품 변경에 맞춰
/// 각 슬롯에 데이터를 채우거나 비우는 인벤토리 뷰입니다.
/// (Grid Layout Group 하위 슬롯은 항상 21개 존재. 아이템 수만큼만 앞에서부터 채워짐)
/// </summary>
[DisallowMultipleComponent]
public sealed class CInventoryView : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("인벤토리 설정")]
    [Tooltip("Grid Layout Group이 붙어있는 슬롯들의 부모")]
    [SerializeField] private Transform _slotContainer;
    [Tooltip("슬롯 하나를 표현하는 프리팹")]
    [SerializeField] private CInventorySlot _slotPrefab;
    [Tooltip("항상 존재하는 슬롯 칸 수 (7 x 3 = 21)")]
    [SerializeField] private int _slotCount = 21;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly List<CInventorySlot> _slots = new();
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        BuildSlots();
    }

    private void OnEnable()
    {
        CEventBus<OnPlayerBagChanged>.Subscribe(BagChangedHandler);
        CEventBus<OnGearUpgraded>.Subscribe(GearUpgradedHandler);
        RefreshSlots(); // 창을 여는 시점에 현재 소지품 상태를 즉시 반영
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerBagChanged>.Unsubscribe(BagChangedHandler);
        CEventBus<OnGearUpgraded>.Unsubscribe(GearUpgradedHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void BagChangedHandler(OnPlayerBagChanged data)
    {
        RefreshSlots();
    }

    // 가방(EDataType.Bag)이 업그레이드되면 용량이 늘어나므로 잠금 상태를 다시 계산한다.
    private void GearUpgradedHandler(OnGearUpgraded ctx)
    {
        if (ctx.gearType != EDataType.Bag) return;
        RefreshSlots();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 21개의 슬롯을 최초 1회만 생성합니다. (이후로는 재사용, Instantiate/Destroy 반복 없음)
    private void BuildSlots()
    {
        if (_slotContainer == null || _slotPrefab == null)
        {
            UDebug.Print("CInventoryView: 슬롯 컨테이너/프리팹이 비어있습니다.", LogType.Error, gameObject);
            return;
        }

        if (_slots.Count > 0) return; // 이미 생성됨

        for (int i = 0; i < _slotCount; ++i)
        {
            CInventorySlot slot = Instantiate(_slotPrefab, _slotContainer);
            slot.Clear();
            _slots.Add(slot);
        }
    }

    // 현재 UPlayer.BagItems 기준으로 앞에서부터 슬롯을 채우고, 나머지는 비웁니다.
    // 실제 가방 용량(UPlayer.BagCapacity)을 넘는 칸은 잠가서 아직 해금되지 않은 칸임을 표시합니다.
    private void RefreshSlots()
    {
        if (_slots.Count == 0) return;

        var bagItems = UPlayer.BagItems;
        int itemCount = bagItems.Count;
        int slotCount = _slots.Count;
        int capacity = UPlayer.BagCapacity;

        for (int i = 0; i < slotCount; ++i)
        {
            bool isLocked = i >= capacity;
            _slots[i].SetLocked(isLocked);

            if (!isLocked)
            {
                _slots[i].Setup(i < itemCount ? bagItems[i] : null);
            }
        }

        if (itemCount > slotCount)
        {
            UDebug.Print($"CInventoryView: 소지품({itemCount})이 슬롯 수({slotCount})를 초과했습니다.", LogType.Warning, gameObject);
        }
    }
    #endregion
}
