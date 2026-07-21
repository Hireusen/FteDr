using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Input System으로 키 입력을 받아 이벤트를 뿌립니다.
/// </summary>
public sealed class CInputManager : ASingleton<CInputManager>, InputDispatcher.IGameMapActions
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private InputDispatcher _input; // 디스페처 주소

    private ECursorReason _cursorReasons = ECursorReason.None;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 게임플레이 입력 상태(커서 잠금·시선 회전 허용)인지 여부입니다.
    /// </summary>
    public bool IsGameplayInput => _cursorReasons == ECursorReason.None;

    /// <summary>
    /// 지정한 사유가 현재 커서를 잡고 있는지 여부입니다.
    /// </summary>
    public bool IsCursorReasonActive(ECursorReason reason) => (_cursorReasons & reason) != 0;

    /// <summary>
    /// 커서 표시 사유를 켜거나 끕니다.
    /// 사유가 하나라도 켜져 있으면 커서가 보이고 시선 회전이 막힙니다.
    /// </summary>
    /// <param name="reason">대상 사유</param>
    /// <param name="active">켤지(true) 끌지(false)</param>
    public void SetCursorReason(ECursorReason reason, bool active)
    {
        ECursorReason next = active ? (_cursorReasons | reason) : (_cursorReasons & ~reason);
        if (next == _cursorReasons) return;

        _cursorReasons = next;
        ApplyCursor();
    }

    /// <summary>
    /// 게임플레이 진입 시 커서 상태를 기준값으로 되돌립니다. (고갈 상태면 커서 유지)
    /// </summary>
    /// <param name="fuelDepleted">진입 시점의 연료 고갈 여부</param>
    public void ResetForGameplay(bool fuelDepleted)
    {
        _cursorReasons = fuelDepleted ? ECursorReason.FuelDepleted : ECursorReason.None;
        ApplyCursor();
    }

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
    public void OnGrab(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputGrab.Publish();
        }
    }
    public void OnCollect(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputCollect.Publish();
        }
    }
    public void OnCheat(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputCheat.Publish();
        }
    }
    public void OnDescent(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputDescent.Publish(true);
        }
        else if (ctx.canceled)
        {
            OnInputDescent.Publish(false);
        }
    }
    public void OnNet(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputNet.Publish();
        }
    }
    public void OnInventory(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputInventory.Publish();
        }
    }
    public void OnRotateTwizerLeft(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputRotateTwizerLeft.Publish(true);
        }
        else if (ctx.canceled)
        {
            OnInputRotateTwizerLeft.Publish(false);
        }
    }

    public void OnRotateTwizerRight(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            OnInputRotateTwizerRight.Publish(true);
        }
        else if (ctx.canceled)
        {
            OnInputRotateTwizerRight.Publish(false);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void Initialize()
    {
        _input = new InputDispatcher();
        _input.GameMap.SetCallbacks(this);
        _input.Enable();

        // 리바인딩 매니저에 이 에셋을 등록 → 저장된 커스텀 키가 있으면 복원됨
        CRebindManager.Ins.SetAsset(_input.asset);

        ApplyCursor(); // 초기 동기화
    }

    private void ApplyCursor()
    {
        bool gameplay = _cursorReasons == ECursorReason.None;
        Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !gameplay;
        UDebug.Print($"커서 표시 이유 = {_cursorReasons}");
        UDebug.Print($"게임플레이 확정 = {gameplay}");
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
