using UnityEngine;

/// <summary>
/// 플레이어 탐지기(레이더)입니다.
/// 주기적으로 소나 핑 소리를 재생하며, 가장 가까운 특수 수집품이 감지 범위 안에서
/// 가까워질수록 핑 주기가 짧아집니다. (가까울수록 더 자주 울림)
///
/// 레이더는 상점 업그레이드 대상이 아니라 고정 스펙(<see cref="CRadarSO"/>)을 사용하며,
/// 일반 수집품은 무시하고 특수 수집품만 탐지합니다.
/// 잠수함 안에 있는 동안에는 울리지 않습니다. (CSubmarineAreaSensor가 밖으로 나감을 알려줘야 가동)
/// </summary>
[DisallowMultipleComponent]
public sealed class CRadar : AGear, IUpdateFrameable
{
    #region ─────────────────────────▶ 상수 ◀─────────────────────────
    // SO를 읽지 못했거나 값이 비정상일 때 사용하는 최후 보루 값입니다.
    // 레이더가 "아무 소리도 안 나는" 상태로 조용히 죽는 것을 막기 위한 안전망입니다.
    private const float FALLBACK_DETECT_DISTANCE = 20f;
    private const float FALLBACK_FAR_INTERVAL = 3f;
    private const float FALLBACK_NEAR_INTERVAL = 0.12f;

    // 기준점이 플레이어 루트에서 이만큼 떨어져 있으면 측정이 틀어진 것으로 보고 경고합니다.
    private const float ORIGIN_OFFSET_TOLERANCE = 5f;

    // 경계에서 들락날락할 때 "첫 진입 핑"이 연속으로 터지지 않도록 주는 여유입니다.
    // 한 번 잡은 뒤에는 감지 거리의 이 배율만큼 멀어져야 놓친 것으로 판정합니다.
    private const float BOUNDARY_HYSTERESIS = 1.05f;

