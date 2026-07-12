using UnityEngine;

/// <summary>
/// 플레이어의 잡기를 제어하는 컴포넌트 입니다.
/// </summary>
public class CPlayerGrab : AFrameable, IUpdateFrameable, IFixedUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Rigidbody _playerRb;
    [SerializeField] private Transform _wristPoint;
    [SerializeField] private Transform _clawObject;
    [SerializeField] private LineRenderer _lineRenderer;

    [Header("발사 설정")]
    [SerializeField] private float _maxGrabDistance;
    [SerializeField] private float _clawSpeed;

    [Header("잡기 설정")]
    [SerializeField] private string _grabTag;
    [SerializeField] private LayerMask _grabLayer;
    [SerializeField] private LayerMask _groundLayer;

    [Header("수집품 고정 설정")]
    [Tooltip("FixedJoint가 끊어지는 힘. 충돌·강한 당김 시 자동 해제")]
    [SerializeField] private float _jointBreakForce = 2000f;
    [SerializeField] private float _jointBreakTorque = 2000f;
    [Tooltip("집게와 플레이어 속도 차가 이 값을 넘으면 보조적으로 해제")]
    [SerializeField] private float _maxSpeedDifference = 15f;

    [Header("던지기 설정")]
    [Tooltip("들고 있던 특수 수집품을 던질 때 가하는 힘")]
    [SerializeField] private float _throwForce = 8f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private EGrabState _currentState = EGrabState.Idle;
    private Vector3 _targetPosition;
    private GameObject _grabbedObject;
    private ACollectible _grabbedCollectible; // 추후에 추상클래스가 생기면 수정
    private Rigidbody _clawRb;
    private FixedJoint _grabJoint;

    // 집게의 원래 부모(손목) 정보. 회수 후 복귀에 사용
    private Transform _clawOriginalParent;
    private Vector3 _clawOriginalLocalPos;
    private Quaternion _clawOriginalLocalRot;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv6;
    public EFixedUpdatePriority FixedUpdatePriority => EFixedUpdatePriority.Lv6;

    public Rigidbody PlayerRb => _playerRb;
    public Rigidbody ClawRb => _clawRb;

    // 프레임 매니저에게 호출당할 함수 (시각 갱신)
    public void ExecuteUpdateFrame()
    {
        DrawClawLine();
    }

    // 프레임 매니저에게 호출당할 함수 (물리 이동·판정)
    public void ExecuteFixedUpdateFrame()
    {
        UpdateClawState();
        CheckSpeedRelease();
    }

    /// <summary>
    /// 아이템(벽 충돌 등)이나 플레이어에 의해 강제로 잡기를 풉니다.
    /// </summary>
    public void ForceRelease()
    {
        DetachJoint();

        if (_grabbedCollectible != null)
        {
            _grabbedCollectible.OnReleased();
            _grabbedCollectible = null;
        }

        _grabbedObject = null;
        _currentState = EGrabState.Retracting;
        UDebug.Print("고정 해제 및 회수");
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void UpdateClawState()
    {
        switch (_currentState)
        {
            case EGrabState.Idle:
                // 손목에 자식으로 붙어 있으므로 부모를 따라감. 별도 이동 불필요.
                break;

            case EGrabState.Firing:
                MoveClawTowards(_targetPosition);

                if (Vector3.Distance(_clawRb.position, _targetPosition) < 0.1f)
                {
                    if (_grabbedObject != null)
                    {
                        OnClawReachedTarget();
                    }
                    else
                    {
                        _currentState = EGrabState.Retracting;
                    }
                }
                break;

            case EGrabState.Grabbing:
                // 아이템은 FixedJoint로 집게에 매달려 따라옴.
                // 해제는 충돌(CClawCollision) 또는 속도차(CheckSpeedRelease)로 처리.
                break;

            case EGrabState.Retracting:
                MoveClawTowards(_wristPoint.position);
                if (Vector3.Distance(_clawRb.position, _wristPoint.position) < 0.1f)
                {
                    CompleteRetract();
                }
                break;
        }
    }

    private void CompleteRetract()
    {
        if (_grabbedCollectible != null)
        {
            if (_grabbedCollectible.CanStoreInInventory)
            {
                // 일반 수집품: 인벤토리 저장 후 제거
                StoreItem(_grabbedCollectible);
                ReAttachClaw();
                _currentState = EGrabState.Idle;
            }
            else
            {
                // 특수 수집품: Joint 유지한 채 들고 있는 상태로
                ReAttachClaw();
                _currentState = EGrabState.Grabbing;
            }
            return;
        }

        // 빈손 회수
        ReAttachClaw();
        _currentState = EGrabState.Idle;
    }

    private void StoreItem(ACollectible collectible)
    {
        DetachJoint();
        collectible.OnReleased();

        // TODO: 인벤토리/가방에 추가하는 처리. 임시로 오브젝트 비활성화.
        UDebug.Print($"{collectible.name} 인벤토리 저장");
        collectible.gameObject.SetActive(false);

        _grabbedCollectible = null;
        _grabbedObject = null;
    }

    private void ThrowItem()
    {
        Rigidbody itemRb = _grabbedObject != null ? _grabbedObject.GetComponent<Rigidbody>() : null;

        DetachJoint();

        if (_grabbedCollectible != null)
        {
            _grabbedCollectible.OnReleased();
        }

        if (itemRb != null)
        {
            // 카메라가 보는 방향으로 던짐
            itemRb.AddForce(_playerCamera.transform.forward * _throwForce, ForceMode.Impulse);
        }

        UDebug.Print("특수 수집품 던지기/내려놓기");

        _grabbedCollectible = null;
        _grabbedObject = null;
        _currentState = EGrabState.Retracting;
    }

    private void MoveClawTowards(Vector3 destination)
    {
        Vector3 next = Vector3.MoveTowards(_clawRb.position, destination, _clawSpeed * Time.fixedDeltaTime);
        _clawRb.MovePosition(next);
    }

    private void DetachClaw()
    {
        if (_clawObject.parent != null)
        {
            _clawOriginalParent = _clawObject.parent;
            _clawOriginalLocalPos = _clawObject.localPosition;
            _clawOriginalLocalRot = _clawObject.localRotation;
        }
        _clawObject.SetParent(null, true);
    }

    private void ReAttachClaw()
    {
        if (_clawOriginalParent == null) return;

        _clawObject.SetParent(_clawOriginalParent, true);
        _clawObject.localPosition = _clawOriginalLocalPos;
        _clawObject.localRotation = _clawOriginalLocalRot;
    }

    private void OnClawReachedTarget()
    {
        _grabbedCollectible = _grabbedObject.GetComponent<ACollectible>();

        if (_grabbedCollectible != null)
        {
            AttachJoint(_grabbedObject.GetComponent<Rigidbody>());
            _grabbedCollectible.OnGrabbed();
            // 잡는 즉시 자동 회수. Joint는 유지한 채 집게가 물체를 매달고 손으로 돌아옴.
            _currentState = EGrabState.Retracting;
        }
        else
        {
            // 벽(Ground) 등: 동작 미정. 우선 잡은 상태 유지.
            // TODO: 벽 동작 결정 시 플레이어 견인 등 분기 추가
            _currentState = EGrabState.Grabbing;
        }
    }

    private void AttachJoint(Rigidbody targetRb)
    {
        if (targetRb == null) return;

        DetachJoint();

        _grabJoint = _clawObject.gameObject.AddComponent<FixedJoint>();
        _grabJoint.connectedBody = targetRb;
        _grabJoint.breakForce = _jointBreakForce;
        _grabJoint.breakTorque = _jointBreakTorque;
        _grabJoint.enableCollision = false;
    }

    private void DetachJoint()
    {
        if (_grabJoint != null)
        {
            Destroy(_grabJoint);
            _grabJoint = null;
        }
    }

    private void CheckSpeedRelease()
    {
        // 잡은 수집품을 매달고 있는 동안(Grabbing 또는 자동 회수 중)에만 검사
        if (_currentState != EGrabState.Grabbing && _currentState != EGrabState.Retracting) return;
        if (_grabbedCollectible == null) return;

        float diff = (_clawRb.velocity - _playerRb.velocity).magnitude;
        if (diff > _maxSpeedDifference)
        {
            ForceRelease();
        }
    }

    private void DrawClawLine()
    {
        if (_lineRenderer != null && _currentState != EGrabState.Idle)
        {
            if (!_lineRenderer.enabled)
            {
                _lineRenderer.enabled = true;
            }
            _lineRenderer.SetPosition(0, _wristPoint.position);
            _lineRenderer.SetPosition(1, _clawObject.position);
        }
        else if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
    }

    private void FireClaw()
    {
        DetachClaw();

        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

        if (Physics.SphereCast(ray, 0.5f, out RaycastHit hit, _maxGrabDistance, _grabLayer))
        {
            _targetPosition = hit.point;

            if (hit.collider.CompareTag(_grabTag) || hit.collider.gameObject.layer == _groundLayer)
            {
                _grabbedObject = hit.collider.gameObject;
                UDebug.Print($"{hit.collider.name} 발견! 발사합니다.");
            }
            else
            {
                _grabbedObject = null;
            }
        }
        else
        {
            _targetPosition = _playerCamera.transform.position + _playerCamera.transform.forward * _maxGrabDistance;
            _grabbedObject = null;
        }

        _currentState = EGrabState.Firing;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();

        CEventBus<OnInputGrab>.Subscribe(GrabInputHandler);

        _clawRb = _clawObject.GetComponent<Rigidbody>();

        // 원래 부모 정보 미리 저장 (시작 시점의 손목 부착 상태 기준)
        _clawOriginalParent = _clawObject.parent;
        _clawOriginalLocalPos = _clawObject.localPosition;
        _clawOriginalLocalRot = _clawObject.localRotation;

        var clawCollision = _clawObject.GetComponent<CClawCollision>();
        if (clawCollision != null)
        {
            clawCollision.Init(this);
        }

        if (_playerRb == null)
        {
            _playerRb = GetComponent<Rigidbody>();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (_currentState == EGrabState.Firing || _currentState == EGrabState.Retracting)
        {
            ReAttachClaw();
        }

        CEventBus<OnInputGrab>.Unsubscribe(GrabInputHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────

    private void GrabInputHandler(OnInputGrab data)
    {
        if (_currentState == EGrabState.Idle)
        {
            FireClaw();
        }
        else if (_currentState == EGrabState.Grabbing)
        {
            // 특수 수집품을 들고 있으면 던지기/내려놓기, 그 외(벽 등)는 단순 해제
            if (_grabbedCollectible != null)
            {
                ThrowItem();
            }
            else
            {
                ForceRelease();
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    private enum EGrabState
    {
        Idle,
        Firing,
        Grabbing,
        Retracting
    }

    #endregion
}
