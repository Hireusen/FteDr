using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어가 사용하는 장비 데이터의 공통 규격을 정의하는 추상 클래스입니다.
/// </summary>
public abstract class AGearSO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("장비 공통 정보")]
    [SerializeField] protected Sprite _shopIcon;
    [Tooltip("(index) 레벨에서 (index + 1)레벨이 되기 위한 경험치/재화")]
    [SerializeField] protected int[] _upgradeCosts;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>상점 및 UI에 표시될 장비 아이콘을 반환합니다.</summary>
    public Sprite ShopIcon => _shopIcon;

    /// <summary>이 장비가 도달할 수 있는 최대 레벨을 반환합니다.</summary>
    public int MaxLevel => _upgradeCosts != null ? _upgradeCosts.Length + 1 : 0;

    /// <summary>
    /// 현재 레벨에서 다음 레벨로 업그레이드하기 위한 비용을 반환합니다. (레벨 1부터 시작)
    /// 배열 범위를 벗어날 경우 -1을 반환합니다.
    /// </summary>
    /// <param name="level">현재 장비 레벨</param>
    public int UpgradeCost(int level) => GetArrayValueSafely(_upgradeCosts, level, -1);
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
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
