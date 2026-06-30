using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 가방 스탯을 정의하는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "BagSO_", menuName = "ScriptableObjects/BagSO", order = 1)]
public class CBagSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("가방 정보")]
    [SerializeField] protected int[] _capacities;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 해당 레벨의 가방 최대 슬롯 수를 반환합니다.
    /// </summary>
    /// <param name="level">장비 레벨 (1부터 시작)</param>
    public int Capacity(int level) => GetArrayValueSafely(_capacities, level, -1);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_type != EDataType.Bag) errorList.Add($"{errorList.Count + 1}. 타입이 Bag이 아닙니다.");
        IncorrectArrayToAddError(errorList, _capacities, 0);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void Reset()
    {
        _type = EDataType.Bag;
    }
    #endregion
}
