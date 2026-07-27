/// <summary>
/// 튜토리얼 재생기
/// </summary>
public static class CFirstGuide
{
    public static void Try()
    {
        var progress = CProgressManager.Ins.Progress;
        if (progress.isFirstGuideComplete) return;

        OnRequestOpenUI.Publish(EUI.TutorialWindow);
        progress.isFirstGuideComplete = true;
    }
}
