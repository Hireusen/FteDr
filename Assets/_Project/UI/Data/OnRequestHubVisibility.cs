/// <summary>
/// 열려있는 UI 창들의 조합에 따라 HUD를 보이거나 숨겨야 할 때 발행합니다.
/// (여러 창이 동시에 HidesHud=true여도 하나만 남을 때까지는 계속 숨김 상태가 유지되도록 CUIManager가 계산해서 발행)
/// </summary>
public readonly struct OnRequestHudVisibility
{
    public readonly bool visible;

    public OnRequestHudVisibility(bool visible)
    {
        this.visible = visible;
    }

    /// <param name="visible">HUD를 보여야 하면 true, 숨겨야 하면 false</param>
    public static void Publish(bool visible)
    {
        CEventBus<OnRequestHudVisibility>.Publish(new OnRequestHudVisibility(visible));
    }
}
