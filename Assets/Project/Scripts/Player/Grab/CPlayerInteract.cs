using UnityEngine;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CPlayerInteract : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Camera _playerCamera;

    [Header("상호작용 설정")]
    [SerializeField] private float _interactionRange;
    [SerializeField] private float _cameraMaxDistance;
    [SerializeField] private float _castRadius = 0.5f;
    [SerializeField] private LayerMask _interactableLayer;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private GameObject _currentTarget;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv6;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (_playerCamera == null || _playerTransform == null) return;

        CheckInteractableObject();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void CheckInteractableObject()
    {
        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);

        if (Physics.SphereCast(ray, _castRadius, out RaycastHit hit, _cameraMaxDistance, _interactableLayer))
        {
            float distanceToPlayer = Vector3.Distance(_playerTransform.position, hit.point);

            if (distanceToPlayer <= _interactionRange)
            {
                if (_currentTarget != hit.collider.gameObject)
                {
                    _currentTarget = hit.collider.gameObject;
                    UDebug.Print($"상호작용 오브젝트 감지 : {_currentTarget.name}");
                }
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    private void ClearTarget()
    {
        if (_currentTarget != null)
        {
            UDebug.Print("상호작용 대상에서 눈을 뗌 (또는 너무 멀어짐)");
            _currentTarget = null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();

        CEventBus<OnInputGrab>.Subscribe(GrabHandler);

        if (_playerCamera == null && Camera.main != null)
        {
            _playerCamera = Camera.main;
        }

        if (_playerTransform == null)
        {
            _playerTransform = transform;
        }
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void GrabHandler(OnInputGrab data)
    {
        if (_currentTarget != null)
        {
            UDebug.Print($"수집품 '{_currentTarget.name}' 줍기 / 상호작용 성공");

            // TODO: _currentTarget에 붙어있는 인터페이스의 기능 실행
        }
        else
        {
            UDebug.Print("허공에 손짓 (앞에 상호작용할 물체가 없거나 너무 멉니다)");
        }
    }
    #endregion
}
