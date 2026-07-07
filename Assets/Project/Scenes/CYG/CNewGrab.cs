using System.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CNewGrab : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] GameObject shoulder;
    [SerializeField] GameObject arm;
    [SerializeField] GameObject armEndPivot;
    [SerializeField] GameObject twizers;
    [SerializeField] ConfigurableJoint twizersJointToArm;
    [SerializeField] float ShootForce = 10f;
    [SerializeField] float ShrinkSpeed = 10f;
    [SerializeField] float twizersRotateSpeed = 1f;
    [SerializeField] Transform playerCam;
    [SerializeField] float maxdistance=3f;

    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private ConfigurableJoint armJoint;
    private Rigidbody armRigidBody;
    private Rigidbody twizersRigidBody;
    private Rigidbody shoulderRg;
    private float armOriginScale;
    private float armOriginLength;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public enum EGrabStatus
    {
        Wait,
        Shooting,
        Connect,
        Grab,
        Bring,
        AdjustArm,
        BringComplete
    }
    public EGrabStatus grabStatus=EGrabStatus.Wait;
    public void ShootWrist()
    {
        twizersRigidBody.isKinematic = false;
        //twizersRigidBody.AddForce(ShootForce * playerCam.forward, ForceMode.Impulse);
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
    private void ShootWristContinuous()
    {
        //충돌하면 중지해야함, 거리제한되면 중지해야함.
        //
        twizersRigidBody.AddForce(playerCam.transform.forward * ShootForce, ForceMode.Force);
        //부력 보정
        Vector3 velocity = twizersRigidBody.velocity;
        velocity.y *= 0.5f;
        twizersRigidBody.velocity = velocity;


        //거리제한되면 자동으로 그랩동작을 시행한 다음 회수해야한다.
        if (!DistanceCk())
        {
            print("동작");
            ConnectTwizers();
        }

    }
    private bool BringDistanceCk()
    {
        float distance = (arm.transform.position - twizers.transform.position).magnitude;
        if(distance<=armOriginLength) return true;
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
    private void ConnectTwizers()
    {
        grabStatus = EGrabStatus.Connect;
        armRigidBody.isKinematic = false;
        JointOn(twizersJointToArm, armRigidBody);
        JointOn(armJoint, shoulderRg);
        twizersJointToArm.connectedAnchor = armEndPivot.transform.localPosition;


    }
    private void ShrinkArm()
    {
        Vector3 scale = arm.transform.localScale;
        scale.z -= ShrinkSpeed * Time.deltaTime;
        arm.transform.localScale = scale;

        //팔이 줄어드는 힘을 따로 구현하고 싶으면 이런식으로 구현해야 함.
        //Vector3 dir=(armEndPivot.transform.position-twizers.transform.position).normalized;

        twizersJointToArm.connectedAnchor = armEndPivot.transform.localPosition;
    }
    private void ChangeStatus(EGrabStatus status)
    {
        switch (status)
        {
            case EGrabStatus.Wait:
                break;
        }
    }
    private void ArmToOriginPos()
    {
        StartCoroutine(ArmToOriginCo());
    }
    IEnumerator ArmToOriginCo()
    {
        yield return null;
        float timeLimit = 2f;
        float timer = 0;
        twizers.transform.SetParent(arm.transform, true);
        while(timer<timeLimit)
        {
            timer += Time.deltaTime;
            arm.transform.localRotation = Quaternion.Slerp(arm.transform.localRotation, Quaternion.Euler(3, -90, -90), timer / timeLimit);
            yield return null;
        }
        arm.transform.localRotation = Quaternion.Euler(3, -90, -90);
        twizers.transform.SetParent(shoulder.transform, true);
        grabStatus = EGrabStatus.Wait;
    }
  
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        armJoint=arm.GetComponent<ConfigurableJoint>();
        twizersJointToArm=twizers.GetComponent<ConfigurableJoint>();
        armRigidBody=arm.GetComponent<Rigidbody>();
        shoulderRg = shoulder.GetComponent<Rigidbody>();
        twizersRigidBody=twizers.GetComponent<Rigidbody>();
        armOriginScale = arm.transform.localScale.z;
        armOriginLength= (arm.transform.position - armEndPivot.transform.position).magnitude;


    }
    //테스트용
    private void Update()
    {   
        switch(grabStatus){
            case EGrabStatus.Wait:
                if (Input.GetKey(KeyCode.Q))
                {
                    //왼쪽집게회전
                    Quaternion temp = twizers.transform.localRotation;
                    temp.x -= twizersRotateSpeed * Time.deltaTime;
                    twizers.transform.localRotation = temp;
                }
                if (Input.GetKey(KeyCode.E))
                {
                    Quaternion temp = twizers.transform.localRotation;
                    temp.x += twizersRotateSpeed * Time.deltaTime;
                    twizers.transform.localRotation = temp;
                    //오른집게회전
                }
                if (Input.GetKey(KeyCode.K))
                {
                    ShootWrist();
                    grabStatus = EGrabStatus.Shooting;
                }
                break;
            case EGrabStatus.Shooting:

                ShootWristContinuous();
                ExtendArm();
                if (Input.GetKey(KeyCode.U))
                {
                     ConnectTwizers();
                }
                break;
            case EGrabStatus.Grab:

                break;
            case EGrabStatus.Connect:
                if (Input.GetKey(KeyCode.N))
                {
                    grabStatus = EGrabStatus.Bring;
                }
                break;
            case EGrabStatus.Bring:
                if (!BringDistanceCk())
                {
                    ShrinkArm();
                }
                else
                {
                    JointFree(armJoint);
                    armRigidBody.isKinematic = true;
                    JointFree(twizersJointToArm);
                    twizersRigidBody.isKinematic = true;
                    ArmToOriginPos();
                    grabStatus = EGrabStatus.AdjustArm;
                }
                break;
            case EGrabStatus.AdjustArm:
                break;

        }
        
    }
    

    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
