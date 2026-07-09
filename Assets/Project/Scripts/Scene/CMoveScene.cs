using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CMoveScene : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    //[Header("주제")]
    //[SerializeField] private Class _class;
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
        OnRequestOpenUI.Publish(EUI.HudWindow);
        OnRequestOpenUI.Publish(EUI.InventoryWindow);
        OnRequestOpenUI.Publish(EUI.PauseMenuWindow);
        
    }
    #endregion
}
