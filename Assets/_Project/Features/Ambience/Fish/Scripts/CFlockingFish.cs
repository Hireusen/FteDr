using UnityEngine;

/// <summary>
/// 군집 이동(Flocking) 행동을 하는 개별 물고기 클래스입니다.
/// 응집(Cohesion)·분리(Separation)·정렬(Alignment) 3규칙에
/// 경계 복귀 힘, Y축 소프트 클램프, 속도/회전 관성, 개체별 노이즈를 더해
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
    [Tooltip("타겟 기준 수직(Y) 허용 반경. 이 값을 벗어나려 하면 되돌리는 힘이 작용합니다.")]
    [SerializeField, Min(0.1f)] private float _verticalRange = 4f;
    [Tooltip("경계 복귀 힘의 세기입니다.")]
    [SerializeField] private float _boundaryWeight = 2.0f;
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
    }

    public void ExecuteUpdateFrame()
    {
        if (_flock == null || _flock.Target == null)
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
        Vector3 targetPos = _flock.Target.position;

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
            float dist = offset.magnitude;
            if (dist > _neighborDistance) continue;

            cohesion += other.transform.position;
            alignment += other.transform.forward;
            neighborSpeedSum += other.CurrentSpeed;
            groupSize++;

            if (dist < _separationDistance && dist > K.SMALL_DISTANCE)
            {
                // 가까울수록 강하게 밀어냅니다(거리 반비례).
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

        // 타겟(무리 중심)으로의 유도. 멀수록 강하게 당겨 무리에서 이탈하지 않도록 합니다.
        Vector3 toTarget = targetPos - selfPos;
        steer += toTarget.normalized * _targetWeight;

        // ── 구형 경계 복귀: 타겟에서 수평으로 너무 멀어지면 되돌립니다. ──
        float planarDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        if (planarDist > _flock.WanderSize)
        {
            float over = (planarDist - _flock.WanderSize) / _flock.WanderSize;
            steer += toTarget.normalized * (_boundaryWeight * over);
        }

        // ── Y축 소프트 클램프: 상/하한 접근 시 반대 방향으로 밀어냅니다. ──
        float dy = selfPos.y - targetPos.y;
        if (Mathf.Abs(dy) > _verticalRange)
        {
            float overY = (Mathf.Abs(dy) - _verticalRange) / _verticalRange;
            steer += Vector3.down * Mathf.Sign(dy) * (_boundaryWeight * overY);
        }

        if (steer.sqrMagnitude > K.SMALL_DISTANCE)
        {
            _heading = Vector3.Slerp(_heading, steer.normalized, 1f).normalized;
        }
    }

    /// <summary>관성 있는 회전·가감속과 유영 노이즈를 적용해 실제로 전진시킵니다.</summary>
    private void ApplyMovement()
    {
        // 속도 관성: 목표속도로 부드럽게 수렴.
        _speed = Mathf.Lerp(_speed, _targetSpeed, _speedLerp * Time.deltaTime);

        // 좌우 미세 흔들림(개체별 위상차) → 살아있는 헤엄 느낌.
        _phase += _wiggleFrequency * Time.deltaTime;
        Vector3 wiggle = transform.right * (Mathf.Sin(_phase) * _wiggleAmplitude);

        Vector3 desiredDir = (_heading + wiggle * 0.15f);
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
