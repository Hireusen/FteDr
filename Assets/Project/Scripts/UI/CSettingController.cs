using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CSettingController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    //[Header("주제")]
    //[SerializeField] private Class _class;
    [SerializeField] private Button _closeButton;                   // 닫기 버튼
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>
    /// 닫기 버튼 클릭 시 호출
    /// </summary>
    private void OnClickClose()
    {
        _closeButton.interactable = false;

        UFade.FadeOut(0.3f, blockRaycasts: true, onComplete: () =>
        {
            gameObject.SetActive(false);
            _closeButton.interactable = true;
            UFade.FadeIn(0.3f);
        });
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(OnClickClose);
        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
