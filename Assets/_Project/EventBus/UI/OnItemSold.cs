using System.Collections.Generic;

/// <summary>
/// 판매된 아이템 한 종류(같은 id끼리 묶임)에 대한 결과 데이터입니다.
/// </summary>
public readonly struct SoldItemEntry
{
    public readonly string collectibleId;
    public readonly int count;
    public readonly int subtotal;

    public SoldItemEntry(string collectibleId, int count, int subtotal)
    {
        this.collectibleId = collectibleId;
        this.count = count;
        this.subtotal = subtotal;
    }
}

/// <summary>
/// 일괄 판매가 완료됐을 때 발행합니다. Result_Canvas가 이 데이터로 판매 결과 목록을 그립니다.
/// </summary>
public readonly struct OnItemsSold
{
    public readonly IReadOnlyList<SoldItemEntry> entries;
    public readonly int totalGold;

    public OnItemsSold(IReadOnlyList<SoldItemEntry> entries, int totalGold)
    {
        this.entries = entries;
        this.totalGold = totalGold;
    }

    /// <param name="entries">아이템 종류별(id 기준 중첩) 판매 결과 목록</param>
    /// <param name="totalGold">전체 판매 합산 금액</param>
    public static void Publish(IReadOnlyList<SoldItemEntry> entries, int totalGold)
    {
        CEventBus<OnItemsSold>.Publish(new OnItemsSold(entries, totalGold));
    }
}
