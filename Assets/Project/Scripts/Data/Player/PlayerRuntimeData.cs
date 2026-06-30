using System.Collections.Generic;

/// <summary>
/// 스테이지 플레이동안 유지되는 휘발성 플레이어 상태입니다.
/// </summary>
public class PlayerRuntimeData
{
    #region ─────────────────────────▶ 산소 ◀─────────────────────────
    /// <summary>현재 산소량</summary>
    public float currentOxygen;
    /// <summary>외부에 의해 깎인 최대 산소량</summary>
    public float oxygenPenalty;
    #endregion

    #region ─────────────────────────▶ 소지품 ◀─────────────────────────
    /// <summary>가방에 담긴 일반 수집품 ID 목록.</summary>
    public readonly List<string> bagItems = new();
    /// <summary>손에 들고 있는 특수 수집품 ID</summary>
    public string heldSpecialItem;
    #endregion

    /// <summary>
    /// 플레이어 상태를 초기화합니다. (산소량은 매니저가 최대치로 다시 채웁니다)
    /// </summary>
    public void ResetForNew()
    {
        currentOxygen = 0f;
        oxygenPenalty = 0f;
        bagItems.Clear();
        heldSpecialItem = null;
    }
}
