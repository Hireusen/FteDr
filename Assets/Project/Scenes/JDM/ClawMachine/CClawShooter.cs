using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [3단계] 집게 발사 & 복귀. 카메라 정면으로 집게 헤드를 뻗고 되돌립니다.
///
/// [핵심 설계 — 팀원이 막힌 Scale 문제의 해법]
///   - 집게 헤드(콜라이더 O)는 Scale이 아니라 Position으로 이동한다. → 물리가 깨지지 않음.
///   - 발사 순간 목표 지점을 월드 좌표에 고정한다. → 플레이어가 움직여도 헤드는 그 자리.
///   - 팔(콜라이더 X 비주얼)은 매 프레임 Muzzle↔Head를 잇도록 Scale/회전만 갱신.
///     → 플레이어가 움직이면 팔이 자연스럽게 비스듬해진다.
///
/// [상태] Idle(대기) → Extending(뻗음) → Retracting(복귀) → Idle
///   같은 키로 발사. Extending 중 재입력 시 즉시 Retracting.
///   (이 단계에선 잡기 없음. 뻗고 돌아오는 것만. 잡기는 5단계.)
///
/// 부착: 플레이어(또는 매니저)에. 인스펙터에 Muzzle/Head/Arm 참조 연결.
/// </summary>
public sealed class CClawShooter : AMono
{
    #region ─────────────────────────▶ 열거형 ◀─────────────────────────
    private enum EState
    {
        Idle = 0,      // 대기 (헤드가 Muzzle에 붙어 있음)
        Extending,     // 발사 (목표를 향해 뻗는 중)
        Retracting,    // 복귀 (Muzzle로 돌아오는 중)
    }
    #endregion

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("참조")]
    [Tooltip("발사 지점 (플레이어 자식)")]
    [SerializeField] private Transform _muzzle;
    [Tooltip("집게 헤드 (독립 오브젝트, 콜라이더 있음)")]
    [SerializeField] private Transform _head;
    [Tooltip("팔 비주얼 큐브 (콜라이더 없음)")]
    [SerializeField] private Transform _arm;
    [Tooltip("발사 방향 기준 (보통 카메라). 비우면 muzzle 사용)")]
    [SerializeField] private Transform _aim;

    [Header("발사")]
    [Tooltip("최대 발사 거리")]
    [SerializeField] private float _maxDistance = 15f;
    [Tooltip("뻗는 속도 (초당 거리)")]
    [SerializeField] private float _extendSpeed = 30f;
    [Tooltip("복귀 속도 (초당 거리)")]
    [SerializeField] private float _retractSpeed = 40f;

    [Header("팔 비주얼")]
    [Tooltip("팔 큐브의 '길이'에 해당하는 로컬 축 (기본 Z)")]
    [SerializeField] private EAxis _armLengthAxis = EAxis.Z;
    [Tooltip("팔 큐브의 기본 두께(길이축 제외한 스케일)")]
    [SerializeField] private float _armThickness = 0.2f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private EState _state = EState.Idle;
    private Vector3 _targetPoint;  // 발사 시 월드에 고정되는 목표 지점
    private bool _prevFirePressed; // 키 엣지 감지용
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 발사 중(대기가 아님)인지 여부입니다.</summary>
    public bool IsBusy => _state != EState.Idle;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_aim == null) _aim = _muzzle;
        SnapHeadToMuzzle();
    }

    private void Update()
    {
        HandleInput();
        UpdateState();
        UpdateArmVisual();
    }
    #endregion

    #region ─────────────────────────▶ 입력 ◀─────────────────────────
    // 발사/집기 키(같은 키). 이 단계에선 좌클릭으로 폴링. (본 게임 이식 시 교체)
    private void HandleInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        bool pressed = mouse.leftButton.isPressed;
        bool justPressed = pressed && !_prevFirePressed;
        _prevFirePressed = pressed;

        if (!justPressed) return;

        // 대기 중이면 발사, 뻗는 중이면 즉시 복귀(= 도중 집기 트리거의 토대)
        if (_state == EState.Idle)
        {
            Fire();
        }
        else if (_state == EState.Extending)
        {
            _state = EState.Retracting;
        }
    }
    #endregion

    #region ─────────────────────────▶ 발사 로직 ◀─────────────────────────
    // 발사 시작: 목표 지점을 월드 좌표로 고정한다. (이후 플레이어가 움직여도 불변)
    private void Fire()
    {
        Vector3 origin = _muzzle.position;
        Vector3 dir = _aim.forward;
        _targetPoint = origin + dir * _maxDistance;

        _state = EState.Extending;
    }

    private void UpdateState()
    {
        switch (_state)
        {
            case EState.Extending:
                MoveHeadToward(_targetPoint, _extendSpeed);
                // 목표(최대 거리)에 거의 도달하면 복귀 시작
                if (ReachedTarget(_targetPoint))
                {
                    _state = EState.Retracting;
                }
                break;

            case EState.Retracting:
                MoveHeadToward(_muzzle.position, _retractSpeed);
                // Muzzle로 돌아오면 대기
                if (ReachedTarget(_muzzle.position))
                {
                    SnapHeadToMuzzle();
                    _state = EState.Idle;
                }
                break;
        }
    }

    // 헤드를 목표점으로 일정 속도 이동 (Position). 헤드가 목표를 바라보게 회전도.
    private void MoveHeadToward(Vector3 point, float speed)
    {
        _head.position = Vector3.MoveTowards(_head.position, point, speed * Time.deltaTime);

        Vector3 look = point - _head.position;
        if (look.sqrMagnitude > 0.0001f)
        {
            _head.rotation = Quaternion.LookRotation(look.normalized);
        }
    }

    private bool ReachedTarget(Vector3 point)
    {
        return (_head.position - point).sqrMagnitude < 0.01f;
    }

    // 헤드를 Muzzle 위치/방향으로 스냅 (대기 상태)
    private void SnapHeadToMuzzle()
    {
        _head.position = _muzzle.position;
        _head.rotation = _muzzle.rotation;
    }
    #endregion

    #region ─────────────────────────▶ 팔 비주얼 ◀─────────────────────────
    // 팔 큐브를 Muzzle↔Head 사이에 잇는다. (콜라이더 없으므로 Scale 사용 안전)
    // 플레이어가 움직이면 Muzzle이 함께 움직여 팔이 비스듬해진다.
    private void UpdateArmVisual()
    {
        if (_arm == null) return;

        Vector3 a = _muzzle.position;
        Vector3 b = _head.position;
        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = b - a;
        float length = dir.magnitude;

        _arm.position = mid;
        if (length > 0.0001f)
        {
            _arm.rotation = Quaternion.LookRotation(dir.normalized);
        }

        // 길이축만 거리에 맞추고 나머지는 두께 유지
        _arm.localScale = BuildArmScale(length);
    }

    // 길이축에만 length를 넣고 나머지 축엔 두께를 넣은 스케일을 만든다.
    private Vector3 BuildArmScale(float length)
    {
        switch (_armLengthAxis)
        {
            case EAxis.X:
                return new Vector3(length, _armThickness, _armThickness);
            case EAxis.Y:
                return new Vector3(_armThickness, length, _armThickness);
            case EAxis.Z:
            default:
                return new Vector3(_armThickness, _armThickness, length);
        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    public enum EAxis
    {
        X = 0,
        Y = 1,
        Z = 2,
    }
    #endregion
}
