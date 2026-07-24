using UnityEngine;

/// <summary>
/// 적(상어) 이동/전투 컨트롤러입니다.
/// </summary>
[DisallowMultipleComponent]
public class CEnemyController : AFrameable, IUpdateFrameable
{
    // 애니메이션/상태 감지가 실패해도 돌진에 영원히 갇히지 않도록 하는 안전 상한(초).
    // 게임플레이용 지속 시간이 아니라 순수 잠금 방지용이므로 넉넉하게 둡니다.
    private const float DASH_SAFETY_SECONDS = 6f;

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("데이터")]
    [Tooltip("단독 배치/테스트용 적 SO ID. 스포너가 Initialize()로 주입하면 무시됩니다. 예: Id.Enemy_NormalShark")]
    [SerializeField] private string _enemyId = Id.Enemy_NormalShark;

    [Header("애니메이터")]
    [Tooltip("상어 Animator. 비우면 자식에서 자동으로 찾습니다.")]
    [SerializeField] private Animator _animator;
    [Tooltip("공격을 재생시킬 Trigger 파라미터 이름.")]
    [SerializeField] private string _attackTrigger = "Attack";
    [Tooltip("공격 상태(State)에 지정한 Tag. 이 Tag로 공격 재생 종료를 감지합니다.")]
    [SerializeField] private string _attackStateTag = "Attack";

    [Header("맵 경계 (배회 볼륨)")]
    [Tooltip("배회 볼륨의 중심. 비우면 월드 원점(0,0,0)을 사용합니다.")]
    [SerializeField] private Transform _spanwPoint;
    [Tooltip("배회 볼륨의 전체 크기(가로/세로/깊이). 이 박스 안에서 목적지를 뽑습니다.")]
    [SerializeField] private Vector3 _mapSize = new Vector3(60f, 20f, 60f);
    [Tooltip("경계 안쪽 이 거리부터 안으로 돌아오도록 서서히 조향을 시작합니다.")]
    [SerializeField, Min(0f)] private float _boundaryMargin = 6f;
    [Tooltip("경계를 벗어났을 때 안으로 되돌리는 조향 강도. 클수록 급하게 방향을 틉니다.")]
    [SerializeField, Min(0f)] private float _containmentStrength = 2f;

    [Header("기지(잠수함) 제외")]
    [Tooltip("고정 기지 참조.")]
    [SerializeField] private Transform _baseTransform;
    [Tooltip("기지를 찾을 태그입니다.")]
    [SerializeField, Min(0f)] private float _baseExcludeRadius = 12f;

    [Header("감지")]
    [Tooltip("플레이어 후보를 좁힐 레이어입니다. (성능용 1차 필터)")]
    [SerializeField] private LayerMask _playerLayer;
    [Tooltip("플레이어로 최종 확정할 태그입니다. 비우면 레이어만으로 판정합니다.")]
    [SerializeField] private string _playerTag = "Player";
    [Tooltip("켜면 시야 사이에 장애물이 있는지 레이캐스트로 확인해 벽 너머는 감지하지 않습니다.")]
    [SerializeField] private bool _useLineOfSight = true;
    [Tooltip("시야를 가로막는 장애물 레이어입니다. (가림 판정용)")]
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("충돌")]
    [Tooltip("장애물 충돌 판정에 쓰는 몸통 반경. 이동 시 이 반경의 구로 벽을 감지해 관통을 막습니다. 0이면 얇은 선으로 판정.")]
    [SerializeField, Min(0f)] private float _bodyRadius = 0.5f;

    [Header("회전")]
    [Tooltip("회전 부드러움/반응성. 클수록 빠르게 정렬(민첩), 작을수록 느긋하고 부드럽게 돕니다. 3~6 권장.")]
    [SerializeField, Min(0.01f)] private float _turnSharpness = 5f;
    [Tooltip("상하로 향할 수 있는 최대 각도(도). 이 값을 넘지 않으므로 수직 근처의 회전 뒤집힘을 방지합니다.")]
    [SerializeField, Range(0f, 89f)] private float _maxPitchAngle = 70f;
    [Tooltip("선회 시 몸을 도는 쪽으로 기울이는 최대 각도(도). 0이면 뱅킹 없음. 25~40 권장.")]
    [SerializeField, Range(0f, 80f)] private float _maxBankAngle = 35f;
    [Tooltip("방향 오차 대비 기울기 정도. 클수록 살짝만 틀어도 크게 기웁니다. 몸이 반대로 기울면 음수로 바꾸세요.")]
    [SerializeField] private float _bankScale = 1.5f;

    [Header("순찰")]
    [Tooltip("목적지에 이만큼 가까워지면 새 목적지를 뽑습니다.")]
    [SerializeField, Min(0.1f)] private float _arriveThreshold = 2f;
    [Tooltip("목적지에 도달하지 못해도 이 시간이 지나면 새 목적지를 뽑습니다. (끼임 방지)")]
    [SerializeField, Min(1f)] private float _repathInterval = 8f;

    [Header("돌진")]
    [Tooltip("돌진 중 플레이어에게 이 거리 안으로 닿으면 피해를 줍니다.")]
    [SerializeField, Min(0.1f)] private float _contactRange = 1.4f;

    [Header("회피 (순찰 / 도주)")]
    [Tooltip("감각 레이가 장애물을 감지하는 거리. 이 거리부터 벽/바닥에서 밀려나기 시작합니다.")]
    [SerializeField, Min(0f)] private float _avoidDistance = 6f;
    [Tooltip("장애물에서 밀려나는 척력의 세기. 클수록 목적지/도주 방향보다 장애물 회피를 우선합니다.")]
    [SerializeField, Min(0f)] private float _avoidStrength = 2f;
    [Tooltip("감각 레이 갱신 간격(초). 매 프레임 대신 이 간격으로만 다시 쏴 부하를 줄입니다. 0이면 매 프레임.")]
    [SerializeField, Min(0f)] private float _avoidRefreshInterval = 0.1f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    // CEnemySO 에서 캐시한 스탯
    private float _moveSpeed;
    private float _fieldOfView;   // 정면 시야 각(도, 전체 폭)
    private float _sightRange;    // 시야 사거리(거리)
    private float _flatDamage;    // 절댓값 피해량
    private float _ratioDamage;   // 비율 피해량(0~1)
    private float _dashSpeed;
    private float _dashWindup;
    private float _fleeSpeed;
    private float _fleeDuration;
    private float _attackCooldown;

    private int _attackTriggerHash;

    private Transform _base;              // 기지(잠수함)
    private Transform _target;            // 추적/공격 대상 플레이어
    private Vector3 _lastPlayerPos;       // 대상 소실 시 도주 방향 산출용

    private Vector3 _patrolDestination;   // 현재 순찰 목적지
    private Vector3 _dashDir;             // 돌진 커밋 방향(정규화)

    private float _stateTimer;            // Windup 잔여 시간 / Dash 안전 상한 / Flee 잔여 시간
    private float _repathTimer;           // 순찰 재추첨 타이머
    private float _nextAttackTime;        // 이 시각 이후에만 새 공격 시작 가능
    private bool _dashHitApplied;         // 이번 돌진에서 피해를 이미 줬는지
    private bool _attackStateEntered;     // 공격 애니메이션 상태에 진입했는지

    private Vector3 _cachedAvoidance;     // 감각 레이 결과 캐시
    private float _avoidRefreshTimer;     // 캐시 갱신 타이머

    private readonly Collider[] _overlapBuffer = new Collider[8];

    private EEnemyState _state = EEnemyState.Patrol;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    /// <summary>현재 적의 행동 상태입니다.</summary>
    public EEnemyState State => _state;

    /// <summary>상어의 스폰위치(배회 중심)를 외부에서 주입합니다. (보통은 인스펙터에서 직접 지정)</summary>
    /// <param name="spawnPoint"></param>
    public void SetSpawnPoint(Transform spawnPoint) => _spanwPoint = spawnPoint;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        float dt = Time.deltaTime;

        switch (_state)
        {
            case EEnemyState.Patrol: TickPatrol(dt); break;
            case EEnemyState.Windup: TickWindup(dt); break;
            case EEnemyState.Dash: TickDash(dt); break;
            case EEnemyState.Flee: TickFlee(dt); break;
        }

        // 폭주 방지를 위한 아주 넓은 안전 클램프 (정상적으론 조향이 먼저 되돌리므로 거의 닿지 않음)
        transform.position = SafetyClamp(transform.position);
    }
    #endregion

    #region ─────────────────────────▶ 상태: 순찰 ◀─────────────────────────
    private void TickPatrol(float dt)
    {
        // 재공격 쿨다운이 지났고 시야에 플레이어가 있으면 조준 시작
        if (Time.time >= _nextAttackTime && TryDetectPlayer(out Transform found))
        {
            _target = found;
            _lastPlayerPos = found.position;
            EnterWindup();
            return;
        }

        _repathTimer -= dt;
        MoveToward(_patrolDestination, _moveSpeed, dt);

        // 장애물은 척력으로 돌아가므로 막혔다고 목적지를 버리지 않습니다.
        // 목적지에 도착했거나, 오래 도달하지 못했을 때(끼임 방지)만 새로 뽑습니다.
        bool arrived = (_patrolDestination - transform.position).sqrMagnitude <= _arriveThreshold * _arriveThreshold;
        if (arrived || _repathTimer <= 0f)
        {
            PickNewPatrolDestination();
        }
    }
    #endregion

    #region ─────────────────────────▶ 상태: 조준 ◀─────────────────────────
    private void EnterWindup()
    {
        _state = EEnemyState.Windup;
        _stateTimer = _dashWindup;
    }

    private void TickWindup(float dt)
    {
        // 대상이 사라지면 순찰 복귀
        if (_target == null)
        {
            EnterPatrol();
            return;
        }

        _lastPlayerPos = _target.position;

        // 제자리에서 대상을 겨냥만 (이동 없음)
        FaceToward(_target.position, dt);

        _stateTimer -= dt;
        if (_stateTimer <= 0f)
        {
            EnterDash();
        }
    }
    #endregion

    #region ─────────────────────────▶ 상태: 돌진 ◀─────────────────────────
    private void EnterDash()
    {
        // 돌진 방향을 지금 확정(커밋)합니다. 이후 방향 전환 없이 직진합니다.
        Vector3 aimPos = _target != null ? _target.position : _lastPlayerPos;
        transform.rotation = ComputeLookRotation(aimPos - transform.position);
        _dashDir = transform.forward;

        _state = EEnemyState.Dash;
        _dashHitApplied = false;
        _attackStateEntered = false;
        _stateTimer = DASH_SAFETY_SECONDS; // 잠금 방지용 안전 상한

        // 공격 애니메이션 트리거
        if (_animator != null && _attackTriggerHash != 0)
        {
            _animator.SetTrigger(_attackTriggerHash);
        }
    }

    private void TickDash(float dt)
    {
        // 커밋한 방향으로 직진하되, 벽에 막히면 관통하지 않고 그 앞에서 멈춥니다.
        bool blocked = MoveClamped(_dashDir, _dashSpeed, dt);

        // 접촉 피해 (돌진 1회당 최대 1번)
        if (!_dashHitApplied && (_flatDamage > 0f || _ratioDamage > 0f) && TryGetPlayerWithin(_contactRange))
        {
            UPlayer.ApplyDamage(_flatDamage, _ratioDamage);
            _dashHitApplied = true;
            // TODO: 피격 사운드/이펙트 연동
        }

        // 종료 판정: 벽에 막혔거나 공격 애니메이션이 끝나면 도주로 전환
        _stateTimer -= dt;
        if (blocked || IsAttackAnimationFinished() || _stateTimer <= 0f)
        {
            EnterFlee();
        }
    }

    // 공격 애니메이션 상태에 진입한 뒤, 재생이 끝났는지 판정합니다.
    private bool IsAttackAnimationFinished()
    {
        // Animator 가 없으면 애니메이션으로 판정할 수 없으므로 안전 상한에 맡깁니다.
        if (_animator == null) return false;

        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);

        if (!_attackStateEntered)
        {
            // 트리거 후 전이가 끝나 공격 상태로 진입했는지 확인
            if (info.IsTag(_attackStateTag)) _attackStateEntered = true;
            return false;
        }

        // 공격 상태에서 밖으로 전이가 시작됐거나(다음 상태로 넘어감) 재생이 끝났으면 종료
        if (!info.IsTag(_attackStateTag)) return true;
        return !_animator.IsInTransition(0) && info.normalizedTime >= 1f;
    }
    #endregion

    #region ─────────────────────────▶ 상태: 도주 ◀─────────────────────────
    private void EnterFlee()
    {
        _state = EEnemyState.Flee;
        _stateTimer = _fleeDuration;
    }

    private void TickFlee(float dt)
    {
        // 기본 진행 방향: 플레이어(마지막 위치) 반대쪽
        Vector3 playerPos = _target != null ? _target.position : _lastPlayerPos;
        Vector3 away = transform.position - playerPos;
        away = away.sqrMagnitude > K.SMALL_DISTANCE ? away.normalized : -transform.forward;

        // 여기에 장애물 척력(감각 레이)을 더해 벽/바닥을 미리 피해 다른 경로로 흘러갑니다.
        Vector3 dir = away + Avoidance(dt) * _avoidStrength;
        dir = dir.sqrMagnitude > K.SMALL_DISTANCE ? dir.normalized : away;

        // 회전(뱅킹 포함) 후 전진. 척력이 못 피한 경우 MoveClamped 가 최종적으로 관통을 막습니다.
        Quaternion desired = ComputeLookRotation(dir) * Quaternion.AngleAxis(ComputeBankAngle(dir), Vector3.forward);
        RotateToward(desired, dt);
        MoveClamped(transform.forward, _fleeSpeed, dt);

        _stateTimer -= dt;
        if (_stateTimer <= 0f)
        {
            _nextAttackTime = Time.time + _attackCooldown; // 복귀 후 재공격 쿨다운 시작
            _target = null;
            EnterPatrol();
        }
    }

    // 감각 레이 결과를 _avoidRefreshInterval 간격으로만 다시 계산해 재사용합니다. (부하 절감)
    private Vector3 Avoidance(float dt)
    {
        _avoidRefreshTimer -= dt;
        if (_avoidRefreshTimer <= 0f)
        {
            _cachedAvoidance = ComputeAvoidance();
            _avoidRefreshTimer = _avoidRefreshInterval;
        }
        return _cachedAvoidance;
    }

    // 여러 방향으로 감각 레이(스피어캐스트)를 쏴서, 부딪힌 면의 법선을 가까울수록 크게 합산합니다.
    // 바닥(법선이 위)이면 위로, 벽(법선이 수평)이면 옆으로 밀어내는 척력이 됩니다.
    private Vector3 ComputeAvoidance()
    {
        if (_avoidDistance <= 0f) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        AddFeeler(ref sum, transform.forward);
        AddFeeler(ref sum, Quaternion.AngleAxis(35f, transform.up) * transform.forward);
        AddFeeler(ref sum, Quaternion.AngleAxis(-35f, transform.up) * transform.forward);
        AddFeeler(ref sum, Quaternion.AngleAxis(35f, transform.right) * transform.forward);
        AddFeeler(ref sum, Quaternion.AngleAxis(-35f, transform.right) * transform.forward);
        AddFeeler(ref sum, Vector3.down); // 바닥
        return sum;
    }

    // 한 방향으로 감각 레이를 쏴서 장애물을 만나면 그 면 법선을 거리 가중치로 누적합니다.
    private void AddFeeler(ref Vector3 sum, Vector3 dir)
    {
        if (Physics.SphereCast(transform.position, _bodyRadius, dir.normalized, out RaycastHit hit, _avoidDistance, _obstacleLayer))
        {
            float weight = 1f - hit.distance / _avoidDistance; // 가까울수록 1에 가까움
            sum += hit.normal * weight;
        }
    }
    #endregion

    #region ─────────────────────────▶ 상태 전이 보조 ◀─────────────────────────
    private void EnterPatrol()
    {
        _state = EEnemyState.Patrol;
        PickNewPatrolDestination();
    }
    #endregion

    #region ─────────────────────────▶ 이동 / 회전 ◀─────────────────────────
    // 목표를 향해 회전하면서 정면으로 전진합니다. (경계 복귀 조향 포함) 벽에 막히면 true 를 반환합니다.
    private bool MoveToward(Vector3 targetPos, float speed, float dt)
    {
        Vector3 dir = targetPos - transform.position;
        dir = dir.sqrMagnitude > K.SMALL_DISTANCE ? dir.normalized : transform.forward;

        // 경계 밖으로 나갈수록 안쪽으로 향하도록 방향을 섞습니다.
        dir += ContainmentBias() * _containmentStrength;

        // 장애물 척력: 목적지로 향하되 벽/바닥을 만나면 돌아서 계속 찾아갑니다.
        dir += Avoidance(dt) * _avoidStrength;

        if (dir.sqrMagnitude < K.SMALL_DISTANCE) dir = transform.forward; // 상쇄되면 현 방향 유지

        // 도는 쪽으로 몸을 기울여(뱅킹) 제자리 회전이 아닌 선회 느낌을 줍니다.
        Quaternion desired = ComputeLookRotation(dir) * Quaternion.AngleAxis(ComputeBankAngle(dir), Vector3.forward);
        RotateToward(desired, dt);
        return MoveClamped(transform.forward, speed, dt);
    }

    // 지정 방향으로 전진하되, _obstacleLayer 장애물을 스피어캐스트로 감지해 관통을 막습니다.
    // 벽에 막혀 그 앞에서 멈췄으면 true 를 반환합니다. (_obstacleLayer 가 비어 있으면 항상 통과)
    private bool MoveClamped(Vector3 dir, float speed, float dt)
    {
        float dist = speed * dt;
        if (dist <= 0f) return false;

        if (Physics.SphereCast(transform.position, _bodyRadius, dir, out RaycastHit hit, dist, _obstacleLayer))
        {
            float allowed = Mathf.Max(hit.distance - 0.01f, 0f); // 벽에 살짝 못 미치게
            transform.position += dir * allowed;
            return true;
        }

        transform.position += dir * dist;
        return false;
    }

    // 목표를 향해 회전만 합니다. (피치 클램프 적용, 이동/뱅킹 없음 — 제자리 조준용)
    private void FaceToward(Vector3 targetPos, float dt)
    {
        RotateToward(ComputeLookRotation(targetPos - transform.position), dt);
    }

    // 목표 회전으로 프레임률 독립 지수 보간(Slerp)합니다.
    // 멀 때 빠르게, 가까울수록 느리게 감속하며 자연스럽게 정렬합니다. _turnSharpness 가 클수록 빠르게 수렴합니다.
    private void RotateToward(Quaternion desired, float dt)
    {
        float t = 1f - Mathf.Exp(-_turnSharpness * dt);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, t);
    }

    // 현재 진행 방향과 목표 방향의 수평 좌/우 오차에 비례해 뱅킹(롤) 각을 계산합니다.
    // 정렬될수록 0으로 수렴하므로 선회가 끝나면 자연히 수평으로 돌아옵니다.
    private float ComputeBankAngle(Vector3 dir)
    {
        if (_maxBankAngle <= 0f) return 0f;

        Vector3 fwdFlat = new Vector3(transform.forward.x, 0f, transform.forward.z);
        Vector3 dirFlat = new Vector3(dir.x, 0f, dir.z);
        if (fwdFlat.sqrMagnitude < K.SMALL_DISTANCE || dirFlat.sqrMagnitude < K.SMALL_DISTANCE) return 0f;

        float signedYaw = Vector3.SignedAngle(fwdFlat, dirFlat, Vector3.up);
        return Mathf.Clamp(-signedYaw * _bankScale, -_maxBankAngle, _maxBankAngle);
    }

    /// <summary>
    /// 방향을 바라보는 회전을 계산하되, 피치 각을 _maxPitchAngle 로 먼저 제한합니다.
    /// 방향이 수직에 가까워지지 않으므로 LookRotation 의 up 기준(Vector3.up)이 무너지지 않아
    /// 급강하/급상승에서도 롤 뒤집힘이 발생하지 않습니다.
    /// </summary>
    private Quaternion ComputeLookRotation(Vector3 direction)
    {
        Vector3 flat = new Vector3(direction.x, 0f, direction.z);
        float flatDist = flat.magnitude;

        Vector3 flatDir;
        if (flatDist < K.SMALL_DISTANCE)
        {
            // 거의 바로 위/아래면 현재 수평 진행 방향 유지
            flatDir = new Vector3(transform.forward.x, 0f, transform.forward.z);
            if (flatDir.sqrMagnitude < K.SMALL_DISTANCE) flatDir = Vector3.forward;
            flatDir.Normalize();
            flatDist = 0f;
        }
        else
        {
            flatDir = flat / flatDist;
        }

        float pitch = Mathf.Atan2(direction.y, Mathf.Max(flatDist, K.SMALL_DISTANCE)) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, -_maxPitchAngle, _maxPitchAngle);

        float verticalPerHorizontal = Mathf.Tan(pitch * Mathf.Deg2Rad);
        Vector3 dir = (flatDir + Vector3.up * verticalPerHorizontal).normalized;

        return Quaternion.LookRotation(dir, Vector3.up);
    }
    #endregion

    #region ─────────────────────────▶ 감지 ◀─────────────────────────
    // 정면 시야 콘(사거리 _sightRange + 각도 _fieldOfView) 안에서, 시야가 막히지 않은
    // 가장 가까운 플레이어를 찾습니다.
    private bool TryDetectPlayer(out Transform player)
    {
        player = null;
        if (_sightRange <= 0f || _fieldOfView <= 0f) return false;

        int count = Physics.OverlapSphereNonAlloc(transform.position, _sightRange, _overlapBuffer, _playerLayer);
        float halfAngle = _fieldOfView * 0.5f;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < count; ++i)
        {
            Collider col = _overlapBuffer[i];
            if (col == null) continue;

            // 태그로 플레이어 최종 확정 (레이어로 좁힌 후 2차 필터)
            if (!IsPlayer(col)) continue;

            Vector3 to = col.transform.position - transform.position;
            float sqr = to.sqrMagnitude;
            if (sqr >= bestSqr) continue;

            // 정면 시야 각 안에 있는가
            if (Vector3.Angle(transform.forward, to) > halfAngle) continue;

            // 시야가 장애물에 막히지 않았는가 (레이캐스트 가림 판정)
            if (_useLineOfSight && IsSightBlocked(to)) continue;

            bestSqr = sqr;
            player = col.transform;
        }
        return player != null;
    }

    // 자신 → 대상 사이에 장애물이 있으면 true.
    private bool IsSightBlocked(Vector3 toTarget)
    {
        float dist = toTarget.magnitude;
        if (dist < K.SMALL_DISTANCE) return false;
        return Physics.Raycast(transform.position, toTarget / dist, dist, _obstacleLayer);
    }

    // 지정 거리 안에 플레이어가 있는지 검사합니다. (돌진 접촉 판정 — 방향 무관)
    private bool TryGetPlayerWithin(float range)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, range, _overlapBuffer, _playerLayer);
        for (int i = 0; i < count; ++i)
        {
            if (IsPlayer(_overlapBuffer[i])) return true;
        }
        return false;
    }

    // 콜라이더가 플레이어인지 태그로 확정합니다. (_playerTag 가 비어 있으면 레이어 통과만으로 인정)
    private bool IsPlayer(Collider col)
    {
        if (col == null) return false;
        if (_playerTag.IsBlank()) return true;
        return col.CompareTag(_playerTag);
    }
    #endregion

    #region ─────────────────────────▶ 순찰 목적지 / 경계 ◀─────────────────────────
    private Vector3 MapCenter => _spanwPoint != null ? _spanwPoint.position : Vector3.zero;

    // 배회 볼륨 안에서 기지 반경을 피해 랜덤 목적지를 뽑습니다.
    private void PickNewPatrolDestination()
    {
        _repathTimer = _repathInterval;

        Vector3 center = MapCenter;
        Vector3 half = _mapSize * 0.5f;
        float excludeSqr = _baseExcludeRadius * _baseExcludeRadius;

        // 기지 반경 밖 지점이 나올 때까지 재추첨 (최대 시도 횟수 제한)
        for (int i = 0; i < 16; ++i)
        {
            Vector3 candidate = center + new Vector3(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y),
                Random.Range(-half.z, half.z));

            if (_base == null || (candidate - _base.position).sqrMagnitude >= excludeSqr)
            {
                _patrolDestination = candidate;
                return;
            }
        }

        // 계속 실패하면(경계가 기지에 다 잡힌 경우 등) 중심이라도 사용
        _patrolDestination = center;
    }

    // 경계 밖으로 나간 정도에 비례해 안쪽으로 향하는 방향(정규화 안 됨)을 돌려줍니다.
    // 여유(margin) 안쪽에선 0, 경계에 가까워질수록 커지고, 완전히 벗어나면 1을 넘어 강하게 당깁니다.
    private Vector3 ContainmentBias()
    {
        Vector3 c = MapCenter;
        Vector3 half = _mapSize * 0.5f;
        return new Vector3(
            AxisBias(transform.position.x, c.x, half.x),
            AxisBias(transform.position.y, c.y, half.y),
            AxisBias(transform.position.z, c.z, half.z));
    }

    private float AxisBias(float p, float center, float half)
    {
        float margin = Mathf.Max(_boundaryMargin, 0.001f);
        float max = center + half;
        float min = center - half;

        if (p > max - margin) return -(p - (max - margin)) / margin; // 안쪽(-)으로
        if (p < min + margin) return ((min + margin) - p) / margin;   // 안쪽(+)으로
        return 0f;
    }

    // 폭주 방지용 넓은 안전 클램프 (경계 + 여유의 몇 배). 조향이 정상 동작하면 거의 닿지 않습니다.
    private Vector3 SafetyClamp(Vector3 p)
    {
        Vector3 c = MapCenter;
        Vector3 half = _mapSize * 0.5f + Vector3.one * (_boundaryMargin * 3f);
        p.x = Mathf.Clamp(p.x, c.x - half.x, c.x + half.x);
        p.y = Mathf.Clamp(p.y, c.y - half.y, c.y + half.y);
        p.z = Mathf.Clamp(p.z, c.z - half.z, c.z + half.z);
        return p;
    }
    #endregion

    #region ─────────────────────────▶ 초기화 ◀─────────────────────────
    private void ApplyStats(CEnemySO so)
    {
        if (so == null)
        {
            UDebug.Print("적 SO가 null 입니다. 스탯을 적용할 수 없습니다.", LogType.Error, gameObject);
            return;
        }

        _moveSpeed = so.MoveSpeed;
        _fieldOfView = so.FieldOfView;
        _sightRange = so.SightRange;
        _flatDamage = so.FlatDamage;
        _ratioDamage = so.RatioDamage;
        _dashSpeed = so.DashSpeed;
        _dashWindup = so.DashWindup;
        _fleeSpeed = so.FleeSpeed;
        _fleeDuration = so.FleeDuration;
        _attackCooldown = so.AttackCooldown;
    }

    // Animator 참조 확보 및 트리거 해시 계산.
    private void ResolveAnimator()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        _attackTriggerHash = _attackTrigger.IsBlank() ? 0 : Animator.StringToHash(_attackTrigger);

        if (_animator == null)
        {
            UDebug.Print("Animator를 찾지 못했습니다. 공격 애니메이션 없이 안전 상한으로 돌진이 종료됩니다.", LogType.Warning, gameObject);
        }
    }

    // 기지 참조 확보: override 우선, 없으면 태그 검색.
    private void ResolveBase()
    {
        if (_baseTransform != null)
        {
            _base = _baseTransform;
            return;
        }
        else
        {
            UDebug.Print("_baseTransform 비어있음, 참조 확인", LogType.Error);
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        // 스탯 주입이 없었다면 _enemyId 로 DB 조회
        if (_moveSpeed <= 0f && !_enemyId.IsBlank())
        {
            ApplyStats(UData.Enemy(_enemyId));
        }

        ResolveAnimator();
        ResolveBase();
        EnterPatrol();

        base.OnEnable(); // 프레임 매니저 등록
    }

#if UNITY_EDITOR
    // 씬뷰 디버그: 배회 볼륨(흰색), 조향 시작 경계(회색), 기지 제외 반경(파랑), 시야(노랑), 접촉 범위(빨강)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(MapCenter, _mapSize);

        Vector3 inner = _mapSize - Vector3.one * (_boundaryMargin * 2f);
        if (inner.x > 0f && inner.y > 0f && inner.z > 0f)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(MapCenter, inner);
        }

        if (_base != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_base.position, _baseExcludeRadius);
        }

        float range = Application.isPlaying ? _sightRange : 0f;
        if (range > 0f)
        {
            // 정면 시야 콘: 중심선 + 상/하/좌/우 가장자리
            Gizmos.color = Color.yellow;
            float half = _fieldOfView * 0.5f;
            Vector3 origin = transform.position;
            Gizmos.DrawRay(origin, transform.forward * range);
            foreach (float ay in new[] { -half, half })
            {
                Gizmos.DrawRay(origin, Quaternion.AngleAxis(ay, transform.up) * transform.forward * range);
                Gizmos.DrawRay(origin, Quaternion.AngleAxis(ay, transform.right) * transform.forward * range);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _contactRange);
    }
#endif
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    /// <summary>적의 행동 상태입니다. (필요 시 EEnemyState.cs 로 분리 권장)</summary>
    public enum EEnemyState : byte
    {
        Patrol = 0,
        Windup = 1,
        Dash = 2,
        Flee = 3,
    }
    #endregion
}
