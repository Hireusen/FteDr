/// <summary>
/// 조종석 지역 이동 UI에서 목적지 노드 하나가 가질 수 있는 상태입니다.
/// </summary>
public enum EStageNodeState
{
    None = 0,
    Visited,    // 이미 다녀온 해역 (이전 목적지)
    Stationed,  // 현재 정박 중인 해역
    Available,  // 연구로 해금되어 갈 수 있지만 아직 가보지 않은 해역
    Locked,     // 아직 연구되지 않아 정체를 모르는 해역
}
