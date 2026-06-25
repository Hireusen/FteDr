using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 수집품 정보를 담는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "CCollectibleSO_", menuName = "ScriptableObjects/CollectibleSO", order = 1)]
public class CCollectibleSO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected GameObject _prefab;
    [SerializeField] protected bool _isSpecial;
    [SerializeField] protected float _weight;
    [SerializeField] protected float _sellPrice; // 접촉 시 산소 대미지
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public GameObject Prefab => _prefab;
    public bool IsSpecial => _isSpecial;
    public float Weight => _weight;
    public float SellPrice => _sellPrice;
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
        if(_weight <= 0f)
        {
            errorList.Add($"{errorList.Count + 1}. 무게가 0 이하입니다.");
        }
        if(_sellPrice < 0f)
        {
            errorList.Add($"{errorList.Count + 1}. 판매 가격이 0 미만입니다.");
        }
    }
    #endregion
}
