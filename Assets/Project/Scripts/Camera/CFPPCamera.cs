using UnityEngine;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CFPPCamera : AFrameable, ILateUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _camera;

    [Header("1인칭 설정")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0.5f, 0.1f);
    [SerializeField] private float _sharpness = 20f;

    [Header("회전 감도")]
    [SerializeField] private float _lookSensitivity = 0.5f;

    [Header("수중 회전 제한")]
    [SerializeField] private float _swimPitchMin = -85f;
    [SerializeField] private float _swimPitchMax = 85f;

    [Header("지상 회전 제한")]
    [SerializeField] private float _groundPitchMin = -40f;
    [SerializeField] private float _groundPitchMax = 60f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CPlayerController _playerController;

    private Transform _camTransform;
    private Vector2 _currentLookInput;
    private float _pitch;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public ELateUpdatePriority LateUpdatePriority => ELateUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteLateUpdateFrame()
    {
        if (_player == null || _camTransform == null) return;

        TickCam();

        _currentLookInput = Vector2.zero;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private float GetSmoothT(float sharpness)
    {
        return 1f - Mathf.Exp(-sharpness * Time.deltaTime);
    }

    private void BuildCamPose(out Vector3 desiredPos, out Quaternion desiredRot, bool snap)
    {
        desiredPos = _player.position + (_player.rotation * _offset);

        _pitch -= _currentLookInput.y * _lookSensitivity;

        float currentMin = _groundPitchMin;
        float currentMax = _groundPitchMax;

        if (_playerController != null && _playerController.CurrentState == EPlayerState.Swimming)
        {
            currentMin = _swimPitchMin;
            currentMax = _swimPitchMax;
        }

        _pitch = Mathf.Clamp(_pitch, currentMin, currentMax);

        desiredRot = _player.rotation * Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void ApplyPose(Vector3 desiredPos, Quaternion desiredRot, float sharpness, bool snap)
    {
        if (snap)
        {
            _camTransform.position = desiredPos;
            _camTransform.rotation = desiredRot;
            return;
        }

        float t = GetSmoothT(sharpness);

        _camTransform.position = Vector3.Lerp(_camTransform.position, desiredPos, t);

        _camTransform.rotation = desiredRot;
    }

    private void InitCam()
    {
        _pitch = 0f;

        Vector3 desiredPos;
        Quaternion desiredRot;

        BuildCamPose(out desiredPos, out desiredRot, true);

        ApplyPose(desiredPos, desiredRot, _sharpness, true);
    }

    private void TickCam()
    {
        Vector3 desiredPos;
        Quaternion desiredRot;

        BuildCamPose(out desiredPos, out desiredRot, false);

        ApplyPose(desiredPos, desiredRot, _sharpness, false);
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
        if (_player != null)
        {
            _playerController = _player.GetComponent<CPlayerController>();
        }

        if (_camera == null)
        {
            GameObject mainCamGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamGO != null)
            {
                _camera = mainCamGO.GetComponent<Camera>();
            }
        }

        if (_player == null || _camera == null)
        {
            UDebug.Log(true, "필수 참조 확인", LogType.Warning);
            enabled = false;
            return;
        }

        _camTransform = _camera.transform;

        InitCam();
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void LookHandler(OnInputLook data)
    {
        _currentLookInput = data.delta;
    }
    #endregion
}
