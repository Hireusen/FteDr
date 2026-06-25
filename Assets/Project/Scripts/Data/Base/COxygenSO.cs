using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 레벨별 산소통을 정의하는 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "COxygenSO_", menuName = "ScriptableObjects/COxygenSO", order = 1)]
public class COxygenSO : AGearSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected float[] _maxOxygen;
    [SerializeField] protected float[] _;
    
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public string Id => _id;
    public EDataType Type => _type;
    public string Name => _name;

    // 값 유효성 검사
    protected virtual void CollectErrorMessage(List<string> errorList)
    {
        if (_id.IsBlank()) {
            errorList.Add($"{errorList.Count + 1}. ID가 비어있습니다.");
        }
        if (_type == EDataType.None) {
            errorList.Add($"{errorList.Count + 1}. 타입이 비어있습니다.");
        }
        if (_name.IsBlank()) {
            errorList.Add($"{errorList.Count + 1}. 이름이 비어있습니다.");
        }
    }
    #endregion
}
