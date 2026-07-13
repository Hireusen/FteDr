using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 그물 스탯을 정의하는 SO 클래스입니다.
/// 조준형(발사체)이라 도달 거리는 발사 속도로, 회수 폭은 발사체 x,z 스케일 배율로 결정됩니다.
/// </summary>
[CreateAssetMenu(fileName = "NetSO_", menuName = "ScriptableObjects/NetSO", order = 1)]
public class CNetSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("그물 정보")]
    [SerializeField] protected float[] _launchSpeeds; // 발사 속도(도달 거리) m/s
    [SerializeField] protected float[] _catchScales;  // 발사체 x,z 스케일 배율 (회수 폭)
    [SerializeField] protected float[] _cooldowns;    // 사용 쿨타임(초)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>해당 레벨의 발사 속도(m/s)를 반환합니다. 클수록 멀리 날아갑니다.</summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float LaunchSpeed(int level) => GetArrayValueSafely(_launchSpeeds, level, -1f);

    /// <summary>해당 레벨의 발사체 x,z 스케일 배율을 반환합니다. (레벨1=1.0 기준 권장)</summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float CatchScale(int level) => GetArrayValueSafely(_catchScales, level, -1f);

    /// <summary>해당 레벨의 사용 쿨타임(초)을 반환합니다.</summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public float Cooldown(int level) => GetArrayValueSafely(_cooldowns, level, -1f);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Net) errorList.Add($"{errorList.Count + 1}. 타입이 Net이 아닙니다.");
        IncorrectArrayToAddError(errorList, _launchSpeeds, 0f);
        IncorrectArrayToAddError(errorList, _catchScales, 0f);
        IncorrectArrayToAddError(errorList, _cooldowns, 0f);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Net;
    }
    #endregion
}
