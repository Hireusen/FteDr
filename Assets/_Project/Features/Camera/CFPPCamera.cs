using Cinemachine;
using UnityEngine;

/// <summary>
/// 1인칭 카메라입니다.
/// </summary>
public class CFPPCamera : AFrameable, ILateUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private CPlayerController _playerController;
    [Tooltip("컨트롤러가 회전을 넣는 눈높이 앵커. 평상시 카메라가 이 트랜스폼의 위치·회전을 복사")]
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private CinemachineVirtualCamera _camera;

    [Header("회전 감도")]
    [Tooltip("기본 감도값. 설정에 감도 옵션이 생기기 전까지 사용되는 폴백 (Sensitivity 프로퍼티 참고)")]
    [SerializeField] private float _lookSensitivity = 0.5f;

    [Header("수중 회전 제한")]
    [SerializeField] private float _swimPitchMin = -85f;
    [SerializeField] private float _swimPitchMax = 85f;

    [Header("지상 회전 제한")]
    [SerializeField] private float _groundPitchMin = -40f;
    [SerializeField] private float _groundPitchMax = 60f;

    [Header("집게 중 둘러보기 제한(쏜 시점 기준 상대각)")]
    [Tooltip("집게 사용 중 좌우로 둘러볼 수 있는 최대 각(±)")]
    [SerializeField] private float _grabYawRange = 60f;
    [Tooltip("집게 사용 중 상하로 둘러볼 수 있는 최대 각(±)")]
    [SerializeField] private float _grabPitchRange = 50f;
    [Tooltip("집게 종료 후 원래(몸) 시점으로 되돌아오는 속도. 클수록 빨리 복귀")]
    [SerializeField] private float _grabReturnSharpness = 10f;
    [Tooltip("복귀가 이 각도(도) 이내로 붙으면 완료 처리")]
    [SerializeField] private float _grabReturnDoneAngle = 0.5f;

    [Header("연결")]
    [Tooltip("집게 컴포넌트")]
    [SerializeField] private CNewGrab _grabTool;

    [Header("디버그 옵션")]
    [SerializeField] private bool _isCameraRotationLock;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Transform _camTransform;

    private Vector2 _currentLookInput;
    private float _yaw;
    private float _pitch;

    // 집게 사용 중 여부와, 진입 시점의 기준 각도(이 각을 중심으로 둘러봄)
    private bool _grabLook;
    private float _grabBaseYaw;
    private float _grabBasePitch;

    // 집게 종료 후 몸 시점으로 되돌아오는 중인지 여부
    private bool _grabReturning;

    private bool _controlSuspended;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public ELateUpdatePriority LateUpdatePriority => ELateUpdatePriority.Lv5;

    public float LookSensitivity => _lookSensitivity;

    public bool IsControlling => !_controlSuspended;

    public bool IsCameraRotationLock
    {
        get { return _isCameraRotationLock; }
        set { _isCameraRotationLock = value; }
    }

    private float Sensitivity => _lookSensitivity;

    public void ExecuteLateUpdateFrame()
    {
        if (_playerController == null || _cameraRoot == null || _camTransform == null) return;

        // 연출 등 외부가 카메라를 제어 중이면 손대지 않음 (시네머신과 충돌 방지)
        if (_controlSuspended) return;

        // 집게 상태에 따라 둘러보기 모드 진입/해제 판정
        UpdateGrabLook();

        // 게임플레이 입력이 아닐 때(메뉴 등)는 회전 입력을 버리고 위치만 유지
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

    /// <summary>
    /// 카메라 제어권을 외부(시네머신 연출 등)에 넘기거나 되돌립니다.
    /// </summary>
    public void SetControlSuspended(bool controlSuspended)
    {
        if (_controlSuspended == controlSuspended) return;

        _controlSuspended = controlSuspended;

        // 스크립트 제어로 복귀할 때, 연출이 옮겨놓은 카메라 방향으로 각도 재동기화
        if (!controlSuspended && _camTransform != null)
        {
            Vector3 e = _camTransform.eulerAngles;
            _yaw = e.y;
            _pitch = NormalizePitch(e.x);
            _currentLookInput = Vector2.zero;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>
    /// 평상시: 입력을 누적해 상태별 범위로 clamp 하고, 컨트롤러에 각도를 넘긴 뒤
    /// 카메라는 CameraRoot(몸)를 복사합니다.
    /// </summary>
    private void NormalLookTick()
    {
        _yaw += _currentLookInput.x * Sensitivity;
        _pitch -= _currentLookInput.y * Sensitivity;

        bool swimming = _playerController.CurrentState == EPlayerState.Swimming;
        float min = swimming ? _swimPitchMin : _groundPitchMin;
        float max = swimming ? _swimPitchMax : _groundPitchMax;
        _pitch = Mathf.Clamp(_pitch, min, max);

        _playerController.SetLookAngles(_yaw, _pitch);

        SnapToRoot();
    }

    /// <summary>
    /// 집게 중: 컨트롤러에 각도를 넘기지 않아 몸은 고정. 카메라만 쏜 시점(_grabBase*)을
    /// 중심으로 상대 범위 내에서 상하좌우 자유 회전합니다.
    /// </summary>
    private void GrabLookTick()
    {
        _yaw += _currentLookInput.x * Sensitivity;
        _pitch -= _currentLookInput.y * Sensitivity;

        // 쏜 시점을 중심으로 한 상대 각도 제한
        _yaw = _grabBaseYaw + Mathf.Clamp(_yaw - _grabBaseYaw, -_grabYawRange, _grabYawRange);
        _pitch = _grabBasePitch + Mathf.Clamp(_pitch - _grabBasePitch, -_grabPitchRange, _grabPitchRange);

        // 위치는 여전히 몸(CameraRoot)에 붙이되, 회전만 카메라 자체 각도로
        _camTransform.SetPositionAndRotation(_cameraRoot.position, Quaternion.Euler(_pitch, _yaw, 0f));
    }

    /// <summary>
    /// 집게 상태를 보고 둘러보기 모드 진입/해제를 처리합니다.
    /// 진입 순간의 현재 각도를 기준(_grabBase*)으로 저장합니다.
    /// </summary>
    private void UpdateGrabLook()
    {
        bool grabbing = _grabTool != null && _grabTool.grabStatus != CNewGrab.EGrabStatus.Wait;

        if (grabbing && !_grabLook)
        {
            // 진입: 현재 시선을 중심으로 둘러보기 시작
            _grabBaseYaw = _yaw;
            _grabBasePitch = _pitch;
            _grabLook = true;
            _grabReturning = false;
        }
        else if (!grabbing && _grabLook)
        {
            // 해제: 즉시 끊지 않고, 몸 시점으로 부드럽게 되돌아오는 복귀 단계로 진입
            _grabLook = false;
            _grabReturning = true;
        }
    }

    /// <summary>
    /// 집게 종료 후, 카메라 회전을 현재 각도에서 몸(CameraRoot) 각도로 Slerp 해 부드럽게 복귀합니다.
    /// 충분히 붙으면 _yaw/_pitch 를 몸 각도로 확정하고 평상시 모드로 넘깁니다.
    /// 복귀 중에는 컨트롤러에 각도를 넘기지 않아 몸은 고정된 채 카메라만 따라옵니다.
    /// </summary>
    private void GrabReturnTick()
    {
        Quaternion current = Quaternion.Euler(_pitch, _yaw, 0f);
        Quaternion targetRot = _cameraRoot.rotation;

        float t = 1f - Mathf.Exp(-_grabReturnSharpness * Time.deltaTime);
        Quaternion next = Quaternion.Slerp(current, targetRot, t);

        _camTransform.SetPositionAndRotation(_cameraRoot.position, next);

        // 카메라가 쓰는 각도도 보간 결과로 갱신 (다음 프레임 기준)
        Vector3 e = next.eulerAngles;
        _yaw = e.y;
        _pitch = NormalizePitch(e.x);

        // 충분히 붙으면 복귀 완료 → 몸 각도로 확정하고 평상시 제어로 전환
        if (Quaternion.Angle(next, targetRot) <= _grabReturnDoneAngle)
        {
            _yaw = _cameraRoot.eulerAngles.y;
            _pitch = NormalizePitch(_cameraRoot.eulerAngles.x);
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
            UDebug.Log(true, "필수 참조 확인", LogType.Warning);
            enabled = false;
            return;
        }

        _camTransform = _camera.transform;

        _yaw = _playerController.transform.eulerAngles.y;
        _pitch = 0f;

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
