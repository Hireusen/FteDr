using System.Collections;

using UnityEngine;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CEndingFuncs : AFrameable, IUpdateFrameable
{
    [SerializeField] private GameObject _submarine;
    [SerializeField] private float upspeed = 10f;
    [SerializeField] private GameObject _arm;
    [SerializeField] private GameObject _armEndpivot;
    [SerializeField] private GameObject _twizers;
    [SerializeField] private ConfigurableJoint _shipJoint;
    [SerializeField] private GameObject _credit;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public void ArmToShip()
    {
        StartCoroutine(ArmToShipCo(5,5));
    }
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    private void ConnectArmAndShip()
    {
        _shipJoint.connectedBody = _twizers.GetComponent<Rigidbody>();
    }
    private IEnumerator ArmToShipCo(float duration,float upDuration)
    {
        float timer = 0;
        Vector3 originScale = _arm.transform.localScale;
        while (timer<duration)
        {
            timer += Time.deltaTime;
            float currentScalez = Mathf.Lerp(originScale.z, 70, timer / duration);
            Vector3 currentScale = originScale;
            currentScale.z = currentScalez;
            _arm.transform.localScale = currentScale;
            _twizers.transform.position = _armEndpivot.transform.position;
            yield return null;
        }
        ConnectArmAndShip();
        timer = 0;
        Vector3 originPos=_submarine.transform.position;
        _shipJoint.GetComponent<Rigidbody>().isKinematic = false;
        while (timer < upDuration)
        {
            timer += Time.deltaTime;
            _submarine.transform.position += _submarine.transform.up * upspeed * Time.deltaTime;
            yield return null;

        }
        UFade.FadeOut(1,true,onComplete:PlayCredit);
    }
    public void PlayCredit()
    {
        UFade.FadeIn(1, true);
        _credit.SetActive(true);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    
    #endregion
}
