using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레이더의 고정 스펙을 정의하는 SO 클래스입니다.
/// 레이더는 상점 업그레이드 대상이 아니라 처음부터 끝까지 같은 성능을 가지므로,
/// 레벨별 배열이 아니라 단일 값을 보유합니다. (AGearSO를 상속하지 않는 이유)
/// </summary>
[CreateAssetMenu(fileName = "RadarSO_", menuName = "ScriptableObjects/RadarSO", order = 1)]
public class CRadarSO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("레이더 정보 (고정 스펙)")]
    [Tooltip("특수 수집품을 감지할 수 있는 최대 거리(m)입니다. 좁게 두면 탐험 중 우연히 발견하는 느낌이, 넓게 두면 핑 간격으로 방향을 안내하는 느낌이 강해집니다.")]
    [SerializeField, Min(0.01f)] protected float _maxDetectDistance = 20f;
    [Tooltip("감지 한계 거리에서의 핑 주기(초). 가장 느린(가장 긴) 주기입니다.")]
    [SerializeField, Min(0.01f)] protected float _farScanInterval = 3f;
    [Tooltip("특수 수집품에 가장 가까울 때(거리 0)의 핑 주기(초). Far보다 작아야 합니다.")]
    [SerializeField, Min(0.01f)] protected float _nearScanInterval = 0.12f;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>레이더의 최대 감지 거리입니다.</summary>
    public float MaxDetectDistance => _maxDetectDistance;

    /// <summary>감지 한계 거리에서의 핑 주기(가장 느린 주기)입니다.</summary>
    public float FarScanInterval => _farScanInterval;

    /// <summary>최근접 시의 핑 주기(가장 빠른 주기)입니다. Far를 넘지 않도록 보정해 반환합니다.</summary>
    public float NearScanInterval => Mathf.Min(_nearScanInterval, _farScanInterval);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Radar) errorList.Add($"{errorList.Count + 1}. 타입이 Radar가 아닙니다.");
        if (_maxDetectDistance <= 0f) errorList.Add($"{errorList.Count + 1}. 최대 감지 거리가 0 이하입니다.");
        if (_farScanInterval <= 0f) errorList.Add($"{errorList.Count + 1}. 원거리 핑 주기가 0 이하입니다.");
        if (_nearScanInterval <= 0f) errorList.Add($"{errorList.Count + 1}. 근거리 핑 주기가 0 이하입니다.");
        if (_nearScanInterval > _farScanInterval) errorList.Add($"{errorList.Count + 1}. 근거리 핑 주기가 원거리 주기보다 깁니다. (가까울수록 빨라야 합니다)");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Radar;
    }
    #endregion
}
