using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 산소통 정보를 담는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "COxygenSO_", menuName = "ScriptableObjects/OxygenSO", order = 1)]
public class COxygenSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected float[] _maxOxygens; // 산소통 최대 용량
    [SerializeField] protected float[] _drainPercents; // 초당 산소 소모 비율 (0 ~ 1)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float MaxOxygen(int level)
    => GetArrayValueSafely(_maxOxygens, level, -1f);

    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float DrainRate(int level)
    => GetArrayValueSafely(_drainPercents, level, -1f);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 값 유효성 검사
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        IncorrectArrayToAddError(errorList, _maxOxygens, 0f);
        IncorrectArrayToAddError(errorList, _drainPercents, 0f);
    }
    #endregion
}
