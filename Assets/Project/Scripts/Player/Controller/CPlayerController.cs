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
    [SerializeField] private float _rotationSharpness = 12f;

    [Header("수영 상승 기울임")]
    [Tooltip("상승(스페이스) 속도가 자세 기울임에 반영되는 정도. 클수록 상승 시 정수리가 더 위로 향함")]
    [SerializeField] private float _ascendTiltInfluence = 0.2f;

    [Header("카메라 참조")]
    [SerializeField] private Transform _cameraTransform;

    [Header("바닥 감지 설정")]
    [SerializeField] private float _groundCheckDistance;
    [SerializeField] private LayerMask _groundLayer;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Rigidbody _rb;
    private Vector2 _currentMoveInput;

    private Vector3 _moveDirection;

    private Vector3 _lastHeading = Vector3.forward;

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

        _moveDirection = CalcMoveDirection();

        _rb.AddForce(_moveDirection * _moveSpeed, ForceMode.Force);

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
        UpdateRotation();
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
    private Vector3 CalcMoveDirection()
    {
        Transform cam = GetCam();
        if (cam == null) return Vector3.zero;

        Vector3 forward;
        Vector3 right;

        if (_currentState == EPlayerState.Swimming)
        {
            forward = cam.forward;
            right = cam.right;
        }
        else
        {
            forward = cam.forward;
            forward.y = 0f;
            forward.Normalize();

            right = cam.right;
            right.y = 0f;
            right.Normalize();
        }

        Vector3 dir = forward * _currentMoveInput.y + right * _currentMoveInput.x;
        return dir.normalized;
    }

    private void UpdateRotation()
    {
        Vector3 flatMove = _moveDirection;
        flatMove.y = 0f;
        if (flatMove.sqrMagnitude > 0.0001f)
        {
            _lastHeading = flatMove.normalized;
        }

        Quaternion targetRot;

        bool hasMove = _moveDirection.sqrMagnitude >= 0.0001f;
        bool isAscending = _currentState == EPlayerState.Swimming && _isJumpPressed;

        if (_currentState == EPlayerState.Swimming && (hasMove || isAscending))
        {
            Vector3 poseDir = _moveDirection;

            if (isAscending)
            {
                float upSpeed = Mathf.Max(0f, _rb != null ? _rb.velocity.y : 0f);
                poseDir += Vector3.up * (upSpeed * _ascendTiltInfluence);
            }

            if (poseDir.sqrMagnitude < 0.0001f) poseDir = Vector3.up;

            targetRot = GetSwimPose(poseDir);
        }
        else if (!hasMove)
        {
            targetRot = Quaternion.LookRotation(_lastHeading, Vector3.up);
        }
        else
        {
            targetRot = Quaternion.LookRotation(_lastHeading, Vector3.up);
        }

        float t = 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
    }

    private Quaternion GetSwimPose(Vector3 dir)
    {
        Vector3 up = dir.normalized;

        Vector3 right = Vector3.Cross(Vector3.up, up);

        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.Cross(_lastHeading, up);
            if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        }

        right.Normalize();

        Vector3 forward = Vector3.Cross(right, up).normalized;

        return Quaternion.LookRotation(forward, up);
    }

    private Transform GetCam()
    {
        if (_cameraTransform != null) return _cameraTransform;
        if (Camera.main != null) _cameraTransform = Camera.main.transform;
        return _cameraTransform;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnInputMove>.Unsubscribe(MoveHandler);
        CEventBus<OnInputJump>.Unsubscribe(JumpHandler);
        CEventBus<OnInputEsc>.Unsubscribe(EscHandler);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        GetCam();

        Vector3 f = transform.forward;
        f.y = 0f;
        _lastHeading = f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;

        EPlayerState startState = _currentState;
        _currentState = EPlayerState.OnGround;
        SetState(startState);

        CEventBus<OnInputMove>.Subscribe(MoveHandler);
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
