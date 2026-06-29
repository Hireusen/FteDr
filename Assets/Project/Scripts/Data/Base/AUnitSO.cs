using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 엔티티/아이템 계열 SO 추상 클래스입니다.
/// </summary>
public abstract class AUnitSO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("유닛 텍스트 정보")]
    [SerializeField] protected string _name = "이름";
    [SerializeField][TextArea] protected string _description = "설명";
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>데이터의 출력용 이름을 반환합니다.</summary>
    public string Name => _name;

    /// <summary>데이터의 상세 설명을 반환합니다.</summary>
    public string Description => _description;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);
        if (_name.IsBlank())
        {
            errorList.Add($"{errorList.Count + 1}. 이름이 비어있습니다.");
        }
    }
    #endregion
}
