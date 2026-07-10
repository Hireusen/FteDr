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
    [Tooltip("정면 시야 각(도, 전체 폭). 이 각도 안의 플레이어만 감지합니다.")]
    [SerializeField, Range(1f, 360f)] protected float _fieldOfView = 90f;
    [Tooltip("시야 사거리(거리). 이 거리 안의 플레이어만 감지합니다.")]
    [SerializeField] protected float _sightRange;
    [SerializeField] protected float _damage;

    [Header("돌진 / 도주")]
    [Tooltip("돌진 시 이동 속도. 순찰 속도(_moveSpeed)보다 커야 돌진처럼 느껴집니다.")]
    [SerializeField] protected float _dashSpeed;
    [Tooltip("돌진 직전 조준(예비 동작) 시간(초). 플레이어에게 반응할 틈을 줍니다. 0이면 즉시 돌진.")]
    [SerializeField, Min(0f)] protected float _dashWindup = 0.4f;
    [Tooltip("도주 시 이동 속도.")]
    [SerializeField] protected float _fleeSpeed;
    [Tooltip("도주를 유지하는 시간(초). 이 시간이 끝나면 순찰로 복귀합니다.")]
    [SerializeField] protected float _fleeDuration;
    [Tooltip("도주 복귀 후 다시 공격에 나서기까지의 대기 시간(초).")]
    [SerializeField, Min(0f)] protected float _attackCooldown = 3f;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>적 엔티티의 원본 프리팹을 반환합니다.</summary>
    public GameObject Prefab => _prefab;

    /// <summary>적의 기본(순찰) 이동 속도를 반환합니다.</summary>
    public float MoveSpeed => _moveSpeed;

    /// <summary>적의 정면 시야 각(도, 전체 폭)을 반환합니다.</summary>
    public float FieldOfView => _fieldOfView;

    /// <summary>적의 시야 사거리(거리)를 반환합니다.</summary>
    public float SightRange => _sightRange;

    /// <summary>플레이어와 접촉 시 입히는 연료/체력 피해량을 반환합니다.</summary>
    public float Damage => _damage;

    /// <summary>돌진 이동 속도입니다.</summary>
    public float DashSpeed => _dashSpeed;

    /// <summary>돌진 전 조준(예비 동작) 시간(초)입니다.</summary>
    public float DashWindup => _dashWindup;

    /// <summary>도주 이동 속도입니다.</summary>
    public float FleeSpeed => _fleeSpeed;

    /// <summary>도주 지속 시간(초)입니다.</summary>
    public float FleeDuration => _fleeDuration;

    /// <summary>도주 복귀 후 재공격까지의 대기 시간(초)입니다.</summary>
    public float AttackCooldown => _attackCooldown;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Enemy) errorList.Add($"{errorList.Count + 1}. 타입이 Enemy가 아닙니다.");
        if (_prefab == null) errorList.Add($"{errorList.Count + 1}. 프리펩이 비어있습니다.");
        if (_moveSpeed <= 0) errorList.Add($"{errorList.Count + 1}. 이동 속도가 0 이하입니다.");
        if (_fieldOfView <= 0) errorList.Add($"{errorList.Count + 1}. 시야 각이 0 이하입니다.");
        if (_sightRange <= 0) errorList.Add($"{errorList.Count + 1}. 시야 사거리가 0 이하입니다.");
        if (_damage <= 0) errorList.Add($"{errorList.Count + 1}. 대미지가 0 이하입니다.");

        if (_dashSpeed <= 0) errorList.Add($"{errorList.Count + 1}. 돌진 속도가 0 이하입니다.");
        if (_dashSpeed <= _moveSpeed) errorList.Add($"{errorList.Count + 1}. 돌진 속도가 순찰 속도 이하입니다. (돌진이 느립니다)");
        if (_fleeSpeed <= 0) errorList.Add($"{errorList.Count + 1}. 도주 속도가 0 이하입니다.");
        if (_fleeDuration <= 0) errorList.Add($"{errorList.Count + 1}. 도주 지속 시간이 0 이하입니다.");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Enemy;
    }
    #endregion
}
