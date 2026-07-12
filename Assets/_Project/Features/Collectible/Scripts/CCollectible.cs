using UnityEngine;

/// <summary>
/// 수집품 루트에 부착하는 컴포넌트입니다.
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

    /// <summary>수집품 가격입니다.</summary>
    public float SellPrice => _data != null ? _data.SellPrice : 0;

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
