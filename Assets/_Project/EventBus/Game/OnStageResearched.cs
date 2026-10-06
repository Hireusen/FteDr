/// <summary>
/// 특수 수집품으로 연구가 완료되어 다음 스테이지가 해금될 때 발행합니다.
/// 해금은 발행 시점에 이미 적용되어 있으므로, 컷신 등 연출은 이 이벤트를 구독해서 재생하면 됩니다.
/// </summary>
public readonly struct OnStageResearched
{
    public readonly int unlockedStage;
    public readonly string specialId;

    public OnStageResearched(int unlockedStage, string specialId)
    {
        this.unlockedStage = unlockedStage;
        this.specialId = specialId;
    }

    /// <param name="unlockedStage">연구 후 해금된 최대 스테이지</param>
    /// <param name="specialId">연구에 소모된 특수 수집품 ID</param>
    public static void Publish(int unlockedStage, string specialId)
    {
        CEventBus<OnStageResearched>.Publish(new OnStageResearched(unlockedStage, specialId));
    }
}
