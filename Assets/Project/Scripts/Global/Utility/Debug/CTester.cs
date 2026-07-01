#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// 런타임에 데이터를 확인하고 조작하는 개발용 디버그 패널입니다.
/// </summary>
public sealed class CTester : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("조작 단위")]
    [SerializeField] private float _fuelStep = 10f; // 연료 증감 단위
    [SerializeField] private int _moneyStep = 100; // 재화 증감 단위

    [Header("표시")]
    [SerializeField] private int _fontSize = 26; // 패널 글자 크기
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const int WINDOW_ID = 13494221; // 임의 고유 ID
    private const int FONT_MIN = 10;
    private const int FONT_MAX = 40;

    private Vector2 _scroll;
    private Rect _window = new Rect(20, 20, 680, 800);

    // GUI 폰트 크기 적용용 (지연 초기화)
    private GUISkin _skin;
    private int _appliedFontSize = -1;

    // 이벤트로 갱신하는 캐시
    private bool _open;
    private int _money;
    private float _fuelCur, _fuelMax;
    private int _bagCount, _bagCap;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 이벤트가 아직 발행되지 않았을 수 있으므로 시작 시 현재값을 한 번 끌어온다.
    private void RefreshCache()
    {
        _fuelCur = UPlayer.CurrentFuel;
        _fuelMax = UPlayer.MaxFuel;
        _bagCount = 0; // TODO
        _bagCap = UPlayer.BagCapacity;
        _money = UPlayer.Money;
    }

    private void FuelHandler(OnPlayerFuelChanged ctx) { _fuelCur = ctx.current; _fuelMax = ctx.max; }
    private void BagHandler(OnPlayerBagChanged ctx) { _bagCount = ctx.count; _bagCap = ctx.capacity; }
    private void MoneyHandler(OnMoneyChanged ctx) { _money = ctx.money; }
    private void CheatHandler(OnInputCheat ctx) { _open = !_open; }

    // 현재 _fontSize를 GUI.skin 전체에 반영한다. (변경 시에만 갱신)
    private void ApplyFontSize()
    {
        // 기본 스킨을 복제해 폰트 크기만 덮어쓴다. (전역 GUI.skin 오염 방지)
        if (_skin == null) _skin = Instantiate(GUI.skin);

        if (_appliedFontSize != _fontSize)
        {
            _skin.label.fontSize = _fontSize;
            _skin.button.fontSize = _fontSize;
            _skin.box.fontSize = _fontSize;
            _skin.window.fontSize = _fontSize;
            _skin.textField.fontSize = _fontSize;
            _appliedFontSize = _fontSize;
        }

        GUI.skin = _skin;
    }

    private void DrawWindow(int id)
    {
        _scroll = GUILayout.BeginScrollView(_scroll);

        DrawFontSection();
        GUILayout.Space(8);
        DrawFuelSection();
        GUILayout.Space(8);
        DrawMoneySection();
        GUILayout.Space(8);
        DrawGearSection();
        GUILayout.Space(8);
        DrawStageSection();
        GUILayout.Space(8);
        DrawOptionSection();

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0, 0, 10000, 20)); // 제목 표시줄로 드래그 이동
    }

    private void DrawFontSection()
    {
        GUILayout.Label($"── 글자 크기 ──  {_fontSize}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("－")) _fontSize = Mathf.Max(FONT_MIN, _fontSize - 2);
        if (GUILayout.Button("＋")) _fontSize = Mathf.Min(FONT_MAX, _fontSize + 2);
        GUILayout.EndHorizontal();
        // 슬라이더로도 조절 (정수 단위로 반올림)
        _fontSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(_fontSize, FONT_MIN, FONT_MAX));
    }

    private void DrawFuelSection()
    {
        GUILayout.Label($"── 연료 ──  {_fuelCur:F1} / {_fuelMax:F1}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"-{_fuelStep}")) UPlayer.ConsumeFuel(_fuelStep);
        if (GUILayout.Button($"+{_fuelStep}")) UPlayer.RecoverFuel(_fuelStep);
        if (GUILayout.Button("가득")) UPlayer.RecoverFuel(_fuelMax); // Max로 clamp됨
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("페널티 +5")) UPlayer.ApplyFuelPenalty(5f);
        if (GUILayout.Button("새 잠수 리셋")) UPlayer.ResetForNew();
        GUILayout.EndHorizontal();
        GUILayout.Label($"가방: {_bagCount} / {_bagCap}");
    }

    private void DrawMoneySection()
    {
        GUILayout.Label($"── 재화 ──  {_money} G");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"-{_moneyStep}")) UPlayer.TrySpendMoney(_moneyStep);
        if (GUILayout.Button($"+{_moneyStep}")) UPlayer.AddMoney(_moneyStep);
        GUILayout.EndHorizontal();
    }

    private void DrawGearSection()
    {
        GUILayout.Label("── 장비 레벨 ──");
        DrawGearRow(EDataType.FuelTank, "연료탱크");
        DrawGearRow(EDataType.Thruster, "추진기");
        DrawGearRow(EDataType.Radar, "레이더");
        DrawGearRow(EDataType.GrabTool, "집게팔");
        DrawGearRow(EDataType.Bag, "가방");
    }

    private void DrawGearRow(EDataType type, string label)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: Lv.{UPlayer.GetGearLevel(type)}", GUILayout.Width(_fontSize * 10));
        if (GUILayout.Button("업그레이드"))
        {
            // 비용 무시하고 강제 레벨업(디버그). UpgradeGear는 MaxLevel 방어 포함.
            if (!UPlayer.UpgradeGear(type))
                UDebug.Print($"[디버그] {label} 업그레이드 실패 (최대 레벨?)", LogType.Warning);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawStageSection()
    {
        GUILayout.Label($"── 스테이지 ──  현재 {UPlayer.CurrentStage} / 해금 {UPlayer.UnlockedStage}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("다음 해금")) UPlayer.UnlockNextStage();
        if (GUILayout.Button("현재 -1")) UPlayer.SetCurrentStage(Mathf.Max(0, UPlayer.CurrentStage - 1));
        if (GUILayout.Button("현재 +1")) UPlayer.SetCurrentStage(UPlayer.CurrentStage + 1);
        GUILayout.EndHorizontal();
    }

    private void DrawOptionSection()
    {
        OptionData opt = CLocalOptionManager.Ins.Option;
        GUILayout.Label("── 옵션(읽기) ──");
        GUILayout.Label($"볼륨 M:{opt.masterVolume:F2} B:{opt.bgmVolume:F2} S:{opt.sfxVolume:F2} A:{opt.ambienceVolume:F2}");
        GUILayout.Label($"해상도 {opt.resolutionWidth}x{opt.resolutionHeight} / {opt.fullScreenMode}");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        RefreshCache();
        // 값 변경 이벤트 구독 → 표시 캐시 갱신
        CEventBus<OnPlayerFuelChanged>.Subscribe(FuelHandler);
        CEventBus<OnPlayerBagChanged>.Subscribe(BagHandler);
        CEventBus<OnMoneyChanged>.Subscribe(MoneyHandler);
        CEventBus<OnInputCheat>.Subscribe(CheatHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerFuelChanged>.Unsubscribe(FuelHandler);
        CEventBus<OnPlayerBagChanged>.Unsubscribe(BagHandler);
        CEventBus<OnMoneyChanged>.Unsubscribe(MoneyHandler);
        CEventBus<OnInputCheat>.Unsubscribe(CheatHandler);
    }

    private void OnGUI()
    {
        if (!_open) return;

        ApplyFontSize(); // 폰트 크기를 이 패널에만 적용
        _window = GUI.Window(WINDOW_ID, _window, DrawWindow, $"디버그 패널 (F1로 토글)");
    }
    #endregion
}
#endif
