using System.Collections.Generic;
using UnityEngine;
using System.Linq;
/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CTwizers : AFrameable, IUpdateFrameable
{
    [SerializeField] CFinger finger1;
    [SerializeField] CFinger finger2;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    GameObject grabObject = null;
    Rigidbody _twizersRG;
    ConfigurableJoint activeJoint = null;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        //잡고 있을때 조인트가 break될시
        if(grabObject!=null && activeJoint == null)
        {
            grabObject = null;
            print("break!");
        }

        //잡기 체크 프로토타입
        if (finger1.Objects.Count > 0 && finger2.Objects.Count>0)
        {
            HashSet<GameObject> temp = new HashSet<GameObject>(finger1.Objects);
            temp.IntersectWith(finger2.Objects);
            if (temp.Count > 0 && grabObject == null)
            {
                print("잡앗음");
                grabObject = temp.First();
                ConfigurableJoint objJoint = grabObject.AddComponent<ConfigurableJoint>();
                objJoint.xMotion = ConfigurableJointMotion.Locked;
                objJoint.yMotion = ConfigurableJointMotion.Locked;
                objJoint.zMotion = ConfigurableJointMotion.Locked;

                SoftJointLimit xlowLimit = objJoint.lowAngularXLimit;
                xlowLimit.limit = -20f;
                objJoint.lowAngularXLimit = xlowLimit;
                SoftJointLimit xhighLimit = objJoint.lowAngularXLimit;
                xhighLimit.limit = 20f;
                objJoint.lowAngularXLimit = xhighLimit;

                SoftJointLimit yLimit = objJoint.angularYLimit;
                yLimit.limit = 30f;
                objJoint.angularYLimit = yLimit;

                SoftJointLimit zLimit = objJoint.angularZLimit;
                zLimit.limit = 30f;
                objJoint.angularZLimit = zLimit;
                objJoint.connectedBody = _twizersRG;
            }
        }
        
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _twizersRG=GetComponent<Rigidbody>();
    }
    #endregion
}
