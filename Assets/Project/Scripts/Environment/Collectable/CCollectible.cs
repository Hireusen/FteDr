using UnityEngine;

/// <summary>
/// 수집품 루트에 부착하는 컴포넌트입니다.
/// 데이터(CCollectibleSO) 보유, 외곽선 제어, 집게 상호작용 진입점을 담당합니다.
///
/// [계층 전제]
/// 루트(이 컴포넌트 + 스폰 중 Rigidbody) 아래에 하나 이상의 Visual 오브젝트가 있고,
/// 각 Visual은 MeshRenderer/MeshCollider/COutline을 가진다.
/// Visual이 여러 개인 프리팹(예: 조각 여러 개)도 있으므로, 자식 구성요소는 항상 복수로 참조한다.
///
/// [집게 상호작용]
/// 잡는 메커니즘(관절/힘 등)은 집게 팔 쪽이 구현한다. 이 컴포넌트는 집게가 필요로 하는
/// 정보(무게·물리 바디)와 상태 전환 훅(OnGrabbed/OnReleased)만 노출한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CCollectible : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("데이터")]
    [Tooltip("이 수집품의 정의 SO")]
    [SerializeField] private CCollectibleSO _data;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private COutline[] _outlines; // 자식의 모든 외곽선 (Visual 여러 개 대응)
    private bool _isHeld;         // 집게에 잡힌 상태인지
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이 수집품의 정의 SO입니다.</summary>
    public CCollectibleSO Data => _data;

    /// <summary>수집품 무게입니다. (집게 팔에 전달할 값)</summary>
    public float Weight => _data != null ? _data.Weight : 0f;

    /// <summary>특수 수집품 여부입니다.</summary>
    public bool IsSpecial => _data != null && _data.IsSpecial;

    /// <summary>루트 물리 바디입니다. 없을 수 있음(안정화 후 제거됨). 집게가 잡을 대상.</summary>
    public Rigidbody Body => TryGetComponent(out Rigidbody rb) ? rb : null;

    /// <summary>현재 집게에 잡혀 있는지 여부입니다.</summary>
    public bool IsHeld => _isHeld;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ 외곽선 ◀─────────────────────────
    /// <summary>모든 Visual의 외곽선을 켭니다.</summary>
    public void ShowOutline()
    {
        if (_outlines == null) return;

        for (int i = 0; i < _outlines.Length; ++i)
        {
            if (_outlines[i] != null) _outlines[i].OutLineOn();
        }
    }

    /// <summary>모든 Visual의 외곽선을 끕니다.</summary>
    public void HideOutline()
    {
        if (_outlines == null) return;

        for (int i = 0; i < _outlines.Length; ++i)
        {
            if (_outlines[i] != null) _outlines[i].OutLineOff();
        }
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ 집게 상호작용 ◀─────────────────────────
    /// <summary>
    /// 집게에 잡혔을 때 호출합니다. (집게 팔이 호출)
    /// 잡는 물리 처리는 집게가 하고, 이 메서드는 수집품 쪽 상태/표현만 갱신합니다.
    /// </summary>
    public void OnGrabbed()
    {
        _isHeld = true;
        ShowOutline();
    }

    /// <summary>
    /// 집게에서 놓였을 때 호출합니다. (집게 팔이 호출)
    /// </summary>
    public void OnReleased()
    {
        _isHeld = false;
        HideOutline();
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _outlines = GetComponentsInChildren<COutline>(true);
    }
    #endregion
}
