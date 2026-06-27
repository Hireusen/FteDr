using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 엔티티의 스탯 및 프리팹 정보를 담는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "EnemySO_", menuName = "ScriptableObjects/EnemySO", order = 1)]
public class CEnemySO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("엔티티 정보")]
    [SerializeField] protected GameObject _prefab;
    [SerializeField] protected float _moveSpeed;
    [SerializeField] protected float _fieldOfView;
    [SerializeField] protected float _damage;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>적 엔티티의 원본 프리팹을 반환합니다.</summary>
    public GameObject Prefab => _prefab;

    /// <summary>적의 기본 이동 속도를 반환합니다.</summary>
    public float MoveSpeed => _moveSpeed;

    /// <summary>적의 플레이어 탐지 시야 반경을 반환합니다.</summary>
    public float FieldOfView => _fieldOfView;

    /// <summary>플레이어와 접촉 시 입히는 산소/체력 피해량을 반환합니다.</summary>
    public float Damage => _damage;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Enemy) errorList.Add($"{errorList.Count + 1}. 타입이 Enemy가 아닙니다.");
        if (_prefab == null) errorList.Add($"{errorList.Count + 1}. 프리펩이 비어있습니다.");
        if (_moveSpeed <= 0) errorList.Add($"{errorList.Count + 1}. 이동 속도가 0 이하입니다.");
        if (_fieldOfView <= 0) errorList.Add($"{errorList.Count + 1}. 시야 범위가 0 이하입니다.");
        if (_damage <= 0) errorList.Add($"{errorList.Count + 1}. 대미지가 0 이하입니다.");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Enemy;
    }
    #endregion
}
