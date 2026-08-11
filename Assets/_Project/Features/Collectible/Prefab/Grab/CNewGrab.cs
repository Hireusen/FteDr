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
    [SerializeField] private CFingerOutCollider _fout1;
    [SerializeField] private CFingerOutCollider _fout2;
    [SerializeField] private ConfigurableJoint _twizersJointToArm;
    [SerializeField] private float _shootForce = 10f;
    [SerializeField] private float _maxdistance = 4f;
    [SerializeField] private float _shrinkSpeed = 10f;
    [SerializeField] private float _twizersRotateSpeed = 1f;
    [SerializeField] private CGrabToolSO _grabToolSO;
    [SerializeField] private int _currentDistantLevel = 0;
    [SerializeField] private int _currentSpeedLevel = 0;
    [SerializeField] private Transform _playerCam;
    [SerializeField] private CDiverToAim _diverToAim;
    [SerializeField] private float _shoulderReadyTime=1f;


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

    private Vector3 _shoulderReadyPos = new Vector3(-0.8f, 7.72f, 23.9f);
    private Vector3 _shoulderWaitPos = new Vector3(1.9f, 7.7f, 0.3f);
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
                    _twizersAnchor.transform.Rotate(
                    Vector3.left,
                    _twizersRotateSpeed * Time.deltaTime,
                    Space.Self);
                }
                if (_rotateRightHeld)
                {
                    _twizersAnchor.transform.Rotate(
                    Vector3.right,
                    _twizersRotateSpeed * Time.deltaTime,
                    Space.Self);
                    //오른집게회전
                }
                break;
            case EGrabStatus.Shooting:
                ExtendArm();

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
                    Vector3 scale = _arm.transform.localScale;
                    scale.z = 1.5f;
                    _arm.transform.localScale = scale;
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
            if (_fout1.CrashCk == true || _fout2.CrashCk == true)
            {
                ChangeStatus(EGrabStatus.Grab);
            }
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
                UDebug.Print("상태변경>wait");
                grabStatus = EGrabStatus.Wait;
                //집게 가리는 코드 추가해야함.
                StartCoroutine(WaitTwizersCo());
                _diverToAim.AimCanvas.SetActive(false);
                _controller.MoveLockOFF();
                break;
            case EGrabStatus.ReadyShoot:
                //USound(조준효과음)
                _diverToAim.AimCanvas.SetActive(true);
                //스피드 제한 추가.(지금은 일단 잠금)
                _controller.MoveLockOn();
                //집게 연출 코루틴 안에서 연출 종료 후 상태변경
                StartCoroutine(ReadyTwizersCo());
                
                break;
            case EGrabStatus.Shooting:
                Ray ray = new Ray(_playerCam.transform.position, _playerCam.transform.forward);
                RaycastHit hit;
                Vector3 aimPos;
                int mask = ~(1 << gameObject.layer);
                if (Physics.Raycast(ray, out hit, AIMDISTANCE, mask))
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


                if (hit.collider != null && hit.transform.root.CompareTag(K.TAG_GRABABLE) && hit.transform.root.GetComponent<Rigidbody>() == null)
                {
                    CCollectible temp = hit.transform.root.GetComponent<CCollectible>();
                    Rigidbody rg = hit.transform.root.AddComponent<Rigidbody>();
                    rg.useGravity = false;
                    if (temp.Data.IsAir == true)
                    {
                        rg.drag = 11.75f;
                        rg.angularDrag = 0.05f;
                        rg.useGravity = false;

                        // 공중 수집품의 부유를 물리(MovePosition) 방식으로 전환. (부착 타이밍 불일치 방지)
                        if (temp.TryGetComponent(out CCollectibleBob bob))
                        {
                            bob.OnBodyAttached(rg);
                        }
                    }

                }


                USound.PlaySfx(Id.SFX_robotics2);
                _fout1.CancelCrashCk();
                _fout2.CancelCrashCk();
                ShootWrist();
                _aimDir = (aimPos - _twizers.transform.position).normalized;
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
    private IEnumerator ReadyTwizersCo()
    {
        //Vector3.MoveTowards(_)
        USound.PlaySfx("SFX_v2_metal_02");
        float timer = 0f;
        while (timer < _shoulderReadyTime)
        {
            timer += Time.deltaTime;
            _shoulder.transform.localPosition=Vector3.Lerp(_shoulder.transform.localPosition, _shoulderReadyPos, timer / _shoulderReadyTime);
            yield return null;
        }
        _shoulder.transform.localPosition = _shoulderReadyPos;
        UDebug.Print("상태변경>Readyshoot");
        grabStatus = EGrabStatus.ReadyShoot;

    }
    private IEnumerator WaitTwizersCo()
    {
        //Vector3.MoveTowards(_)
        float timer = 0f;
        while (timer < _shoulderReadyTime)
        {
            timer += Time.deltaTime;
            _shoulder.transform.localPosition = Vector3.Lerp(_shoulder.transform.localPosition, _shoulderWaitPos, timer / _shoulderReadyTime);
            yield return null;
        }
        _shoulder.transform.localPosition = _shoulderWaitPos;

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
            itemObj.SetActive(false);
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
        //조준 단계 추가
        if (grabStatus == EGrabStatus.Wait)
        {
            ChangeStatus(EGrabStatus.ReadyShoot);
            return;
        }
        if (grabStatus == EGrabStatus.ReadyShoot)
        {
            ChangeStatus(EGrabStatus.Shooting);
        }
    }
    private void CollectInputHandler(OnInputCollect ctx)
    {
        //조준 단계 추가
        if (grabStatus == EGrabStatus.ReadyShoot)
        {
            ChangeStatus(EGrabStatus.Wait);
            return;
        }
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
        ReadyShoot,
        Shooting,
        Connect,
        Grab,
        Bring,
        AdjustArm,
        BringComplete
    }
    #endregion
}
