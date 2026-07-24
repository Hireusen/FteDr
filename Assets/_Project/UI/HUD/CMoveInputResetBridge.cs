using UnityEngine;

/// <summary>
/// CPlayerController를 건드리지 않고, 이동 잠금이 새로 걸리는 순간 입력을 강제로 0으로 되돌리는 브릿지입니다.
///
/// CPlayerController의 MoveHandler/JumpHandler/DescentHandler는 그대로 살아있으므로,
/// 여기서 OnInputMove/OnInputJump/OnInputDescent를 값 0으로 대신 발행해주면
/// 그 기존 핸들러들이 이 값을 받아 자기 내부 상태를 0으로 갱신한다.
/// (잠기기 직전까지 눌려있던 입력이 남아, 잠금이 풀리는 순간 갑자기 밀리듯 이동하는 것을 방지)
/// </summary>
public sealed class CMoveInputResetBridge : AMono
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    // CPlayerController의 _uiLockReasons와 동일한 로직을 이쪽에서도 별도로 추적한다. (private라 직접 참조 불가)
    private EMoveLockReason _lockReasons = EMoveLockReason.None;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnSetMoveLockReason>.Subscribe(MoveLockHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnSetMoveLockReason>.Unsubscribe(MoveLockHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void MoveLockHandler(OnSetMoveLockReason ctx)
    {
        bool wasLocked = _lockReasons != EMoveLockReason.None;
        _lockReasons = ctx.active ? (_lockReasons | ctx.reason) : (_lockReasons & ~ctx.reason);
        bool isLocked = _lockReasons != EMoveLockReason.None;

        // 잠금이 없다가 새로 걸리는 순간에만 리셋한다 (이미 잠긴 상태에서 사유가 추가되는 경우는 굳이 재발행 안 함)
        if (!wasLocked && isLocked)
        {
            OnInputMove.Publish(Vector2.zero);
            OnInputJump.Publish(false);
            OnInputDescent.Publish(false);
        }
    }
    #endregion
}
