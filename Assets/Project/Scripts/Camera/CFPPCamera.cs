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
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public ELateUpdatePriority LateUpdatePriority => ELateUpdatePriority.Lv5;

    public float LookSensitivity => _lookSensitivity;

    public void ExecuteLateUpdateFrame()
    {
        if (_playerController == null || _cameraRoot == null || _camTransform == null) return;

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
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();

        CEventBus<OnInputLook>.Subscribe(LookHandler);

        Cursor.lockState = CursorLockMode.Locked;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnInputLook>.Unsubscribe(LookHandler);
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
    #endregion
}
