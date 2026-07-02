// todo : 연료 고갈 이벤트 + 플레이어 매니저에 연료 고갈 이벤트 발행

using UnityEngine;

/// <summary>
/// 수중에서 연료를 틱 단위로 소모시키는 컴포넌트입니다.
/// </summary>
public class CFuelSystem : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("소모 설정")]
    [Tooltip("수중에서 한 틱마다 소모하는 기본량")]
    [SerializeField] private float _baseConsumePerTick = 1f;
    [SerializeField] private float _tickInterval = 0.1f;

    [Tooltip("집게 1회 사용 소모량 (지속형이면 매 프레임 * Time.deltaTime 또는 별도 틱으로 호출)")]
    [SerializeField] private float _clawConsume = 5f;

    [Header("참조")]
    [SerializeField] private CPlayerController _player;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _tickTimer;

    private float _stageMultiplier = 1f;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    /// <summary>
    /// 현재 적용 중인 틱당 소모량(기본량 × 스테이지 배율)입니다.
    /// </summary>
    public float ConsumePerTick => _baseConsumePerTick * _stageMultiplier;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (_player == null) return;

        if (CPlayerManager.Ins.CurrentFuel <= 0f) return;

        if (_player.CurrentState == EPlayerState.OnGround)
        {
            _tickTimer = 0f;
            return;
        }

        _tickTimer += Time.deltaTime;

        float amount = ConsumePerTick;

        while (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            CPlayerManager.Ins.ConsumeFuel(amount);

            if (CPlayerManager.Ins.CurrentFuel <= 0f) break;
        }
    }

    /// <summary>
    /// 집게 1회 사용 소모.
    /// </summary>
    public void UseClaw()
    {
        if (CPlayerManager.Ins.CurrentFuel <= 0f) return;
        CPlayerManager.Ins.ConsumeFuel(_clawConsume);
    }

    /// <summary>
    /// 스테이지(깊이) 진입 시 틱당 소모 배율을 설정합니다.
    /// </summary>
    public void SetStageMultiplier(float multiplier)
    {
        _stageMultiplier = Mathf.Max(0f, multiplier);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();

        if (_player == null)
        {
            _player = GetComponent<CPlayerController>();
        }

        _tickTimer = 0f;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }
    #endregion
}
