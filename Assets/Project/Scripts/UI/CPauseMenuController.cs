using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CPauseMenuController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("Pause Buttons")]
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnTitle;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        // 설정 버튼 클릭시 이벤트 발행
        if (_btnOptions != null)
        {
            UDebug.Print("설정의 옵션을 요청합니다.");
            OnRequestOpenSettings.Publish();
        }

        // 계속하기 버튼
        if (_btnResume != null)
        {
            _btnResume.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                Time.timeScale = 1.0f;
            });
        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
