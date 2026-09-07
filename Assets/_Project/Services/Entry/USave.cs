/// <summary>
/// 게임 종료·타이틀 복귀처럼 되돌릴 수 없는 행동 직전에 진행상황 저장을 일괄 수행하는 퍼사드입니다.
/// 하나라도 실패하면 호출부가 행동을 취소할 수 있도록 실패한 대상을 알려줍니다.
/// </summary>
public static class USave
{
    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 저장해야 할 모든 진행상황을 저장합니다. 하나라도 실패하면 즉시 중단합니다.
    /// </summary>
    /// <param name="failedTarget">실패한 저장 대상 이름 (성공 시 null)</param>
    /// <returns>모두 저장에 성공했다면 True</returns>
    public static bool TrySaveAll(out string failedTarget)
    {
        failedTarget = null;

        // 진행도 (골드·업그레이드·스테이지)
        CProgressManager progress = CProgressManager.Ins;
        if (progress == null || !progress.Save())
        {
            failedTarget = "진행도";
            return false;
        }

        // 현재 씬의 수집품 (게임플레이 씬이 아니면 로더가 없으므로 건너뛴다)
        CStageLoader loader = UObject.FindComponent<CStageLoader>(false);
        if (loader != null && !loader.SaveCollectible())
        {
            failedTarget = "수집품";
            return false;
        }

        // 환경설정
        CLocalOptionManager option = CLocalOptionManager.Ins;
        if (option == null || !option.Save())
        {
            failedTarget = "환경설정";
            return false;
        }

        return true;
    }
    #endregion
}
