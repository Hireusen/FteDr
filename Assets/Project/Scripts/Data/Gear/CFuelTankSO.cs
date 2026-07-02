using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 연료탱크 스탯을 정의하는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "FuelTankSO_", menuName = "ScriptableObjects/FuelTankSO", order = 1)]
public class CFuelTankSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("연료탱크 정보")]
    [SerializeField] protected float[] _maxFuels;
    [SerializeField] protected float[] _moveFuelCosts; // 이동 시 소모되는 연료 절댓값
    [SerializeField] protected float[] _warningThresholds; // 0.0 ~ 1.0
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 해당 레벨의 최대 연료 용량을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float MaxFuel(int level) => GetArrayValueSafely(_maxFuels, level, -1f);

    /// <summary>
    /// 해당 레벨에서 이동 시 소모되는 연료량(절댓값)을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float MoveFuelCost(int level) => GetArrayValueSafely(_moveFuelCosts, level, -1f);

    /// <summary>
    /// 해당 레벨의 연료 부족 경고 임계값(%)을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float WarningThreshold(int level) => GetArrayValueSafely(_warningThresholds, level, -1f);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.FuelTank) errorList.Add($"{errorList.Count + 1}. 타입이 FuelTank가 아닙니다.");
        IncorrectArrayToAddError(errorList, _maxFuels, 0f);
        IncorrectArrayToAddError(errorList, _moveFuelCosts, 0f);
        IncorrectArrayToAddError(errorList, _warningThresholds, 0f);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.FuelTank;
    }
    #endregion
}
