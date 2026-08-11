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

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public static string PendingSellNotice = null;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_btnSellAll != null)
        {
            _btnSellAll.onClick.AddListener(() => ExecuteSellAll(true));
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    public static void ExecuteSellAll(bool showResultUI)
    {
        var bagItems = UPlayer.BagItems;
        if (bagItems.Count == 0)
        {
            if (showResultUI) OnRequestNotice.Publish("판매할 아이템이 없습니다.");
            return;
        }

        int totalItemCount = bagItems.Count;

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

        // 가방 비우기 
        CPlayerManager manager = CPlayerManager.Ins;
        if (manager != null)
        {
            manager.Runtime.bagItems.Clear();
            OnPlayerBagChanged.Publish(0, UPlayer.BagCapacity);
            OnPlayerWeightChanged.Publish(0f, UPlayer.MaxWeight);
        }

        // 골드 지급 
        UPlayer.AddMoney(totalGold);

        // showResultUI 플래그에 따라 결산 창 호출 여부 결정
        if (showResultUI)
        {
            OnRequestOpenUI.Publish(EUI.ResultWindow);
        }
        else
        {
            PendingSellNotice = $"총 {totalItemCount}개의 아이템을 판매하여 {totalGold}골드를 얻었습니다.";
        }

            // 데이터 전달
            OnItemsSold.Publish(entries, totalGold);
    }
    #endregion
}