    // 스테이지별 감지 거리를 읽어올 스폰 SO의 ID입니다. (EScene.Stage_1부터 순서대로)
    private static readonly string[] STAGE_SPAWNER_IDS =
    {
        Id.StageSpawner_1,
        Id.StageSpawner_2,
        Id.StageSpawner_3,
        Id.StageSpawner_4,
        Id.StageSpawner_5,
        Id.StageSpawner_6,
    };
    #endregion

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("핑 연출")]
    [Tooltip("거리에 따른 주기 변화 곡선. X: 정규화 거리(0=최근접, 1=감지 한계), Y: near~far 보간 비율(0=near, 1=far).")]
    [SerializeField] private AnimationCurve _proximityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("동작 옵션")]
    [Tooltip("감지 범위에 특수 수집품이 없을 때도 최대 주기로 핑을 울릴지 여부입니다.")]
    [SerializeField] private bool _pingWhenNoTarget = false;
    [Tooltip("집게에 잡혀 있는 수집품은 탐지 대상에서 제외할지 여부입니다.")]
    [SerializeField] private bool _ignoreHeld = true;
    [Tooltip("씬의 수집품 목록을 다시 수집하는 주기(초). 이 사이에는 캐시를 재사용합니다. 스폰/수거 반영 지연이 됩니다.")]
    [SerializeField, Min(0.1f)] private float _targetRefreshInterval = 1f;
    [Tooltip("재생할 핑 사운드 ID입니다.")]
    [SerializeField] private string _pingSoundId = Id.SFX_Sonar_Ping;

    [Header("기즈모")]
    [Tooltip("켜면 이 오브젝트를 선택하지 않아도 감지 범위 기즈모를 항상 표시합니다.")]
    [SerializeField] private bool _drawGizmoAlways = false;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _maxDetectDistance; // 고정 스펙의 최대 감지 거리
    private float _farInterval;       // 감지 한계에서의 핑 주기 (가장 느림)
    private float _nearInterval;      // 최근접 시의 핑 주기 (가장 빠름)
    private float _phase;             // 다음 핑까지의 진행률(0~1). 매 프레임 현재 주기로 누적

    private CCollectible[] _cache;    // 수집품 목록 캐시 (주기적으로 갱신)
    private float _nextRefreshTime;   // 다음 캐시 갱신 시점
    private bool _hadTarget;          // 지난 프레임에 감지 범위 안에 대상이 있었는지 (첫 진입 판정용)

    // 시작 시 잠수함 안이라고 가정합니다. (CSubmarineRadarHud와 동일한 규칙)
    // 센서가 "밖으로 나감"을 알려주기 전까지는 울리지 않습니다.
    private bool _isInsideSubmarine = true;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override EDataType GearType => EDataType.Radar;

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (!IsActive) return;

        // 잠수함 안에서는 탐지를 멈춥니다. 진행률을 채워둬서 밖으로 나가는 즉시 첫 핑이 울립니다.
        if (_isInsideSubmarine)
        {
            _phase = 1f;
            _hadTarget = false;
            return;
        }

        // 매 프레임 현재 거리로 주기를 다시 계산합니다.
        // 주기 중간에 가까워지면 진행률이 그만큼 빨리 차서 다음 핑이 앞당겨집니다.
        float interval = ComputeInterval(out bool hasTarget);
        if (interval <= 0f) return; // 방어

        // 대상이 감지 범위에 처음 들어온 순간에는 주기를 기다리지 않고 바로 첫 핑을 울립니다.
        // 그냥 두면 경계에서의 주기(최대 _farInterval)만큼 흘려보내므로, 헤엄쳐 들어가는 동안
        // 경계를 한참 지난 뒤에야 소리가 나기 시작합니다. 경계를 또렷한 "발견 지점"으로 만듭니다.
        if (hasTarget && !_hadTarget) _phase = 1f;
        _hadTarget = hasTarget;

        _phase += Time.deltaTime / interval;
        if (_phase < 1f) return;

        _phase = 0f;

        if (hasTarget || _pingWhenNoTarget)
        {
            Ping();
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 다음 핑까지의 주기를 계산합니다. 가까울수록 짧아집니다.
    private float ComputeInterval(out bool hasTarget)
    {
        hasTarget = TryGetNearestDistance(out float distance);
        if (!hasTarget)
        {
            return _farInterval;
        }

        // t: 최근접(0) ~ 감지 한계(1). near가 항상 far보다 짧도록 최종 clamp.
        float t = _maxDetectDistance > 0f ? Mathf.Clamp01(distance / _maxDetectDistance) : 1f;
        t = Mathf.Clamp01(_proximityCurve.Evaluate(t));

        float nearInterval = Mathf.Min(_nearInterval, _farInterval);
        return Mathf.Lerp(nearInterval, _farInterval, t);
    }

    // 감지 범위 안에서 가장 가까운 특수 수집품까지의 거리를 구합니다.
    private bool TryGetNearestDistance(out float distance)
    {
        distance = 0f;
        if (_maxDetectDistance <= 0f) return false;

        RefreshCacheIfDue();
        if (_cache == null) return false;

        Vector3 origin = Origin.position;

        // 경계에 딱 걸쳐 있으면 감지가 매 프레임 켜졌다 꺼지면서 "첫 진입 핑"이 연달아 터집니다.
        // 한 번 잡은 뒤에는 조금 더 멀어져야 놓친 것으로 보도록 여유를 줍니다.
        float effectiveMax = _hadTarget ? _maxDetectDistance * BOUNDARY_HYSTERESIS : _maxDetectDistance;
        float maxSqr = effectiveMax * effectiveMax;
        float bestSqr = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < _cache.Length; ++i)
        {
            CCollectible collectible = _cache[i];
            if (collectible == null) continue;      // 수거/파괴되어 사라진 항목
            if (!collectible.IsSpecial) continue;   // 일반 수집품은 탐지하지 않습니다.
            if (_ignoreHeld && collectible.IsHeld) continue;

            float sqr = (collectible.transform.position - origin).sqrMagnitude;
            if (sqr > maxSqr) continue;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                found = true;
            }
        }

        if (!found) return false;

        distance = Mathf.Sqrt(bestSqr);
        return true;
    }

    // 갱신 주기가 지났으면 씬의 수집품 목록을 다시 수집합니다.
    private void RefreshCacheIfDue()
    {
        if (_cache != null && Time.time < _nextRefreshTime) return;

        _cache = FindObjectsByType<CCollectible>(FindObjectsSortMode.None);
        _nextRefreshTime = Time.time + _targetRefreshInterval;
    }

    // 핑 소리를 1회 재생합니다.
    private void Ping()
    {
        if (_pingSoundId.IsBlank()) return;

        CSoundManager sound = CSoundManager.Ins;
        if (sound == null) return;

        sound.PlaySfx(_pingSoundId);
    }
    #endregion

    #region ─────────────────────────▶ AGear 구현 ◀─────────────────────────
    // 레이더는 상점에서 업그레이드하지 않으므로 레벨을 조회하지 않습니다.
    protected override bool IsUpgradeable => false;

    // 고정 스펙을 SO에서 읽어옵니다. (활성화 시)
    protected override void OnStatsRefreshed()
    {
        CRadarSO so = UData.Radar();
        if (so == null)
        {
            UDebug.Print($"레이더 SO를 찾을 수 없어 기본 스펙으로 동작합니다. (감지 {FALLBACK_DETECT_DISTANCE}m)", LogType.Error, gameObject);
            ApplyFallbackStats();
            return;
        }

        _maxDetectDistance = so.MaxDetectDistance;
        _farInterval = so.FarScanInterval;
        _nearInterval = so.NearScanInterval;

        // 현재 스테이지에 감지 거리가 지정되어 있으면 그 값을 씁니다.
        // 지정하지 않은(0 이하) 스테이지는 위에서 읽은 RadarSO의 기본 감지 거리를 그대로 유지합니다.
        if (TryGetStageDetectDistance(out float stageDistance))
        {
            _maxDetectDistance = stageDistance;
        }

        // SO 값 미설정 등으로 유효하지 않은 값 방어.
        // 하나라도 0 이하면 레이더가 영구 무음이 되므로 기본 스펙으로 되돌립니다.
        if (_maxDetectDistance <= 0f || _farInterval <= 0f || _nearInterval <= 0f)
        {
            UDebug.Print($"레이더 스펙이 유효하지 않아(감지 {_maxDetectDistance}, far {_farInterval}, near {_nearInterval}) 기본 스펙으로 되돌립니다. RadarSO를 확인하세요.", LogType.Warning, gameObject);
            ApplyFallbackStats();
        }
    }

    // 스테이지가 바뀌면 그 스테이지의 감지 거리를 다시 읽습니다.
    private void SceneLoadEndHandler(OnSceneLoadEnd ctx)
    {
        OnStatsRefreshed();

        // 이전 씬의 수집품 목록은 버립니다.
        _cache = null;
        _hadTarget = false;
    }

    // 잠수함 안/밖 상태를 받아둡니다. 안에 있는 동안에는 핑을 울리지 않습니다.
    private void SubmarineAreaHandler(OnPlayerSubmarineAreaChanged ctx)
    {
        _isInsideSubmarine = ctx.isInsideSubmarine;

        // 밖으로 나온 순간에는 목록을 새로 수집해 즉시 반응하게 합니다.
        if (!_isInsideSubmarine)
        {
            _cache = null;
            _hadTarget = false;
        }
    }

    // 잠수함 안/밖을 알려줄 센서가 실제로 붙어 있는지 확인합니다.
    // 센서가 없으면 _isInsideSubmarine이 true로 고정되어 레이더가 영구 무음이 되므로 경고합니다.
    private void ValidateSubmarineSensor()
    {
        Transform root = transform.root;
        if (root == null) return;
        if (root.GetComponentInChildren<CSubmarineAreaSensor>(true) != null) return;

        UDebug.Print($"플레이어({root.name})에 CSubmarineAreaSensor가 없어 잠수함 안/밖을 알 수 없습니다. " +
                     $"레이더가 계속 잠수함 안으로 판정되어 울리지 않습니다.",
                     LogType.Warning, gameObject);
    }

    // 거리 측정 기준점이 플레이어 본체에 붙어 있는지 확인합니다.
    // 장비 노드(Equipment 등)가 플레이어에서 떨어진 위치에 놓이면 Origin도 같이 끌려가
    // "가까워질수록 빨라진다"가 전부 엉뚱한 방향으로 동작하므로, 조용히 넘기지 않고 경고합니다.
    private void ValidateOrigin()
    {
        Transform root = transform.root;
        if (root == null) return;

        float offset = Vector3.Distance(Origin.position, root.position);
        if (offset <= ORIGIN_OFFSET_TOLERANCE) return;

        UDebug.Print($"레이더 기준점이 플레이어 루트({root.name})에서 {offset:0.#}m 떨어져 있어 거리 측정이 틀어집니다. " +
                     $"CRadar의 Origin Override를 플레이어 루트로 지정하거나 장비 노드의 로컬 위치를 0으로 맞추세요.",
                     LogType.Warning, gameObject);
    }

    // 현재 스테이지의 스폰 SO에서 감지 거리를 읽어옵니다.
    // 게임플레이 씬이 아니거나 스테이지가 값을 지정하지 않았으면 false를 돌려줍니다.
    private bool TryGetStageDetectDistance(out float distance)
    {
        distance = 0f;

        int index = (int)UScene.Current - (int)EScene.Stage_1;
        if (!UArray.InBounds(STAGE_SPAWNER_IDS, index)) return false; // 스테이지 씬이 아님

        CStageSpawnSO so = UData.Get<CStageSpawnSO>(STAGE_SPAWNER_IDS[index]);
        if (so == null) return false;

        distance = so.RadarDetectDistance;
        return distance > 0f;
    }

    // SO를 신뢰할 수 없을 때 쓰는 기본 스펙입니다.
    private void ApplyFallbackStats()
    {
        _maxDetectDistance = FALLBACK_DETECT_DISTANCE;
        _farInterval = FALLBACK_FAR_INTERVAL;
        _nearInterval = FALLBACK_NEAR_INTERVAL;
    }

    // 가동 시작 시 즉시 첫 핑을 울리도록 진행률을 채우고 캐시를 비웁니다.
    protected override void OnActivated()
    {
        _phase = 1f;
        _cache = null;
        _hadTarget = false;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable(); // 프레임 등록 + 이벤트 구독 + 스탯 갱신

        CEventBus<OnPlayerSubmarineAreaChanged>.Subscribe(SubmarineAreaHandler);
        CEventBus<OnSceneLoadEnd>.Subscribe(SceneLoadEndHandler);

        ValidateOrigin();
        ValidateSubmarineSensor();

        // 활성화 직후 즉시 첫 핑을 울리고 목록을 새로 수집합니다.
        _phase = 1f;
        _cache = null;
        _hadTarget = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnPlayerSubmarineAreaChanged>.Unsubscribe(SubmarineAreaHandler);
        CEventBus<OnSceneLoadEnd>.Unsubscribe(SceneLoadEndHandler);
    }
    #endregion

    #region ─────────────────────────▶ 기즈모 ◀─────────────────────────
