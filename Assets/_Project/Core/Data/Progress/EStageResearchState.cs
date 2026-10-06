/// <summary>
/// 상점의 스테이지 연구 진행 가능 여부를 정의하는 열거형입니다.
/// </summary>
public enum EStageResearchState
{
    None = 0,
    NeedSpecial,    // 특수 수집품을 아직 가방에 담아오지 않음
    Ready,          // 연구를 시작할 수 있음
    AllResearched,  // 더 이상 해금할 스테이지가 없음
}
