using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 레이더 정보를 담는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "CRadarSO_", menuName = "ScriptableObjects/RadarSO", order = 1)]
public class CRadarSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected float[] _maxDetectDistances; // 최대 감지 거리
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float MaxDetectDistance(int level)
    => GetArrayValueSafely(_maxDetectDistances, level, -1f);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 값 유효성 검사
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        IncorrectArrayToAddError(errorList, _maxDetectDistances, 0f);
    }
    #endregion
}
