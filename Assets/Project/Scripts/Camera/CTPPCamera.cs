using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 3인칭 카메라 스크립트 입니다.
/// </summary>
public class CTPPCamera : AFrameable, ILateUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private Transform _player;
    [SerializeField] private Camera _camera;

    [Header("3인칭")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, -2f);
    [SerializeField] private float _lookAtHeight = 1.5f;
    [SerializeField] private float _sharpness = 18f;

    [Header("3인칭 옵션")]
    [SerializeField] private bool _useOrbit = true;
    [SerializeField] private float _orbitSensitivity = 0.5f;

    [Header("수중 회전 제한")]
    [SerializeField] private float _swimOrbitPitchMin = -70f;
    [SerializeField] private float _swimOrbitPitchMax = 70f;

    [Header("지상 회전 제한")]
    [SerializeField] private float _groundOrbitPitchMin = -10f;
    [SerializeField] private float _groundOrbitPitchMax = 25f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CPlayerController _playerController;

    private Transform _camTransform;
    private float _orbitYaw;
    private float _orbitPitch;

    private Vector2 _currentLookInput;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public Transform CamTransform => _camTransform;

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

    private void BuildCamPose(out Vector3 desiredPos)
    {
        if (_useOrbit)
        {
            _orbitYaw = _player.eulerAngles.y;
            _orbitPitch -= _currentLookInput.y * _orbitSensitivity;

            float currentMin = _groundOrbitPitchMin;
            float currentMax = _groundOrbitPitchMax;

            if (_playerController != null && _playerController.CurrentState == EPlayerState.Swimming)
            {
                currentMin = _swimOrbitPitchMin;
                currentMax = _swimOrbitPitchMax;
            }

            _orbitPitch = Mathf.Clamp(_orbitPitch, currentMin, currentMax);

            Quaternion orbitRot = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            desiredPos = _player.position + (orbitRot * _offset);
        }
        else
        {
            desiredPos = _player.position + (_player.rotation * _offset);
        }
    }

    private void ApplyPose(Vector3 desiredPos, float sharpness, bool snap)
    {
        if (snap)
        {
            _camTransform.position = desiredPos;
        }
        else
        {
            float t = GetSmoothT(sharpness);
            _camTransform.position = Vector3.Lerp(_camTransform.position, desiredPos, t);
        }

        Vector3 lookPos = _player.position + Vector3.up * _lookAtHeight;
        _camTransform.rotation = Quaternion.LookRotation(lookPos - _camTransform.position, Vector3.up);
    }

    private void InitCam()
    {
        _orbitYaw = _player.eulerAngles.y;
        _orbitPitch = 12.0f;

        Vector3 desiredPos;

        BuildCamPose(out desiredPos);

        ApplyPose(desiredPos, _sharpness, true);
    }

    private void TickCam()
    {
        Vector3 desiredPos;

        BuildCamPose(out desiredPos);

        ApplyPose(desiredPos, _sharpness, false);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();

        Cursor.lockState = CursorLockMode.Locked;

        CEventBus<OnInputLook>.Subscribe(LookHandler);
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
