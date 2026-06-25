using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 추진기 정보를 담는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "CThrusterSO_", menuName = "ScriptableObjects/ThrusterSO", order = 1)]
public class CThrusterSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected float[] _maxSpeeds; // 최대 이동속도
    [SerializeField] protected float[] _forwardThurstPowers; // 전방 가속력
    [SerializeField] protected float[] _verticalThurstPowers; // 수직 가속력
    [SerializeField] protected float[] _maxVelocitys; // 최대 속도
    [SerializeField] protected AnimationCurve _accelerationCurve; // 가속력 곡선
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float MaxSpeed(int level)
    => GetArrayValueSafely(_maxSpeeds, level, -1f);

    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float ForwardThrustPower(int level)
    => GetArrayValueSafely(_forwardThurstPowers, level, -1f);

    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float VerticalThrustPower(int level)
    => GetArrayValueSafely(_verticalThurstPowers, level, -1f);

    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public float MaxVelocity(int level)
    => GetArrayValueSafely(_maxVelocitys, level, -1f);

    public AnimationCurve AccelerationCurve => _accelerationCurve;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 값 유효성 검사
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        IncorrectArrayToAddError(errorList, _maxSpeeds, 0f);
        IncorrectArrayToAddError(errorList, _forwardThurstPowers, 0f);
        IncorrectArrayToAddError(errorList, _verticalThurstPowers, 0f);
        IncorrectArrayToAddError(errorList, _maxVelocitys, 0f);
        if (_accelerationCurve == null)
        {
            errorList.Add($"{errorList.Count + 1}. 애니메이션 커브가 할당되지 않았습니다.");
        }
    }
    #endregion
}
