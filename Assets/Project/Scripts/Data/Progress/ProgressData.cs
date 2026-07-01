using System;
using System.Collections.Generic;

/// <summary>
/// 게임을 껐다 켜도 유지되는 영속 데이터입니다.
/// </summary>
[Serializable]
public class ProgressData
{
    #region ─────────────────────────▶ 재화 ◀─────────────────────────
    public int money = 0; // 보유 골드
    #endregion

    #region ─────────────────────────▶ 업그레이드 레벨 ◀─────────────────────────
    // 각 장비의 현재 레벨
    public int fuelTankLevel = 1;
    public int radarLevel = 1;
    public int bagLevel = 1;
    public int thrusterLevel = 1;
    public int grabToolLevel = 1;
    #endregion

    #region ─────────────────────────▶ 진행 상황 ◀─────────────────────────
    public int unlockedStage = 0; // 돈으로 해금한 최대 스테이지 (갈 수 있는 범위의 상한)
    public int currentStage = 0;  // 현재 위치한 스테이지 (해금 범위 내에서 상승/하강)
    #endregion
}
