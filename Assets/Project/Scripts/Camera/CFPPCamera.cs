using UnityEngine;

/// <summary>
/// 1인칭 카메라입니다.
/// </summary>
public class CFPPCamera : AFrameable, ILateUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private CPlayerController _playerController;
    [Tooltip("컨트롤러가 회전을 넣는 눈높이 앵커. 카메라가 이 트랜스폼의 위치·회전을 복사")]
    [SerializeField] private Transform _cameraRoot;
    [SerializeField] private Camera _camera;

    [Header("회전 감도")]
    [Tooltip("기본 감도값. 설정에 감도 옵션이 생기기 전까지 사용되는 폴백 (Sensitivity 프로퍼티 참고)")]
    [SerializeField] private float _lookSensitivity = 0.5f;

    [Header("수중 회전 제한")]
    [SerializeField] private float _swimPitchMin = -85f;
    [SerializeField] private float _swimPitchMax = 85f;

    [Header("지상 회전 제한")]
    [SerializeField] private float _groundPitchMin = -40f;
    [SerializeField] private float _groundPitchMax = 60f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Transform _camTransform;

    private Vector2 _currentLookInput;
    private float _yaw;
    private float _pitch;

    private bool _controlSuspended;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public ELateUpdatePriority LateUpdatePriority => ELateUpdatePriority.Lv5;

    public float LookSensitivity => _lookSensitivity;

    public bool IsControlling => !_controlSuspended;

    public void ExecuteLateUpdateFrame()
    {
        if (_playerController == null || _cameraRoot == null || _camTransform == null) return;

        // 게임플레이 입력이 아닐 때(메뉴, 연료 고갈 등)는 회전 입력을 막습니다.
        // 카메라는 앵커를 계속 복사해 몸에 붙어 있게 하고, 누적 입력만 버립니다.
        if (!CInputManager.Ins.IsGameplayInput)
        {
            _currentLookInput = Vector2.zero;
            _camTransform.SetPositionAndRotation(_cameraRoot.position, _cameraRoot.rotation);
            return;
        }

        // 1) 마우스 입력 누적
        _yaw += _currentLookInput.x * LookSensitivity;
        _pitch -= _currentLookInput.y * LookSensitivity;

        // 2) 상태별 pitch 범위 제한
        bool swimming = _playerController.CurrentState == EPlayerState.Swimming;
        float min = swimming ? _swimPitchMin : _groundPitchMin;
        float max = swimming ? _swimPitchMax : _groundPitchMax;
        _pitch = Mathf.Clamp(_pitch, min, max);

        // 3) 컨트롤러에 각도 전달 (몸/앵커 회전은 컨트롤러의 FixedUpdate 에서 적용)
        _playerController.SetLookAngles(_yaw, _pitch);

        // 4) 카메라는 EyeAnchor 의 위치·회전을 그대로 복사 (같은 보간 흐름 → 지터 없음)
        _camTransform.SetPositionAndRotation(_cameraRoot.position, _cameraRoot.rotation);

        _currentLookInput = Vector2.zero;
    }

    /// <summary>
    /// 카메라 제어권을 외부(시네머신 연출 등)에 넘기거나 되돌립니다.<br/>
    /// suspend=true 면 이 스크립트가 카메라 트랜스폼을 건드리지 않아, 시네머신 브레인이 온전히 제어합니다.<br/>
    /// suspend=false 로 되돌릴 때는 현재 카메라 자세에 맞춰 yaw/pitch 를 재동기화해 시선이 튀지 않게 합니다.
    /// </summary>
    public void SetControlSuspended(bool controlSuspended)
    {
        if (_controlSuspended == controlSuspended) return;

        _controlSuspended = controlSuspended;

        if (controlSuspended && _camTransform != null)
        {
            Vector3 e = _camTransform.eulerAngles;
            _yaw = e.y;
            _pitch = NormalizePitch(e.x);
            _currentLookInput = Vector2.zero;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
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

        // 게임플레이 진입: 커서를 기준값으로 되돌림 (이미 고갈 상태면 커서 유지)
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
        if (_camera == null)
        {
            GameObject mainCamGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamGO != null)
            {
                _camera = mainCamGO.GetComponent<Camera>();
            }
        }

        if (_playerController == null || _cameraRoot == null || _camera == null)
        {
            UDebug.Log(true, "필수 참조 확인", LogType.Warning);
            enabled = false;
            return;
        }

        _camTransform = _camera.transform;

        _yaw = _playerController.transform.eulerAngles.y;
        _pitch = 0f;

        _camTransform.SetPositionAndRotation(_cameraRoot.position, _cameraRoot.rotation);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void LookHandler(OnInputLook data)
    {
        _currentLookInput = data.delta;
    }

    private void FuelStateHandler(OnPlayerFuelStateChanged e)
    {
        // 고갈되면 커서 사유를 켜고(→ 회전 차단), 고갈에서 벗어나면 사유를 끕니다.
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
