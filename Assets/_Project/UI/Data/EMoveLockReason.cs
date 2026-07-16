using System;

/// <summary>
/// 플레이어 이동/조작이 막혀야 하는 사유입니다.
/// 여러 사유가 동시에 성립할 수 있으며, 하나라도 켜져 있으면 이동이 막힙니다.
/// Time.timeScale로 시간 자체를 멈추는 Pause와 달리, 시간은 그대로 흐르되 조작만 막고 싶을 때 씁니다.
/// </summary>
[Flags]
public enum EMoveLockReason
{
    None = 0,
    Shop = 1 << 0,      // 상점창 열림
    Inventory = 1 << 1, // 인벤토리창 열림
    Result = 1 << 2,    // 결과창 열림
}
