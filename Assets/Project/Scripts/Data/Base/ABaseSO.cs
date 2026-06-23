using System.Collections.Generic;
using UnityEngine;
using System.Text;

/// <summary>
/// 모든 SO의 기반이 되는 클래스입니다.
/// </summary>
public abstract class ABaseSO : ScriptableObject
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected string _id = null;
    [SerializeField] protected string _name = "이름";
    [SerializeField] protected EDataType _type = EDataType.None;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public string Id => _id;
    public string Name => _name;
    public EDataType Type => _type;

    // 값 유효성 검사
    protected virtual void CollectErrorMessage(List<string> errorList)
    {
        if (_id.IsBlank())
        {
            errorList.Add($"{errorList.Count + 1}. ID가 비어있습니다.");
        }
        if (_name.IsBlank())
        {
            errorList.Add($"{errorList.Count + 1}. 이름이 비어있습니다.");
        }
        if (_type == EDataType.None)
        {
            errorList.Add($"{errorList.Count + 1}. 타입이 비어있습니다.");
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected virtual void OnValidate()
    {
        List<string> errorList = new();
        StringBuilder sb = new();
        CollectErrorMessage(errorList);
        if (errorList.Count > 0)
        {
            sb.AppendLine($"SO 인스턴스({this.name})의 값이 올바르지 않습니다.");
            int length = errorList.Count;
            for (int i = 0; i < length; ++i)
            {
                sb.AppendLine(errorList[i]);
            }
            UDebug.PrintOnce(sb, LogType.Warning);
        }
    }
    #endregion
}
