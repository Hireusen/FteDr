using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테이블 SO의 기반을 구현하는 추상화 클래스
/// 상속받아서 편하게 테이블 SO를 만들 것
/// </summary>
public abstract class ATableSO<T> : ScriptableObject where T : ABaseSO
{
    [Header("SO 인스턴스 목록")]
    [SerializeField] protected List<T> _elements;

    protected Dictionary<string, T> _elementDict;

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 인스턴스 SO를 문자열 ID로 가져옵니다.
    /// </summary>
    /// <param name="id">ID</param>
    public T GetSO(string id)
    {
        if (_elementDict.TryGetValue(id, out T so))
        {
            return so;
        }
        // 데이터베이스 매니저에 위임
        // UDebug.Print($"존재하지 않는 ID({id})로 SO 인스턴스를 가져오려 시도했습니다.", LogType.Assert);
        return null;
    }

    // 값 유효성 검사
    public virtual bool IsValid()
    {
        if (!UArray.IsInitedList(_elements)) return false;
        return true;
    }

    /// <summary>
    /// 딕셔너리 초기화를 위해 외부에서 호출해주는 메서드
    /// </summary>
    public void Initialize()
    {
        BuildElementDict();
    }

    /// <summary>
    /// 순회를 위해 원본 리스트 받기
    /// </summary>
    public IReadOnlyList<T> ReadList() => _elements;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 인스펙터의 리스트로 SoundSO를 담는 딕셔너리 작성
    private void BuildElementDict()
    {
        if (UDebug.IsNull(_elements))
        {
            return;
        }
        _elementDict = new();
        // 리스트 순회
        int length = _elements.Count;
        int success = 0;
        for (int i = 0; i < length; ++i)
        {
            T so = _elements[i];
            if (so == null)
            {
                UDebug.Print($"테이블({typeof(T).Name})의 {i}번째 공간이 비어있습니다.", LogType.Warning);
                continue;
            }
            // 딕셔너리 등록 시도
            if (!_elementDict.TryAdd(so.Id, so))
            {
                UDebug.Print($"SO 중복 등록이 감지되었습니다. (ID = {so.Id})");
                continue; // 실패 → 중복 등록
            }
            success++;
        }
        UDebug.Print($"테이블의 딕셔너리 작성을 완료했습니다. (총 {success}개)");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected virtual void OnValidate()
    {
        if (!IsValid())
        {
            UDebug.PrintOnce($"테이블({this.name})의 값이 올바르지 않습니다. (Type = {this.GetType().Name})", LogType.Warning);
        }
    }
    #endregion
}
