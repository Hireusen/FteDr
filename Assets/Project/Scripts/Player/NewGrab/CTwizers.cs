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
                activeJoint = grabObject.AddComponent<ConfigurableJoint>();
                activeJoint.xMotion = ConfigurableJointMotion.Locked;
                activeJoint.yMotion = ConfigurableJointMotion.Locked;
                activeJoint.zMotion = ConfigurableJointMotion.Locked;

                SoftJointLimit xlowLimit = activeJoint.lowAngularXLimit;
                xlowLimit.limit = -20f;
                activeJoint.lowAngularXLimit = xlowLimit;
                SoftJointLimit xhighLimit = activeJoint.lowAngularXLimit;
                xhighLimit.limit = 20f;
                activeJoint.lowAngularXLimit = xhighLimit;

                SoftJointLimit yLimit = activeJoint.angularYLimit;
                yLimit.limit = 30f;
                activeJoint.angularYLimit = yLimit;

                SoftJointLimit zLimit = activeJoint.angularZLimit;
                zLimit.limit = 30f;
                activeJoint.angularZLimit = zLimit;
                activeJoint.connectedBody = _twizersRG;


                ///물건에서 받아와야함.
                activeJoint.breakForce = 5f;
                activeJoint.breakTorque = 5f;
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
