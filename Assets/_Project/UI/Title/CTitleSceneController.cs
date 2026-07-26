using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 씬의 버튼과 씬 전환을 담당하는 컨트롤러입니다.
/// 새 게임/이어하기 시 데이터를 처리한 뒤 다음 씬으로 페이드 전환합니다.
/// </summary>
public class CTitleSceneController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("타이틀 버튼")]
    [SerializeField] private Button _btnNewGame;  // 새 게임
    [SerializeField] private Button _btnLoad;      // 이어하기
    [SerializeField] private Button _btnOptions;   // 옵션
    [SerializeField] private Button _btnCredits;   // 크레딧

    [Header("씬 전환 / 연출")]
    [SerializeField] private float _fadeOutDuration = 0.45f; // 페이드 아웃 시간
    [SerializeField] private float _fadeInDuration = 0.45f;  // 페이드 인 시간
    [SerializeField] private Color _fadeColor = Color.black; // 페이드 색
    [SerializeField] private float _hoverScaleFactor = 1.08f; // 호버 확대 비율
    [SerializeField] private float _animationDuration = 0.15f; // 크기 변화 연출 시간
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private bool _transitioning; // 씬 전환 중복 방지
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        InitButtonEvents();
        SetupResponsiveButtons();
        RefreshLoadButton();
    }
    #endregion

    #region ─────────────────────────▶ 버튼 초기화 ◀─────────────────────────
    // 버튼 클릭 이벤트를 등록한다.
    private void InitButtonEvents()
    {
        if (_btnNewGame != null) _btnNewGame.onClick.AddListener(OnNewGameClicked);
        if (_btnLoad != null) _btnLoad.onClick.AddListener(OnLoadGameClicked);

        // 옵션/크레딧: 로컬 패널을 직접 켜지 않고 전역 UI 매니저(CUIManager)에 열기 요청
        if (_btnOptions != null) _btnOptions.onClick.AddListener(() => OnRequestOpenUI.Publish(EUI.SettingsWindow));
        if (_btnCredits != null) _btnCredits.onClick.AddListener(() => OnRequestOpenUI.Publish(EUI.CreditsWindow));
    }

    // 각 버튼에 호버 확대 연출 컴포넌트를 동적으로 장착하고 파라미터를 주입한다.
    private void SetupResponsiveButtons()
    {
        Button[] targets = { _btnNewGame, _btnLoad, _btnOptions, _btnCredits };

        for (int i = 0; i < targets.Length; ++i)
        {
            if (targets[i] == null) continue;

            GameObject go = targets[i].gameObject;
            CScaleResponsiveButton responsive = go.GetComponent<CScaleResponsiveButton>();
            if (responsive == null) responsive = go.AddComponent<CScaleResponsiveButton>();

            responsive.Initialize(_hoverScaleFactor, _animationDuration);
        }
    }

    // 저장 데이터가 없으면 이어하기 버튼을 비활성화한다.
    private void RefreshLoadButton()
    {
        if (_btnLoad == null) return;

        _btnLoad.interactable = CProgressManager.Ins.HasSave;
    }
    #endregion

    #region ─────────────────────────▶ 버튼 동작 ◀─────────────────────────
    // 새 게임
    private void OnNewGameClicked()
    {
        if (_transitioning) return;

        CProgressManager.Ins.ResetProgress(); // 진행도 초기화(저장 파일 삭제)
        UPlayer.ResetForNew();                // 런타임 데이터 초기화
        UDebug.Print("새 게임 시작 → 데이터 초기화 완료");

        MoveToNextScene();
    }

    // 이어하기
    private void OnLoadGameClicked()
    {
        if (_transitioning) return;
        if (!CProgressManager.Ins.HasSave)
        {
            UDebug.Print("저장 데이터가 없어 이어하기를 취소합니다.", LogType.Warning);
            return;
        }

        CProgressManager.Ins.Load(); // 저장된 진행도 로드
        UDebug.Print("이어하기 → 진행도 로드 완료");

        MoveToNextScene();
    }

    // 다음 빌드 씬으로 페이드 전환한다.
    private void MoveToNextScene()
    {
        ApplyFadeColor();

        _transitioning = true;
        bool started = UScene.NextLoadWithFade(0f, _fadeOutDuration, _fadeInDuration,
            onProgress: p => OnSceneLoadProgress.Publish(p));
        if (!started)
        {
            UDebug.Print("다음 씬이 빌드 세팅 범위를 벗어났습니다.", LogType.Error);
            _transitioning = false;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 페이드 색을 UFade에 반영한다.
    private void ApplyFadeColor()
    {
        UFade.SetColor(_fadeColor);
    }
    #endregion
}
