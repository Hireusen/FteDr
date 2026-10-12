using UnityEngine;

/// <summary>
/// 플레이어 장비의 공통 추상 클래스입니다.
/// </summary>
public abstract class AGear : AFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("장비 공통")]
    [Tooltip("장비 기준 트랜스폼(측정/발사 원점 등). 비우면 이 오브젝트 위치를 사용합니다.")]
    [SerializeField] private Transform _originOverride;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private bool _isActive = true;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public abstract EDataType GearType { get; }

    public bool IsActive => _isActive;

    public void Activate()
    {
        if (_isActive) return;

        _isActive = true;
        OnActivated();
    }

    public void Deactivate()
    {
        if (!_isActive) return;

        _isActive = false;
        OnDeactivated();
    }
    #endregion

    #region ─────────────────────────▶ 확장 지점 ◀───────────────────────────
    /// <summary>
    /// 현재 장비 레벨 입니다.
    /// </summary>
    protected int Level { get; private set; }

    /// <summary>
    /// 이 장비가 상점 업그레이드 대상인지 여부입니다.
    /// False면 진행도에서 레벨을 조회하지 않습니다. (고정 스펙 장비 ─ 레이더 등)
    /// </summary>
    protected virtual bool IsUpgradeable => true;

    /// <summary>
    /// 기준 트랜스폼입니다.
    /// </summary>
    protected Transform Origin => _originOverride != null ? _originOverride : transform;

    /// <summary>
    /// 레벨이 갱신될 때 호출됩니다.
    /// </summary>
    protected abstract void OnStatsRefreshed();

    /// <summary>
    /// 가동이 시작될 때 호출됩니다.
    /// </summary>
    protected virtual void OnActivated() { }

    /// <summary>
    /// 가동이 정지될 때 호출됩니다.
    /// </summary>
    protected virtual void OnDeactivated() { }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void RefreshLevel()
    {
        // 고정 스펙 장비는 진행도에 레벨이 없으므로 조회를 건너뜁니다.
        if (IsUpgradeable)
        {
            Level = UPlayer.GetGearLevel(GearType);
        }
        OnStatsRefreshed();
    }

    private void GearUpgradedHandler(OnGearUpgraded ctx)
    {
        if (ctx.gearType == GearType)
        {
            RefreshLevel();
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();

        CEventBus<OnGearUpgraded>.Subscribe(GearUpgradedHandler);
        RefreshLevel();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnGearUpgraded>.Unsubscribe(GearUpgradedHandler);
    }
    #endregion
}
