using Cinemachine;
using UnityEngine;

// 1인칭 카메라
public class CFPPCamera : AFrameable, ILateUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private LayerMask _groundLayer;

    [Header("필수 연결")]
    [SerializeField] private CPlayerController _playerController;
    [Tooltip("평상시 카메라가 위치/회전을 동기화할 눈높이 앵커")]
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private CinemachineVirtualCamera _camera;

    [Header("회전 감도")]
    [Tooltip("옵션 매니저 로드 전 기본 감도")]
    [SerializeField] private float _lookSensitivity = K.CAMERA_SENSITIVITY_BASE;

    [Header("카메라 스무딩")]
    [Tooltip("부드러운 카메라 회전 적용 여부")]
    [SerializeField] private bool _useSmoothLook = true;
    [Tooltip("스무딩 지연 시간")]
    [SerializeField] private float _smoothLookTime = 0.05f;

    [Header("수중 회전 제한")]
    [SerializeField] private float _swimPitchMin = -85f;
    [SerializeField] private float _swimPitchMax = 85f;

    [Header("지상 회전 제한")]
    [SerializeField] private float _groundPitchMin = -40f;
    [SerializeField] private float _groundPitchMax = 60f;

    [Header("집게 중 둘러보기 제한(쏜 시점 기준 상대각)")]
    [Tooltip("집게 사용 중 좌우 최대 시야각(±)")]
    [SerializeField] private float _grabYawRange = 60f;
    [Tooltip("집게 사용 중 상하 최대 시야각(±)")]
    [SerializeField] private float _grabPitchRange = 50f;
    [Tooltip("집게 종료 시 카메라 복귀 속도")]
    [SerializeField] private float _grabReturnSharpness = 10f;
    [Tooltip("카메라 복귀 완료 판정 허용 오차 각도")]
    [SerializeField] private float _grabReturnDoneAngle = 0.5f;

    [Header("연결")]
    [Tooltip("집게 컴포넌트 참조")]
    [SerializeField] private CNewGrab _grabTool;

    [Header("디버그 옵션")]
    [SerializeField] private bool _isCameraRotationLock;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Transform _camTransform;
    private Vector2 _currentLookInput;

    // 현재 및 목표 카메라 회전 각도
    private float _yaw;
    private float _pitch;
    private float _targetYaw;
    private float _targetPitch;

    // 스무딩 연산용 현재 속도
    private float _yawVelocity;
    private float _pitchVelocity;

    // 집게 시점 제어 상태
    private bool _grabLook;
    private float _grabBaseYaw;
    private float _grabBasePitch;
    private bool _grabReturning;

    // 외부 시점 제어 여부
    private bool _controlSuspended;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public ELateUpdatePriority LateUpdatePriority => ELateUpdatePriority.Lv5;
    public float LookSensitivity => Sensitivity;
    public bool IsControlling => !_controlSuspended;

    public bool IsCameraRotationLock
    {
        get { return _isCameraRotationLock; }
        set { _isCameraRotationLock = value; }
    }

    // 현재 적용할 카메라 회전 감도
    private float Sensitivity
    {
        get
        {
            CLocalOptionManager option = CLocalOptionManager.Ins;
            return option != null ? option.CameraSensitivity : _lookSensitivity;
        }
    }

    public void ExecuteLateUpdateFrame()
    {
        if (_playerController == null || _cameraRoot == null || _camTransform == null) return;

        if (_controlSuspended) return;

        // 일시정지(Time.timeScale = 0) 중에는 deltaTime이 0이 되고, 그 프레임에 SmoothDamp를 돌리면
        // 내부의 오버슈트 보정이 속도를 0으로 나눠 NaN을 만든다. 프레임 매니저는 timeScale과 무관하게
        // 돌기 때문에 여기서 직접 걸러낸다.
        // 이때 들어와 있던 입력은 버린다. 남겨두면 정지가 풀리는 첫 프레임에 한꺼번에 적용되어 시점이 튄다.
        if (Time.deltaTime <= 0f)
        {
            _currentLookInput = Vector2.zero;
            return;
        }

        // 어떤 경로로든 각도나 속도가 NaN이 되면 다음 프레임의 입력으로 다시 들어가 영구히 남으므로,
        // 계산을 시작하기 전에 되돌려 놓는다.
        RecoverLookIfBroken();

        UpdateGrabLook();

        // 비 게임플레이 상태 시 입력 무시 및 위치 고정
        if (!CInputManager.Ins.IsGameplayInput)
        {
            _currentLookInput = Vector2.zero;
            SnapToRoot();
            return;
        }

        if (_grabLook)
        {
            GrabLookTick();
        }
        else if (_grabReturning)
        {
            GrabReturnTick();
        }
        else
        {
            NormalLookTick();
        }

        _currentLookInput = Vector2.zero;
    }

    // 지정한 각도로 시선 즉시 동기화
    public void SyncLookYaw(float yaw)
    {
        _yaw = yaw;
        _targetYaw = yaw;
        _pitch = 0f;
        _targetPitch = 0f;

        _yawVelocity = 0f;
        _pitchVelocity = 0f;
        _currentLookInput = Vector2.zero;

        _grabLook = false;
        _grabReturning = false;
        _grabBaseYaw = yaw;
        _grabBasePitch = 0f;

        if (_playerController != null) _playerController.SetLookAngles(_yaw, _pitch);
        if (_camTransform != null && _cameraRoot != null) SnapToRoot();
    }

    // 외부 제어 상태 전환 및 시점 재동기화
    public void SetControlSuspended(bool controlSuspended)
    {
        if (_controlSuspended == controlSuspended) return;

        _controlSuspended = controlSuspended;

        if (!controlSuspended && _camTransform != null)
        {
            Vector3 e = _camTransform.eulerAngles;
            _yaw = e.y;
            _targetYaw = e.y;
            _pitch = NormalizePitch(e.x);
            _targetPitch = _pitch;

            _yawVelocity = 0f;
            _pitchVelocity = 0f;
            _currentLookInput = Vector2.zero;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 평상시 시점 회전 및 위치 동기화 처리
    private void NormalLookTick()
    {
        _targetYaw += _currentLookInput.x * Sensitivity;
        float lookY = _currentLookInput.y;

        if (IsArmCrashedGround() && lookY < 0)
        {
            lookY = 0;
        }

        _targetPitch -= lookY * Sensitivity;

        bool swimming = _playerController.CurrentState == EPlayerState.Swimming;
        float min = swimming ? _swimPitchMin : _groundPitchMin;
        float max = swimming ? _swimPitchMax : _groundPitchMax;
        _targetPitch = Mathf.Clamp(_targetPitch, min, max);

        if (_useSmoothLook)
        {
            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, _smoothLookTime);
            _pitch = Mathf.SmoothDampAngle(_pitch, _targetPitch, ref _pitchVelocity, _smoothLookTime);
        }
        else
        {
            _yaw = _targetYaw;
            _pitch = _targetPitch;
        }

        _playerController.SetLookAngles(_yaw, _pitch);
        SnapToRoot();
    }

    // 집게 사용 중 상태의 상대적 시점 회전 처리
    private void GrabLookTick()
    {
        _targetYaw += _currentLookInput.x * Sensitivity;
        _targetPitch -= _currentLookInput.y * Sensitivity;

        _targetYaw = _grabBaseYaw + Mathf.Clamp(_targetYaw - _grabBaseYaw, -_grabYawRange, _grabYawRange);
        _targetPitch = _grabBasePitch + Mathf.Clamp(_targetPitch - _grabBasePitch, -_grabPitchRange, _grabPitchRange);

        if (_useSmoothLook)
        {
            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVelocity, _smoothLookTime);
            _pitch = Mathf.SmoothDampAngle(_pitch, _targetPitch, ref _pitchVelocity, _smoothLookTime);
        }
        else
        {
            _yaw = _targetYaw;
            _pitch = _targetPitch;
        }

        _camTransform.SetPositionAndRotation(_cameraRoot.position, Quaternion.Euler(_pitch, _yaw, 0f));
    }

    // 집게 사용 상태에 따른 시점 모드 전환 판정
    private void UpdateGrabLook()
    {
        bool grabbing = _grabTool != null && _grabTool.grabStatus != CNewGrab.EGrabStatus.Wait && _grabTool.grabStatus != CNewGrab.EGrabStatus.ReadyShoot && _grabTool.grabStatus != CNewGrab.EGrabStatus.WaittoReady;

        if (grabbing && !_grabLook)
        {
            _grabBaseYaw = _yaw;
            _grabBasePitch = _pitch;

            _targetYaw = _yaw;
            _targetPitch = _pitch;
            _yawVelocity = 0f;
            _pitchVelocity = 0f;

            _grabLook = true;
            _grabReturning = false;
        }
        else if (!grabbing && _grabLook)
        {
            _grabLook = false;
            _grabReturning = true;
        }
    }

    // 집게 사용 종료 후 원래 시점으로의 복귀 보간 처리
    private void GrabReturnTick()
    {
        Quaternion current = Quaternion.Euler(_pitch, _yaw, 0f);
        Quaternion targetRot = _cameraRoot.rotation;

        float t = 1f - Mathf.Exp(-_grabReturnSharpness * Time.deltaTime);
        Quaternion next = Quaternion.Slerp(current, targetRot, t);

        _camTransform.SetPositionAndRotation(_cameraRoot.position, next);

        Vector3 e = next.eulerAngles;
        _yaw = e.y;
        _pitch = NormalizePitch(e.x);

        _targetYaw = _yaw;
        _targetPitch = _pitch;

        // 허용 오차 내 진입 시 복귀 완료 처리
        if (Quaternion.Angle(next, targetRot) <= _grabReturnDoneAngle)
        {
            _yaw = _cameraRoot.eulerAngles.y;
            _targetYaw = _yaw;
            _pitch = NormalizePitch(_cameraRoot.eulerAngles.x);
            _targetPitch = _pitch;

            _yawVelocity = 0f;
            _pitchVelocity = 0f;

            _grabReturning = false;
        }
    }

    private void SnapToRoot()
    {
        _camTransform.SetPositionAndRotation(_cameraRoot.position, _cameraRoot.rotation);
    }

    private float NormalizePitch(float euler)
    {
        if (euler > 180f) euler -= 360f;
        return euler;
    }

    // 시선 각도와 스무딩 속도가 NaN/Infinity가 됐으면 성한 값으로 되돌린다.
    // NaN은 SmoothDamp에 다시 들어가면 그대로 NaN을 내놓기 때문에, 한 번 들어오면 저절로 낫지 않는다.
    // 속도만 0으로 돌려도 각도가 NaN이면 복구되지 않으므로 둘 다 본다.
    private void RecoverLookIfBroken()
    {
        if (!IsFinite(_yawVelocity)) _yawVelocity = 0f;
        if (!IsFinite(_pitchVelocity)) _pitchVelocity = 0f;

        // 목표 각도를 먼저 세운다. 현재 각도가 성하면 그 값을, 둘 다 망가졌으면 몸이 보는 방향을 기준으로 삼는다.
        if (!IsFinite(_targetYaw))
        {
            _targetYaw = IsFinite(_yaw) ? _yaw : _playerController.transform.eulerAngles.y;
        }
        if (!IsFinite(_targetPitch))
        {
            _targetPitch = IsFinite(_pitch) ? _pitch : 0f;
        }

        if (!IsFinite(_yaw)) _yaw = _targetYaw;
        if (!IsFinite(_pitch)) _pitch = _targetPitch;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private bool IsArmCrashedGround()
    {
        bool result = true;
        int mask = ~LayerMask.GetMask("Player");
        result = Physics.CheckSphere(_cameraRoot.transform.position, 0.1f, mask);

        return false;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();

        CEventBus<OnInputLook>.Subscribe(LookHandler);
        CEventBus<OnPlayerFuelStateChanged>.Subscribe(FuelStateHandler);

        bool depleted = CPlayerManager.Ins != null && CPlayerManager.Ins.FuelState == EFuelState.Depleted;
        CInputManager.Ins.ResetForGameplay(depleted);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnInputLook>.Unsubscribe(LookHandler);
        CEventBus<OnPlayerFuelStateChanged>.Unsubscribe(FuelStateHandler);
    }

    private void Start()
    {
        if (_playerController == null || _cameraRoot == null || _camera == null)
        {
            UDebug.Log(true, "필수 참조 누락", LogType.Warning);
            enabled = false;
            return;
        }

        _camTransform = _camera.transform;

        _yaw = _playerController.transform.eulerAngles.y;
        _targetYaw = _yaw;
        _pitch = 0f;
        _targetPitch = 0f;

        SnapToRoot();
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void LookHandler(OnInputLook data)
    {
        if (_isCameraRotationLock) return;

        _currentLookInput = data.delta;
    }

    private void FuelStateHandler(OnPlayerFuelStateChanged e)
    {
        if (e.state == EFuelState.Depleted)
        {
            CInputManager.Ins.SetCursorReason(ECursorReason.FuelDepleted, true);
        }
        else if (e.previous == EFuelState.Depleted)
        {
            CInputManager.Ins.SetCursorReason(ECursorReason.FuelDepleted, false);
        }
    }
    #endregion
}
