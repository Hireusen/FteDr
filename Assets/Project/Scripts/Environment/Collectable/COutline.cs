using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 원하는 타입을 정해놓으면 해당 타입에 맞게 아웃라인이 생깁니다.
/// 아웃라인은 처음에는 비활성화 상태이며, 공개 함수인 OutLineOn과 OutLineOff로 제어 가능합니다. 
/// </summary>
public class COutline : AMono
{

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    //[Header("주제")]
    //[SerializeField] private Class _class;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Outline _outline;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public enum EOutLineType
    {
        Normal
    }
    [SerializeField] EOutLineType outLineType = EOutLineType.Normal;
    public void OutLineOn()
    {
        _outline.enabled = true;
    }
    public void OutLineOff()
    {
        _outline.enabled = false;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _outline =gameObject.AddComponent<Outline>();
        _outline.enabled = false;
        switch (outLineType)
        {
            case EOutLineType.Normal:
                _outline.OutlineWidth = 8.27f;
                _outline.OutlineColor = Color.yellow;
                break;
        }
        
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
