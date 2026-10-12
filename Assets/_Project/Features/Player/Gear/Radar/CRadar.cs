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
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _maxDetectDistance; // 고정 스펙의 최대 감지 거리
    private float _farInterval;       // 감지 한계에서의 핑 주기 (가장 느림)
    private float _nearInterval;      // 최근접 시의 핑 주기 (가장 빠름)
    private float _phase;             // 다음 핑까지의 진행률(0~1). 매 프레임 현재 주기로 누적

    private CCollectible[] _cache;    // 수집품 목록 캐시 (주기적으로 갱신)
    private float _nextRefreshTime;   // 다음 캐시 갱신 시점

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
            return;
        }

        // 매 프레임 현재 거리로 주기를 다시 계산합니다.
        // 주기 중간에 가까워지면 진행률이 그만큼 빨리 차서 다음 핑이 앞당겨집니다.
        float interval = ComputeInterval(out bool hasTarget);
        if (interval <= 0f) return; // 방어

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
        float maxSqr = _maxDetectDistance * _maxDetectDistance;
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

        // SO 값 미설정 등으로 유효하지 않은 값 방어.
        // 하나라도 0 이하면 레이더가 영구 무음이 되므로 기본 스펙으로 되돌립니다.
        if (_maxDetectDistance <= 0f || _farInterval <= 0f || _nearInterval <= 0f)
        {
            UDebug.Print($"레이더 스펙이 유효하지 않아(감지 {_maxDetectDistance}, far {_farInterval}, near {_nearInterval}) 기본 스펙으로 되돌립니다. RadarSO를 확인하세요.", LogType.Warning, gameObject);
            ApplyFallbackStats();
        }
    }

    // 잠수함 안/밖 상태를 받아둡니다. 안에 있는 동안에는 핑을 울리지 않습니다.
    private void SubmarineAreaHandler(OnPlayerSubmarineAreaChanged ctx)
    {
        _isInsideSubmarine = ctx.isInsideSubmarine;

        // 밖으로 나온 순간에는 목록을 새로 수집해 즉시 반응하게 합니다.
        if (!_isInsideSubmarine) _cache = null;
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
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable(); // 프레임 등록 + 이벤트 구독 + 스탯 갱신

        CEventBus<OnPlayerSubmarineAreaChanged>.Subscribe(SubmarineAreaHandler);

        ValidateOrigin();
        ValidateSubmarineSensor();

        // 활성화 직후 즉시 첫 핑을 울리고 목록을 새로 수집합니다.
        _phase = 1f;
        _cache = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnPlayerSubmarineAreaChanged>.Unsubscribe(SubmarineAreaHandler);
    }
    #endregion
}
