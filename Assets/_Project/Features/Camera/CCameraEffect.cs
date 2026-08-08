using System.Collections;
using UnityEngine;

/// <summary>
/// 카메라 이펙트 모음
/// </summary>
public class CCameraEffect : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private Transform _cam;
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
        Vector3 origin = _cam.localPosition;
        Vector3 target = origin + Vector3.down * 0.05f;

        float t = 0;

        while (t < 0.03f)
        {
            _cam.localPosition = Vector3.Lerp(origin, target, t / 0.03f);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0;

        while (t < 0.12f)
        {
            _cam.localPosition = Vector3.Lerp(target, origin, t / 0.12f);
            t += Time.deltaTime;
            yield return null;
        }

        _cam.localPosition = origin;
    }
    #endregion
}