#if UNITY_EDITOR
    // 기즈모용 캐시. 에디트 모드에서는 런타임 스탯이 비어 있어 SO를 직접 읽어옵니다.
    // CDatabaseManager.Ins를 쓰면 에디터 씬에 싱글톤 오브젝트가 생겨버리므로 Resources에서 직접 찾습니다.
    private CRadarSO _gizmoSo;

    private void OnDrawGizmos()
    {
        if (_drawGizmoAlways) DrawRadarGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmoAlways) DrawRadarGizmo();
    }

    private void DrawRadarGizmo()
    {
        if (!TryGetGizmoStats(out float maxDistance, out float nearInterval, out float farInterval)) return;

        Vector3 center = Origin.position;

        // 감지 한계: 수평 원 + 와이어 스피어. 원은 위에서 볼 때, 스피어는 입체감을 주기 위함입니다.
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        DrawWireCircle(center, maxDistance);
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.25f);
        Gizmos.DrawWireSphere(center, maxDistance);

        // 핑 주기 그라디언트. 25% 간격의 고리마다 그 거리에서의 실제 주기를 적어줍니다.
        // "어디서부터 빨라지는지"를 눈으로 확인하는 것이 이 기즈모의 핵심입니다.
        for (int i = 1; i <= 4; ++i)
        {
            float f = i * 0.25f;
            float ringDistance = maxDistance * f;
            float interval = EvaluateInterval(f, nearInterval, farInterval);

            // 가까울수록(빠를수록) 따뜻한 색, 멀수록 차가운 색
            Gizmos.color = Color.Lerp(new Color(1f, 0.35f, 0.1f, 0.8f), new Color(0.2f, 0.6f, 1f, 0.5f), f);
            DrawWireCircle(center, ringDistance);

            UnityEditor.Handles.color = Gizmos.color;
            UnityEditor.Handles.Label(center + new Vector3(0f, 0f, ringDistance), $"{ringDistance:0.#}m / {interval:0.00}초");
        }

        // 실제로 탐지 중인 특수 수집품까지 선을 그어 측정 대상과 거리를 확인합니다.
        // TryGetNearestDistance는 캐시를 갱신하는 부수효과가 있어, 기즈모에서는 쓰지 않습니다.
        CCollectible target = Application.isPlaying ? FindNearestSpecial() : null;
        if (target != null)
        {
            float targetDistance = Vector3.Distance(center, target.transform.position);
            float t = maxDistance > 0f ? Mathf.Clamp01(targetDistance / maxDistance) : 1f;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center, target.transform.position);
            Gizmos.DrawWireSphere(target.transform.position, 1f);

            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(target.transform.position + Vector3.up * 1.5f,
                $"{target.Data.name}\n{targetDistance:0.#}m / {EvaluateInterval(t, nearInterval, farInterval):0.00}초");
        }

        // 요약 라벨.
        // 에디트 모드에서는 스테이지별 값을 알 수 없으므로(UScene.Current가 런타임 매니저를 깨움)
        // RadarSO의 기본값을 보여주고 있다는 점을 명시합니다.
        string source = Application.isPlaying
            ? (_isInsideSubmarine ? " (잠수함 안 ─ 정지)" : string.Empty)
            : " (RadarSO 기본값 ─ 스테이지별 값은 플레이 중 확인)";
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(center + Vector3.up * 2f,
            $"레이더 {maxDistance:0.#}m\n{nearInterval:0.00}초 ~ {farInterval:0.00}초{source}");
    }

    // 런타임이면 적용 중인 값을, 에디트 모드면 SO의 값을 돌려줍니다.
    private bool TryGetGizmoStats(out float maxDistance, out float nearInterval, out float farInterval)
    {
        if (Application.isPlaying && _maxDetectDistance > 0f)
        {
            maxDistance = _maxDetectDistance;
            nearInterval = _nearInterval;
            farInterval = _farInterval;
            return true;
        }

        if (_gizmoSo == null)
        {
            CRadarSO[] all = Resources.LoadAll<CRadarSO>(K.RESOURCE_SO_PATH);
            for (int i = 0; i < all.Length; ++i)
            {
                if (all[i] != null && all[i].Id == Id.Radar)
                {
                    _gizmoSo = all[i];
                    break;
                }
            }
        }

        if (_gizmoSo == null)
        {
            maxDistance = nearInterval = farInterval = 0f;
            return false;
        }

        maxDistance = _gizmoSo.MaxDetectDistance;
        nearInterval = _gizmoSo.NearScanInterval;
        farInterval = _gizmoSo.FarScanInterval;
        return maxDistance > 0f;
    }

    // 정규화 거리(0=최근접, 1=감지 한계)에서의 핑 주기를 계산합니다. ComputeInterval과 같은 식입니다.
    private float EvaluateInterval(float normalizedDistance, float nearInterval, float farInterval)
    {
        float t = Mathf.Clamp01(normalizedDistance);
        if (_proximityCurve != null) t = Mathf.Clamp01(_proximityCurve.Evaluate(t));

        return Mathf.Lerp(Mathf.Min(nearInterval, farInterval), farInterval, t);
    }

    // 기즈모 표시용으로 현재 탐지 대상을 한 번 더 찾습니다. (런타임에서만 호출)
    private CCollectible FindNearestSpecial()
    {
        if (_cache == null) return null;

        Vector3 origin = Origin.position;
        float bestSqr = _maxDetectDistance * _maxDetectDistance;
        CCollectible best = null;

        for (int i = 0; i < _cache.Length; ++i)
        {
            CCollectible collectible = _cache[i];
            if (collectible == null) continue;
            if (!collectible.IsSpecial) continue;
            if (_ignoreHeld && collectible.IsHeld) continue;

            float sqr = (collectible.transform.position - origin).sqrMagnitude;
            if (sqr > bestSqr) continue;

            bestSqr = sqr;
            best = collectible;
        }

        return best;
    }

    // CCollectibleSpawner와 같은 방식의 수평 원 그리기입니다.
    private void DrawWireCircle(Vector3 center, float radius)
    {
        const int SEG = 48;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= SEG; ++i)
        {
            float a = (i / (float)SEG) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
    #endregion
}
