using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 산소통 스탯을 정의하는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "OxygenTankSO_", menuName = "ScriptableObjects/OxygenTankSO", order = 1)]
public class COxygenTankSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("산소통 정보")]
    [SerializeField] protected float[] _maxOxygens;
    [SerializeField] protected float[] _drainPercents;
    [SerializeField] protected float[] _warningThresholds; // 0.0 ~ 1.0
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 해당 레벨의 최대 산소 용량을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float MaxOxygen(int level) => GetArrayValueSafely(_maxOxygens, level, -1f);

    /// <summary>
    /// 해당 레벨의 초당 산소 소모 비율을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float DrainRate(int level) => GetArrayValueSafely(_drainPercents, level, -1f);

    /// <summary>
    /// 해당 레벨의 산소 부족 경고 임계값(%)을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float WarningThreshold(int level) => GetArrayValueSafely(_warningThresholds, level, -1f);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.OxygenTank) errorList.Add($"{errorList.Count + 1}. 타입이 OxygenTank가 아닙니다.");
        IncorrectArrayToAddError(errorList, _maxOxygens, 0f);
        IncorrectArrayToAddError(errorList, _drainPercents, 0f);
        IncorrectArrayToAddError(errorList, _warningThresholds, 0f);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.OxygenTank;
    }
    #endregion
}
