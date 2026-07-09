using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [3단계] 집게 발사 & 복귀. 카메라 정면으로 집게 헤드를 뻗고 되돌립니다.
///
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

    [Header("발톱 / 감지")]
    [Tooltip("발톱 개폐 컴포넌트 (ClawHead의 CClawPincer)")]
    [SerializeField] private CClawPincer _pincer;
    [Tooltip("헤드 중심에서 잡을 대상을 찾는 반경")]
    [SerializeField] private float _grabRadius = 0.6f;
    [Tooltip("감지 대상 레이어 (비우면 전체)")]
    [SerializeField] private LayerMask _grabMask = ~0;
    [Tooltip("자동 집기: 발사 중 대상에 닿으면 자동으로 집음")]
    [SerializeField] private bool _autoGrab = false;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private EState _state = EState.Idle;
    private Vector3 _targetPoint;  // 발사 시 월드에 고정되는 목표 지점
    private Vector3 _fireDir;      // 발사 시 향한 방향 (복귀 중에도 이 방향 유지)
    private bool _prevFirePressed; // 키 엣지 감지용
    private CCollectible _detected; // 감지된 잡기 후보 (5단계에서 실제로 잡음)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 발사 중(대기가 아님)인지 여부입니다.</summary>
    public bool IsBusy => _state != EState.Idle;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_aim == null) _aim = _muzzle;
        _fireDir = _aim.forward;
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

        // 대기 중이면 발사, 뻗는 중이면 집기(수동 모드의 집기 트리거)
        if (_state == EState.Idle)
        {
            Fire();
        }
        else if (_state == EState.Extending)
        {
            BeginGrab();
        }
    }
    #endregion

    #region ─────────────────────────▶ 발사 로직 ◀─────────────────────────
    // 발사 시작: 목표 지점을 월드 좌표로 고정한다. (이후 플레이어가 움직여도 불변)
    private void Fire()
    {
        Vector3 origin = _muzzle.position;
        Vector3 dir = _aim.forward;
        _fireDir = dir;                        // 발사 방향 저장 (복귀 중에도 유지)
        _targetPoint = origin + dir * _maxDistance;

        _detected = null;
        if (_pincer != null) _pincer.Open(); // 발사 중엔 발톱 열림
        _state = EState.Extending;
    }

    private void UpdateState()
    {
        switch (_state)
        {
            case EState.Idle:
                // 대기 중엔 매 프레임 Muzzle을 따라다닌다 (플레이어가 움직여도 붙어 있음)
                SnapHeadToMuzzle();
                break;

            case EState.Extending:
                // 복귀/발사 내내 발사 방향을 유지 (이동 방향으로 홱 돌지 않게)
                _head.rotation = Quaternion.LookRotation(_fireDir);
                MoveHeadToward(_targetPoint, _extendSpeed);

                // 헤드 주변에서 잡을 대상(CCollectible) 감지
                _detected = DetectCollectible();

                // 자동 모드: 대상에 닿으면 즉시 집기
                if (_autoGrab && _detected != null)
                {
                    BeginGrab();
                    break;
                }

                if (ReachedTarget(_targetPoint))
                {
                    _state = EState.Retracting;
                }
                break;

            case EState.Retracting:
                // 복귀 중에도 발사했던 방향을 유지 (뒤로 돌지 않게)
                _head.rotation = Quaternion.LookRotation(_fireDir);
                MoveHeadToward(_muzzle.position, _retractSpeed);
                if (ReachedTarget(_muzzle.position))
                {
                    if (_pincer != null) _pincer.Open(); // 대기 준비: 발톱 다시 열기
                    _state = EState.Idle;
                }
                break;
        }
    }

    // 헤드를 목표점으로 일정 속도 이동 (Position만. 회전은 호출부가 발사 방향으로 유지).
    private void MoveHeadToward(Vector3 point, float speed)
    {
        _head.position = Vector3.MoveTowards(_head.position, point, speed * Time.deltaTime);
    }

    private bool ReachedTarget(Vector3 point)
    {
        return (_head.position - point).sqrMagnitude < 0.01f;
    }

    // 집기 트리거: 발톱을 (물체 크기에 맞춰) 닫고 복귀 상태로 전환한다.
    // (이 단계에선 감지/닫힘까지. 실제 물리 잡기(FixedJoint)는 5단계.)
    private void BeginGrab()
    {
        if (_detected != null)
        {
            float radius = GetObjectRadius(_detected);
            if (_pincer != null) _pincer.CloseOnObject(radius);
            UDebug.Print($"[집게] 집기 → 대상: {_detected.name} (반지름 {radius:F2})");
        }
        else
        {
            if (_pincer != null) _pincer.Close();
            UDebug.Print("[집게] 집기 → 대상 없음 (빈손 복귀)");
        }

        _state = EState.Retracting;
    }

    // 수집품의 모든 콜라이더 bounds를 합쳐 대략적인 반지름을 구한다.
    // (다중 Visual = 콜라이더 여러 개인 경우까지 전체를 감싸도록)
    // bounds 읽기는 삼각형 수와 무관한 상수 비용이고, 집는 순간 1회만 호출되므로 성능 부담 없음.
    private float GetObjectRadius(CCollectible c)
    {
        Collider[] cols = c.GetComponentsInChildren<Collider>();
        if (cols.Length == 0) return 0f;

        // 첫 콜라이더로 bounds 초기화 후 나머지를 합침
        Bounds total = cols[0].bounds;
        for (int i = 1; i < cols.Length; ++i)
        {
            total.Encapsulate(cols[i].bounds);
        }

        // 무는 방향(가로/세로) 기준 근사 반지름 = 가로·세로 절반 중 작은 쪽
        Vector3 ext = total.extents;
        return Mathf.Min(ext.x, ext.z);
    }

    // 헤드 중심 주변에서 CCollectible을 가진 가장 가까운 대상을 찾는다.
    private CCollectible DetectCollectible()
    {
        Collider[] hits = Physics.OverlapSphere(_head.position, _grabRadius, _grabMask);

        CCollectible nearest = null;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < hits.Length; ++i)
        {
            // 콜라이더가 자식 Visual에 있을 수 있으니 부모까지 탐색
            CCollectible c = hits[i].GetComponentInParent<CCollectible>();
            if (c == null) continue;

            float sqr = (c.transform.position - _head.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = c;
            }
        }
        return nearest;
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

    #region ─────────────────────────▶ 기즈모 ◀─────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (_head == null) return;

        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(_head.position, _grabRadius);
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
