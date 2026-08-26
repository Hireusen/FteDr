using UnityEngine;

/// <summary>
/// 지형을 회피하며 물고기 군집의 타겟(목표점)을 안전한 위치로 이동시킵니다.
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
    private float _timer;              // 다음 목표점 갱신까지 남은 시간
    private bool _isInitialized;       // 외부 데이터 초기화 완료 여부 플래그
    private CFlockingGroup _flock;     // 이동 경계를 가져오기 위한 군집 참조

    // 타겟 관련 좌표 및 캐싱 트랜스폼
    private Transform _targetTransform;
    private Vector3 _targetPosition;
    private Vector3 _originalPosition; // Anchor 모드 시 기준이 되는 스폰 원점
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    /// <summary>타겟이 한 스텝에 이동할 수 있는 최대 반경입니다.</summary>
    public float MoveRange => _moveRange;
    /// <summary>현재 적용 중인 이동 모드입니다.</summary>
    public EMoveMode MoveMode => _moveMode;

    /// <summary>
    /// 기즈모용 기준점입니다. Anchor 모드는 스폰 원점을, Free 모드는 타겟 현재 위치를 반환합니다.
    /// </summary>
    public Vector3 GizmoBasePosition
    {
        get
        {
            if (_moveMode == EMoveMode.Free && _targetTransform != null)
                return _targetTransform.position;
            return Application.isPlaying ? _originalPosition : transform.position;
        }
    }
    #endregion

    #region ─────────────────────────▶ 초기화 ◀─────────────────────────
    /// <summary>외부 스크립트에서 이동 반경과 속도를 설정하여 컴포넌트를 초기화합니다.</summary>
    public void Initialize(float moveRange, float moveSpeed, Vector2 positionChangeSpeed)
    {
        // 전달받은 이동 관련 설정 덮어쓰기
        _moveRange = moveRange;
        _moveSpeed = moveSpeed;
        _positionChangeSpeed = positionChangeSpeed;
        _isInitialized = true;

        // 첫 목표점 갱신 타이머 시작
        _timer = Random.Range(_positionChangeSpeed.x, _positionChangeSpeed.y);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>생성 시 타겟 트랜스폼 및 군집 참조를 가져와 캐싱합니다.</summary>
    private void Awake()
    {
        // 초기 기준점 및 소속 군집 할당
        _originalPosition = transform.position;
        _flock = GetComponent<CFlockingGroup>();

        // 하위에 할당된 타겟 트랜스폼 검색 및 캐싱
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

    /// <summary>매 프레임 타겟을 목표점으로 이동시키거나, 타이머 소진 시 새 목적지를 탐색합니다.</summary>
    public void ExecuteUpdateFrame()
    {
        if (_targetTransform == null) return;

        if (_timer >= 0f)
        {
            // 타이머 차감 및 타겟 위치를 향해 스무스 이동
            _timer -= Time.deltaTime;
            _targetTransform.position = Vector3.MoveTowards(
                _targetTransform.position,
                _targetPosition,
                _moveSpeed * Time.deltaTime
            );
        }
        else
        {
            // 타이머 리셋 및 안전한 다음 경로 계산
            float minInterval = _isInitialized ? _positionChangeSpeed.x : 3f;
            float maxInterval = _isInitialized ? _positionChangeSpeed.y : 8f;
            _timer = Random.Range(minInterval, maxInterval);

            _targetPosition = GetNextSafeTargetPosition();
        }
    }

    /// <summary>물리 레이캐스트를 활용하여 지형에 막히지 않는 새 목적지 좌표를 반환합니다.</summary>
    private Vector3 GetNextSafeTargetPosition()
    {
        // 이동 모드에 따른 탐색 기준점 설정
        Vector3 basePos = _moveMode == EMoveMode.Anchor ? _originalPosition : _targetTransform.position;
        Vector3 candidatePos = basePos;
        bool pathIsClear = false;
        const int MAX_RETRIES = 8; // 무한루프 및 연산 스파이크 방지 제한

        // 최대 허용 횟수만큼 난수 목적지를 찍고 검사 반복
        for (int i = 0; i < MAX_RETRIES; ++i)
        {
            // 새 목적지 후보 생성 후 경계 제한
            Vector3 randomOffset = Random.insideUnitSphere * _moveRange;
            candidatePos = ClampToBounds(basePos + randomOffset);

            // 현재 위치에서 후보지까지의 방향 및 거리 계산
            Vector3 origin = _targetTransform.position;
            Vector3 direction = candidatePos - origin;
            float distance = direction.magnitude;

            // 이동 거리가 매우 짧으면 즉시 통과
            if (distance < K.SMALL_DISTANCE)
            {
                pathIsClear = true;
                break;
            }

            direction.Normalize();

            // 구체형 레이캐스트를 쏘아 중간에 지형이 겹치는지 체크
            if (!Physics.SphereCast(origin, _avoidanceRadius, direction, out RaycastHit hit, distance, _terrainLayer))
            {
                pathIsClear = true;
                break; // 막힌 곳이 없다면 탐색 성공
            }
        }

        // 반복 검사에도 막혔다면, 강제로 뒤로 후퇴하는 위치 지정
        if (!pathIsClear)
        {
            candidatePos = ClampToBounds(
                _targetTransform.position - _targetTransform.forward * (_moveRange * 0.5f));
        }

        return candidatePos;
    }

    /// <summary>좌표를 검사해 군집의 경계 박스를 벗어나지 않게 클램프합니다.</summary>
    private Vector3 ClampToBounds(Vector3 pos)
    {
        if (_flock == null) return pos;

        // X, Y, Z 각각에 대해 한계 경계 제한 적용
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
