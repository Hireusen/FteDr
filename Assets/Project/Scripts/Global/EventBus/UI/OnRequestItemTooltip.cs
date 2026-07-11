using UnityEngine;

/// <summary>
/// 아이템 툴팁 표시를 요청할 때 발생하는 이벤트 구조체입니다.
/// </summary>
public readonly struct OnRequestShowTooltip
{
    public readonly CCollectibleSO data;
    public readonly Vector2 screenPosition;

    public OnRequestShowTooltip(CCollectibleSO data, Vector2 screenPosition)
    {
        this.data = data;
        this.screenPosition = screenPosition;
    }

    /// <param name="data">표시할 수집품 SO 데이터</param>
    /// <param name="screenPosition">마우스 커서의 화면 위치</param>
    public static void Publish(CCollectibleSO data, Vector2 screenPosition)
    {
        CEventBus<OnRequestShowTooltip>.Publish(new OnRequestShowTooltip(data, screenPosition));
    }
}

/// <summary>
/// 아이템 툴팁 닫기를 요청할 때 발생하는 이벤트 구조체입니다.
/// </summary>
public readonly struct OnRequestHideTooltip
{
    /// <summary>
    /// 툴팁 닫기 요청 이벤트를 발행합니다.
    /// </summary>
    public static void Publish()
    {
        CEventBus<OnRequestHideTooltip>.Publish(new OnRequestHideTooltip());
    }
}
