using UnityEngine;

/// <summary>
/// WASD 키를 누를 때마다 이벤트를 발행합니다.
/// </summary>
public readonly struct OnInputMove
{
    public readonly Vector2 moved;

    public OnInputMove(Vector2 moved)
    {
        this.moved = moved;
    }

    /// <param name="moved">이동량</param>
    public static void Publish(Vector2 moved)
    {
        CEventBus<OnInputMove>.Publish(new OnInputMove(moved));
    }
}
