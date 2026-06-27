using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬의 UI 제어를 담당하며, 기존 CEventBus 및 EScene 구조를 활용하여 씬 로딩을 요청하는 컨트롤러입니다.
/// </summary>
public class CTitleSceneController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("Title Buttons")]
    [SerializeField] private Button _btnNewGame;                                // 새 게임 버튼
    [SerializeField] private Button _btnLoad;                                   // 이어하기 버튼
    [SerializeField] private Button _btnOptions;                                // 옵션 버튼
    [SerializeField] private Button _btnCredits;                                // 크레딧 버튼

    [Header("Popup UI Panels (Local)")]
    [SerializeField] private GameObject _panelOptions;                          // 로컬 옵션 패널 (필요시 사용)
    [SerializeField] private GameObject _panelCredits;                          // 로컬 크레딧 패널 (로컬 토글용)
    [SerializeField] private Button _btnCloseOptions;                           // 옵션 패널 닫기 버튼
    [SerializeField] private Button _btnCloseCredits;                           // 크레딧 패널 닫기 버튼

    [Header("Transition Settings")]
    [SerializeField] private EScene _currentScene = EScene.Title;               // 현재 씬 ID (일반적으로 Title)
    [SerializeField] private EScene _nextScene = EScene.Game;                   // 새 게임 시작 시 전환할 다음 씬 ID
    [SerializeField] private float _hoverScaleFactor = 1.08f;                   // 마우스 호버 시 버튼 확대 비율
    [SerializeField] private float _animationDuration = 0.15f;                  // 크기 변화 연출 시간
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>
    /// 버튼들의 클릭 이벤트를 등록합니다.
    /// </summary>
    private void InitButtonEvents()
    {
        // 새 게임 버튼: 클릭 시 기존 구축된 OnSceneLoadStart 이벤트를 발행하여 씬 전환 시작
        if (_btnNewGame != null)
        {
            _btnNewGame.onClick.AddListener(() =>
            {
                UDebug.Print($"[CTitleSceneController] 새 게임 클릭: {_currentScene} -> {_nextScene} 로딩 이벤트를 발행합니다.");
                OnSceneLoadStart.Publish(_currentScene, _nextScene);
            });
        }

        // 이어하기 버튼: 세이브 데이터 로드 후 해당 저장된 씬으로 로딩을 유도하도록 확장 가능
        if (_btnLoad != null)
        {
            _btnLoad.onClick.AddListener(OnLoadGameClicked);
        }

        // 로컬 옵션 패널 켜고 끄기 리스너 등록
        if (_btnOptions != null)
        {
            _btnOptions.onClick.AddListener(() => ToggleOptionsPanel(true));
        }
        if (_btnCloseOptions != null)
        {
            _btnCloseOptions.onClick.AddListener(() => ToggleOptionsPanel(false));
        }

        // 로컬 크레딧 패널 켜고 끄기 리스너 등록
        if (_btnCredits != null)
        {
            _btnCredits.onClick.AddListener(() => ToggleCreditsPanel(true));
        }
        if (_btnCloseCredits != null)
        {
            _btnCloseCredits.onClick.AddListener(() => ToggleCreditsPanel(false));
        }
    }

    /// <summary>
    /// 각 버튼 오브젝트에 반응형 스케일 연출 스크립트를 동적으로 장착 및 파라미터를 주입합니다.
    /// </summary>
    private void SetupResponsiveButtons()
    {
        // GC 방지를 위해 1차원 배열을 만들어 indexing 루프로 초기화 진행 (for 루프 사용 규칙 준수)
        Button[] targetButtons = new Button[] { _btnNewGame, _btnLoad, _btnOptions, _btnCredits };

        for (int i = 0; i < targetButtons.Length; i++)
        {
            if (targetButtons[i] != null)
            {
                GameObject buttonObj = targetButtons[i].gameObject;

                // 이미 해당 스크립트가 컴포넌트로 붙어있는지 검사 후 동적 추가 (중복 방지)
                ScaleResponsiveButton responsiveScript = buttonObj.GetComponent<ScaleResponsiveButton>();
                if (responsiveScript == null)
                {
                    responsiveScript = buttonObj.AddComponent<ScaleResponsiveButton>();
                }

                // 설정된 딜레이 및 크기 주입
                responsiveScript.Initialize(_hoverScaleFactor, _animationDuration);
            }
        }
    }

    /// <summary>
    /// 이어하기 클릭 시의 로직입니다.
    /// </summary>
    private void OnLoadGameClicked()
    {
        UDebug.Print("[CTitleSceneController] 이어하기 기능이 호출되었습니다.");

        // EScene savedScene = SaveSystem.GetLastSavedScene();
        // OnSceneLoadStart.Publish(_currentScene, savedScene);
    }

    /// <summary>
    /// 로컬 옵션 패널을 켜거나 끕니다.
    /// </summary>
    private void ToggleOptionsPanel(bool isActive)
    {
        if (_panelOptions != null)
        {
            _panelOptions.SetActive(isActive);
        }
    }

    /// <summary>
    /// 로컬 크레딧 패널을 켜거나 끕니다.
    /// </summary>
    private void ToggleCreditsPanel(bool isActive)
    {
        if (_panelCredits != null)
        {
            _panelCredits.SetActive(isActive);
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        // 모든 버튼 클릭 이벤트 바인딩
        InitButtonEvents();

        // 버튼들의 마우스 호버 확대 연출 자동 셋업
        SetupResponsiveButtons();

        // [초기화 원칙] 시작 시 옵션 패널과 크레딧 패널은 비활성화 상태로 세팅
        ToggleOptionsPanel(false);
        ToggleCreditsPanel(false);
    }
    #endregion
}
