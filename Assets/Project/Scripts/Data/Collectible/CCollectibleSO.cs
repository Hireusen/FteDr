using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 수집품 정보를 담는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "CollectibleSO_", menuName = "ScriptableObjects/CollectibleSO", order = 1)]
public class CCollectibleSO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected GameObject _prefab;
    [SerializeField] protected bool _isSpecial;
    [SerializeField] protected float _weight;
    [SerializeField] protected float _sellPrice; // 판매 가격

    [Header("크기 범위 (스폰 시 이 범위에서 랜덤)")]
    [SerializeField] protected float _minScale = 1f;
    [SerializeField] protected float _maxScale = 1f;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public GameObject Prefab => _prefab;
    public bool IsSpecial => _isSpecial;
    public float Weight => _weight;
    public float SellPrice => _sellPrice;
    public float MinScale => _minScale;
    public float MaxScale => _maxScale;

    /// <summary>min~max 범위에서 랜덤한 크기 배율을 반환합니다.</summary>
    public float GetRandomScale() => Random.Range(_minScale, _maxScale);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 값 유효성 검사
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);

        if (_prefab == null)
        {
            errorList.Add($"{errorList.Count + 1}. 프리펩이 비어있습니다.");
        }
        if (_weight <= 0f)
        {
            errorList.Add($"{errorList.Count + 1}. 무게가 0 이하입니다.");
        }
        if (_sellPrice < 0f)
        {
            errorList.Add($"{errorList.Count + 1}. 판매 가격이 0 미만입니다.");
        }
        if (_minScale <= 0f)
        {
            errorList.Add($"{errorList.Count + 1}. 최소 크기 배율이 0 이하입니다.");
        }
        if (_maxScale < _minScale)
        {
            errorList.Add($"{errorList.Count + 1}. 최대 크기 배율이 최소보다 작습니다.");
        }
    }
    #endregion
}
