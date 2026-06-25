using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 정보를 담는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "CEnemySO_", menuName = "ScriptableObjects/EnemySO", order = 1)]
public class CEnemySO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected GameObject _prefab;
    [SerializeField] protected float _moveSpeed;
    [SerializeField] protected float _fieldOfView; // 시야 범위
    [SerializeField] protected float _damage; // 접촉 시 산소 대미지
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public GameObject Prefab => _prefab;
    public float MoveSpeed => _moveSpeed;
    public float FieldOfView => _fieldOfView;
    public float Damage => _damage;
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
        if(_moveSpeed <= 0)
        {
            errorList.Add($"{errorList.Count + 1}. 이동 속도가 0 이하입니다.");
        }
        if(_fieldOfView <= 0)
        {
            errorList.Add($"{errorList.Count + 1}. 시야 범위가 0 이하입니다.");
        }
        if(_damage <= 0)
        {
            errorList.Add($"{errorList.Count + 1}. 대미지가 0 이하입니다.");
        }
    }
    #endregion
}
