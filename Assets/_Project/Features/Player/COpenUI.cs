using UnityEngine;

/// <summary>
/// 키 입력을 받아 UI를 여는 컴포넌트입니다.
/// </summary>
public class COpenUI : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("참조 연결")]
    [SerializeField] private Transform _cam;
    [SerializeField] private CPlayerController _controller;

    [Header("옵션")]
    [SerializeField] private LayerMask _interactorMask;
    [SerializeField] private float _rayMaxDistance = 2f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void InventoryHandler(OnInputInventory ctx)
    {
        OnRequestOpenUI.Publish(EUI.InventoryWindow);
    }
    private void ShopHandler(OnInputGrab ctx)
    {
        if (_controller.CurrentState != EPlayerState.OnGround) return;

        if (Physics.Raycast(transform.position, _cam.forward, _rayMaxDistance, )) // 상점에 마우스가 가있을 때
        {

        }

        OnRequestOpenUI.Publish(EUI.InventoryWindow);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        CEventBus<OnInputInventory>.Subscribe(InventoryHandler);
        CEventBus<OnInputGrab>.Subscribe(ShopHandler);
    }
    private void OnDisable()
    {
        CEventBus<OnInputInventory>.Unsubscribe(InventoryHandler);
        CEventBus<OnInputGrab>.Unsubscribe(ShopHandler);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if(_interactorMask.)
        {
            UDebug.Print($"레이어 마스크가 비어있습니다.", LogType.Error);
        }
    }
#endif
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
