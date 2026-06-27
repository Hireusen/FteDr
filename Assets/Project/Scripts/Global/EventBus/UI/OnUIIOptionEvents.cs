/// <summary>
/// 전역 설정(Options) 팝업창을 열어달라고 요청하는 이벤트 구조체입니다.
/// </summary>
public readonly struct OnRequestOpenSettings
{
    /// <summary>
    /// 설정창 오픈 이벤트를 발행합니다.
    /// </summary>
    public static void Publish()
    {
        CEventBus<OnRequestOpenSettings>.Publish(new OnRequestOpenSettings());
    }
}

/// <summary>
/// 전역 설정(Options) 팝업창을 닫아달라고 요청하는 이벤트 구조체입니다.
/// </summary>
public readonly struct OnRequestCloseSettings
{
    /// <summary>
    /// 설정창 닫기 이벤트를 발행합니다.
    /// </summary>
    public static void Publish()
    {
        CEventBus<OnRequestCloseSettings>.Publish(new OnRequestCloseSettings());
    }
}
