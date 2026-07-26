/// <summary>
/// HUD의 "숨길 수 있는 요소"(가방 버튼, 그물 버튼, 잠수함 레이더 마커)를 켜고 끌 때 발행합니다.
/// 산소 게이지처럼 항상 보여야 하는 요소는 이 이벤트와 무관합니다.
/// </summary>
public readonly struct OnRequestHudElementsVisibility
{
    public readonly bool visible;

    public OnRequestHudElementsVisibility(bool visible)
    {
        this.visible = visible;
    }

    /// <param name="visible">true면 표시, false면 숨김</param>
    public static void Publish(bool visible)
    {
        CEventBus<OnRequestHudElementsVisibility>.Publish(new OnRequestHudElementsVisibility(visible));
    }
}
