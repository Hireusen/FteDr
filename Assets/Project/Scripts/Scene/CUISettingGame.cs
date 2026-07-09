/// <summary>
/// 씬 시작 시 Ui를 게임 씬에 맞게 정리합니다.
/// </summary>
public class CUISettingGame : AMono
{
    private static void CloseTitleUI()
    {

    }

    private static void OpenGameUI()
    {
        OnRequestOpenUI.Publish(EUI.HudWindow);
    }

    private void Start()
    {
        CloseTitleUI();
        OpenGameUI();
    }
}
