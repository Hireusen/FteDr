/// <summary>
/// UI 오픈을 요청하는 통합 이벤트 구조체 입니다.
/// </summary>
public readonly struct OnRequestOpenUI
{
    public readonly EUI uIType;

    public OnRequestOpenUI(EUI uiType)
    {
        this.uIType = uiType;
    }

    /// <summary>
    /// 매개변수로 전달받은 UI를 열도록 이벤트를 발행합니다.
    /// </summary>
    public static void Publish(EUI uiType)
    {
        CEventBus<OnRequestOpenUI>.Publish(new OnRequestOpenUI(uiType));
    }
}

/// <summary>
/// UI 폐쇄을 요청하는 통합 이벤트 구조체 입니다.
/// </summary>
public readonly struct OnRequestCloseUI
{
    public readonly EUI uIType;

    public OnRequestCloseUI(EUI uiType)
    {
        this.uIType = uiType;
    }

    /// <summary>
    /// 매개변수로 전달받은 UI를 폐쇄하도록 이벤트를 발행합니다.
    /// </summary>
    public static void Publish(EUI uiType)
    {
        CEventBus<OnRequestCloseUI>.Publish(new OnRequestCloseUI(uiType));
    }
}

