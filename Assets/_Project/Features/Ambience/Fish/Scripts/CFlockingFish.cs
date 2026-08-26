using UnityEngine;

/// <summary>
/// 군집(Flocking) 알고리즘을 기반으로 개별 물고기의 유영 행동을 제어합니다.
/// </summary>
public sealed class CFlockingFish : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("이동 설정")]
    [SerializeField] private float _averageSpeed = 2f;
    [SerializeField, Min(1)] private int _performance = 4;

    [Header("보이드 가중치")]
    [SerializeField] private float _cohesionWeight = 1.0f;
    [SerializeField] private float _separationWeight = 1.5f;
    [SerializeField] private float _alignmentWeight = 0.8f;
    [SerializeField] private float _targetWeight = 0.6f;

    [Header("감지 반경")]
    [SerializeField, Min(0.1f)] private float _neighborDistance = 3.0f;
    [SerializeField, Min(0.05f)] private float _separationDistance = 0.75f;

    [Header("경계 · 관성")]
    [Tooltip("경계 박스 안쪽으로 되돌리는 힘의 세기입니다. 경계는 소속 군집(CFlockingGroup)에서 설정합니다.")]
    [SerializeField] private float _boundaryWeight = 2.0f;
    [Tooltip("경계에 이 거리 이내로 접근하면 되돌리는 힘이 서서히 커집니다.")]
    [SerializeField, Min(0.1f)] private float _boundaryMargin = 8f;
    [Tooltip("회전 관성. 낮을수록 부드럽게(느리게) 방향을 틉니다.")]
    [SerializeField, Range(0.5f, 12f)] private float _turnSpeed = 3f;
    [Tooltip("속도 변화 관성. 낮을수록 부드럽게 가감속합니다.")]
    [SerializeField, Range(0.2f, 8f)] private float _speedLerp = 2f;

    [Header("유영 노이즈")]
    [Tooltip("좌우로 미세하게 흔들리는 헤엄 진폭입니다.")]
    [SerializeField] private float _wiggleAmplitude = 0.4f;
    [SerializeField] private float _wiggleFrequency = 2.5f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private int _fishIndex;            // 프레임 연산 분산용 고유 식별 인덱스
    private CFlockingGroup _flock;     // 소속 군집 참조
    private Vector3 _heading;          // 현재 바라보는 진행 방향 (정규화 벡터)

    // 속도 및 노이즈 관련 변수
    private float _speed;              // 현재 유영 속도
    private float _targetSpeed;        // 도달하고자 하는 목표 속도
    private float _phase;              // 개체별 흔들림(노이즈) 위상차

    // 연산 최적화용 캐싱 변수 (제곱 거리)
    private float _neighborDistanceSqr;
    private float _separationDistanceSqr;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public CFlockingGroup Flock
    {
        get => _flock;
        set => _flock = value;
    }

    /// <summary>개체의 고유 인덱스를 설정합니다. (프레임 연산 분산용)</summary>
    public int FishIndex
    {
        set => _fishIndex = value;
    }

    /// <summary>외부 초기화 시 할당받을 평균 속도 프로퍼티입니다.</summary>
    public float AverageSpeed
    {
        get => _averageSpeed;
        set => _averageSpeed = value;
    }

    /// <summary>이웃 계산용으로 노출하는 현재 속도값입니다.</summary>
    public float CurrentSpeed => _speed;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>시작 시 기본 속도, 방향 및 최적화 변수들을 초기화합니다.</summary>
    private void Start()
    {
        // 초기 이동 속도 및 노이즈 위상 난수 설정
        _targetSpeed = Random.Range(0.5f, 1.5f) * _averageSpeed;
        _speed = _targetSpeed;
        _phase = Random.Range(0f, Mathf.PI * 2f);
        _heading = transform.forward;

        // 매 프레임 연산을 줄이기 위한 제곱 거리 캐싱
        _neighborDistanceSqr = _neighborDistance * _neighborDistance;
        _separationDistanceSqr = _separationDistance * _separationDistance;
    }

    /// <summary>매 프레임마다 물고기의 조향 및 이동을 처리합니다.</summary>
    public void ExecuteUpdateFrame()
    {
        // 소속 군집이 없다면 로직 없이 직진만 수행
        if (_flock == null)
        {
            transform.Translate(0f, 0f, Time.deltaTime * _speed);
            return;
        }

        // 성능 분산: 프레임과 고유 인덱스를 활용하여 주기적으로만 조향 갱신 (비용 높은 Random 제거)
        if ((Time.frameCount + _fishIndex) % _performance == 0)
        {
            UpdateHeading();
        }

        // 결정된 목표 수치를 바탕으로 실제 이동 적용
        ApplyMovement();
    }

    /// <summary>보이드 규칙, 경계, 타겟을 종합하여 진행 방향과 목표 속도를 계산합니다.</summary>
    private void UpdateHeading()
    {
        // 이웃 순회를 위한 기본 변수 세팅
        Vector3 selfPos = transform.position;
        var allFish = _flock.AllFish;
        int fishCount = allFish.Count;

        Vector3 cohesion = Vector3.zero;
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        float neighborSpeedSum = 0f;
        int groupSize = 0;

        // 전체 이웃을 순회하며 군집 규칙(응집, 분리, 정렬) 요소 누적
        for (int i = 0; i < fishCount; ++i)
        {
            CFlockingFish other = allFish[i];
            if (other == this || other == null) continue;

            // 제곱 거리 비교를 통한 연산 최적화
            Vector3 offset = other.transform.position - selfPos;
            float sqrDist = offset.sqrMagnitude;
            if (sqrDist > _neighborDistanceSqr) continue;

            // 응집 및 정렬 성분 누적
            cohesion += other.transform.position;
            alignment += other.transform.forward;
            neighborSpeedSum += other.CurrentSpeed;
            groupSize++;

            // 분리 거리 이내일 경우 밀어내는 힘 누적
            if (sqrDist < _separationDistanceSqr && sqrDist > K.SMALL_DISTANCE)
            {
                float dist = Mathf.Sqrt(sqrDist);
                separation += (-offset / dist) / dist;
            }
        }

        Vector3 steer = Vector3.zero;

        // 주변 이웃 유무에 따른 조향 및 속도 결정
        if (groupSize > 0)
        {
            // 누적된 힘의 평균을 내고 가중치 적용
            cohesion = (cohesion / groupSize) - selfPos;
            alignment /= groupSize;

            steer += cohesion.normalized * _cohesionWeight;
            steer += separation * _separationWeight;
            steer += alignment.normalized * _alignmentWeight;

            // 무리의 평균 속도로 동기화
            _targetSpeed = neighborSpeedSum / groupSize;
        }
        else
        {
            // 이웃이 없으면 개별 난수 속도로 방랑
            _targetSpeed = Random.Range(0.5f, 1.5f) * _averageSpeed;
        }

        // 타겟(무리 중심)을 향한 유도력 추가
        if (_flock.Target != null)
        {
            Vector3 toTarget = _flock.Target.position - selfPos;
            steer += toTarget.normalized * _targetWeight;
        }

        // AABB 경계 박스 복귀 힘 추가
        steer += ComputeBoundarySteer(selfPos);

        // 유효한 조향력이 있다면 최종 진행 방향 갱신
        if (steer.sqrMagnitude > K.SMALL_DISTANCE)
        {
            _heading = steer.normalized;
        }
    }

    /// <summary>경계 박스에 가까워질수록 안쪽으로 반사시키는 조향력을 계산합니다.</summary>
    private Vector3 ComputeBoundarySteer(Vector3 pos)
    {
        // 각 축(X, Y, Z)별로 독립적인 경계 복귀 힘 산출
        Vector3 min = _flock.BoundsMin;
        Vector3 max = _flock.BoundsMax;
        Vector3 steer = Vector3.zero;

        steer.x = AxisSteer(pos.x, min.x, max.x);
        steer.y = AxisSteer(pos.y, min.y, max.y);
        steer.z = AxisSteer(pos.z, min.z, max.z);

        return steer * _boundaryWeight;
    }

    /// <summary>단일 축에서 경계 여백 침범 정도에 비례한 반환값을 구합니다.</summary>
    private float AxisSteer(float v, float lo, float hi)
    {
        // 하한선 침범 시 안쪽(+)으로 밀어냄
        if (v < lo + _boundaryMargin)
        {
            return Mathf.Clamp01((lo + _boundaryMargin - v) / _boundaryMargin)
                   + Mathf.Max(0f, lo - v) / _boundaryMargin;
        }
        // 상한선 침범 시 안쪽(-)으로 밀어냄
        if (v > hi - _boundaryMargin)
        {
            return -(Mathf.Clamp01((v - (hi - _boundaryMargin)) / _boundaryMargin)
                     + Mathf.Max(0f, v - hi) / _boundaryMargin);
        }
        return 0f;
    }

    /// <summary>결정된 조향 방향과 속도로 회전 보간 및 전진 처리를 수행합니다.</summary>
    private void ApplyMovement()
    {
        // 목표 속도로 부드럽게 가감속 보간
        _speed = Mathf.Lerp(_speed, _targetSpeed, _speedLerp * Time.deltaTime);

        // 사인파 기반 좌우 유영 노이즈(Wiggle) 산출
        _phase += _wiggleFrequency * Time.deltaTime;
        Vector3 wiggle = transform.right * (Mathf.Sin(_phase) * _wiggleAmplitude);

        // 노이즈가 더해진 최종 방향으로 트랜스폼 회전
        Vector3 desiredDir = _heading + wiggle * 0.15f;
        if (desiredDir.sqrMagnitude > K.SMALL_DISTANCE)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                _turnSpeed * Time.deltaTime
            );
        }

        // 로컬 Z축을 향해 전진
        transform.Translate(0f, 0f, Time.deltaTime * _speed);
    }
    #endregion
}
