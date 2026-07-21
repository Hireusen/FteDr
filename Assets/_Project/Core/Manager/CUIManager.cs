using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 창들의 생명주기와 "열림 순서(스택)"를 관리하는 매니저입니다.
/// Title의 버튼 4개(Start/Load/Setting/Credit)는 이 매니저 대상이 아닙니다 —> 그냥 씬 버튼입니다.
/// 이 매니저가 다루는 건 "창(Window)" 단위: Pause/Settings/Credits/Inventory/Shop/Loading/Result.
///
/// 이 매니저는 CBootManager가 부팅 시점에 새로 생성하는 전역 오브젝트이므로,
/// 씬에 있는 창들을 미리 인스펙터로 연결해둘 방법이 없습니다. 대신 각 창(CUIWindow)이
/// 자신의 Start()에서 스스로를 등록(<see cref="RegisterWindow"/>)하고, 파괴될 때 등록 해제합니다.
/// Settings/Credits처럼 씬 경계를 넘어 재사용되는 UI만 프리팹으로 등록(<see cref="_uiRegistry"/>)해 지연 생성합니다.
/// </summary>
public sealed class CUIManager : ASingleton<CUIManager>
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("프리팹 기반 창 (씬 경계를 넘나드는 UI: Settings/Credits/Loading 등)")]
    [SerializeField] private CUIRegistrySO _uiRegistry;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override bool IsGlobal => true;

    /// <summary>현재 열려있는 창들을 연 순서대로 반환합니다. (가장 나중에 연 것이 마지막)</summary>
    public IReadOnlyList<EUI> OpenStack => _openStack;

    public bool IsOpen(EUI ui)
    {
        int length = _openStack.Count;
        for (int i = 0; i < length; ++i)
        {
            if (_openStack[i] == ui) return true;
        }
        return false;
    }
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly Dictionary<EUI, GameObject> _uiPrefabDict = new();   // EUI → 프리팹
    private readonly Dictionary<EUI, GameObject> _uiInstanceDict = new(); // EUI → 실제 인스턴스(씬 배치 or 생성됨)
    private readonly List<EUI> _openStack = new();                        // 연 순서대로 쌓이는 스택

    private bool _lastHudVisible = true;
    #endregion
    
    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnRequestOpenUI>.Subscribe(OpenUI);
        CEventBus<OnRequestCloseUI>.Subscribe(CloseUI);
        CEventBus<OnInputEsc>.Subscribe(EscHandler);
        CEventBus<OnSceneLoadEnd>.Subscribe(SceneLoadEndHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnRequestOpenUI>.Unsubscribe(OpenUI);
        CEventBus<OnRequestCloseUI>.Unsubscribe(CloseUI);
        CEventBus<OnInputEsc>.Unsubscribe(EscHandler);
        CEventBus<OnSceneLoadEnd>.Unsubscribe(SceneLoadEndHandler);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // ASingleton이 최초 1회 호출한다.
    protected override void Initialize()
    {
        InitializePrefab();
        RefreshCursor(); // 최초 진입 시점(예: Title)에도 씬 컨텍스트에 맞는 커서 상태를 바로 반영
    }

    private void InitializePrefab()
    {
        if (_uiRegistry == null || _uiRegistry.UIList == null) return;

        int count = _uiRegistry.UIList.Count;
        for (int i = 0; i < count; ++i)
        {
            var data = _uiRegistry.UIList[i];
            if (data.uIType == EUI.None || data.uIPrefab == null) continue;
            if (_uiPrefabDict.ContainsKey(data.uIType)) continue;

            _uiPrefabDict.Add(data.uIType, data.uIPrefab);
        }
    }

    /// <summary>
    /// 창(CUIWindow)이 자신의 Start()에서 스스로를 등록합니다. 이미 같은 타입이 등록되어 있으면 덮어씁니다.
    /// 등록 직후에는 기본적으로 숨김 상태로 만듭니다(요청이 와야 열림).
    /// </summary>
    public void RegisterWindow(EUI uiType, GameObject instance)
    {
        if (uiType == EUI.None || instance == null) return;

        if (_uiInstanceDict.TryGetValue(uiType, out GameObject existing) && existing != null && existing != instance)
        {
            UDebug.Print($"CUIManager: EUI '{uiType}'가 이미 다른 인스턴스로 등록되어 있습니다. 같은 타입을 가진 창이 씬에 중복으로 있는지 확인해주세요.", LogType.Warning, instance);
        }

        _uiInstanceDict[uiType] = instance;
        instance.SetActive(false);
    }

    /// <summary>
    /// 창(CUIWindow)이 파괴될 때(씬 전환 등) 스스로 등록 해제합니다.
    /// </summary>
    public void UnregisterWindow(EUI uiType, GameObject instance)
    {
        if (uiType == EUI.None) return;
        if (!_uiInstanceDict.TryGetValue(uiType, out GameObject existing) || existing != instance) return;

        _uiInstanceDict.Remove(uiType);

        if (_openStack.Remove(uiType))
        {
            RefreshHudVisibility();
            RefreshCursor();
        }
    }

    private void OpenUI(OnRequestOpenUI ctx)
    {
        EUI targetUI = ctx.uIType;

        // 이미 등록된 인스턴스(씬 배치 or 이전에 생성됨)가 있으면 그대로 사용
        if (_uiInstanceDict.TryGetValue(targetUI, out GameObject instance) && instance != null)
        {
            OpenInstance(instance);
            PushStack(targetUI);
            RefreshHudVisibility();
            RefreshCursor();
            return;
        }

        // 없으면 프리팹으로 지연 생성
        if (_uiPrefabDict.TryGetValue(targetUI, out GameObject prefab) && prefab != null)
        {
            GameObject newInstance = Instantiate(prefab);
            OpenInstance(newInstance);
            _uiInstanceDict[targetUI] = newInstance;
            PushStack(targetUI);
            RefreshHudVisibility();
            RefreshCursor();
        }
        else
        {
            UDebug.Print($"등록되지 않은 UI 호출 시도: {targetUI}", LogType.Warning);
        }
    }

    private void CloseUI(OnRequestCloseUI ctx)
    {
        EUI targetUI = ctx.uIType;
        if (_uiInstanceDict.TryGetValue(targetUI, out GameObject instance) && instance != null)
        {
            if (instance.TryGetComponent(out IUIWindow window)) window.Close();
            else instance.SetActive(false);
        }

        _openStack.Remove(targetUI);
        RefreshHudVisibility();
        RefreshCursor();
    }

    // ESC: 열려있는 게 있으면 가장 최근에 연 것만 닫고, 아무것도 없으면 일시정지창을 새로 연다.
    // (Pause 위에 Settings를 열어둔 상태에서 ESC → Settings만 닫히고 Pause는 그대로 유지됨)
    // 단, Result가 떠있는 동안은 ESC 자체를 무시한다 (정산 화면에서는 닫기/상점이동 버튼으로만 나가야 함).
    private void EscHandler(OnInputEsc ctx)
    {
        if (_openStack.Contains(EUI.ResultWindow)) return;

        if (_openStack.Count > 0)
        {
            EUI top = _openStack[^1];
            OnRequestCloseUI.Publish(top);
        }
        else
        {
            OnRequestOpenUI.Publish(EUI.PauseMenuWindow);
        }
    }

    private void PushStack(EUI uiType)
    {
        _openStack.Remove(uiType);  // 이미 스택에 있었다면 제거 후
        _openStack.Add(uiType);     // 맨 위로 다시 쌓는다 (같은 창을 다시 열어도 순서가 갱신됨)
    }

    // 씬이 바뀌면(Title↔Game) 커서가 필요한 컨텍스트도 바뀌므로 다시 계산한다.
    private void SceneLoadEndHandler(OnSceneLoadEnd ctx)
    {
        RefreshCursor();
    }

    // IUIWindow가 있으면 페이드 인, 없으면 즉시 활성화.
    private void OpenInstance(GameObject instance)
    {
        if (instance.TryGetComponent(out IUIWindow window)) window.Open();
        else instance.SetActive(true);
    }

    // 지금 스택에 쌓여있는 창들 중 하나라도 HidesHud=true면 HUD를 숨긴다.
    // 값이 이전과 달라졌을 때만 이벤트를 발행한다. (Shop/Inventory 둘 다 닫혀야 다시 보임)
    private void RefreshHudVisibility()
    {
        bool shouldHide = false;
        int count = _openStack.Count;
        for (int i = 0; i < count; ++i)
        {
            if (!_uiInstanceDict.TryGetValue(_openStack[i], out GameObject instance) || instance == null) continue;
            if (instance.TryGetComponent(out IUIWindow window) && window.HidesHud)
            {
                shouldHide = true;
                break;
            }
        }

        bool nextVisible = !shouldHide;
        if (nextVisible == _lastHudVisible) return;

        _lastHudVisible = nextVisible;
        OnRequestHudVisibility.Publish(nextVisible);
    }

    // 커서 표시 여부를 개별 창이 아니라 여기서 한 곳에서만 계산한다.
    // 스택에 하나라도 열려있으면 커서가 필요하고,
    // Game 씬이 아니면(Title 등) 게임플레이 잠금 자체가 의미 없으므로 항상 커서가 필요하다.
    // 여러 창이 겹쳐 있다가 하나만 닫혀도 나머지가 남아있으면 커서가 계속 보이도록, 매번 전체 상태를 다시 계산해서 반영한다.
    private void RefreshCursor()
    {
        int length = _openStack.Count;
        for (int i = 0; i < length; ++i)
        {
            UDebug.Print($"현재 열려있는 {i}번째 UI = {_openStack[i]}");
        }
        UDebug.Print($"현재 게임플레이 = {UScene.Current.IsGameplay()}");

        bool needsCursor = (_openStack.Count > 0) || (!UScene.Current.IsGameplay());
        UDebug.Print($"NeedsCursor = {needsCursor}");
        CInputManager.Ins?.SetCursorReason(ECursorReason.Menu, needsCursor);
    }
    #endregion
}
