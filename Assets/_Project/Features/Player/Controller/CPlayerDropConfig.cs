using UnityEngine;

public class CPlayerDropConfig : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("드롭 연출 설정")]
    [Tooltip("수집품이 흩뿌려질 최대 반경")]
    [SerializeField] private float _scatterRadius = 3f;

    [Tooltip("수집품이 튀어오르는 위쪽 힘 (지상 수집품 전용)")]
    [SerializeField] private float _scatterUpForce = 5f;

    [Tooltip("수집품이 바깥으로 퍼지는 힘")]
    [SerializeField] private float _scatterOutForce = 3f;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public float ScatterRadius => _scatterRadius;
    public float ScatterUpForce => _scatterUpForce;
    public float ScatterOutForce => _scatterOutForce;
    #endregion
}
