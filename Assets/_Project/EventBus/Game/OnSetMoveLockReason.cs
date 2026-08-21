/// <summary>
/// 플레이어 이동 잠금 사유를 켜거나 끌 때 발행합니다. (CInputManager는 관여하지 않음)
/// CPlayerController처럼 실제로 이동을 적용하는 쪽이 이 이벤트를 구독해서 자신의 잠금 상태를 갱신합니다.
/// </summary>
public readonly struct OnSetMoveLockReason
{
    public readonly EMoveLockReason reason;
    public readonly bool active;

    public OnSetMoveLockReason(EMoveLockReason reason, bool active)
    {
        this.reason = reason;
        this.active = active;
    }

    /// <param name="reason">대상 사유 (Shop/Inventory/Result 등)</param>
    /// <param name="active">켤지(true) 끌지(false)</param>
    public static void Publish(EMoveLockReason reason, bool active)
    {
        CEventBus<OnSetMoveLockReason>.Publish(new OnSetMoveLockReason(reason, active));
    }
}
