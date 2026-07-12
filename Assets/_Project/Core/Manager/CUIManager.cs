using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 전체에서 공용으로 쓰이는 전역 UI의 생명주기를 관리하는 매니저입니다.
/// </summary>
public class CUIManager : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("전역 UI 설정")]
    [SerializeField] private CUIRegistrySO _uiRegistry; // UI 프리팹들을 등록한 SO
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
    }

    private void OnEnable()
    {
        if (_instance != this) return;

        CEventBus<OnRequestOpenUI>.Subscribe(OpenUI);
        CEventBus<OnRequestCloseUI>.Subscribe(CloseUI);
    }

    private void OnDisable()
    {
        if (_instance != this) return;

        CEventBus<OnRequestOpenUI>.Unsubscribe(OpenUI);
        CEventBus<OnRequestCloseUI>.Unsubscribe(CloseUI);
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

    private void OpenUI(OnRequestOpenUI ctx)
    {
        EUI targetUI = ctx.uIType;

        // 1) 이미 생성된 인스턴스가 있으면 활성화만
        if (_uiInstanceDict.TryGetValue(targetUI, out GameObject instance) && instance != null)
        {
            instance.SetActive(true);
            return;
        }

        // 2) 없으면 프리팹으로 지연 생성 (프리팹이 자체 Canvas를 가지므로 부모 없이 생성)
        if (_uiPrefabDict.TryGetValue(targetUI, out GameObject prefab) && prefab != null)
        {
            GameObject newInstance = Instantiate(prefab);
            newInstance.SetActive(true);
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
            instance.SetActive(false);
        }
    }
    #endregion
}
