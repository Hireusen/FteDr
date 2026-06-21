using UnityEngine;

/// <summary>
/// 마우스를 움직일 때마다 이벤트를 발행합니다.
/// </summary>
public readonly struct OnInputLook
{
    public readonly Vector2 delta;

    public OnInputLook(Vector2 delta)
    {
        this.delta = delta;
    }

    /// <param name="delta">이동량</param>
    public static void Publish(Vector2 delta)
    {
        CEventBus<OnInputLook>.Publish(new OnInputLook(delta));
    }
}
