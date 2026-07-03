using UnityEngine;

/// <summary>
/// 플레이어 탐지기(레이더)입니다.
/// 주기적으로 소나 핑 소리를 재생하며, 가장 가까운 수집품이 감지 범위 안에서 가까워질수록 핑 주기가 짧아집니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CRadar : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("측정 기준점")]
    [Tooltip("거리 측정 기준 트랜스폼. 비우면 이 오브젝트 위치를 사용합니다.")]
    [SerializeField] private Transform _originOverride;

    [Header("핑 주기")]
    [Tooltip("수집품이 가장 가까울 때(거리 0)의 최소 핑 주기(초). 감지 거리 끝에서의 주기(레이더 SO의 ScanInterval)보다 작아야 합니다.")]
    [SerializeField, Min(0.01f)] private float _nearInterval = 0.12f;
    [Tooltip("거리에 따른 주기 변화 곡선. X: 정규화 거리(0=최근접, 1=감지 한계), Y: near~far 보간 비율(0=near, 1=far).")]
    [SerializeField] private AnimationCurve _proximityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("동작 옵션")]
    [Tooltip("감지 범위에 수집품이 없을 때도 최대 주기로 핑을 울릴지 여부입니다.")]
    [SerializeField] private bool _pingWhenNoTarget = true;
    [Tooltip("집게에 잡혀 있는 수집품은 탐지 대상에서 제외할지 여부입니다.")]
    [SerializeField] private bool _ignoreHeld = true;
    [Tooltip("씬의 수집품 목록을 다시 수집하는 주기(초). 이 사이에는 캐시를 재사용합니다. 스폰/수거 반영 지연이 됩니다.")]
    [SerializeField, Min(0.1f)] private float _targetRefreshInterval = 1f;
    [Tooltip("재생할 핑 사운드 ID입니다.")]
    [SerializeField] private string _pingSoundId = Id.SFX_Sonar_Ping;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _maxDetectDistance;
    private float _farInterval;
    private float _timer;
    private bool _isScanning = true;

    private CCollectible[] _cache;
    private float _nextRefreshTime;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    /// <summary>
    /// 현재 탐지가 동작 중인지 여부입니다.
    /// </summary>
    public bool IsScanning => _isScanning;

    public void ExecuteUpdateFrame()
    {
        if (!_isScanning) return;

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        float interval = ComputeInterval(out bool hasTarget);
        _timer = interval;

        if (hasTarget || _pingWhenNoTarget)
        {
            Ping();
        }
    }

    /// <summary>
    /// 탐지를 시작합니다. (즉시 첫 핑 평가)
    /// </summary>
    public void StartScan()
    {
        _isScanning = true;
        _timer = 0f;
    }

    /// <summary>
    /// 탐지를 중지합니다. (핑 정지)
    /// </summary>
    public void StopScan()
    {
        _isScanning = false;
    }

    /// <summary>
    /// 현재 장비 레벨에 맞춰 감지 거리와 핑 주기를 다시 읽어옵니다.
    /// 레이더 업그레이드 시 자동으로 호출됩니다.
    /// </summary>
    public void RefreshStats()
    {
        CRadarSO so = UData.Radar();
        if (so == null)
        {
            UDebug.Print("CRadar: 레이더 SO를 찾을 수 없습니다.", LogType.Error, gameObject);
            return;
        }

        int level = UPlayer.GetGearLevel(EDataType.Radar);
        _maxDetectDistance = so.MaxDetectDistance(level);
        _farInterval = so.ScanInterval(level);

        // SO 배열 미설정 등으로 유효하지 않은 값 방어
        if (_maxDetectDistance <= 0f)
        {
            UDebug.Print($"CRadar: 레벨 {level}의 감지 거리가 유효하지 않습니다({_maxDetectDistance}). SO 배열을 확인하세요.", LogType.Warning, gameObject);
            _maxDetectDistance = 0f;
        }
        if (_farInterval <= 0f)
        {
            UDebug.Print($"CRadar: 레벨 {level}의 스캔 주기가 유효하지 않습니다({_farInterval}). SO 배열을 확인하세요.", LogType.Warning, gameObject);
            _farInterval = Mathf.Max(_nearInterval, 1f);
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

    // 감지 범위 안에서 가장 가까운 수집품까지의 거리를 구합니다.
    private bool TryGetNearestDistance(out float distance)
    {
        distance = 0f;
        if (_maxDetectDistance <= 0f) return false;

        RefreshCacheIfDue();
        if (_cache == null) return false;

        Vector3 origin = (_originOverride != null ? _originOverride : transform).position;
        float maxSqr = _maxDetectDistance * _maxDetectDistance;
        float bestSqr = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < _cache.Length; ++i)
        {
            CCollectible collectible = _cache[i];
            if (collectible == null) continue; // 수거/파괴되어 사라진 항목
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

    // 핑 소리를 1회 재생합니다. (플레이어 장비음이므로 2D)
    private void Ping()
    {
        if (_pingSoundId.IsBlank()) return;

        CSoundManager sound = CSoundManager.Ins;
        if (sound == null) return;

        sound.PlaySfx(_pingSoundId);
    }

    // 레이더 업그레이드 이벤트 처리. 레이더 타입일 때만 스탯 재갱신.
    private void GearUpgradedHandler(OnGearUpgraded ctx)
    {
        if (ctx.gearType == EDataType.Radar)
        {
            RefreshStats();
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable(); 

        CEventBus<OnGearUpgraded>.Subscribe(GearUpgradedHandler);
        RefreshStats();
        _cache = null;
        _timer = 0f;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnGearUpgraded>.Unsubscribe(GearUpgradedHandler);
    }
    #endregion
}
