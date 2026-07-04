using UnityEngine;

/// <summary>
/// 플레이어의 조작을 담당하는 컴포넌트 입니다.
/// </summary>
public class CPlayerController : AFrameable, IFixedUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("플레이어 세팅")]
    [Tooltip("수영 이동에 가하는 힘")]
    [SerializeField] private float _moveSpeed;
    [Tooltip("지상 이동 속도(m/s). 속도 직접 제어라 미끄러짐 없음")]
    [SerializeField] private float _groundMoveSpeed = 6f;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _ascendForce;

    [Header("수중 물리 세팅")]
    [SerializeField] private float _waterGravity;
    [SerializeField] private float _waterDrag;

    [Header("시선 회전 설정")]
    [SerializeField] private float _lookSensitivity = 0.5f;
    [Tooltip("상하 시선(pitch) 제한 각도")]
    [SerializeField] private float _pitchMin = -80f;
    [SerializeField] private float _pitchMax = 80f;
    [SerializeField] private Transform _cameraRoot;

    [Header("바닥 감지 설정")]
    [SerializeField] private float _groundCheckDistance;
    [SerializeField] private LayerMask _groundLayer;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Rigidbody _rb;
    private Vector2 _currentMoveInput;

    private Vector3 _moveDirection;

    private float _yaw;
    private float _pitch;

    private EPlayerState _currentState = EPlayerState.OnGround;

    private bool _isJumpPressed = false;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EFixedUpdatePriority FixedUpdatePriority => EFixedUpdatePriority.Lv5;

    public EPlayerState CurrentState => _currentState;

    public void ExecuteFixedUpdateFrame()
    {
        if (_rb == null) return;

        if (_currentState == EPlayerState.Swimming || _currentState == EPlayerState.WaterGround)
        {
            CheckWaterGround();
        }

        // 시선 회전 적용: 수영이면 pitch 를 몸통에, 그 외엔 머리에
        ApplyLookRotation();

        _moveDirection = CalcMoveDirection();

        if (_currentState == EPlayerState.Swimming)
        {
            // 수영: 힘 기반 이동 (물의 관성 유지)
            _rb.AddForce(_moveDirection * _moveSpeed, ForceMode.Force);

            _rb.AddForce(Vector3.down * _waterGravity, ForceMode.Acceleration);

            if (_isJumpPressed)
            {
                _rb.AddForce(Vector3.up * _ascendForce, ForceMode.Force);
            }
        }
        else
        {
            // 지상 / 수중바닥: 수평 속도 직접 제어로 미끄러짐 제거 (y속도는 유지)
            Vector3 horizontalVel = _moveDirection * _groundMoveSpeed;
            _rb.velocity = new Vector3(horizontalVel.x, _rb.velocity.y, horizontalVel.z);

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
            // 수영에서 넘어왔을 때 물 drag 가 그대로 남아 점프가 붕 뜨지 않도록 명시적으로 초기화
            _rb.drag = 0f;
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
    private void ApplyLookRotation()
    {
        bool swimming = _currentState == EPlayerState.Swimming;

        float bodyPitch = swimming ? _pitch : 0f;
        _rb.MoveRotation(Quaternion.Euler(bodyPitch, _yaw, 0f));

        if (_cameraRoot != null)
        {
            float headPitch = swimming ? 0f : _pitch;
            _cameraRoot.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
        }
    }

    private Vector3 CalcMoveDirection()
    {
        Vector3 forward;
        Vector3 right;

        if (_currentState == EPlayerState.Swimming)
        {
            // 수영: 몸통이 pitch 까지 기울어 있으므로 몸 forward 가 곧 시야 방향
            forward = transform.forward;
            right = transform.right;
        }
        else
        {
            // 지상 / 수중바닥: 몸은 수평(yaw만)이므로 몸 forward 가 이미 수평
            forward = transform.forward;
            forward.y = 0f;
            right = transform.right;
            right.y = 0f;
        }

        forward.Normalize();
        right.Normalize();

        Vector3 dir = forward * _currentMoveInput.y + right * _currentMoveInput.x;
        return dir.normalized;
    }
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

        // 현재 몸 방향에서 시선 각도 초기화
        Vector3 e = transform.eulerAngles;
        _yaw = e.y;
        _pitch = 0f;

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
        _yaw += data.delta.x * _lookSensitivity;
        _pitch -= data.delta.y * _lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);
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
