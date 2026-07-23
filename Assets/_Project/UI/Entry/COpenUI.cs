using UnityEngine;

/// <summary>
/// 키 입력을 받아 UI를 여는 컴포넌트입니다.
/// </summary>
public class COpenUI : AFrameable,IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("참조 연결")]
    [SerializeField] private Transform _cam;
    [SerializeField] private CPlayerController _controller;
    [SerializeField] private CInterectPopup _interectPopup;

    [Header("옵션")]
    [SerializeField] private LayerMask _interactorMask;
    [SerializeField] private float _rayMaxDistance = 2f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private bool _canInventory = true;
    private CPlayerToCockpit _cPlayerToCockpit;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    public void ExecuteUpdateFrame()
    {
        // 마우스를 갖다댔을때 상호작용이 가능하다는 팝업 표시용 코드
        if (_interectPopup.gameObject.activeSelf)
        {
            _interectPopup.gameObject.SetActive(false);
        }
        if (!_controller.IsControlLocked)
        {
            if (_controller.CurrentState != EPlayerState.OnGround) return;
            

            if (Physics.Raycast(_cam.position, _cam.forward, out RaycastHit hit, _rayMaxDistance, _interactorMask)) // 상점에 마우스가 가있을 때
            {
                if (hit.collider.TryGetComponent(out CShopEntry comp))
                {
                    _interectPopup.title.text = "Shop";
                    _interectPopup.gameObject.SetActive(true);
                }
                if(hit.collider.TryGetComponent(out CPlayerToCockpit cock))
                {
                    _interectPopup.title.text = "Cockpit";
                    _interectPopup.gameObject.SetActive(true);
                }
                
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void InventoryHandler(OnInputInventory ctx)
    {
        if(_canInventory==false) return;
        OnRequestOpenUI.Publish(EUI.InventoryWindow);
    }
    private void ShopHandler(OnInputGrab ctx)
    {
        UDebug.Print("상점이 레이캐스트 검출 시도합니다.");
        if (_controller.CurrentState != EPlayerState.OnGround) return;

        if (Physics.Raycast(_cam.position, _cam.forward, out RaycastHit hit, _rayMaxDistance, _interactorMask))
        {
            if (CUIManager.Ins.IsOpen(EUI.ShopWindow)) return;
            if (!hit.collider.TryGetComponent(out CShopEntry comp)) return;

            OnRequestOpenUI.Publish(EUI.ShopWindow);
            UDebug.Print("상점이 레이캐스트에 잡혔습니다~");
        }
        else
        {
            UDebug.Print("상점이 레이캐스트에 잡히지 않았습니다!");
        }
    }
    private void ToCockpitHandler(OnInputGrab ctx)
    {
        if (_controller.CurrentState != EPlayerState.OnGround) return;

        if (Physics.Raycast(_cam.position, _cam.forward, out RaycastHit hit, _rayMaxDistance, _interactorMask))
        {
            _cPlayerToCockpit=hit.collider.GetComponent<CPlayerToCockpit>();
            if (_cPlayerToCockpit==null) return;
            if (_cPlayerToCockpit.SitCockpit== true) return;

            _cPlayerToCockpit.MoveToCockpit();
            _canInventory = false;
            
            
        }
    }
    private void CockpitToPlayer(OnInputMove ctx)
    {
        if (_cPlayerToCockpit == null) return;
        if (_cPlayerToCockpit.SitCockpit == false) return;
        if (ctx.moved.sqrMagnitude >= 0.0001) return;
        _canInventory = true;
        _cPlayerToCockpit.CockpitToPlayer();
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();
        CEventBus<OnInputInventory>.Subscribe(InventoryHandler);
        CEventBus<OnInputGrab>.Subscribe(ShopHandler);
        CEventBus<OnInputGrab>.Subscribe(ToCockpitHandler);
        CEventBus<OnInputMove>.Subscribe(CockpitToPlayer);
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        CEventBus<OnInputInventory>.Unsubscribe(InventoryHandler);
        CEventBus<OnInputGrab>.Unsubscribe(ShopHandler);
        CEventBus<OnInputGrab>.Unsubscribe(ToCockpitHandler);
        CEventBus<OnInputMove>.Unsubscribe(CockpitToPlayer);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_interactorMask.IsEmpty())
        {
            UDebug.Print($"레이어 마스크가 비어있습니다.", LogType.Error);
        }
    }
#endif
    #endregion
}
