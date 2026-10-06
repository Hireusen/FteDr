/// <summary>
/// 스테이지 연구(해금) 규칙을 한곳에 모아둔 유틸리티입니다.
/// 기획: 특수 수집품을 가방에 담아 돌아와서 상점과 상호작용하면 상점창 대신 연구창이 열리고,
/// 분석 연출(프로그레스 바)이 끝나는 시점에 다음 스테이지가 해금됩니다.
/// 골드는 쓰지 않고 특수 수집품 1개만 소모합니다.
/// 연출 타이밍은 연구창(CResearchController)이 쥐고 있고, 이 유틸은 '지금 연구가 되는가 / 적용'만 담당합니다.
/// (추가 연출은 OnStageResearched를 구독해서 재생하면 됩니다. 해금은 발행 시점에 이미 적용된 상태입니다)
/// 해금 가능 횟수의 상한은 CSubmarineSO의 MaxLevel(= _upgradeCosts 길이 + 1)을 그대로 씁니다.
/// </summary>
public static class UStageResearch
{
    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 현재 연구 단계입니다. (1부터 시작, "스테이지 N까지 이동 가능"의 N과 같습니다)
    /// UPlayer.UnlockedStage는 "기본 스테이지 외에 추가로 해금한 수"라서 +1을 더해 1-based로 맞춥니다.
    /// </summary>
    public static int CurrentLevel => UPlayer.UnlockedStage + 1;

    /// <summary>더 이상 연구할 수 없는 상한 단계입니다. (CSubmarineSO 기준)</summary>
    public static int MaxLevel
    {
        get
        {
            CSubmarineSO submarine = UData.Submarine();
            return submarine != null ? submarine.MaxLevel : 1;
        }
    }

    /// <summary>모든 스테이지를 이미 연구했는지 여부입니다.</summary>
    public static bool IsAllResearched => CurrentLevel >= MaxLevel;

    /// <summary>해당 단계의 목적지 이름입니다. (CSubmarineSO에 입력된 값)</summary>
    /// <param name="level">스테이지 단계 (1부터 시작)</param>
    public static string DestinationName(int level)
    {
        CSubmarineSO submarine = UData.Submarine();
        return submarine != null ? submarine.DestinationName(level) : string.Empty;
    }

    /// <summary>연구로 해금될 다음 목적지의 이름입니다. (더 연구할 게 없으면 빈 문자열)</summary>
    public static string NextDestinationName => IsAllResearched ? string.Empty : DestinationName(CurrentLevel + 1);

    /// <summary>연구 재료인 특수 수집품을 가방에 담아왔는지 여부입니다.</summary>
    public static bool HasSpecialInBag => !UPlayer.SpecialInBag.IsBlank();

    /// <summary>지금 연구를 시작할 수 있는지를 나타내는 상태입니다. (UI 표시 및 클릭 판정 공용)</summary>
    public static EStageResearchState State
    {
        get
        {
            if (IsAllResearched) return EStageResearchState.AllResearched;
            return HasSpecialInBag ? EStageResearchState.Ready : EStageResearchState.NeedSpecial;
        }
    }

    /// <summary>
    /// 가방의 특수 수집품을 소모해 연구를 진행하고 다음 스테이지를 해금합니다.
    /// 성공 시 안내 토스트까지 이 안에서 발행하므로, 호출부는 실패 메시지만 처리하면 됩니다.
    /// </summary>
    /// <param name="failMessage">실패 사유 (성공 시 null)</param>
    /// <returns>연구 성공 여부</returns>
    public static bool TryResearch(out string failMessage)
    {
        failMessage = null;

        if (IsAllResearched)
        {
            failMessage = "더 이상 연구할 스테이지가 없습니다.";
            return false;
        }

        if (!UPlayer.TryConsumeSpecialInBag(out string specialId))
        {
            failMessage = "연구에 필요한 특수 수집품이 가방에 없습니다.";
            return false;
        }

        UPlayer.UnlockNextStage();

        // 결과 표시는 연구창이 담당하므로 여기서는 데이터 이벤트만 알린다. (연출도 이 이벤트를 구독)
        OnStageResearched.Publish(UPlayer.UnlockedStage, specialId);
        return true;
    }
    #endregion
}
