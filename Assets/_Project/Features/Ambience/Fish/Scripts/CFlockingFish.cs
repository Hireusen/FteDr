using UnityEngine;

/// <summary>
/// 군집 이동(Flocking) 행동을 하는 개별 물고기 클래스입니다.
/// 응집(Cohesion)·분리(Separation)·정렬(Alignment) 3규칙에
/// 월드 좌표 경계 박스(AABB) 복귀 힘, 속도/회전 관성, 개체별 노이즈를 더해
/// 자연스러운 유영을 표현합니다.
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
    private float _speed;
    private float _targetSpeed;
    private float _phase;              // 개체별 노이즈 위상차
    private Vector3 _heading;          // 현재 진행 방향(정규화)
    private CFlockingGroup _flock;

    // 순회 중 매번 곱하지 않도록 제곱값을 미리 캐싱합니다.
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
    private void Start()
    {
        _targetSpeed = Random.Range(0.5f, 1.5f) * _averageSpeed;
        _speed = _targetSpeed;
        _phase = Random.Range(0f, Mathf.PI * 2f);
        _heading = transform.forward;

        _neighborDistanceSqr = _neighborDistance * _neighborDistance;
        _separationDistanceSqr = _separationDistance * _separationDistance;
    }

    public void ExecuteUpdateFrame()
    {
        if (_flock == null)
        {
            transform.Translate(0f, 0f, Time.deltaTime * _speed);
            return;
        }

        // 성능 분산: 매 프레임 전체 이웃 순회 대신 확률적으로 조향을 갱신합니다.
        // (조향을 건너뛰는 프레임에도 이동/노이즈는 계속 적용되어 끊김이 없습니다.)
        if (Random.Range(0, _performance + 1) < 1)
        {
            UpdateHeading();
        }

        ApplyMovement();
    }

    /// <summary>보이드 규칙 + 경계 + 타겟을 종합해 진행 방향과 목표 속도를 갱신합니다.</summary>
    private void UpdateHeading()
    {
        Vector3 selfPos = transform.position;

        var allFish = _flock.AllFish;
        int fishCount = allFish.Count;

        Vector3 cohesion = Vector3.zero;      // 이웃 중심으로 모임
        Vector3 separation = Vector3.zero;    // 너무 가까운 이웃 회피
        Vector3 alignment = Vector3.zero;     // 이웃 방향에 정렬
        float neighborSpeedSum = 0f;
        int groupSize = 0;

        for (int i = 0; i < fishCount; ++i)
        {
            CFlockingFish other = allFish[i];
            if (other == this || other == null) continue;

            Vector3 offset = other.transform.position - selfPos;
            // 제곱 거리로 비교하여 매 순회의 sqrt(거리 연산) 비용을 제거합니다.
            float sqrDist = offset.sqrMagnitude;
            if (sqrDist > _neighborDistanceSqr) continue;

            cohesion += other.transform.position;
            alignment += other.transform.forward;
            neighborSpeedSum += other.CurrentSpeed;
            groupSize++;

            if (sqrDist < _separationDistanceSqr && sqrDist > K.SMALL_DISTANCE)
            {
                // 가까울수록 강하게 밀어냅니다(거리 반비례). 정규화는 여기서 한 번만 수행.
                float dist = Mathf.Sqrt(sqrDist);
                separation += (-offset / dist) / dist;
            }
        }

        Vector3 steer = Vector3.zero;

        if (groupSize > 0)
        {
            cohesion = (cohesion / groupSize) - selfPos;
            alignment /= groupSize;

            steer += cohesion.normalized * _cohesionWeight;
            steer += separation * _separationWeight;
            steer += alignment.normalized * _alignmentWeight;

            // 이웃 평균 속도로 목표속도를 부드럽게 맞춰 무리의 페이스를 공유합니다.
            _targetSpeed = neighborSpeedSum / groupSize;
        }
        else
        {
            // 이웃이 없으면 개별 난수 속도로 방랑합니다.
            _targetSpeed = Random.Range(0.5f, 1.5f) * _averageSpeed;
        }

        // 타겟(무리 중심)으로의 유도. Free 모드에서는 타겟이 자유롭게 흘러가므로
        // 물고기는 이 힘으로 무리를 따라가되, 경계 박스가 최종 이탈을 막습니다.
        if (_flock.Target != null)
        {
            Vector3 toTarget = _flock.Target.position - selfPos;
            steer += toTarget.normalized * _targetWeight;
        }

        // 월드 좌표 경계 박스(AABB) 복귀 힘.
        steer += ComputeBoundarySteer(selfPos);

        if (steer.sqrMagnitude > K.SMALL_DISTANCE)
        {
            _heading = steer.normalized;
        }
    }

    /// <summary>
    /// 축별로 경계에 가까워질수록 안쪽으로 밀어내는 힘을 산출합니다(경계 반사).
    /// 난수 없이 결정론적으로 동작하며, 각 축 독립적으로 계산해 모서리에서도 안정적입니다.
    /// </summary>
    private Vector3 ComputeBoundarySteer(Vector3 pos)
    {
        Vector3 min = _flock.BoundsMin;
        Vector3 max = _flock.BoundsMax;
        Vector3 steer = Vector3.zero;

        steer.x = AxisSteer(pos.x, min.x, max.x);
        steer.y = AxisSteer(pos.y, min.y, max.y);
        steer.z = AxisSteer(pos.z, min.z, max.z);

        return steer * _boundaryWeight;
    }

    /// <summary>단일 축에서 경계 여백 안으로 들어온 정도에 비례한 복귀 성분을 반환합니다.</summary>
    private float AxisSteer(float v, float lo, float hi)
    {
        if (v < lo + _boundaryMargin)
        {
            // 하한 여백 안: 안쪽(+)으로. 이미 벗어났으면 1 이상으로 강하게.
            return Mathf.Clamp01((lo + _boundaryMargin - v) / _boundaryMargin)
                   + Mathf.Max(0f, lo - v) / _boundaryMargin;
        }
        if (v > hi - _boundaryMargin)
        {
            // 상한 여백 안: 안쪽(-)으로.
            return -(Mathf.Clamp01((v - (hi - _boundaryMargin)) / _boundaryMargin)
                     + Mathf.Max(0f, v - hi) / _boundaryMargin);
        }
        return 0f;
    }

    /// <summary>관성 있는 회전·가감속과 유영 노이즈를 적용해 실제로 전진시킵니다.</summary>
    private void ApplyMovement()
    {
        _speed = Mathf.Lerp(_speed, _targetSpeed, _speedLerp * Time.deltaTime);

        _phase += _wiggleFrequency * Time.deltaTime;
        Vector3 wiggle = transform.right * (Mathf.Sin(_phase) * _wiggleAmplitude);

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

        transform.Translate(0f, 0f, Time.deltaTime * _speed);
    }
    #endregion
}
