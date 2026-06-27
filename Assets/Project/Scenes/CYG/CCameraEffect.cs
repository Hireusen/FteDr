using System.Collections;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CCameraEffect : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    //[Header("주제")]
    //[SerializeField] private Class _class;
    [SerializeField] Transform cam;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public void CameraKick()
    {
        StartCoroutine(CameraKickCo());
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private IEnumerator CameraKickCo()
    {
        Vector3 origin = cam.localPosition;
        Vector3 target = origin + Vector3.down * 0.05f;

        float t = 0;

        while (t < 0.03f)
        {
            cam.localPosition = Vector3.Lerp(origin, target, t / 0.03f);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0;

        while (t < 0.12f)
        {
            cam.localPosition = Vector3.Lerp(target, origin, t / 0.12f);
            t += Time.deltaTime;
            yield return null;
        }

        cam.localPosition = origin;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
