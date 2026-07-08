using UnityEngine;

/// <summary>
/// 플레이어 탐지기(레이더)입니다.
/// 주기적으로 소나 핑 소리를 재생하며, 가장 가까운 특수 수집품이 감지 범위 안에서
/// 가까워질수록 핑 주기가 짧아집니다. (가까울수록 더 자주 울림)
/// </summary>
[DisallowMultipleComponent]
public sealed class CRadar : AGear, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("핑 주기")]
    [Tooltip("수집품이 가장 가까울 때(거리 0)의 최소 핑 주기(초). 감지 거리 끝에서의 주기(레이더 SO의 ScanInterval)보다 작아야 합니다.")]
    [SerializeField, Min(0.01f)] private float _nearInterval = 0.12f;
    [Tooltip("거리에 따른 주기 변화 곡선. X: 정규화 거리(0=최근접, 1=감지 한계), Y: near~far 보간 비율(0=near, 1=far).")]
    [SerializeField] private AnimationCurve _proximityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("동작 옵션")]
    [Tooltip("특수 수집품만 탐지 대상으로 삼을지 여부입니다. (끄면 모든 수집품 탐지)")]
    [SerializeField] private bool _detectSpecialOnly = true;
    [Tooltip("감지 범위에 수집품이 없을 때도 최대 주기로 핑을 울릴지 여부입니다.")]
    [SerializeField] private bool _pingWhenNoTarget = false;
    [Tooltip("집게에 잡혀 있는 수집품은 탐지 대상에서 제외할지 여부입니다.")]
    [SerializeField] private bool _ignoreHeld = true;
    [Tooltip("씬의 수집품 목록을 다시 수집하는 주기(초). 이 사이에는 캐시를 재사용합니다. 스폰/수거 반영 지연이 됩니다.")]
    [SerializeField, Min(0.1f)] private float _targetRefreshInterval = 1f;
    [Tooltip("재생할 핑 사운드 ID입니다.")]
    [SerializeField] private string _pingSoundId = Id.SFX_Sonar_Ping;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _maxDetectDistance; // 현재 레벨의 최대 감지 거리
    private float _farInterval;       // 현재 레벨의 최대(가장 느린) 핑 주기 = SO.ScanInterval
    private float _phase;             // 다음 핑까지의 진행률(0~1). 매 프레임 현재 주기로 누적

    private CCollectible[] _cache;    // 수집품 목록 캐시 (주기적으로 갱신)
    private float _nextRefreshTime;   // 다음 캐시 갱신 시점
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override EDataType GearType => EDataType.Radar;

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (!IsActive) return;

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

    // 감지 범위 안에서 가장 가까운 수집품까지의 거리를 구합니다.
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
            if (collectible == null) continue; // 수거/파괴되어 사라진 항목
            if (_detectSpecialOnly && !collectible.IsSpecial) continue; // 특수만 탐지
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
    // 현재 레벨에 맞춰 감지 거리와 핑 주기를 다시 읽어옵니다. (활성화 + 업그레이드 시)
    protected override void OnStatsRefreshed()
    {
        CRadarSO so = UData.Radar();
        if (so == null)
        {
            UDebug.Print("레이더 SO를 찾을 수 없습니다.", LogType.Error, gameObject);
            return;
        }

        _maxDetectDistance = so.MaxDetectDistance(Level);
        _farInterval = so.ScanInterval(Level);

        // SO 배열 미설정 등으로 유효하지 않은 값 방어
        if (_maxDetectDistance <= 0f)
        {
            UDebug.Print($"레벨 {Level}의 감지 거리가 유효하지 않습니다({_maxDetectDistance}). SO 배열을 확인하세요.", LogType.Warning, gameObject);
            _maxDetectDistance = 0f;
        }
        if (_farInterval <= 0f)
        {
            UDebug.Print($"레벨 {Level}의 스캔 주기가 유효하지 않습니다({_farInterval}). SO 배열을 확인하세요.", LogType.Warning, gameObject);
            _farInterval = Mathf.Max(_nearInterval, 1f);
        }
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
        base.OnEnable(); // 프레임 등록 + 이벤트 구독 + 레벨/스탯 갱신

        // 활성화 직후 즉시 첫 핑을 울리고 목록을 새로 수집합니다.
        _phase = 1f;
        _cache = null;
    }
    #endregion
}
