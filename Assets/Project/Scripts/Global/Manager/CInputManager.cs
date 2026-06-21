using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input System으로 키 입력을 받아 이벤트를 뿌립니다.
/// </summary>
public sealed class CInputManager : ASingleton<CInputManager>, InputDispatcher.IGameMapActions
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private InputDispatcher _input; // 디스페처 주소
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 공개 멤버 함수 모두 외부 호출 용도가 아닙니다.
    public void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 moveInput = ctx.ReadValue<Vector2>();
        if (ctx.performed || ctx.canceled) {
            OnInputMove.Publish(moveInput);
        }
    }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) {
            OnInputJump.Publish(true);
        }
        else if (ctx.canceled) {
            OnInputJump.Publish(false);
        }
    }
    public void OnEsc(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) {
            OnInputEsc.Publish();
        }
    }
    public void OnLook(InputAction.CallbackContext ctx)
    {
        Vector2 lookInput = ctx.ReadValue<Vector2>();
        if (ctx.performed || ctx.canceled) {
            OnInputLook.Publish(lookInput);
        }
    }
    public void OnGrap(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputGrap.Publish();
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void Initialize()
    {
        _input = new InputDispatcher();
        _input.GameMap.SetCallbacks(this);
        _input.Enable();
    }
    #endregion

    #region ─────────────────────────▶ 메세지 함수 ◀─────────────────────────
    // ↓ 외부에서 호출해도 OK
    public void OnEnable()
    {
        if (_input == null)
        {
            UDebug.Print($"인풋 디스패처를 할당하지 않았습니다!", LogType.Error, this);
            return;
        }

        _input.Enable();
    }
    public void OnDisable()
    {
        if (_input == null)
        {
            UDebug.Print($"인풋 디스패처를 할당하지 않았습니다!", LogType.Error, this);
            return;
        }

        _input.Disable();
    }
    #endregion
}
