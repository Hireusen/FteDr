using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 전체에서 공용으로 쓰이는 전역 UI의 생명주기를 관리하는 매니저입니다.
/// 인스턴스가 IUIWindow를 구현하면 Open/Close를 호출해 공통 페이드 연출을 태우고,
/// 구현하지 않았다면 예전처럼 즉시 SetActive로 처리합니다. (하위호환)
/// </summary>
public class CUIManager : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("전역 UI 설정")]
    [SerializeField] private CUIRegistrySO _uiRegistry; // 프리팹 기반 UI (지연 생성)
    [Tooltip("이미 씬에 배치된 자식 캔버스 (Instantiate 없이 바로 등록). Pause/Shop/Inventory 등")]
    [SerializeField] private List<CUIRegistrySO.UIMappingData> _scenePlacedUI = new();
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public CUIManager Ins => _instance;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private static CUIManager _instance;

    // EUI → 프리팹 (SO에서 초기화)
    private readonly Dictionary<EUI, GameObject> _uiPrefabDict = new Dictionary<EUI, GameObject>();

    // EUI → 생성된 인스턴스 (지연 생성 후 캐싱)
    private readonly Dictionary<EUI, GameObject> _uiInstanceDict = new Dictionary<EUI, GameObject>();
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        // 중복 방어
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePrefab();
        RegisterScenePlacedUI();
    }

    private void OnEnable()
    {
        if (_instance != this) return;

        CEventBus<OnRequestOpenUI>.Subscribe(OpenUI);
        CEventBus<OnRequestCloseUI>.Subscribe(CloseUI);
        CEventBus<OnInputEsc>.Subscribe(EscHandler);
    }

    private void OnDisable()
    {
        if (_instance != this) return;

        CEventBus<OnRequestOpenUI>.Unsubscribe(OpenUI);
        CEventBus<OnRequestCloseUI>.Unsubscribe(CloseUI);
        CEventBus<OnInputEsc>.Unsubscribe(EscHandler);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // SO에 등록된 UI 목록으로 프리팹 딕셔너리를 초기화한다.
    private void InitializePrefab()
    {
        if (_uiRegistry == null || _uiRegistry.UIList == null)
        {
            UDebug.Print("UI Registry가 지정되지 않았거나 비어있습니다.", LogType.Warning);
            return;
        }

        int count = _uiRegistry.UIList.Count;
        for (int i = 0; i < count; ++i)
        {
            var data = _uiRegistry.UIList[i];
            if (data.uIType == EUI.None || data.uIPrefab == null) continue;
            if (_uiPrefabDict.ContainsKey(data.uIType)) continue;

            _uiPrefabDict.Add(data.uIType, data.uIPrefab);
        }
    }

    // 씬에 이미 배치된 자식들을 Instantiate 없이 바로 인스턴스 딕셔너리에 등록한다. (Pause/Shop/Inventory 등)
    private void RegisterScenePlacedUI()
    {
        int count = _scenePlacedUI.Count;
        for (int i = 0; i < count; ++i)
        {
            var data = _scenePlacedUI[i];
            if (data.uIType == EUI.None || data.uIPrefab == null) continue;
            if (_uiInstanceDict.ContainsKey(data.uIType)) continue;

            _uiInstanceDict.Add(data.uIType, data.uIPrefab);
            data.uIPrefab.SetActive(false); // 시작 시에는 숨김 (요청이 와야 열림)
        }
    }

    // 게임 씬에서만 ESC로 일시정지창을 토글한다. (타이틀 등 다른 씬에서는 무시)
    private void EscHandler(OnInputEsc ctx)
    {
        if (UScene.Current != EScene.Game) return;

        bool isOpen = _uiInstanceDict.TryGetValue(EUI.PauseMenuWindow, out GameObject instance) && instance != null && instance.activeSelf;

        if (isOpen)
        {
            OnRequestCloseUI.Publish(EUI.PauseMenuWindow);
        }
        else
        {
            OnRequestOpenUI.Publish(EUI.PauseMenuWindow);
        }
    }

    private void OpenUI(OnRequestOpenUI ctx)
    {
        EUI targetUI = ctx.uIType;

        // 이미 생성된 인스턴스가 있으면 Open (IUIWindow 없으면 즉시 활성화)
        if (_uiInstanceDict.TryGetValue(targetUI, out GameObject instance) && instance != null)
        {
            OpenInstance(instance);
            return;
        }

        // 없으면 프리팹으로 지연 생성 (프리팹이 자체 Canvas를 가지므로 부모 없이 생성)
        if (_uiPrefabDict.TryGetValue(targetUI, out GameObject prefab) && prefab != null)
        {
            GameObject newInstance = Instantiate(prefab);
            OpenInstance(newInstance);
            _uiInstanceDict[targetUI] = newInstance;
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
            if (instance.TryGetComponent(out IUIWindow window))
            {
                window.Close();
            }
            else
            {
                instance.SetActive(false);
            }
        }
    }

    // IUIWindow가 있으면 페이드 인, 없으면 즉시 활성화. (신규 생성/재사용 공통 경로)
    private void OpenInstance(GameObject instance)
    {
        if (instance.TryGetComponent(out IUIWindow window))
        {
            window.Open();
        }
        else
        {
            instance.SetActive(true);
        }
    }
    #endregion
}
