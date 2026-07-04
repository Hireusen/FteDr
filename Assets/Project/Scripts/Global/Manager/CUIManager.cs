using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 전체에서 공용으로 사용되는 전역 UI(설정창, 알림창 등)의 생명주기를 관리하는 매니저 클래스입니다.
/// </summary>
public class CUIManager : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("Global UI Settings")]
    [SerializeField] private CUIRegistrySO _uiRegistry;                        // UI 프리팹들을 등록한 SO
    [SerializeField] private Transform _globalCanvasTransform;                 // DontDestroyOnLoad 처리된 글로벌 캔버스 Transform
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    // 프리펩 캐싱용 딕셔너리
    private readonly Dictionary<EUI, GameObject> _uiPrefabDict = new Dictionary<EUI, GameObject>();

    // 생성된 인스턴스 캐싱용 딕셔너리
    private readonly Dictionary<EUI, GameObject> _uiInstanceDict = new Dictionary<EUI, GameObject>();
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        // 씬 전환 시 파괴 방지 (Singleton 처리가 되어있다고 가정하거나 단순 방지)
        DontDestroyOnLoad(gameObject);
        InitializePrefab();
    }

    private void OnEnable()
    {
        // [구독] 설정창 열기/닫기 이벤트 등록
        CEventBus<OnRequestOpenUI>.Subscribe(OpenUI);
        CEventBus<OnRequestCloseUI>.Subscribe(CloseUI);
    }

    private void OnDisable()
    {
        // [해제] 메모리 누수 방지
        CEventBus<OnRequestOpenUI>.Unsubscribe(OpenUI);
        CEventBus<OnRequestCloseUI>.Unsubscribe(CloseUI);
    }

    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>
    /// ScriptableObject 데이터를 기반으로 Dictionary를 초기화합니다.
    /// </summary>
    private void InitializePrefab()
    {
        if (_uiRegistry == null || _uiRegistry.UIList == null)
        {
            UDebug.Print("[CUIManager] UI Registry가 지정되지 않았거나 비어있습니다.");
            return;
        }
        // [최적화] foreach 대신 규칙에 맞는 for 루프를 사용하여 Dictionary 매핑 수행
        int dataCount = _uiRegistry.UIList.Count;
        for (int i = 0; i < dataCount; i++)
        {
            var data = _uiRegistry.UIList[i];
            if (data.uIType != EUI.None && data.uIPrefab != null)
            {
                if (!_uiPrefabDict.ContainsKey(data.uIType))
                {
                    _uiPrefabDict.Add(data.uIType, data.uIPrefab);
                }
            }
        }
    }
    /// <summary>
    /// 이벤트를 수신받아 특정 UI를 열어주고 필요한 경우 캐싱합니다.
    /// </summary>
    private void OpenUI(OnRequestOpenUI ctx)
    {
        EUI targetUI = ctx.UIType;
        // 1. 이미 캐싱되어 생성된 인스턴스가 있다면 활성화만 해주고 종료
        if (_uiInstanceDict.TryGetValue(targetUI, out GameObject instance) && instance != null)
        {
            instance.SetActive(true);
            return;
        }
        // 2. 캐싱된 인스턴스가 없을 경우 프리팹을 찾아 지연 생성(Lazy Initialization)
        if (_uiPrefabDict.TryGetValue(targetUI, out GameObject prefab) && prefab != null)
        {
            if (_globalCanvasTransform != null)
            {
                GameObject newInstance = Instantiate(prefab, _globalCanvasTransform);
                newInstance.SetActive(true);
                // 생성된 인스턴스를 즉시 딕셔너리에 추가하여 다음 요청 시 활용
                _uiInstanceDict[targetUI] = newInstance;
            }
            else
            {
                UDebug.Print("[CUIManager] 글로벌 캔버스 트랜스폼이 지정되지 않았습니다.");
            }
        }
        else
        {
            UDebug.Print($"[CUIManager] 등록되지 않은 UI 호출 시도: {targetUI}");
        }
    }
    /// <summary>
    /// 이벤트를 수신받아 특정 UI를 비활성화(캐싱 보존)합니다.
    /// </summary>
    private void CloseUI(OnRequestCloseUI ctx)
    {
        EUI targetUI = ctx.UIType;
        // 딕셔너리에서 인스턴스를 찾고, 굳이 Destroy 하지 않고 SetActive(false)로 처리
        if (_uiInstanceDict.TryGetValue(targetUI, out GameObject instance) && instance != null)
        {
            instance.SetActive(false);
        }
    }
    #endregion


}
