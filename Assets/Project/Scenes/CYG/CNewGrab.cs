using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CNewGrab : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] GameObject arm;
    [SerializeField] GameObject armEndPivot;
    [SerializeField] GameObject twizers;
    [SerializeField] ConfigurableJoint twizersJointToArm;
    [SerializeField] ConfigurableJoint twizersJointToObj;
    [SerializeField] float ShootForce = 10f;
    [SerializeField] Transform playerCam;
    [SerializeField] float maxdistance=3f;

    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private ConfigurableJoint armJoint;
    private Rigidbody armRigidBody;
    private Rigidbody twizersRigidBody;
    private float armOriginScale;
    private float armOriginLength;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public enum EGrabStatus
    {
        Wait,
        Shooting,
        DoGrab,
        Grabed,
        Bring
    }
    public EGrabStatus grabStatus=EGrabStatus.Wait;
    public void ShootWrist()
    {
        twizersRigidBody.isKinematic = false;
        twizersRigidBody.AddForce(ShootForce * playerCam.forward, ForceMode.Impulse);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void JointFree(ConfigurableJoint joint)
    {
        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;
        joint.connectedBody = null;
    }
    private void JointOn(ConfigurableJoint joint, Rigidbody objRg)
    {
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.connectedBody = objRg;
    }
    private bool DistanceCk()
    {
        float distance = (arm.transform.position - twizers.transform.position).magnitude;
        if (distance < maxdistance) return true;
        else return false;
    }
    private void ExtendArm()
    {

        Vector3 dir = (twizers.transform.position - arm.transform.position);
        float distance = dir.magnitude;

        arm.transform.forward = dir.normalized;

        Vector3 scale = arm.transform.localScale;
        scale.z = distance / armOriginLength * armOriginScale;
        arm.transform.localScale = scale;

    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        armJoint=arm.GetComponent<ConfigurableJoint>();
        twizersJointToArm=twizers.GetComponent<ConfigurableJoint>();
        armRigidBody=arm.GetComponent<Rigidbody>();
        twizersRigidBody=twizers.GetComponent<Rigidbody>();
        armOriginScale = arm.transform.localScale.z;
        armOriginLength= (arm.transform.position - armEndPivot.transform.position).magnitude;


    }
    //테스트용
    private void Update()
    {
        
        switch(grabStatus){
            case EGrabStatus.Wait:
                if (Input.GetKey(KeyCode.K))
                {
                    ShootWrist();
                    grabStatus = EGrabStatus.Shooting;
                }
                break;
            case EGrabStatus.Shooting:
                if (DistanceCk())
                {
                    ExtendArm();
                }
                else
                {

                }
                break;
            case EGrabStatus.Bring:

                break;
        }
        
    }
    

    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
