using UnityEngine;

/// <summary>
/// 수중에서 연료를 틱 단위로 소모시키는 장비입니다.
/// </summary>
public sealed class CFuelSystem : AGear, IUpdateFrameable
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
    public override EDataType GearType => EDataType.FuelTank;

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    /// <summary>
    /// 현재 적용 중인 틱당 소모량(기본량 × 스테이지 배율)입니다.
    /// </summary>
    public float ConsumePerTick => _baseConsumePerTick * _stageMultiplier;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (!IsActive) return;
        if (_player == null) return;

        if (CPlayerManager.Ins.CurrentFuel <= 0f) return;

        // 지상이거나, 시선을 제외한 이동 조작이 없으면 소모하지 않습니다.
        if (_player.CurrentState == EPlayerState.OnGround || !_player.HasMovementInput)
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

    #region ─────────────────────────▶ AGear 구현 ◀─────────────────────────
    protected override void OnStatsRefreshed() { }

    // 가동 시작 시 틱 누적을 초기화합니다.
    protected override void OnActivated()
    {
        _tickTimer = 0f;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable(); // 프레임 등록 + 이벤트 구독 + 레벨/스탯 갱신

        if (_player == null)
        {
            _player = GetComponent<CPlayerController>();
        }

        _tickTimer = 0f;
    }

    protected override void OnDisable()
    {
        base.OnDisable(); // 프레임 해제 + 이벤트 구독 해제
    }
    #endregion

    #region ─────────────────────────▶ 디버그 ◀─────────────────────────
    #if UNITY_EDITOR
    [Header("디버그 (임시)")]
    [Tooltip("연료 상태/소모량을 화면 좌상단에 임시로 표시합니다.")]
    [SerializeField] private bool _showDebugOverlay = true;

    private GUIStyle _debugStyle; // 임시 오버레이용 (지연 생성)
    private GUIStyle _debugButtonStyle; // 임시 버튼용 (지연 생성)

    // 임시: 연료 소모를 화면에서 확인하기 위한 오버레이입니다. (기획 확정 후 제거/HUD로 대체)
    /*
    private void OnGUI()
    {
        if (!_showDebugOverlay) return;

        CPlayerManager pm = CPlayerManager.Ins;
        if (pm == null) return;

        if (_debugStyle == null)
        {
            _debugStyle = new GUIStyle(GUI.skin.label) { fontSize = 30 };
        }
        if (_debugButtonStyle == null)
        {
            _debugButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 26 };
        }

        float perSecond = _tickInterval > 0f ? ConsumePerTick / _tickInterval : 0f;
        string state = _player != null ? _player.CurrentState.ToString() : "-";

        string text =
            $"연료: {pm.CurrentFuel:F1} / {pm.MaxFuel:F1}\n" +
            $"틱당 소모: {ConsumePerTick:F2}  (틱 {_tickInterval:F2}s)\n" +
            $"초당 소모: {perSecond:F2}/s\n" +
            $"상태: {state}";

        var rect = new Rect(16f, 16f, 620f, 200f);
        GUI.Box(rect, GUIContent.none);
        _debugStyle.normal.textColor = pm.IsFuelLow ? Color.red : Color.white;
        GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, rect.height - 24f), text, _debugStyle);

        // 임시: 연료를 최대로 채우는 버튼 (기존 RecoverFuel이 MaxFuel로 클램프됨)
        if (GUI.Button(new Rect(rect.x + 16f, rect.yMax + 10f, 300f, 60f), "연료 최대 충전", _debugButtonStyle))
        {
            pm.RecoverFuel(pm.MaxFuel);
        }
    }
    */
    #endif
    #endregion
}
