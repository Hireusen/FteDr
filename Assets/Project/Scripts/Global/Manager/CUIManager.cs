using UnityEngine;

/// <summary>
/// 씬 전체에서 공용으로 사용되는 전역 UI(설정창, 알림창 등)의 생명주기를 관리하는 매니저 클래스입니다.
/// </summary>
public class CGlobalUIManager : MonoBehaviour
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("Global UI Prefabs")]
    [SerializeField] private GameObject _settingsPrefab;                       // 동적으로 생성할 설정창 프리팹
    [SerializeField] private Transform _globalCanvasTransform;                 // DontDestroyOnLoad 처리된 글로벌 캔버스 Transform
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private GameObject _activeSettingsInstance;                               // 현재 화면에 생성된 설정창 인스턴스
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        // 씬 전환 시 파괴 방지 (Singleton 처리가 되어있다고 가정하거나 단순 방지)
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // [구독] 설정창 열기/닫기 이벤트 등록
        CEventBus<OnRequestOpenSettings>.Subscribe(OpenSettingsWindow);
        CEventBus<OnRequestCloseSettings>.Subscribe(CloseSettingsWindow);
    }

    private void OnDisable()
    {
        // [해제] 메모리 누수 방지
        CEventBus<OnRequestOpenSettings>.Unsubscribe(OpenSettingsWindow);
        CEventBus<OnRequestCloseSettings>.Unsubscribe(CloseSettingsWindow);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    /// <summary>
    /// 설정창 열기 이벤트를 수신하면 설정창 프리팹을 글로벌 캔버스 아래에 동적으로 생성합니다.
    /// </summary>
    private void OpenSettingsWindow(OnRequestOpenSettings ctx)
    {
        // 중복 생성 방지
        if (_activeSettingsInstance != null)
        {
            _activeSettingsInstance.SetActive(true);
            return;
        }

        if (_settingsPrefab != null && _globalCanvasTransform != null)
        {
            // 필요한 시점에만 프리팹을 동적으로 인스턴스화
            _activeSettingsInstance = Instantiate(_settingsPrefab, _globalCanvasTransform);
        }
        else
        {
            Debug.LogWarning("[CGlobalUIManager] 프리팹 또는 글로벌 캔버스 트랜스폼이 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// 설정창 닫기 이벤트를 수신하면 설정창 인스턴스를 완전 파괴하여 메모리를 반환합니다.
    /// </summary>
    private void CloseSettingsWindow(OnRequestCloseSettings ctx)
    {
        if (_activeSettingsInstance != null)
        {
            // 단순히 끄는 것보다, 사용하지 않을 때는 파괴하여 메모리를 GC 대상이 되도록 유도
            Destroy(_activeSettingsInstance);
            _activeSettingsInstance = null;
        }
    }
    #endregion
}
