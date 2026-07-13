using UnityEngine;

/// <summary>
/// 플레이어 그물입니다. 입력 시 1회 발동하여 조준 방향으로 그물 발사체를 포물선으로 던집니다.
/// 실제 수집(비특수 수집품 회수)은 발사체(CNetProjectile)가 담당합니다.
/// 발동 후에는 레벨별 쿨타임 동안 다시 쓸 수 없습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CNet : AGear
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("발사")]
    [Tooltip("그물 발사체 프리팹. (CNetProjectile 필요)")]
    [SerializeField] private GameObject _projectilePrefab;
    [Tooltip("발사 위치. 비우면 이 장비의 기준점(Origin)을 사용합니다.")]
    [SerializeField] private Transform _muzzle;
    [Tooltip("발사 방향 기준. 보통 카메라 트랜스폼. 비우면 발사 위치의 forward를 사용합니다.")]
    [SerializeField] private Transform _aimSource;
    [Tooltip("잡힌 수집품이 끌려올 대상. 비우면 발사 위치를 사용합니다. (보통 플레이어)")]
    [SerializeField] private Transform _reelTarget;

    [Header("동작 옵션")]
    [Tooltip("발동 사운드 ID입니다. (비우면 무음)")]
    [SerializeField] private string _netSoundId = "";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _launchSpeed;   // 현재 레벨의 발사 속도
    private float _catchScale;    // 현재 레벨의 발사체 x,z 스케일 배율
    private float _cooldown;      // 현재 레벨의 쿨타임(초)
    private float _nextReadyTime; // 다시 사용 가능한 시각 (Time.time 기준)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이 장비의 데이터 타입입니다.</summary>
    public override EDataType GearType => EDataType.Net;

    /// <summary>지금 사용 가능한지(쿨타임이 끝났는지) 여부입니다.</summary>
    public bool IsReady => Time.time >= _nextReadyTime;

    /// <summary>남은 쿨타임(초)입니다. UI 표시용.</summary>
    public float CooldownRemaining => Mathf.Max(0f, _nextReadyTime - Time.time);

    /// <summary>
    /// 그물을 1회 발동(발사)합니다. 비활성/쿨타임 중이면 무시하고 false를 반환합니다.
    /// </summary>
    public bool TryCast()
    {
        if (!IsActive) return false;
        if (!IsReady) return false;
        if (_projectilePrefab == null)
        {
            UDebug.Print("그물 발사체 프리팹이 비어 있습니다.", LogType.Warning, gameObject);
            return false;
        }

        Launch();
        _nextReadyTime = Time.time + _cooldown;

        if (!_netSoundId.IsBlank())
        {
            CSoundManager sound = CSoundManager.Ins;
            if (sound != null) sound.PlaySfx(_netSoundId);
        }

        UDebug.Print("[Net] 그물 사용");
        return true;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 조준 방향으로 발사체를 생성하고 초기 속도를 부여합니다.
    private void Launch()
    {
        Transform muzzle = _muzzle != null ? _muzzle : Origin;
        Vector3 dir = (_aimSource != null ? _aimSource.forward : muzzle.forward).normalized;
        Transform reelTarget = _reelTarget != null ? _reelTarget : muzzle;

        GameObject go = Instantiate(_projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));

        if (go.TryGetComponent(out CNetProjectile projectile))
        {
            projectile.Launch(dir * _launchSpeed, _catchScale, reelTarget);
        }
        else
        {
            UDebug.Print("발사체 프리팹에 CNetProjectile이 없습니다.", LogType.Warning, go);
        }
    }
    #endregion

    #region ─────────────────────────▶ AGear 구현 ◀─────────────────────────
    // 현재 레벨에 맞춰 발사 속도와 쿨타임을 다시 읽어옵니다. (활성화 + 업그레이드 시)
    protected override void OnStatsRefreshed()
    {
        CNetSO so = UData.Net();
        if (so == null)
        {
            UDebug.Print("그물 SO를 찾을 수 없습니다.", LogType.Error, gameObject);
            return;
        }

        _launchSpeed = so.LaunchSpeed(Level);
        _catchScale = so.CatchScale(Level);
        _cooldown = so.Cooldown(Level);

        if (_launchSpeed <= 0f)
        {
            UDebug.Print($"레벨 {Level}의 발사 속도가 유효하지 않습니다({_launchSpeed}). SO 배열을 확인하세요.", LogType.Warning, gameObject);
            _launchSpeed = 0f;
        }
        if (_catchScale <= 0f)
        {
            UDebug.Print($"레벨 {Level}의 스케일 배율이 유효하지 않습니다({_catchScale}). SO 배열을 확인하세요.", LogType.Warning, gameObject);
            _catchScale = 1f; // 방어: 기본 배율
        }
        if (_cooldown < 0f)
        {
            UDebug.Print($"레벨 {Level}의 쿨타임이 유효하지 않습니다({_cooldown}). SO 배열을 확인하세요.", LogType.Warning, gameObject);
            _cooldown = 0f;
        }
    }

    // 가동 시작 시 즉시 사용 가능하도록 쿨타임을 초기화합니다.
    protected override void OnActivated()
    {
        _nextReadyTime = Time.time;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable(); // 프레임 등록 + 업그레이드 구독 + 레벨/스탯 갱신

        CEventBus<OnInputNet>.Subscribe(NetHandler);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        CEventBus<OnInputNet>.Unsubscribe(NetHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void NetHandler(OnInputNet _)
    {
        TryCast();
    }
    #endregion
}
