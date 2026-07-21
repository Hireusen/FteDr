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
    [SerializeField] private float _rayMaxDistance = 4f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    public void ExecuteUpdateFrame()
    {
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
                
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void InventoryHandler(OnInputInventory ctx)
    {
        OnRequestOpenUI.Publish(EUI.InventoryWindow);
        
    }
    private void ShopHandler(OnInputGrab ctx)
    {
        UDebug.Print("상점이 레이캐스트 검출 시도합니다.");
        if (_controller.CurrentState != EPlayerState.OnGround) return;

        if (Physics.Raycast(_cam.position, _cam.forward, out RaycastHit hit, _rayMaxDistance, _interactorMask)) // 상점에 마우스가 가있을 때
        {
            if (!hit.collider.TryGetComponent(out CShopEntry comp)) return;

            OnRequestOpenUI.Publish(EUI.ShopWindow);
            UDebug.Print("상점이 레이캐스트에 잡혔습니다~");
        }
        else
        {
            UDebug.Print("상점이 레이캐스트에 잡히지 않았습니다!");
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();
        CEventBus<OnInputInventory>.Subscribe(InventoryHandler);
        CEventBus<OnInputGrab>.Subscribe(ShopHandler);
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        CEventBus<OnInputInventory>.Unsubscribe(InventoryHandler);
        CEventBus<OnInputGrab>.Unsubscribe(ShopHandler);
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
