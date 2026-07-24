using UnityEngine;

/// <summary>
/// CPlayerController를 건드리지 않고, 같은 콜라이더에 나란히 붙여서 잠수함 트리거 진입/이탈을 감지하는 센서입니다.
/// Unity는 OnTriggerEnter/Exit를 같은 오브젝트의 모든 컴포넌트에 각각 호출해주므로,
/// CPlayerController의 트리거 판정 로직(레이어 검사)과 독립적으로 동일한 이벤트를 받을 수 있습니다.
///
/// 주의: _submarineLayer는 CPlayerController의 것과 별개로 여기서도 같은 값으로 설정해야 합니다.
/// (private 필드라 서로 참조할 수 없어 부득이하게 중복 설정합니다)
/// </summary>
public sealed class CSubmarineAreaSensor : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Tooltip("CPlayerController의 잠수함 판정 레이어와 동일하게 맞춰주세요.")]
    [SerializeField] private LayerMask _submarineLayer;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.IsInLayerMask(_submarineLayer))
        {
            OnPlayerSubmarineAreaChanged.Publish(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.IsInLayerMask(_submarineLayer))
        {
            OnPlayerSubmarineAreaChanged.Publish(false);
        }
    }
    #endregion
}
