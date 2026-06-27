using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어가 사용하는 장비를 정의하는 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "AGearSO_", menuName = "ScriptableObjects/GearSO", order = 1)]
public abstract class AGearSO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected Sprite _shopIcon;
    [Tooltip("(index) 레벨에서 (index + 1)레벨이 되기 위한 경험치")]
    [SerializeField] protected int[] _upgradeCosts;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public Sprite ShopIcon => _shopIcon;

    /// <summary>
    /// level에서 업그레이드하는 가격을 반환받습니다. (레벨 1부터 시작)
    /// 존재하지 않는 레벨일 경우 -1을 반환합니다.
    /// </summary>
    public int UpgradeCost(int level)
    => GetArrayValueSafely(_upgradeCosts, level, -1);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 값 유효성 검사
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_shopIcon == null)
        {
            errorList.Add($"{errorList.Count + 1}. 상점 아이콘이 비어있습니다.");
        }
        IncorrectArrayToAddError(errorList, _upgradeCosts, 0);
    }
    #endregion
}
