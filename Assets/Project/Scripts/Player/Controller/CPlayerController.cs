using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CPlayerController : AFrameable, IFixedUpdateFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("플레이어 세팅")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _ascendForce;

    [Header("회전 설정")]
    [SerializeField] private float _lookSensitivity = 0.5f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Rigidbody _rb;
    private Vector2 _currentMoveInput;
    private Vector2 _currentLookInput;

    private EPlayerState _currentState = EPlayerState.Swimming;

    private bool _isJumpActionRequired = false;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EFixedUpdatePriority FixedUpdatePriority => EFixedUpdatePriority.Lv5;
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public EPlayerState CurrentState => _currentState;

    public void ExecuteFixedUpdateFrame()
    {
        if (_rb == null) return;

        Vector3 moveDirection = (transform.right * _currentMoveInput.x + transform.forward * _currentMoveInput.y).normalized;

        _rb.AddForce(moveDirection * _moveSpeed, ForceMode.Force);

        if (_isJumpActionRequired)
        {
            ApplyJumpOrAscendForce();
            _isJumpActionRequired = false;
        }
    }

    public void ExecuteUpdateFrame()
    {
        if (_currentLookInput.x != 0)
        {
            transform.Rotate(Vector3.up, _currentLookInput.x * _lookSensitivity);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void ApplyJumpOrAscendForce()
    {
        switch (_currentState)
        {
            case EPlayerState.OnGround:
                _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                break;
            case EPlayerState.Swimming:
                _rb.AddForce(Vector3.up * _ascendForce, ForceMode.Force);
                break;
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnInputMove>.Unsubscribe(MoveHandler);
        CEventBus<OnInputLook>.Unsubscribe(LookHandler);
        CEventBus<OnInputJump>.Unsubscribe(JumpHandler);
        CEventBus<OnInputGrab>.Unsubscribe(GrabHandler);
        CEventBus<OnInputEsc>.Unsubscribe(EscHandler);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        _rb = GetComponent<Rigidbody>();

        CEventBus<OnInputMove>.Subscribe(MoveHandler);
        CEventBus<OnInputLook>.Subscribe(LookHandler);
        CEventBus<OnInputJump>.Subscribe(JumpHandler);
        CEventBus<OnInputGrab>.Subscribe(GrabHandler);
        CEventBus<OnInputEsc>.Subscribe(EscHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void MoveHandler(OnInputMove data)
    {
        _currentMoveInput = data.moved;
    }

    private void LookHandler(OnInputLook data)
    {
        _currentLookInput = data.delta;
    }

    private void JumpHandler(OnInputJump data)
    {
        if (!data.jumpPressed) return;

        _isJumpActionRequired = true;
    }

    private void GrabHandler(OnInputGrab data)
    {
        UDebug.Print("수집품 줍기 / 상호작용 시도!");
    }

    private void EscHandler(OnInputEsc data)
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    #endregion
}
