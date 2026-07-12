using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 추진기 스탯을 정의하는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "ThrusterSO_", menuName = "ScriptableObjects/ThrusterSO", order = 1)]
public class CThrusterSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("추진기 정보")]
    [SerializeField] protected float[] _maxSpeeds;
    [SerializeField] protected float[] _forwardThurstPowers;
    [SerializeField] protected float[] _verticalThurstPowers;
    [SerializeField] protected float[] _maxVelocitys;
    [SerializeField] protected float[] _boostConsumptions;
    [SerializeField] protected AnimationCurve _accelerationCurve;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 해당 레벨의 최고 이동 속도를 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float MaxSpeed(int level) => GetArrayValueSafely(_maxSpeeds, level, -1f);

    /// <summary>
    /// 해당 레벨의 전방 가속력을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float ForwardThrustPower(int level) => GetArrayValueSafely(_forwardThurstPowers, level, -1f);

    /// <summary>
    /// 해당 레벨의 수직 가속력을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float VerticalThrustPower(int level) => GetArrayValueSafely(_verticalThurstPowers, level, -1f);

    /// <summary>
    /// 해당 레벨의 물리 최대 한계 속도를 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float MaxVelocity(int level) => GetArrayValueSafely(_maxVelocitys, level, -1f);

    /// <summary>
    /// 해당 레벨의 부스트 사용 시 추가 자원 소모량을 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float BoostConsumption(int level) => GetArrayValueSafely(_boostConsumptions, level, -1f);

    /// <summary>
    /// 추진기의 가속력 적용을 위한 애니메이션 커브를 반환합니다.
    /// </summary>
    public AnimationCurve AccelerationCurve => _accelerationCurve;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Thruster) errorList.Add($"{errorList.Count + 1}. 타입이 Thruster가 아닙니다.");
        IncorrectArrayToAddError(errorList, _maxSpeeds, 0f);
        IncorrectArrayToAddError(errorList, _forwardThurstPowers, 0f);
        IncorrectArrayToAddError(errorList, _verticalThurstPowers, 0f);
        IncorrectArrayToAddError(errorList, _maxVelocitys, 0f);
        IncorrectArrayToAddError(errorList, _boostConsumptions, 0f);
        if (_accelerationCurve == null)
        {
            errorList.Add($"{errorList.Count + 1}. 애니메이션 커브가 할당되지 않았습니다.");
        }
    }

    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Thruster;
    }
    #endregion
}
