using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점의 "일괄 판매" 버튼을 담당합니다. 가방의 아이템을 전부 판매하고 비운 뒤, Result_Canvas에 결과를 전달합니다.
/// CPlayerManager/UPlayer는 건드리지 않고, 이미 공개되어 있는 Runtime.bagItems를 직접 비우는 방식을 씁니다
/// (CInventoryTester의 가방 비우기와 동일한 방식).
/// </summary>
[DisallowMultipleComponent]
public sealed class CShopSellController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private Button _btnSellAll;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_btnSellAll != null)
        {
            _btnSellAll.onClick.AddListener(OnClickSellAll);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void OnClickSellAll()
    {
        var bagItems = UPlayer.BagItems;
        if (bagItems.Count == 0)
        {
            OnRequestNotice.Publish("판매할 아이템이 없습니다.");
            return;
        }

        // id별 개수 집계
        Dictionary<string, int> counts = new();
        int itemCount = bagItems.Count;
        for (int i = 0; i < itemCount; ++i)
        {
            string id = bagItems[i];
            counts.TryGetValue(id, out int current);
            counts[id] = current + 1;
        }

        // 판매 결과 목록 + 총액 계산
        List<SoldItemEntry> entries = new(counts.Count);
        int totalGold = 0;
        foreach (var pair in counts)
        {
            CCollectibleSO so = UData.Collectible(pair.Key);
            int sellPrice = so != null ? (int)so.SellPrice : 0;
            int subtotal = sellPrice * pair.Value;

            entries.Add(new SoldItemEntry(pair.Key, pair.Value, subtotal));
            totalGold += subtotal;
        }

        // 가방 비우기 (CPlayerManager를 건드리지 않고 공개된 Runtime 직접 조작 + 이벤트 수동 발행)
        CPlayerManager manager = CPlayerManager.Ins;
        if (manager != null)
        {
            manager.Runtime.bagItems.Clear();
            OnPlayerBagChanged.Publish(0, UPlayer.BagCapacity);
            OnPlayerWeightChanged.Publish(0f, UPlayer.MaxWeight);
        }

        // 골드 지급 (이미 있는 메서드 재사용, OnMoneyChanged는 내부에서 자동 발행됨)
        UPlayer.AddMoney(totalGold);

        // 먼저 Result 창을 열어 CResultController를 활성화(구독 시작)시킨 뒤
        OnRequestOpenUI.Publish(EUI.ResultWindow);

        // 그 다음에 데이터를 전달한다
        OnItemsSold.Publish(entries, totalGold);
    }
    #endregion
}
