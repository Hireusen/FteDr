using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 레이더 스탯을 정의하는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "RadarSO_", menuName = "ScriptableObjects/RadarSO", order = 1)]
public class CRadarSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("레이더 정보")]
    [SerializeField] protected float[] _maxDetectDistances;
    [SerializeField] protected float[] _scanIntervals;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 해당 레벨의 레이더 최대 감지 거리를 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float MaxDetectDistance(int level) => GetArrayValueSafely(_maxDetectDistances, level, -1f);

    /// <summary>
    /// 해당 레벨의 레이더 스캔(핑) 갱신 주기를 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float ScanInterval(int level) => GetArrayValueSafely(_scanIntervals, level, -1f);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Radar) errorList.Add($"{errorList.Count + 1}. 타입이 Radar가 아닙니다.");
        IncorrectArrayToAddError(errorList, _maxDetectDistances, 0f);
        IncorrectArrayToAddError(errorList, _scanIntervals, 0f);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Radar;
    }
    #endregion
}
