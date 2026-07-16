using System.Collections;
using UnityEngine;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CEndingFuncs : AFrameable, IUpdateFrameable
{
    [SerializeField] private GameObject _arm;
    [SerializeField] private GameObject _armEndpivot;
    [SerializeField] private GameObject _twizers;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public void ArmToShip()
    {
        StartCoroutine(ArmToShipCo(5));
    }
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    
    private IEnumerator ArmToShipCo(float duration)
    {
        float timer = 0;
        Vector3 originScale = _arm.transform.localScale;
        while (timer<duration)
        {
            timer += Time.deltaTime;
            float currentScalez = Mathf.Lerp(originScale.z, 97, timer / duration);
            Vector3 currentScale = originScale;
            currentScale.z = currentScalez;
            _arm.transform.localScale = currentScale;
            _twizers.transform.position = _armEndpivot.transform.position;
            yield return null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    
    #endregion
}
