using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CNewGrab : AFrameable, IUpdateFrameable, IFixedUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private CPlayerController _controller;
    [SerializeField] private GameObject _shoulder;
    [SerializeField] private GameObject _arm;
    [SerializeField] private GameObject _armEndPivot;
    [SerializeField] private GameObject _twizersAnchor;
    [SerializeField] private CTwizers _twizers;
    [SerializeField] private ConfigurableJoint _twizersJointToArm;
    [SerializeField] private float _shootForce = 10f;
    [SerializeField] private float _maxdistance = 4f;
    [SerializeField] private float _shrinkSpeed = 10f;
    [SerializeField] private float _twizersRotateSpeed = 1f;
    [SerializeField] private CGrabToolSO _grabToolSO;
    [SerializeField] private int _currentDistantLevel = 0;
    [SerializeField] private int _currentSpeedLevel = 0;
    [SerializeField] private Transform _playerCam;

    //test모드가 활성화 중이면 테스트 데이터(스피드,거리)로 작동 
    [SerializeField] private bool _testmode = false;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private ConfigurableJoint _armJoint;
    private Rigidbody _armRigidBody;
    private Rigidbody _twizersRigidBody;
    private Rigidbody _shoulderRg;
    private float _armOriginScale;
    private float _armOriginLength;
    private Quaternion _grabOffset;
    private Vector3 _aimDir;
    private const float AIMDISTANCE = 10f;

    private bool _rotateLeftHeld;
    private bool _rotateRightHeld;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EGrabStatus grabStatus = EGrabStatus.Wait;
    public void ShootWrist()
    {
        _twizersRigidBody.isKinematic = false;
        //twizersRigidBody.AddForce(ShootForce * playerCam.forward, ForceMode.Impulse);
    }
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    //테스트용
    public void ExecuteUpdateFrame()
    {
        switch (grabStatus)
        {
            case EGrabStatus.Wait:
                if (_rotateLeftHeld)
                {
                    //왼쪽집게회전
                    Quaternion temp = _twizersAnchor.transform.localRotation;
                    temp.x -= _twizersRotateSpeed * Time.deltaTime;
                    _twizersAnchor.transform.localRotation = temp;
                }
                if (_rotateRightHeld)
                {
                    Quaternion temp = _twizersAnchor.transform.localRotation;
                    temp.x += _twizersRotateSpeed * Time.deltaTime;
                    _twizersAnchor.transform.localRotation = temp;
                    //오른집게회전
                }
                break;
            case EGrabStatus.Shooting:
                ExtendArm();
                /*
                if (Input.GetKey(KeyCode.U))
                {
                    
                    ChangeStatus(EGrabStatus.Grab);
                }
                */
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
    public float GetMaxDistance()
    {
        float maxdistance = _testmode ? _maxdistance : UData.GrabTool().ReachDistance(CProgressManager.Ins.GetGearLevel(EDataType.GrabTool));
        return maxdistance;
    }
    public float GetMaxGrabSpeed()
    {
        float maxPower = _testmode ? _shootForce : UData.GrabTool().GrabSpeed(CProgressManager.Ins.GetGearLevel(EDataType.GrabTool));
        return maxPower;
    }
    public EFixedUpdatePriority FixedUpdatePriority => EFixedUpdatePriority.Lv5;
    public void ExecuteFixedUpdateFrame()
    {
        if (grabStatus == EGrabStatus.Shooting)
        {
            ShootWristContinuous();
        }
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
        float maxdistance = GetMaxDistance();
        if (distance < maxdistance) return true;
        else return false;
    }
    private void ShootWristContinuous()
    {
        _twizersRigidBody.AddForce(_aimDir * GetMaxGrabSpeed(), ForceMode.Force);

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
        if (distance <= _armOriginLength || _arm.transform.localScale.z < 2.2f) return true;
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
        scale.z -= _shrinkSpeed * Time.deltaTime;
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
                _twizers.GrabSetting(false);
                USound.PlaySfx("SFX_v2_metal_02");
                _controller.IsControlLockedByGrab = false;
                print("상태변경>wait");
                grabStatus = EGrabStatus.Wait;
                break;
            case EGrabStatus.Shooting:
                Ray ray = new Ray(_playerCam.transform.position, _playerCam.transform.forward);
                RaycastHit hit;
                Vector3 aimPos;
                if (Physics.Raycast(ray, out hit, AIMDISTANCE))
                {
                    aimPos = hit.point;
                }
                else
                {
                    aimPos = ray.origin + ray.direction * AIMDISTANCE;
                }

                //너무 가까우면 안쏴지게 함.(각도 이상해져서)
                float aimDistance = (aimPos - _playerCam.transform.localPosition).magnitude;
                if (aimDistance < 0.2f) return;

                CCollectible temp = hit.transform.root.GetComponent<CCollectible>();
                if (hit.collider != null && hit.transform.root.CompareTag(K.TAG_GRABABLE) && hit.transform.root.GetComponent<Rigidbody>() == null)
                {
                    Rigidbody rg=hit.transform.root.AddComponent<Rigidbody>();
                    if (temp.Data.IsAir == true)
                    {
                        rg.drag = 11.75f;
                        rg.angularDrag = 0.05f;
                        rg.useGravity = false;
                    }
                        
                }


                USound.PlaySfx(Id.SFX_robotics2);
                ShootWrist();
                _aimDir = (aimPos - _arm.transform.position).normalized;
                _twizersRigidBody.constraints = RigidbodyConstraints.FreezeRotation;
                _controller.IsControlLockedByGrab = true;
                _twizers.OpenGrabContinuous();
                print("상태변경>shooting");
                grabStatus = EGrabStatus.Shooting;
                _twizersRigidBody.useGravity = false;
                break;
            case EGrabStatus.Grab:
                _twizers.GrabSetting(true);
                print("상태변경>grab");
                grabStatus = EGrabStatus.Grab;
                _twizersRigidBody.isKinematic = true;
                _twizersRigidBody.useGravity = true;
                _twizers.GrabContinuous();
                break;
            case EGrabStatus.Connect:
                print("상태변경>connect");
                _twizers.GrabSetting(false);
                _twizersRigidBody.isKinematic = false;
                //_armRigidBody.isKinematic = false;
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
        GameObject itemObj = _twizers.GetItemAndPutdown();
        // 제대로 잡혀있는지 검사
        if (itemObj == null) return null;
        Debug.Log(itemObj);
        item = itemObj.GetComponent<CCollectible>();
        var data = item.Data;
        bool success = UPlayer.TryAddToBag(data.Id); // 배낭 입력 시도
        // 배낭 입력 성공 및 아이템 삭제
        if (success)
        {
            Destroy(itemObj);
        }
        // 배낭 입력 실패 및 아이템 놓기
        else
        {
            if (item != null)
            {
                _twizers.CollisionOn();
            }
        }
        return item;
    }
    private void ArmToOriginPos()
    {
        _twizers.GrabToOriginContinuous();
        StartCoroutine(ArmToOriginCo());
    }

    private void GrabInputHandler(OnInputGrab ctx)
    {
        //if (grabStatus != EGrabStatus.Wait) return;
        if (_controller.CurrentState == EPlayerState.OnGround) return;
        // if (UPlayer.CurrentFuel <= 0f) return;
        if (_controller.IsControlLocked) return;
        if (Time.timeScale == 0f) return;

        ChangeStatus(EGrabStatus.Shooting);
    }
    private void CollectInputHandler(OnInputCollect ctx)
    {
        if (grabStatus == EGrabStatus.Shooting) ChangeStatus(EGrabStatus.Grab);
    }
    private IEnumerator ArmToOriginCo()
    {
        yield return null;
        float timeLimit = 0.5f;
        float timer = 0;
        _twizersAnchor.transform.localRotation = _arm.transform.localRotation * _grabOffset;
        _twizersAnchor.transform.SetParent(_arm.transform, true);
        while (timer < timeLimit)
        {
            timer += Time.deltaTime;
            _arm.transform.localRotation = Quaternion.Slerp(_arm.transform.localRotation, Quaternion.Euler(3, -90, -90), timer / timeLimit);
            yield return null;
        }
        _arm.transform.localRotation = Quaternion.Euler(3, -90, -90);
        _twizersAnchor.transform.SetParent(_shoulder.transform, true);
        ChangeStatus(EGrabStatus.Wait);
    }
    private void RotateLeftHandler(OnInputRotateTwizerLeft ctx)
    {
        _rotateLeftHeld = ctx.leftPressed;
    }
    private void RotateRightHandler(OnInputRotateTwizerRight ctx)
    {
        _rotateRightHeld = ctx.rightPressed;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _armJoint = _arm.GetComponent<ConfigurableJoint>();
        _twizersJointToArm = _twizersAnchor.GetComponent<ConfigurableJoint>();
        _armRigidBody = _arm.GetComponent<Rigidbody>();
        _shoulderRg = _shoulder.GetComponent<Rigidbody>();
        _twizersRigidBody = _twizersAnchor.GetComponent<Rigidbody>();
        _armOriginScale = _arm.transform.localScale.z;
        _armOriginLength = (_arm.transform.position - _armEndPivot.transform.position).magnitude;

        _grabOffset = Quaternion.Inverse(_arm.transform.localRotation) * _twizersAnchor.transform.localRotation;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CEventBus<OnInputGrab>.Subscribe(GrabInputHandler);
        CEventBus<OnInputCollect>.Subscribe(CollectInputHandler);
        CEventBus<OnInputRotateTwizerLeft>.Subscribe(RotateLeftHandler);
        CEventBus<OnInputRotateTwizerRight>.Subscribe(RotateRightHandler);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CEventBus<OnInputGrab>.Unsubscribe(GrabInputHandler);
        CEventBus<OnInputCollect>.Unsubscribe(CollectInputHandler);
        CEventBus<OnInputRotateTwizerLeft>.Unsubscribe(RotateLeftHandler);
        CEventBus<OnInputRotateTwizerRight>.Unsubscribe(RotateRightHandler);


    }

    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
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
    #endregion
}
