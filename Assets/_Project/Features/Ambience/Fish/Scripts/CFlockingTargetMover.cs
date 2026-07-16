using UnityEngine;

/// <summary>
/// 물고기 군집의 이동 형태(고정/자유)를 결정하고, SphereCast를 사용해 지형을 회피하며 이동하도록 제어하는 클래스입니다.
/// Free 모드에서도 소속 군집(CFlockingGroup)의 경계 박스 안에서만 목표점을 선택합니다.
/// </summary>
public sealed class CFlockingTargetMover : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("이동 모드 설정")]
    [SerializeField] private EMoveMode _moveMode = EMoveMode.Anchor;

    [Header("이동 및 타이밍")]
    [SerializeField] private float _moveRange = 5f;
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private Vector2 _positionChangeSpeed = new Vector2(3f, 8f);

    [Header("지형 회피 시스템")]
    [Tooltip("지형으로 판정할 콜라이더 레이어를 지정합니다.")]
    [SerializeField] private LayerMask _terrainLayer;
    [Tooltip("지형 감지용 가상 구체의 반지름입니다. 물고기 떼의 부피에 맞춰 설정하세요.")]
    [SerializeField, Min(0.1f)] private float _avoidanceRadius = 1.5f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _timer;
    private Vector3 _originalPosition; // 스폰 지점 (Anchor 모드 기준점)
    private Vector3 _targetPosition;
    private Transform _targetTransform;
    private CFlockingGroup _flock;     // 경계 박스 참조용
    private bool _isInitialized = false;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    /// <summary>타겟이 한 스텝에 이동할 수 있는 최대 반경입니다.</summary>
    public float MoveRange => _moveRange;
    /// <summary>현재 이동 모드입니다.</summary>
    public EMoveMode MoveMode => _moveMode;

    /// <summary>
    /// 기즈모용 기준점입니다. Anchor 모드는 스폰 원점을, Free 모드는 타겟 현재 위치를 반환합니다.
    /// 비재생(에디터) 상태에서는 원점이 아직 잡히지 않았으므로 트랜스폼 위치로 대체합니다.
    /// </summary>
    public Vector3 GizmoBasePosition
    {
        get
        {
            if (_moveMode == EMoveMode.Free && _targetTransform != null)
                return _targetTransform.position;
            // Anchor: 재생 중이면 스폰 원점, 아니면 현재 위치.
            return Application.isPlaying ? _originalPosition : transform.position;
        }
    }
    #endregion

    #region ─────────────────────────▶ 초기화 ◀─────────────────────────
    public void Initialize(float moveRange, float moveSpeed, Vector2 positionChangeSpeed)
    {
        _moveRange = moveRange;
        _moveSpeed = moveSpeed;
        _positionChangeSpeed = positionChangeSpeed;
        _isInitialized = true;

        _timer = Random.Range(_positionChangeSpeed.x, _positionChangeSpeed.y);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void Awake()
    {
        _originalPosition = transform.position;
        _flock = GetComponent<CFlockingGroup>();

        if (transform.childCount > 0)
        {
            _targetTransform = transform.GetChild(0);
            _targetPosition = _targetTransform.position;
        }
        else
        {
            UDebug.Print("CFlockingTargetMover: 자식으로 등록된 타겟 트랜스폼을 찾을 수 없습니다.", LogType.Error, this);
        }
    }

    public void ExecuteUpdateFrame()
    {
        if (_targetTransform == null) return;

        if (_timer >= 0f)
        {
            _timer -= Time.deltaTime;
            _targetTransform.position = Vector3.MoveTowards(
                _targetTransform.position,
                _targetPosition,
                _moveSpeed * Time.deltaTime
            );
        }
        else
        {
            float minInterval = _isInitialized ? _positionChangeSpeed.x : 3f;
            float maxInterval = _isInitialized ? _positionChangeSpeed.y : 8f;
            _timer = Random.Range(minInterval, maxInterval);

            // 안전 경로 연산을 통해 지형과 부딪히지 않는 최적의 다음 목표점을 선택합니다.
            _targetPosition = GetNextSafeTargetPosition();
        }
    }

    /// <summary>
    /// 구형 레이캐스트(SphereCast)를 활용하여 지형이 없는 안전한 목적지를 탐색하여 반환합니다.
    /// Free 모드에서도 최종 후보지는 군집 경계 박스 안으로 클램프됩니다.
    /// </summary>
    private Vector3 GetNextSafeTargetPosition()
    {
        // Anchor 모드라면 스폰 원점을 기준으로, Free 모드라면 현재 타겟 위치를 기준으로 난수 좌표를 산출합니다.
        Vector3 basePos = _moveMode == EMoveMode.Anchor ? _originalPosition : _targetTransform.position;
        Vector3 candidatePos = basePos;
        bool pathIsClear = false;

        // 연산 부하 방지용 최대 재시도(Retry) 횟수
        const int MAX_RETRIES = 8;

        for (int i = 0; i < MAX_RETRIES; ++i)
        {
            Vector3 randomOffset = Random.insideUnitSphere * _moveRange;
            candidatePos = ClampToBounds(basePos + randomOffset);

            Vector3 origin = _targetTransform.position;
            Vector3 direction = candidatePos - origin;
            float distance = direction.magnitude;

            if (distance < K.SMALL_DISTANCE)
            {
                pathIsClear = true;
                break;
            }

            direction.Normalize();

            // 구형 레이캐스트를 쏴서 다음 타겟 후보지 경로 사이에 지형이 있는지 검사합니다.
            if (!Physics.SphereCast(origin, _avoidanceRadius, direction, out RaycastHit hit, distance, _terrainLayer))
            {
                pathIsClear = true;
                break;
            }
        }

        // 예외 방어: 만약 8번의 난수 생성 결과가 모두 지형에 막혀있다면 (예: 막다른 골목에 고립)
        // 뒤쪽 방향으로 강제 후퇴 좌표를 만들어 갇히지 않도록 탈출시킵니다.
        if (!pathIsClear)
        {
            candidatePos = ClampToBounds(
                _targetTransform.position - _targetTransform.forward * (_moveRange * 0.5f));
        }

        return candidatePos;
    }

    /// <summary>후보 좌표를 군집 경계 박스 안으로 제한합니다. 군집 참조가 없으면 그대로 반환합니다.</summary>
    private Vector3 ClampToBounds(Vector3 pos)
    {
        if (_flock == null) return pos;

        Vector3 min = _flock.BoundsMin;
        Vector3 max = _flock.BoundsMax;
        pos.x = Mathf.Clamp(pos.x, min.x, max.x);
        pos.y = Mathf.Clamp(pos.y, min.y, max.y);
        pos.z = Mathf.Clamp(pos.z, min.z, max.z);
        return pos;
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    public enum EMoveMode
    {
        Anchor, // 위치 고정형 (에디터 배치 지점 주변을 멂)
        Free    // 자유 이동형 (지형을 피해 자유롭게 방랑함)
    }
    #endregion
}
