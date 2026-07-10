using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEditorInternal;
using UnityEngine;
/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CTwizers : AFrameable, IUpdateFrameable
{
    [SerializeField] private float customGravity = -10f;
    [SerializeField] private CFinger _finger1;
    [SerializeField] private CFinger _finger2;
    [SerializeField] private GameObject _finger1Real;
    [SerializeField] private GameObject _finger2Real;
    [SerializeField] private BoxCollider _finger1OutCollider;
    [SerializeField] private BoxCollider _finger2OutCollider;
    [SerializeField] private float _grabSpeed = 10f;
    [SerializeField] private float _grab1OpenAngle = 30;
    [SerializeField] private float _grab1CloseAngle = -16;
    [SerializeField] private float _grab2OpenAngle = -40;
    [SerializeField] private float _grab2CloseAngle = 20;
    
    [SerializeField] private float grabTime = 3f;
    [SerializeField] private float grabOpenTime = 1f;
    [SerializeField] private float breakWaitTimeSetting = 1f;
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private GameObject grabObject = null;
    private CFinger.CrashInfo grabInfo = new CFinger.CrashInfo();
    private Rigidbody _twizersRG;
    private ConfigurableJoint activeJoint = null;
    private Quaternion _finger1OpenRot;
    private Quaternion _finger2OpenRot;
    private Quaternion _finger1CloseRot;
    private Quaternion _finger2CloseRot;
    private Quaternion _finger1StartRot;
    private Quaternion _finger2StartRot;
    private Quaternion _finger1OriginRot;
    private Quaternion _finger2OriginRot;
    private Collider _finger1Col;
    private Collider _finger2Col;
    private Collider _objectCol;
    

    private float grabTimer = 0f;
    private float openTimer = 0f;
    private float breakWaitTimer = 0f;
    private Coroutine _grabCo;
    private Coroutine _openCo;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────]
    public bool Grabed { get; private set; } = false;
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        //잡고 있을때 조인트가 break될시
        if (grabInfo.crashedObject != null && activeJoint == null)
        {
            grabInfo.crashedObject = null;

            print("break!");
            //힘을 받아서 놓쳤을 경우 잡기 쿨타임
            breakWaitTimer = breakWaitTimeSetting;
        }
        if(breakWaitTimer>0) breakWaitTimer-=Time.deltaTime;

        //잡기 체크 프로토타입2
        if (breakWaitTimer <= 0 && _finger1.crashObjects.Count > 0 && _finger2.crashObjects.Count > 0)
        {
            HashSet<CFinger.CrashInfo> crashInfos= new HashSet<CFinger.CrashInfo>(_finger1.crashObjects);
            crashInfos.IntersectWith(_finger2.crashObjects);



            if (crashInfos.Count > 0 && grabInfo.crashedObject == null)
            {
                print("잡기 시도");
                grabInfo = crashInfos.First();
                Vector3 f1joint = Vector3.zero;
                Vector3 f2joint = Vector3.zero;
                if (_finger1.crashObjects.TryGetValue(grabInfo, out CFinger.CrashInfo f1data))
                {
                    f1joint = f1data.crashPoint;
                    print("finger1 joint set : " + f1joint);
                }
                else
                {
                    print("f1 joint error");
                }
                if (_finger2.crashObjects.TryGetValue(grabInfo, out CFinger.CrashInfo f2data))
                {
                    f2joint = f2data.crashPoint;
                    print("finger2 joint set: " + f2joint);
                }
                else
                {
                    print("f2 joint error");
                }
                activeJoint = grabInfo.crashedObject.AddComponent<ConfigurableJoint>();
                activeJoint.xMotion = ConfigurableJointMotion.Locked;
                activeJoint.yMotion = ConfigurableJointMotion.Locked;
                activeJoint.zMotion = ConfigurableJointMotion.Locked;

                SoftJointLimit xlowLimit = activeJoint.lowAngularXLimit;
                xlowLimit.limit = 0f;
                activeJoint.lowAngularXLimit = xlowLimit;
                SoftJointLimit xhighLimit = activeJoint.lowAngularXLimit;
                xhighLimit.limit = 0f;
                activeJoint.lowAngularXLimit = xhighLimit;

                SoftJointLimit yLimit = activeJoint.angularYLimit;
                yLimit.limit = 10f;
                activeJoint.angularYLimit = yLimit;

                SoftJointLimit zLimit = activeJoint.angularZLimit;
                zLimit.limit = 0f;
                activeJoint.angularZLimit = zLimit;
                activeJoint.connectedBody = _twizersRG;

                //조인트 앵커 설정
                Vector3 avgPoint = (f1joint + f2joint) / 2;
                activeJoint.autoConfigureConnectedAnchor = false;
                activeJoint.enableCollision = false;
                activeJoint.connectedAnchor = _twizersRG.transform.InverseTransformPoint(avgPoint);
                activeJoint.anchor = grabInfo.crashedObject.transform.InverseTransformPoint(avgPoint);

                // 살짝 벌려서 지터링 방지
                Vector3 temp = _finger1Real.transform.localEulerAngles;
                temp.x += 5;
                _finger1Real.transform.localRotation = Quaternion.Euler(temp);
                temp = _finger2Real.transform.localEulerAngles;
                temp.x -= 5;
                _finger2Real.transform.localRotation= Quaternion.Euler(temp);
                // collision 무시
                _objectCol=grabInfo.crashedObject.GetComponent<BoxCollider>();
                Physics.IgnoreCollision(_finger1Col, _objectCol,true);
                Physics.IgnoreCollision(_finger2Col, _objectCol, true);
                Physics.IgnoreCollision(_finger1OutCollider, _objectCol, true);
                Physics.IgnoreCollision(_finger2OutCollider, _objectCol, true);



                //브레이크 포스 설정 물건에서 받아와야함
                // activeJoint.breakForce = 50f;

                //잡았으면 집게 접는거 멈추게 해야함.
                StopGrabMove();

            }

        }

    }

    public GameObject GetItemInfo()
    {
        if (grabInfo.crashedObject != null)
        {
            CFinger.CrashInfo temp = grabInfo;
            Destroy(activeJoint);
            activeJoint = null;

            grabInfo = default;

            return temp.crashedObject;
            
        }
        return null;
    }
    public void GrabContinuous()
    {
        
        grabTimer = 0;
        _finger1StartRot = _finger1Real.transform.localRotation;
        _finger2StartRot = _finger2Real.transform.localRotation;

        if(_openCo!=null)StopCoroutine(_openCo);
        _grabCo= StartCoroutine(GrabContinueCo());
    }
    public void StopGrabMove()
    {
        if(_grabCo!=null)StopCoroutine(_grabCo);
        
        Grabed = true;
    }
    public void OpenGrabContinuous()
    {
        Grabed = false;
        openTimer = 0;
        _finger1StartRot = _finger1Real.transform.localRotation;
        _finger2StartRot = _finger2Real.transform.localRotation;

        _openCo=StartCoroutine(OpenGrabCo());
    }
    public void GrabToOriginContinuous()
    {
        openTimer = 0;
        _finger1StartRot = _finger1Real.transform.localRotation;
        _finger2StartRot = _finger2Real.transform.localRotation;

        _openCo = StartCoroutine(GrabToOriginCo());
    }
    public void CollisionOn()
    {
        Physics.IgnoreCollision(_finger1Col, _objectCol, false);
        Physics.IgnoreCollision(_finger2Col, _objectCol, false);
        Physics.IgnoreCollision(_finger1OutCollider, _objectCol, false);
        Physics.IgnoreCollision(_finger2OutCollider, _objectCol, false);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────


    private IEnumerator GrabContinueCo()
    {
        while (grabTimer < grabTime)
        {
            grabTimer += Time.deltaTime;
            _finger1Real.transform.localRotation = Quaternion.Slerp(_finger1StartRot, _finger1CloseRot, grabTimer / grabTime);
            _finger2Real.transform.localRotation = Quaternion.Slerp(_finger2StartRot, _finger2CloseRot, grabTimer / grabTime);
            yield return null;
        }
        _finger1Real.transform.localRotation = _finger1CloseRot;
        _finger2Real.transform.localRotation = _finger2CloseRot;
        Grabed = true;
    }
    private IEnumerator OpenGrabCo()
    {
        print("open start");
        while(openTimer < grabOpenTime)
        {
            openTimer += Time.deltaTime;
            _finger1Real.transform.localRotation=Quaternion.Slerp(_finger1StartRot,_finger1OpenRot, openTimer / grabOpenTime);
            _finger2Real.transform.localRotation=Quaternion.Slerp(_finger2StartRot,_finger2OpenRot, openTimer / grabOpenTime);
            yield return null;
        }
        print("open complete");
        _finger1Real.transform.localRotation = _finger1OpenRot;
        _finger2Real.transform.localRotation = _finger2OpenRot;
    }
    private IEnumerator GrabToOriginCo()
    {
        print("toOrigin start");
        while (openTimer < grabOpenTime)
        {
            openTimer += Time.deltaTime;
            _finger1Real.transform.localRotation = Quaternion.Slerp(_finger1StartRot, _finger1OriginRot, openTimer / grabOpenTime);
            _finger2Real.transform.localRotation = Quaternion.Slerp(_finger2StartRot, _finger2OriginRot, openTimer / grabOpenTime);
            yield return null;
        }
        print("toOrigin complete");
        _finger1Real.transform.localRotation = _finger1OriginRot;
        _finger2Real.transform.localRotation = _finger2OriginRot;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _twizersRG=GetComponent<Rigidbody>();
        _finger1Col=_finger1.GetComponent<BoxCollider>();
        _finger2Col=_finger2.GetComponent<BoxCollider>();
        _finger1OriginRot = _finger1Real.transform.localRotation;
        _finger2OriginRot = _finger2Real.transform.localRotation;

        // Finger1
        Vector3 finger1Euler = _finger1OriginRot.eulerAngles;

        Vector3 finger1OpenEuler = finger1Euler;
        finger1OpenEuler.x += _grab1OpenAngle;
        _finger1OpenRot = Quaternion.Euler(finger1OpenEuler);

        Vector3 finger1CloseEuler = finger1Euler;
        finger1CloseEuler.x += _grab1CloseAngle;
        _finger1CloseRot = Quaternion.Euler(finger1CloseEuler);

        // Finger2
        Vector3 finger2Euler = _finger2OriginRot.eulerAngles;

        Vector3 finger2OpenEuler = finger2Euler;
        finger2OpenEuler.x += _grab2OpenAngle;
        _finger2OpenRot = Quaternion.Euler(finger2OpenEuler);

        Vector3 finger2CloseEuler = finger2Euler;
        finger2CloseEuler.x += _grab2CloseAngle;
        _finger2CloseRot = Quaternion.Euler(finger2CloseEuler);
    }
    #endregion
}
