using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Runtime.CompilerServices;

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
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
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

    /// <summary>
    /// 안전하게 배열 값을 가져오는 유틸리티 내부 함수
    /// 직관성을 위해 외부에서 1레벨부터 받으며 내부에서 -1하여 인덱스로 사용
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static T GetArrayValueSafely<T>(T[] array, int level, T fallback)
    {
        level--;
        if (!UArray.InBounds(array, level))
        {
            UDebug.Print($"존재하지 않는 {typeof(T).Name} 배열 인덱스에 접근했습니다.", LogType.Error);
            return fallback;
        }
        return array[level];
    }

    /// <summary>
    /// 유효하지 않은 배열일 경우 에러 목록에 추가
    /// </summary>
    protected static void IncorrectArrayToAddError<T>
        (List<string> errorList, T[] array, T defaultValue) where T : struct
    {
        if (array == null)
        {
            errorList.Add($"{errorList.Count + 1} {typeof(T).Name} 배열이 비어있습니다.");
        }
        else
        {
            int length = array.Length;
            for (int i = 0; i < length; ++i)
            {
                if (!EqualityComparer<T>.Default.Equals(array[i], defaultValue)) continue;
                errorList.Add($"{errorList.Count + 1}. {typeof(T).Name} 배열의 {i}번째 인덱스에 올바른 값이 할당되지 않았습니다.");
            }
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
