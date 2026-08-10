using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CFingerOutCollider : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private LayerMask _crashLayers;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public bool CrashCk { get; private set;}
    public void CancelCrashCk()
    {
        CrashCk = false;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnCollisionEnter(Collision collision)
    {
        UDebug.Print("crashckout");
        if ((_crashLayers.value & (1 << collision.gameObject.layer)) > 0)
        {
            UDebug.Print("crashck");
            CrashCk = true;
        }
    }
    
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
