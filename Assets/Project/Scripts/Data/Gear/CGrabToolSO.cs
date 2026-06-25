using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 집게 도구 정보를 담는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "CGrabToolSO_", menuName = "ScriptableObjects/GrabToolSO", order = 1)]
public class CGrabToolSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected float[] _reachDistances; // 집게 발사 최대 거리
    [SerializeField] protected float[] _grabSpeeds; // 발사된 집게 전진 속도
    [SerializeField] protected float[] _grabForces; // 집게 힘
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float ReachDistance(int level)
    => GetArrayValueSafely(_reachDistances, level, -1f);

    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float GrabSpeed(int level)
    => GetArrayValueSafely(_grabSpeeds, level, -1f);

    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float GrabForce(int level)
    => GetArrayValueSafely(_grabForces, level, -1f);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 값 유효성 검사
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        IncorrectArrayToAddError(errorList, _reachDistances, 0f);
        IncorrectArrayToAddError(errorList, _grabSpeeds, 0f);
        IncorrectArrayToAddError(errorList, _grabForces, 0f);
    }
    #endregion
}
