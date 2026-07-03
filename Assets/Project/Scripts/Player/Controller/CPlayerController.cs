using UnityEngine;

/// <summary>
/// 플레이어의 조작을 담당하는 컴포넌트 입니다.
/// </summary>
public class CPlayerController : AFrameable, IFixedUpdateFrameable, IUpdateFrameable
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

    [Header("회전 설정")]
    [SerializeField] private float _rotationSharpness = 12f;
    [Tooltip("사이드 이동 시 몸이 도는 비율. 0.5면 순수 사이드 입력에서 45도")]
    [Range(0f, 1f)]
    [SerializeField] private float _sideTurnRatio = 0.5f;

    [Header("수영 눕기 조건")]
    [Tooltip("이동 방향의 수직 성분(|y|, 0~1)이 이 값 이하면 눕지 않고 서서 이동")]
    [Range(0f, 1f)]
    [SerializeField] private float _tiltDeadZone = 0.15f;
    [Tooltip("수직 성분이 이 값 이상이면 완전히 눕기. 데드존과 이 값 사이는 부드럽게 blend")]
    [Range(0f, 1f)]
    [SerializeField] private float _tiltFullAt = 0.6f;

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

    // 마지막으로 유효했던 수평 진행방향 (수직 상승/정지 시 yaw 안정화용)
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

        // 회전은 물리 스텝에서 MoveRotation 으로 갱신
        // (transform.rotation 을 Update 에서 직접 대입하면 Rigidbody 의 위치 보간이 리셋되어
        //  먼 배경에 지터가 발생함 — 특히 자세 전환폭이 큰 하강→상승 흐름에서 심함)
        UpdateRotation();
    }

    public void ExecuteUpdateFrame()
    {
        // 회전은 ExecuteFixedUpdateFrame 에서 처리 (Rigidbody 보간과 정합성 유지)
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
    private Vector3 CalcMoveDirection()
    {
        Transform cam = GetCam();
        if (cam == null) return Vector3.zero;

        Vector3 forward;
        Vector3 right;

        if (_currentState == EPlayerState.Swimming)
        {
            // 수영: 카메라가 바라보는 방향 그대로 (상하 포함)
            forward = cam.forward;
            right = cam.right;
        }
        else
        {
            // 지상 / 수중바닥: 수평 이동만
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
        // 수평 이동 성분 분해
        Vector3 flatMove = _moveDirection;
        flatMove.y = 0f;
        float flatMag = flatMove.magnitude;

        // 사이드 입력 시 몸이 이동방향까지 다 돌지 않도록 yaw 를 카메라 forward 와의 중간으로 축소
        if (flatMag > 0.0001f)
        {
            _lastHeading = GetReducedHeading(flatMove / flatMag);
        }

        Quaternion targetRot;

        bool hasMove = _moveDirection.sqrMagnitude >= 0.0001f;
        bool isAscending = _currentState == EPlayerState.Swimming && _isJumpPressed;

        if (_currentState == EPlayerState.Swimming && (hasMove || isAscending))
        {
            // 수영: 정수리(로컬 up)가 "실제 진행 방향"을 바라보도록.
            // 입력이 아닌 Rigidbody 속도를 쓰므로 수중 중력으로 아래로 흐르는 흐름도 자세에 반영됨.
            Vector3 vel = _rb != null ? _rb.velocity : Vector3.zero;

            // 수평 성분: yaw 방향(_lastHeading)과 실제 수평 속력을 곱해 사용
            Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
            float horizontalSpeed = flatVel.magnitude;

            // 수직 성분: 물리엔 영향 없이 자세 계산에서만 배율로 부풀려, 미세한 상승·하강도
            // 몸의 기울임에 시각적으로 잘 드러나도록 함
            Vector3 poseDir = _lastHeading * horizontalSpeed + Vector3.up * vel.y;

            // 속도가 거의 0이면 (막 진입, 정지) 입력 방향을 대신 사용
            if (poseDir.sqrMagnitude < 0.01f)
            {
                poseDir = hasMove
                    ? _lastHeading * flatMag + Vector3.up * _moveDirection.y
                    : Vector3.up;
            }

            // 수직 성분(|y|)이 작으면(순수 수평 이동) 눕지 않고 서서 이동,
            // 클수록 눕는 자세로 부드럽게 blend
            float verticalRatio = Mathf.Abs(poseDir.normalized.y);
            float tiltWeight = Mathf.InverseLerp(_tiltDeadZone, _tiltFullAt, verticalRatio);

            Quaternion uprightRot = Quaternion.LookRotation(_lastHeading, Vector3.up);
            Quaternion swimRot = GetSwimPose(poseDir);

            targetRot = Quaternion.Slerp(uprightRot, swimRot, tiltWeight);
        }
        else
        {
            // 지상 이동 / 정지(기립 복귀): 축소된 yaw 방향을 바라보고 몸은 세움
            targetRot = Quaternion.LookRotation(_lastHeading, Vector3.up);
        }

        float t = 1f - Mathf.Exp(-_rotationSharpness * Time.fixedDeltaTime);
        Quaternion nextRot = Quaternion.Slerp(_rb.rotation, targetRot, t);
        _rb.MoveRotation(nextRot);
    }

    /// <summary>
    /// 이동 방향의 yaw 를 카메라 forward 기준으로 _sideTurnRatio 만큼만 돌린 방향을 반환합니다.
    /// 순수 사이드 입력이면 45도(비율 0.5), 대각선이면 22.5도가 됩니다.
    /// 순수 후진처럼 카메라와 정반대면 중간 방향이 정의되지 않으므로 이동 방향을 그대로 씁니다.
    /// </summary>
    private Vector3 GetReducedHeading(Vector3 flatMoveDir)
    {
        Transform cam = GetCam();
        if (cam == null) return flatMoveDir;

        Vector3 camFlat = cam.forward;
        camFlat.y = 0f;

        if (camFlat.sqrMagnitude < 0.0001f) return flatMoveDir;
        camFlat.Normalize();

        // 카메라와 정반대(후진): 중간 방향 특이점 → 이동 방향 그대로
        if (Vector3.Dot(camFlat, flatMoveDir) < -0.99f) return flatMoveDir;

        return Vector3.Slerp(camFlat, flatMoveDir, _sideTurnRatio).normalized;
    }

    /// <summary>
    /// 정수리(로컬 up)가 진행방향을 향하는 수영 자세를 계산합니다.
    /// 좌우로 비틀리지(roll) 않도록 right축을 수평으로 고정하고,
    /// 진행방향이 거의 수직일 때는 마지막 수평 진행방향으로 yaw를 고정해 진동을 막습니다.
    /// </summary>
    private Quaternion GetSwimPose(Vector3 dir)
    {
        Vector3 up = dir.normalized; // 정수리를 진행방향으로

        // up과 월드 up 모두에 수직인 수평 right축 (좌우 비틀림 방지)
        Vector3 right = Vector3.Cross(Vector3.up, up);

        if (right.sqrMagnitude < 0.0001f)
        {
            // 거의 수직: 캐시된 진행방향 기준으로 yaw 고정
            // (외적 순서 주의: Cross(up, heading) 이어야 forward 가 heading 을 향함.
            //  반대로 하면 forward 가 -heading, 즉 카메라 쪽을 바라보는 버그가 생김)
            right = Vector3.Cross(up, _lastHeading);
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

        // 시작 시점의 수평 바라보는 방향을 기준으로 초기화
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
