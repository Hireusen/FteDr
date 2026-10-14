/// <summary>
/// 스팀 도전과제·통계의 진입점 역할을 하는 퍼사드 클래스입니다.
/// </summary>
/// <remarks>
/// Steamworks 타입을 전혀 노출하지 않으므로 호출부는 플랫폼 가드를 신경 쓸 필요가 없습니다.
/// 스팀이 꺼져 있거나 플레이 종료 중이면 모든 호출이 조용히 무시되므로,
/// 과제 처리 실패가 게임플레이를 멈추게 하지 않습니다.
/// </remarks>
public static class USteam
{
    #region ─────────────────────────▶ 내부 멤버 ◀─────────────────────────
    /// <summary>
    /// 도전과제 모듈에 접근합니다.
    /// 플레이 종료 중에는 싱글톤이 null을 반환하므로 여기서 한 번만 막습니다.
    /// </summary>
    private static CSteamAchievement Achievement
    {
        get
        {
            CSteamManager manager = CSteamManager.Ins;
            return manager != null ? manager.Achievement : null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 상태 ◀─────────────────────────
    /// <summary>스팀 연동 자체가 성립했는지 여부입니다. 옵션 화면의 연동 상태 표시 등에 씁니다.</summary>
    public static bool IsRunning
    {
        get
        {
            CSteamManager manager = CSteamManager.Ins;
            return manager != null && manager.IsSteamRunning;
        }
    }

    /// <summary>
    /// 지금 도전과제를 읽고 쓸 수 있는지 여부입니다.
    /// 해금만 할 때는 확인할 필요가 없습니다. 준비 전 요청은 내부에서 보류한 뒤 자동 처리됩니다.
    /// </summary>
    public static bool IsReady => Achievement?.IsStatsReady ?? false;
    #endregion

    #region ─────────────────────────▶ 도전과제 ◀─────────────────────────
    /// <summary>도전과제를 해금합니다. 이미 해금됐거나 스팀이 없으면 아무 일도 하지 않습니다.</summary>
    /// <param name="apiName">파트너 사이트에 등록한 API Name (K.ACH_*)</param>
    public static void Unlock(string apiName) => Achievement?.Unlock(apiName);

    /// <summary>해당 도전과제가 이미 해금되어 있는지 확인합니다.</summary>
    /// <returns>해금되어 있다면 True. 확인할 수 없는 상태면 False.</returns>
    public static bool IsUnlocked(string apiName) => Achievement?.IsUnlocked(apiName) ?? false;

    /// <summary>진행도 토스트를 띄웁니다. 이 호출만으로는 해금되지 않습니다.</summary>
    public static void IndicateProgress(string apiName, int current, int max)
        => Achievement?.IndicateProgress(apiName, current, max);
    #endregion

    #region ─────────────────────────▶ 통계 ◀─────────────────────────
    /// <summary>누적 통계값을 덮어씁니다.</summary>
    /// <param name="apiName">파트너 사이트에 등록한 통계 API Name (K.STAT_*)</param>
    public static void SetStat(string apiName, int value) => Achievement?.SetStat(apiName, value);

    /// <summary>누적 통계값을 읽습니다.</summary>
    /// <returns>읽기에 성공했다면 True</returns>
    public static bool TryGetStat(string apiName, out int value)
    {
        value = 0;

        CSteamAchievement achievement = Achievement;
        if (achievement == null) return false;

        return achievement.TryGetStat(apiName, out value);
    }
    #endregion

#if UNITY_EDITOR
    #region ─────────────────────────▶ 테스트 ◀─────────────────────────
    /// <summary>모든 통계와 도전과제를 초기화합니다. 에디터 전용입니다.</summary>
    public static void ResetAll() => Achievement?.ResetAll();
    #endregion
#endif
}
