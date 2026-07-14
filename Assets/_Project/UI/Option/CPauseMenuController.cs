using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 창(Resume / Setting / Title / Quit)을 담당하는 컨트롤러입니다.
/// ESC 입력으로 열고 닫기를 토글하며, 시간 정지는 창의 활성/비활성에 자동으로 연동됩니다.
/// </summary>
public class CPauseMenuController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("Pause Buttons")]
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnTitle;
    [SerializeField] private Button _btnQuit;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_btnResume != null)
        {
            _btnResume.onClick.AddListener(() => OnRequestCloseUI.Publish(EUI.PauseMenuWindow));
        }

        if (_btnOptions != null)
        {
            _btnOptions.onClick.AddListener(() => OnRequestOpenUI.Publish(EUI.SettingsWindow));
        }

        if (_btnTitle != null)
        {
            _btnTitle.onClick.AddListener(OnClickTitle);
        }

        if (_btnQuit != null)
        {
            _btnQuit.onClick.AddListener(OnClickQuit);
        }
    }

    private void OnEnable()
    {
        // 이 창이 열려있는 동안에는 게임 시간을 멈춘다. (버튼으로 열든 ESC로 열든 동일하게 적용)
        Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void OnClickTitle()
    {
        Time.timeScale = 1f; // 씬 전환 전에 명시적으로 복구 (안전장치)
        UScene.LoadWithFade(EScene.Title);
    }

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion
}
