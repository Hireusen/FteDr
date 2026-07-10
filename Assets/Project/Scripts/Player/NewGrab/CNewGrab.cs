using System.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CNewGrab : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private GameObject _shoulder;
    [SerializeField] private GameObject _arm;
    [SerializeField] private GameObject _armEndPivot;
    [SerializeField] private GameObject _twizersAnchor;
    [SerializeField] private CTwizers _twizers;
    [SerializeField] private ConfigurableJoint _twizersJointToArm;
    [SerializeField] private float _ShootForce = 10f;
    [SerializeField] private float _ShrinkSpeed = 10f;
    [SerializeField] private float _twizersRotateSpeed = 1f;

    [SerializeField] private Transform _playerCam;
    [SerializeField] private float _maxdistance=3f;
    
    

    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private ConfigurableJoint _armJoint;
    private Rigidbody _armRigidBody;
    private Rigidbody _twizersRigidBody;
    private Rigidbody _shoulderRg;
    private float _armOriginScale;
    private float _armOriginLength;
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
        _twizersRigidBody.isKinematic = false;
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
        float distance = (_arm.transform.position - _twizersAnchor.transform.position).magnitude;
        if (distance < _maxdistance) return true;
        else return false;
    }
    private void ShootWristContinuous()
    {
        _twizersRigidBody.AddForce(_playerCam.transform.forward * _ShootForce, ForceMode.Force);
        //부력 보정
        Vector3 velocity = _twizersRigidBody.velocity;
        velocity.y *= 0.5f;
        _twizersRigidBody.velocity = velocity;


        //거리제한되면 자동으로 그랩동작을 시행한 다음 상태변경한다. 
        if (!DistanceCk())
        {
            print("거리제한");
            ChangeStatus(EGrabStatus.Grab);
        }

    }
    private bool BringDistanceCk()
    {
        float distance = (_arm.transform.position - _twizersAnchor.transform.position).magnitude;
        if(distance<=_armOriginLength) return true;
        else return false;
    }
    private void ExtendArm()
    {

        Vector3 dir = (_twizersAnchor.transform.position - _arm.transform.position);
        float distance = dir.magnitude;

        _arm.transform.forward = dir.normalized;

        Vector3 scale = _arm.transform.localScale;
        scale.z = distance / _armOriginLength * _armOriginScale;
        _arm.transform.localScale = scale;
        

    }
    private void ShrinkArm()
    {
        Vector3 scale = _arm.transform.localScale;
        scale.z -= _ShrinkSpeed * Time.deltaTime;
        _arm.transform.localScale = scale;

        //팔이 줄어드는 힘을 따로 구현하고 싶으면 이런식으로 구현해야 함.
        //Vector3 dir=(armEndPivot.transform.position-twizers.transform.position).normalized;

        _twizersJointToArm.connectedAnchor = _armEndPivot.transform.localPosition;
    }
    private void ChangeStatus(EGrabStatus status)
    {
        if (grabStatus == status) return;

        switch (status)
        {
            case EGrabStatus.Wait:
                _twizers.CollisionOn();
                print("상태변경>wait");
                grabStatus = EGrabStatus.Wait;
                break;
            case EGrabStatus.Shooting:
                _twizers.OpenGrabContinuous();
                print("상태변경>shooting");
                grabStatus= EGrabStatus.Shooting;
                
                break;
            case EGrabStatus.Grab:
                print("상태변경>grab");
                grabStatus = EGrabStatus.Grab;
                _twizersRigidBody.isKinematic = true;
                _twizers.GrabContinuous();
                break;
            case EGrabStatus.Connect:
                print("상태변경>connect");
                _twizersRigidBody.isKinematic = false;
                _armRigidBody.isKinematic = false;
                JointOn(_twizersJointToArm, _armRigidBody);
                JointOn(_armJoint, _shoulderRg);
                _twizersJointToArm.connectedAnchor = _armEndPivot.transform.localPosition;
                grabStatus = EGrabStatus.Connect;
                break;
            case EGrabStatus.AdjustArm:
                grabStatus = EGrabStatus.AdjustArm;
                break;
        }
    }
    private CCollectible GetItem()
    {
        CCollectible item = null;
        GameObject itemObj= _twizers.GetItemInfo();
        if(itemObj != null)
        {
            item=itemObj.GetComponent<CCollectible>();
            itemObj.SetActive(false);
        }
        return item; 
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
        _twizersAnchor.transform.SetParent(_arm.transform, true);
        while(timer<timeLimit)
        {
            timer += Time.deltaTime;
            _arm.transform.localRotation = Quaternion.Slerp(_arm.transform.localRotation, Quaternion.Euler(3, -90, -90), timer / timeLimit);
            yield return null;
        }
        _arm.transform.localRotation = Quaternion.Euler(3, -90, -90);
        _twizersAnchor.transform.SetParent(_shoulder.transform, true);
        grabStatus = EGrabStatus.Wait;
    }
  
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _armJoint=_arm.GetComponent<ConfigurableJoint>();
        _twizersJointToArm=_twizersAnchor.GetComponent<ConfigurableJoint>();
        _armRigidBody=_arm.GetComponent<Rigidbody>();
        _shoulderRg = _shoulder.GetComponent<Rigidbody>();
        _twizersRigidBody=_twizersAnchor.GetComponent<Rigidbody>();
        _armOriginScale = _arm.transform.localScale.z;
        _armOriginLength= (_arm.transform.position - _armEndPivot.transform.position).magnitude;
       


    }
    //테스트용
    private void Update()
    {   
        switch(grabStatus){
            case EGrabStatus.Wait:
                if (Input.GetKey(KeyCode.Q))
                {
                    //왼쪽집게회전
                    Quaternion temp = _twizersAnchor.transform.localRotation;
                    temp.x -= _twizersRotateSpeed * Time.deltaTime;
                    _twizersAnchor.transform.localRotation = temp;
                }
                if (Input.GetKey(KeyCode.E))
                {
                    Quaternion temp = _twizersAnchor.transform.localRotation;
                    temp.x += _twizersRotateSpeed * Time.deltaTime;
                    _twizersAnchor.transform.localRotation = temp;
                    //오른집게회전
                }
                if (Input.GetKey(KeyCode.K))
                {
                    ShootWrist();
                    ChangeStatus(EGrabStatus.Shooting);
                }
                break;
            case EGrabStatus.Shooting:

                ShootWristContinuous();
                ExtendArm();
                if (Input.GetKey(KeyCode.U))
                {
                    
                    ChangeStatus(EGrabStatus.Grab);
                }
                break;
            case EGrabStatus.Grab:
                //물건을 집거나, 집게를 다 닫으면 connect로 이동.
                if (_twizers.Grabed == true)
                {
                    ChangeStatus(EGrabStatus.Connect);
                }
                break;
            case EGrabStatus.Connect:
                grabStatus = EGrabStatus.Bring;
                break;
            case EGrabStatus.Bring:
                if (!BringDistanceCk())
                {
                    ShrinkArm();
                    
                }
                else
                {
                    GetItem();
                    JointFree(_armJoint);
                    _armRigidBody.isKinematic = true;
                    JointFree(_twizersJointToArm);
                    _twizersRigidBody.isKinematic = true;
                    ArmToOriginPos();
                    ChangeStatus(EGrabStatus.AdjustArm);

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
