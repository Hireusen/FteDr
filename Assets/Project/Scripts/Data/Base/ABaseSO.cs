using System.Collections.Generic;
using UnityEngine;
using System.Text;

/// <summary>
/// 모든 SO의 기반이 되는 추상 클래스입니다.
/// </summary>
public abstract class ABaseSO : ScriptableObject
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("기본 정보")]
    [SerializeField] protected string _id = null;
    [SerializeField] protected string _name = "이름";
    [SerializeField][TextArea] protected string _description = "설명";
    [SerializeField] protected EDataType _type = EDataType.None;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>데이터의 고유 문자열 ID를 반환합니다.</summary>
    public string Id => _id;

    /// <summary>데이터의 출력용 이름을 반환합니다.</summary>
    public string Name => _name;

    /// <summary>데이터의 상세 설명을 반환합니다.</summary>
    public string Description => _description;

    /// <summary>데이터의 카테고리 타입을 반환합니다.</summary>
    public EDataType Type => _type;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터에 노출된 값들의 유효성을 검사하여 에러 목록에 수집합니다.
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

    // 배열의 인덱스 범위를 벗어나는 접근을 방지하고 안전하게 값을 반환합니다. (외부는 1레벨 기준, 내부는 0인덱스 기준)
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

    // 배열이 할당되지 않았거나 기본값(0 등)이 들어있는 경우 에러 목록에 추가합니다.
    protected static void IncorrectArrayToAddError<T>(List<string> errorList, T[] array, T defaultValue) where T : struct
    {
        if (array == null || array.Length == 0)
        {
            errorList.Add($"{errorList.Count + 1}. {typeof(T).Name} 배열이 비어있거나 길이가 0입니다.");
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
    // 에디터에서 값이 변경될 때마다 자동 실행되어 유효성 검사를 수행합니다.
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

    protected virtual void Reset()
    {
        _type = EDataType.None;
    }
    #endregion
}
