/// <summary>
/// 상점 목록의 장비 로우에 마우스를 올렸을 때, 오른쪽 상세 패널에 표시할 정보를 요청하는 이벤트입니다.
/// </summary>
public readonly struct OnRequestShowGearDetail
{
    public readonly string displayName;
    public readonly AGearSO gear;
    public readonly int currentLevel;

    public OnRequestShowGearDetail(string displayName, AGearSO gear, int currentLevel)
    {
        this.displayName = displayName;
        this.gear = gear;
        this.currentLevel = currentLevel;
    }

    /// <param name="displayName">화면에 표시할 장비 이름</param>
    /// <param name="gear">대표 스탯 요약(GetStatSummary)을 뽑아낼 장비 SO</param>
    /// <param name="currentLevel">현재 레벨</param>
    public static void Publish(string displayName, AGearSO gear, int currentLevel)
    {
        CEventBus<OnRequestShowGearDetail>.Publish(new OnRequestShowGearDetail(displayName, gear, currentLevel));
    }
}
