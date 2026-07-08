using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [2단계] 형체 없는 플레이어(카메라)의 3D 자유 비행 이동입니다.
/// WASD로 전후좌우, Q/E로 상하 이동합니다. 물리 없이 Transform을 직접 제어합니다.
/// 이동은 카메라(자신)가 바라보는 방향 기준 → 이후 집게 발사 방향과 일관됩니다.
///
/// [입력] Input System 전용 프로젝트에서도 동작하도록 Keyboard.current로 직접 폴링합니다.
/// (본 게임 이식 시에는 프로젝트 입력 이벤트로 교체 가능)
///
/// 부착: 카메라(또는 카메라를 자식으로 둔 플레이어 루트)에 붙입니다.
/// </summary>
public sealed class CClawPlayer : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("이동 속도")]
    [Tooltip("초당 이동 거리")]
    [SerializeField] private float _moveSpeed = 6f;
    [Tooltip("빠른 이동 배율 (Shift)")]
    [SerializeField] private float _sprintMultiplier = 2f;

    [Header("시점 회전")]
    [Tooltip("마우스 감도")]
    [SerializeField] private float _lookSensitivity = 0.1f;
    [Tooltip("상하 회전 제한 각도")]
    [SerializeField] private float _pitchLimit = 85f;
    [Tooltip("우클릭 중에만 시점 회전")]
    [SerializeField] private bool _rotateOnlyWhenRightMouse = true;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _yaw;
    private float _pitch;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        Vector3 e = transform.eulerAngles;
        _yaw = e.y;
        _pitch = e.x;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        HandleLook();
        HandleMove(kb);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // WASD(전후좌우) + Q/E(상하) 이동. 카메라 방향 기준.
    private void HandleMove(Keyboard kb)
    {
        Vector3 dir = Vector3.zero;

        if (kb.wKey.isPressed) dir += transform.forward;
        if (kb.sKey.isPressed) dir -= transform.forward;
        if (kb.dKey.isPressed) dir += transform.right;
        if (kb.aKey.isPressed) dir -= transform.right;
        if (kb.eKey.isPressed) dir += Vector3.up;    // 상승 (월드 기준)
        if (kb.qKey.isPressed) dir -= Vector3.up;    // 하강

        if (dir.sqrMagnitude < 0.0001f) return;

        float speed = _moveSpeed;
        if (kb.leftShiftKey.isPressed) speed *= _sprintMultiplier;

        transform.position += dir.normalized * speed * Time.deltaTime;
    }

    // 마우스로 시점 회전 (yaw/pitch).
    private void HandleLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 우클릭 중에만 회전하는 옵션 (자유 비행 중 실수 회전 방지)
        if (_rotateOnlyWhenRightMouse && !mouse.rightButton.isPressed) return;

        Vector2 delta = mouse.delta.ReadValue();
        _yaw += delta.x * _lookSensitivity;
        _pitch -= delta.y * _lookSensitivity;
        _pitch = Mathf.Clamp(_pitch, -_pitchLimit, _pitchLimit);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
    #endregion
}
