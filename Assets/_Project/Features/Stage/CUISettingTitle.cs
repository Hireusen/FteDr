/// <summary>
/// 씬 시작 시 Ui를 타이틀 씬에 맞게 정리합니다.
/// </summary>
public class CUISettingTitle : AMono
{
    private static void CloseGameUI()
    {
        OnRequestCloseUI.Publish(EUI.HudWindow);
        OnRequestCloseUI.Publish(EUI.InventoryWindow);
        OnRequestCloseUI.Publish(EUI.PauseMenuWindow);
        OnRequestCloseUI.Publish(EUI.ShopWindow);
    }

    private static void OpenTitleUI()
    {
        
    }

    private void Start()
    {
        CloseGameUI();
        OpenTitleUI();
    }
}
