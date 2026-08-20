using UnityEngine;

/// <summary>
/// 4방향으로 이동하는 간단한 테스트 스크립트입니다.
/// </summary>
public class CTestObjectMover : AFrameable, IUpdateFrameable
{
    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 50f;

    private Vector2 _moveInput;

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (_moveInput == Vector2.zero) return;

        Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);
        transform.Translate(moveDir * _moveSpeed * Time.deltaTime, Space.World);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void MoveHandler(OnInputMove ctx)
    {
        _moveInput = ctx.moved;
    }
    private void JumpHandler(OnInputJump ctx)
    {
        if (ctx.jumpPressed)
        {
            UDebug.Print("점프 키를 누르고 있습니다.");
        }
        else
        {
            UDebug.Print("점프 키를 뗏습니다");
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();
        CEventBus<OnInputMove>.Subscribe(MoveHandler);
        CEventBus<OnInputJump>.Subscribe(JumpHandler);
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        CEventBus<OnInputMove>.Unsubscribe(MoveHandler);
        CEventBus<OnInputJump>.Unsubscribe(JumpHandler);
    }
    #endregion
}
