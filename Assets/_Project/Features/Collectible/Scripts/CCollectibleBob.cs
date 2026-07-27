using UnityEngine;

/// <summary>
/// 공중 수집품에 상하 부유감을 주는 컴포넌트입니다.
/// 기준점 Y에 사인파를 더한 "목표 위치"로 오르내립니다. (강도·속도는 인스펙터 고정)
///
/// 수집품은 세 국면을 거칩니다.
///  1) Rigidbody 없음 + 안 잡힘 : Update에서 transform을 직접 목표 위치로 이동 (kinematic 부유)
///  2) Rigidbody 있음 + 안 잡힘 : FixedUpdate에서 rb.MovePosition으로 동일한 목표 Y를 재현 (물리 부유)
///                                XZ는 집게 견인에 맡기고 Y만 부유시킨다.
///  3) 잡힘(CCollectible.IsHeld) : 아무것도 하지 않는다. (집게가 위치를 지배)
///
/// 상태 1→2 전환(Rigidbody 부착)은 붙이는 쪽(CNewGrab)이 OnBodyAttached(rb)로 알려줍니다.
/// 폴링은 IsHeld(캐시된 bool) 읽기뿐이라 비용이 없습니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CCollectible))]
public sealed class CCollectibleBob : AFrameable, IUpdateFrameable, IFixedUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("상하 부유")]
    [Tooltip("위아래로 흔들리는 진폭(m). 기준점에서 ± 이만큼 움직입니다.")]
    [SerializeField] private float _amplitude = 0.15f;
    [Tooltip("부유 속도(초당 라디안). 클수록 빠르게 오르내립니다.")]
    [SerializeField] private float _frequency = 1.5f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CCollectible _collectible; // 상태(IsHeld) 조회용
    private Rigidbody _rb;             // 부착되면 물리 부유로 전환 (null이면 kinematic 부유)
    private Vector3 _basePosition;     // 부유 기준점 (스폰 위치)
    private float _phase;              // 위상 오프셋 (개체마다 랜덤 → 군무처럼 어긋남)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv3;
    public EFixedUpdatePriority FixedUpdatePriority => EFixedUpdatePriority.Lv3;

    /// <summary>
    /// 스포너가 위치를 확정한 뒤 호출합니다. 현재 위치를 부유 기준점으로 캡처하고 위상을 무작위화합니다.
    /// 프리팹에 이 컴포넌트가 미리 붙어 있어도, 이 호출 시점의 위치가 기준점이 됩니다.
    /// </summary>
    public void Initialize()
    {
        _basePosition = transform.position;
        _phase = Random.Range(0f, Mathf.PI * 2f);
    }

    /// <summary>
    /// 집게가 이 수집품에 Rigidbody를 붙인 직후 호출합니다. 이후 부유는 물리(MovePosition) 방식으로 전환됩니다.
    /// </summary>
    /// <param name="rb">방금 부착된 Rigidbody</param>
    public void OnBodyAttached(Rigidbody rb)
    {
        _rb = rb;
    }

    // 상태 1: Rigidbody가 없고 잡히지 않은 동안 transform을 직접 목표 위치로 옮긴다.
    public void ExecuteUpdateFrame()
    {
        if (_rb != null || _collectible.IsHeld) return;

        transform.position = TargetPosition(_basePosition);
    }

    // 상태 2: Rigidbody가 붙었고 잡히지 않은 동안 물리로 목표 Y를 재현한다. (XZ는 집게 견인에 맡김)
    public void ExecuteFixedUpdateFrame()
    {
        if (_rb == null || _collectible.IsHeld) return;

        // XZ는 현재 물리 위치를 유지하고 Y만 부유 목표로 이동시킨다.
        Vector3 target = _rb.position;
        target.y = TargetY(_basePosition.y);
        _rb.MovePosition(target);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 기준점 + 상하 사인파 오프셋을 적용한 목표 위치. (X/Z는 기준점 유지)
    private Vector3 TargetPosition(Vector3 basePos)
    {
        basePos.y = TargetY(basePos.y);
        return basePos;
    }

    // 기준 Y에 사인파 오프셋을 더한 목표 Y.
    private float TargetY(float baseY)
        => baseY + Mathf.Sin(Time.time * _frequency + _phase) * _amplitude;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _collectible = GetComponent<CCollectible>();
        _basePosition = transform.position;
        _phase = Random.Range(0f, Mathf.PI * 2f);
    }
    #endregion
}
