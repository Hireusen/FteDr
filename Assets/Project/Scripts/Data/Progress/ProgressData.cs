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
    public int oxygenTankLevel = 1;
    public int radarLevel = 1;
    public int bagLevel = 1;
    public int thrusterLevel = 1;
    public int grabToolLevel = 1;
    #endregion

    #region ─────────────────────────▶ 진행 상황 ◀─────────────────────────
    public int unlockedStage = 0; // 씬 진행도
    #endregion
}
