using UnityEngine;

/// <summary>
/// 플레이어의 조작을 담당하는 컴포넌트 입니다.
/// </summary>
public class CPlayerController : AFrameable, IFixedUpdateFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("플레이어 세팅")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _ascendForce;

    [Header("수중 물리 세팅")]
    [SerializeField] private float _waterGravity;
    [SerializeField] private float _waterDrag;
    [SerializeField] private float _groundDrag;

    [Header("회전 설정")]
    [SerializeField] private float _lookSensitivity = 0.5f;

    [Header("바닥 감지 설정")]
    [SerializeField] private float _groundCheckDistance;
    [SerializeField] private LayerMask _groundLayer;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Rigidbody _rb;
    private Vector2 _currentMoveInput;
    private Vector2 _currentLookInput;

    private float _playerYaw;

    private EPlayerState _currentState = EPlayerState.OnGround;

    private bool _isJumpPressed = false;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EFixedUpdatePriority FixedUpdatePriority => EFixedUpdatePriority.Lv5;
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public EPlayerState CurrentState => _currentState;

    public void ExecuteFixedUpdateFrame()
    {
        if (_rb == null) return;

        if (_currentState == EPlayerState.Swimming || _currentState == EPlayerState.WaterGround)
        {
            CheckWaterGround();
        }

        Vector3 moveDirection = (transform.right * _currentMoveInput.x + transform.forward * _currentMoveInput.y);
        moveDirection.y = 0f;
        moveDirection.Normalize();

        _rb.AddForce(moveDirection * _moveSpeed, ForceMode.Force);

        if (_currentState == EPlayerState.Swimming)
        {
            _rb.AddForce(Vector3.down * _waterGravity, ForceMode.Acceleration);

            if (_isJumpPressed)
            {
                _rb.AddForce(Vector3.up * _ascendForce, ForceMode.Force);
            }
        }
        else
        {
            if (_isJumpPressed)
            {
                if (_rb.velocity.y <= 0.1f && Physics.Raycast(transform.position, Vector3.down, _groundCheckDistance, _groundLayer))
                {
                    _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
                    _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                }
            }
        }
    }

    public void ExecuteUpdateFrame()
    {
        transform.rotation = Quaternion.Euler(0f, _playerYaw, 0f);
    }
    #endregion

    #region ─────────────────────────▶ 상태 제어 ◀─────────────────────────
    /// <summary>
    /// 플레이어의 상태를 변경합니다.
    /// </summary>
    /// <param name="newState"></param>
    public void SetState(EPlayerState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        UDebug.Print($"플레이어 상태 변경됨 : {_currentState}");

        if (_currentState == EPlayerState.Swimming || _currentState == EPlayerState.WaterGround)
        {
            _rb.useGravity = false;
            _rb.drag = _waterDrag;
        }
        else if (_currentState == EPlayerState.OnGround)
        {
            _rb.useGravity = true;
            _rb.drag = _groundDrag;
        }
    }

    private void CheckWaterGround()
    {
        bool isGround = Physics.Raycast(transform.position, Vector3.down, _groundCheckDistance, _groundLayer);

        if (isGround && _currentState == EPlayerState.Swimming)
        {
            SetState(EPlayerState.WaterGround);
        }
        else if (!isGround && _currentState == EPlayerState.WaterGround)
        {
            SetState(EPlayerState.Swimming);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnInputMove>.Unsubscribe(MoveHandler);
        CEventBus<OnInputLook>.Unsubscribe(LookHandler);
        CEventBus<OnInputJump>.Unsubscribe(JumpHandler);
        CEventBus<OnInputEsc>.Unsubscribe(EscHandler);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        _rb = GetComponent<Rigidbody>();

        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _playerYaw = transform.eulerAngles.y;

        EPlayerState startState = _currentState;
        _currentState = EPlayerState.OnGround;
        SetState(startState);

        CEventBus<OnInputMove>.Subscribe(MoveHandler);
        CEventBus<OnInputLook>.Subscribe(LookHandler);
        CEventBus<OnInputJump>.Subscribe(JumpHandler);
        CEventBus<OnInputEsc>.Subscribe(EscHandler);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Submarine"))
        {
            SetState(EPlayerState.OnGround);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Submarine"))
        {
            SetState(EPlayerState.Swimming);
        }
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

        _playerYaw += _currentLookInput.x * _lookSensitivity;
    }

    private void JumpHandler(OnInputJump data)
    {
        _isJumpPressed = data.jumpPressed;
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
