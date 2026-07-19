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
    [SerializeField] private float _verticalForce;

    [Header("수중 물리 세팅")]
    [SerializeField] private float _waterDrag;

    [Header("이동 방향 참조")]
    [Tooltip("이동 방향 기준이 되는 카메라 트랜스폼")]
    [SerializeField] private Transform _cameraTransform;

    [Header("시선")]
    [Tooltip("눈높이 앵커(플레이어 자식). 카메라가 이 트랜스폼의 위치·회전을 복사함")]
    [SerializeField] private Transform _cameraRoot;
    [Tooltip("수영↔지상 전환 시 몸이 눕고/서는 속도. 클수록 빨리 전환")]
    [SerializeField] private float _postureBlendSharpness = 8f;

    [Header("바닥 감지 설정")]
    [SerializeField] private float _groundCheckDistance;
    [Tooltip("바닥 판정 레이를 발 위치보다 살짝 위에서 쏘는 거리. 몸이 땅에 조금 파묻혀도 감지되게 함")]
    [SerializeField] private float _groundCheckUpOffset = 0.3f;
    [SerializeField] private LayerMask _groundLayer;

    [Header("잠수함 판정")]
    [Tooltip("잠수함(내부) 판정에 사용할 레이어. 진입 시 지상, 이탈 시 수영 상태로 전환")]
    [SerializeField] private LayerMask _submarineLayer;

    [Header("연료 상태")]
    [Tooltip("연료 부족(Low) 시 이동 속도 배율. 작을수록 급격히 느려짐")]
    [SerializeField, Range(0f, 1f)] private float _lowFuelSpeedMultiplier = 0.3f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Rigidbody _rb;
    private Vector2 _currentMoveInput;

    private Vector3 _moveDirection;

    // 카메라가 넘겨주는 시선 각도
    private float _yaw;
    private float _pitch;

    // 몸에 pitch 를 싣는 비율. 수영 1, 지상 0 을 향해 서서히 이동 (기립/눕기 부드럽게)
    private float _postureBlend;

    private EPlayerState _currentState = EPlayerState.OnGround;

    private bool _isJumpPressed = false;
    private bool _isDescentPressed = false;

    private float _fuelSpeedMultiplier = 1f;

    // 조작 잠금 사유들. 하나라도 true 면 잠금. (집게/고갈이 서로를 덮어쓰지 않도록 분리)
    private bool _lockByGrab = false;
    private bool _lockByFuel = false;
    private bool _lockByCutscene = false; // 잠수함 연출 등으로 잠수함에 종속된 동안 조작 잠금

    // 잠수함 연출 종속 이전의 물리 상태를 복원하기 위해 보관
    private bool _prevKinematic = false;
    private Transform _originalParent = null;

    private EMoveLockReason _uiLockReasons = EMoveLockReason.None;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EFixedUpdatePriority FixedUpdatePriority => EFixedUpdatePriority.Lv5;

    public EPlayerState CurrentState => _currentState;

    /// <summary>하나의 사유라도 서 있으면 조작이 잠깁니다.</summary>
    public bool IsControlLocked => _lockByGrab || _lockByFuel || _lockByCutscene || _uiLockReasons != EMoveLockReason.None;

    /// <summary>
    /// 시선(회전)을 제외한 실제 이동 조작(이동/상승)이 들어오고 있는지 여부입니다.
    /// 연료 소모 판정용이며, 조작이 잠긴 동안에는 실제 이동이 없으므로 false입니다.
    /// </summary>
    public bool HasMovementInput
    {
        get
        {
            if (IsControlLocked) return false;
            if (_isJumpPressed) return true; // 상승(수중) / 점프
            return _currentMoveInput.sqrMagnitude > 0.0001f;
        }
    }

    /// <summary>집게 사용 등으로 인한 조작 잠금 사유를 켜고 끕니다.</summary>
    public bool IsControlLockedByGrab
    {
        get { return _lockByGrab; }
        set { _lockByGrab = value; }
    }

    /// <summary>
    /// 카메라가 시선 각도를 넘겨주는 창구입니다. 실제 회전은 FixedUpdate 에서 적용됩니다.
    /// 집게 사용 중에는 카메라가 각도를 넘기지 않으므로 몸은 마지막 각도로 고정됩니다.
    /// </summary>
    public void SetLookAngles(float yaw, float pitch)
    {
        _yaw = yaw;
        _pitch = pitch;
    }

    /// <summary>
    /// 플레이어를 지정한 트랜스폼의 위치·회전으로 즉시 이동시킵니다.
    /// </summary>
    /// <param name="target">이동시킬 목표 트랜스폼(예: 잠수함의 SpawnPoint)</param>
    public void Teleport(Transform target)
    {
        if (target == null)
        {
            UDebug.Print("Teleport 대상이 null입니다.", LogType.Error);
            return;
        }

        transform.SetPositionAndRotation(target.position, target.rotation);
        _yaw = target.eulerAngles.y;
        _pitch = 0f;

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 플레이어를 지정한 부모(잠수함 등)에 종속시킵니다.
    /// 연출 동안 부모의 이동을 그대로 따라가도록 Rigidbody를 kinematic으로 전환하고 조작을 잠급니다.
    /// Detach로 반드시 원복해야 합니다.
    /// </summary>
    /// <param name="parent">종속시킬 부모 트랜스폼</param>
    public void AttachTo(Transform parent)
    {
        if (parent == null)
        {
            UDebug.Print("AttachTo 대상이 null입니다.", LogType.Error);
            return;
        }

        _originalParent = transform.parent;
        transform.SetParent(parent, worldPositionStays: true);

        if (_rb != null)
        {
            _prevKinematic = _rb.isKinematic;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true; // 물리 시뮬레이션에서 빠져 부모 이동을 트랜스폼으로 따라감
        }

        _lockByCutscene = true;
    }

    /// <summary>
    /// AttachTo로 걸었던 종속을 해제하고 원래 물리 상태·부모로 되돌립니다.
    /// </summary>
    public void Detach()
    {
        transform.SetParent(_originalParent, worldPositionStays: true);
        _originalParent = null;

        if (_rb != null)
        {
            _rb.isKinematic = _prevKinematic;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        _lockByCutscene = false;
    }

    public void ExecuteFixedUpdateFrame()
    {
        if (_rb == null) return;

        if (_currentState == EPlayerState.Swimming || _currentState == EPlayerState.WaterGround)
        {
            CheckWaterGround();
        }

        ApplyLookRotation();

        _moveDirection = CalcMoveDirection();
        if (IsControlLocked) _moveDirection = Vector3.zero; // 잠금: 이동 무시

        if (_currentState == EPlayerState.Swimming)
        {
            // 수영: 힘 기반 이동 (물의 관성 유지). 수중 중력 없음 → 입력 없으면 그 자리에 부유
            _rb.AddForce(_moveDirection * (_moveSpeed * _fuelSpeedMultiplier), ForceMode.Force);

            if (_isJumpPressed && !IsControlLocked)
            {
                _rb.AddForce(Vector3.up * (_verticalForce * _fuelSpeedMultiplier), ForceMode.Force);
            }

            if (_isDescentPressed && !IsControlLocked)
            {
                _rb.AddForce(Vector3.down * (_verticalForce * _fuelSpeedMultiplier), ForceMode.Force);
            }
        }
        else
        {
            // 지상 / 수중바닥: 수평 속도 직접 제어로 미끄러짐 제거 (y속도는 유지)
            Vector3 horizontalVel = _moveDirection * (_groundMoveSpeed * _fuelSpeedMultiplier);
            _rb.velocity = new Vector3(horizontalVel.x, _rb.velocity.y, horizontalVel.z);

            if (_isJumpPressed && !IsControlLocked)
            {
                if (_rb.velocity.y <= 0.1f && IsGrounded())
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
        bool isGround = IsGrounded();

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
    /// <summary>
    /// 몸과 CameraRoot 에 회전을 적용합니다.
    /// 집게 사용 등으로 잠긴 동안에는 몸 회전을 갱신하지 않아 몸이 고정됩니다.
    /// (카메라는 CFPPCamera 에서 독립적으로 회전)
    /// </summary>
    private void ApplyLookRotation()
    {
        if (IsControlLocked) return;

        float target = (_currentState == EPlayerState.Swimming) ? 1f : 0f;
        float t = 1f - Mathf.Exp(-_postureBlendSharpness * Time.fixedDeltaTime);
        _postureBlend = Mathf.Lerp(_postureBlend, target, t);

        float bodyPitch = _pitch * _postureBlend;
        _rb.MoveRotation(Quaternion.Euler(bodyPitch, _yaw, 0f));

        if (_cameraRoot != null)
        {
            float headPitch = _pitch * (1f - _postureBlend);
            _cameraRoot.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
        }
    }

    private Vector3 CalcMoveDirection()
    {
        if (_cameraTransform == null) return Vector3.zero;

        Vector3 forward = _cameraTransform.forward;
        Vector3 right = _cameraTransform.right;

        if (_currentState != EPlayerState.Swimming)
        {
            // 지상 / 수중바닥: 수평 이동만
            forward.y = 0f;
            right.y = 0f;
        }

        forward.Normalize();
        right.Normalize();

        Vector3 dir = forward * _currentMoveInput.y + right * _currentMoveInput.x;
        return dir.normalized;
    }

    // 지면 위에 있는지 판정합니다. 몸이 땅에 조금 파묻혀도 감지되도록
    // 시작점을 살짝 위로 올리고, 그만큼 레이 길이를 늘려서 아래로 쏩니다.
    private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * _groundCheckUpOffset;
        float distance = _groundCheckUpOffset + _groundCheckDistance;
        return Physics.Raycast(origin, Vector3.down, distance, _groundLayer);
    }

    private void ApplyFuelState(EFuelState state)
    {
        switch (state)
        {
            case EFuelState.Normal:
                _fuelSpeedMultiplier = 1f;
                _lockByFuel = false;
                break;

            case EFuelState.Low:
                _fuelSpeedMultiplier = _lowFuelSpeedMultiplier;
                _lockByFuel = false;
                break;

            case EFuelState.Depleted:
                _fuelSpeedMultiplier = 0f;
                _lockByFuel = true;
                break;
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnInputMove>.Unsubscribe(MoveHandler);
        CEventBus<OnInputJump>.Unsubscribe(JumpHandler);
        CEventBus<OnInputDescent>.Unsubscribe(DescentHandler);
        CEventBus<OnPlayerFuelStateChanged>.Unsubscribe(FuelStateHandler);
        CEventBus<OnSetMoveLockReason>.Unsubscribe(MoveLockHandler);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        _rb = GetComponent<Rigidbody>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 현재 몸 방향에서 yaw 초기화 (카메라가 이후 값을 덮어씀)
        _yaw = transform.eulerAngles.y;
        _pitch = 0f;
        _postureBlend = 0f; // 지상 시작

        EPlayerState startState = _currentState;
        _currentState = EPlayerState.OnGround;
        SetState(startState);

        CEventBus<OnInputMove>.Subscribe(MoveHandler);
        CEventBus<OnInputJump>.Subscribe(JumpHandler);
        CEventBus<OnInputDescent>.Subscribe(DescentHandler);
        CEventBus<OnPlayerFuelStateChanged>.Subscribe(FuelStateHandler);
        CEventBus<OnSetMoveLockReason>.Subscribe(MoveLockHandler);

        ApplyFuelState(CPlayerManager.Ins.FuelState);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.IsInLayerMask(_submarineLayer))
        {
            SetState(EPlayerState.OnGround);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.IsInLayerMask(_submarineLayer))
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

    private void DescentHandler(OnInputDescent data)
    {
        _isDescentPressed = data.descentPressed;
    }

    private void FuelStateHandler(OnPlayerFuelStateChanged e)
    {
        ApplyFuelState(e.state);
    }

    private void MoveLockHandler(OnSetMoveLockReason ctx)
    {
        _uiLockReasons = ctx.active ? (_uiLockReasons | ctx.reason) : (_uiLockReasons & ~ctx.reason);
    }
    #endregion

}
