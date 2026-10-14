/// <summary>
/// 스팀 도전과제·통계의 API Name과 달성 기준값을 정의하는 매크로 클래스입니다.
/// </summary>
public static partial class K
{
    #region ─────────────────────────▶ 도전과제 API Name ◀─────────────────────────
    // TODO: 아래 값은 모두 임시입니다. 스팀웍스 파트너 사이트에 과제를 등록하면서
    //       받은 API Name과 정확히 일치시켜야 합니다. 표시 이름(Display Name)이 아닙니다.
    //       틀리면 해금이 조용히 실패하고 콘솔에 에러만 남습니다.

    /// <summary>첫 잠수 — 잠수함을 처음 벗어났을 때</summary>
    public const string ACH_FIRST_DIVE = "ACH_FIRST_DIVE";

    /// <summary>첫 연구 — 처음으로 연구를 진행했을 때</summary>
    public const string ACH_FIRST_UNLOCKED = "ACH_FIRST_UNLOCKED";

    /// <summary>스테이지 해금 — 연구로 다음 스테이지2를 처음 해금했을 때</summary>
    public const string ACH_STAGE2_UNLOCKED = "ACH_STAGE2_UNLOCKED";

    /// <summary>스테이지 해금 — 연구로 다음 스테이지3를 처음 해금했을 때</summary>
    public const string ACH_STAGE3_UNLOCKED = "ACH_STAGE3_UNLOCKED";

    /// <summary>스테이지 해금 — 연구로 다음 스테이지4를 처음 해금했을 때</summary>
    public const string ACH_STAGE4_UNLOCKED = "ACH_STAGE4_UNLOCKED";

    /// <summary>스테이지 해금 — 연구로 다음 스테이지5를 처음 해금했을 때</summary>
    public const string ACH_STAGE5_UNLOCKED = "ACH_STAGE5_UNLOCKED";

    /// <summary>스테이지 해금 — 연구로 다음 스테이지6를 처음 해금했을 때</summary>
    public const string ACH_STAGE6_UNLOCKED = "ACH_STAGE6_UNLOCKED";

    /// <summary>스테이지 해금 — 연구로 다음 스테이지7를 처음 해금했을 때</summary>
    public const string ACH_STAGE7_UNLOCKED = "ACH_STAGE7_UNLOCKED";

    /// <summary>엔딩 — 바다 협곡으로 이동을 눌러 엔딩 연출이 시작됐을 때</summary>
    public const string ACH_ENDING = "ACH_ENDING";

    /// <summary>누적 획득 골드 — 동</summary>
    public const string ACH_TOTAL_MONEY_BRONZE = "ACH_TOTAL_MONEY_BRONZE";

    /// <summary>누적 획득 골드 — 은</summary>
    public const string ACH_TOTAL_MONEY_SILVER = "ACH_TOTAL_MONEY_SILVER";

    /// <summary>누적 획득 골드 — 금</summary>
    public const string ACH_TOTAL_MONEY_GOLD = "ACH_TOTAL_MONEY_GOLD";

    /// <summary>누적 수영 시간 — 동</summary>
    public const string ACH_DIVE_TIME_BRONZE = "ACH_DIVE_TIME_BRONZE";

    /// <summary>누적 수영 시간 — 은</summary>
    public const string ACH_DIVE_TIME_SILVER = "ACH_DIVE_TIME_SILVER";

    /// <summary>누적 수영 시간 — 금</summary>
    public const string ACH_DIVE_TIME_GOLD = "ACH_DIVE_TIME_GOLD";
    #endregion

    #region ─────────────────────────▶ 통계 API Name ◀─────────────────────────
    // TODO: 통계도 파트너 사이트에 별도로 등록해야 합니다. 과제와 다른 목록입니다.
    //       등록하지 않으면 GetStat이 실패하고 누적이 전혀 쌓이지 않습니다.
    //       각 과제의 Progress Stat으로 아래 통계를 연결해두면 오버레이에
    //       진행도 바가 자동으로 표시됩니다. 금·은·동 3개 과제에 같은 통계를
    //       연결해도 되며, 각 과제의 목표치만 다르게 잡으면 됩니다.

    /// <summary>누적 획득 골드 (INT)</summary>
    public const string STAT_TOTAL_MONEY = "STAT_TOTAL_MONEY";

    /// <summary>누적 수영 시간, 초 단위 (INT) — 타이틀·잠수함 내부·일시정지는 제외</summary>
    public const string STAT_DIVE_SECONDS = "STAT_DIVE_SECONDS";
    #endregion

    #region ─────────────────────────▶ 달성 기준값 ◀─────────────────────────
    /// <summary>누적 획득 골드 — 동 (골드) · 기획 확정: 처음 돈을 얻는 즉시 해금</summary>
    public const int ACH_TOTAL_MONEY_BRONZE_THRESHOLD = 1;

    /// <summary>누적 획득 골드 — 은 (골드)</summary>
    public const int ACH_TOTAL_MONEY_SILVER_THRESHOLD = 100000;

    /// <summary>누적 획득 골드 — 금 (골드)</summary>
    public const int ACH_TOTAL_MONEY_GOLD_THRESHOLD = 10000000;

    /// <summary>누적 수영 시간 — 동 (초) · 현재 10분</summary>
    public const int ACH_DIVE_TIME_BRONZE_THRESHOLD_SECONDS = 600;

    /// <summary>누적 수영 시간 — 은 (초) · 현재 30분</summary>
    public const int ACH_DIVE_TIME_SILVER_THRESHOLD_SECONDS = 1800;

    /// <summary>누적 수영 시간 — 금 (초) · 현재 1시간</summary>
    public const int ACH_DIVE_TIME_GOLD_THRESHOLD_SECONDS = 3600;
    #endregion
}
