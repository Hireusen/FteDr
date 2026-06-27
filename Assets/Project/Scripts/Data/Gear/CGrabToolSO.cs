using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 집게 도구 스탯을 정의하는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "GrabToolSO_", menuName = "ScriptableObjects/GrabToolSO", order = 1)]
public class CGrabToolSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("집게 도구 정보")]
    [SerializeField] protected float[] _reachDistances;
    [SerializeField] protected float[] _grabSpeeds;
    [SerializeField] protected float[] _grabForces;
    [SerializeField] protected float[] _cooldowns;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 해당 레벨의 집게 발사 최대 사거리를 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float ReachDistance(int level) => GetArrayValueSafely(_reachDistances, level, -1f);

    /// <summary>
    /// 해당 레벨의 발사된 집게 전진 속도를 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float GrabSpeed(int level) => GetArrayValueSafely(_grabSpeeds, level, -1f);

    /// <summary>
    /// 해당 레벨의 물체를 끌어당기는 힘(물리력)을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float GrabForce(int level) => GetArrayValueSafely(_grabForces, level, -1f);

    /// <summary>
    /// 해당 레벨의 집게 연속 발사 대기 시간(쿨타임)을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float Cooldown(int level) => GetArrayValueSafely(_cooldowns, level, -1f);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.GrabTool) errorList.Add($"{errorList.Count + 1}. 타입이 GrabTool이 아닙니다.");
        IncorrectArrayToAddError(errorList, _reachDistances, 0f);
        IncorrectArrayToAddError(errorList, _grabSpeeds, 0f);
        IncorrectArrayToAddError(errorList, _grabForces, 0f);
        IncorrectArrayToAddError(errorList, _cooldowns, 0f);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.GrabTool;
    }
    #endregion
}
