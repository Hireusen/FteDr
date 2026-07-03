using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 한 스테이지에서 스폰할 수집품 목록을 정의하는 SO 클래스입니다.
/// </summary>
[CreateAssetMenu(fileName = "StageSpawnSO_", menuName = "ScriptableObjects/StageSpawnSO", order = 1)]
public class CStageSpawnSO : ABaseSO
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("스폰 목록")]
    [SerializeField] protected SpawnEntry[] _entries;

    [Header("스테이지 총량 (모든 항목 개수 합의 목표 범위)")]
    [SerializeField, Min(0)] protected int _minTotal;
    [SerializeField, Min(0)] protected int _maxTotal;

    [Header("일괄 적용 (아래 값을 컨텍스트 메뉴로 모든 항목에 복사)")]
    [SerializeField, Min(0)] protected int _bulkMinCount;
    [SerializeField, Min(0)] protected int _bulkMaxCount;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이 스테이지의 스폰 항목 배열입니다.</summary>
    public IReadOnlyList<SpawnEntry> Entries => _entries;

    /// <summary>스테이지 총량 최소치입니다.</summary>
    public int MinTotal => _minTotal;

    /// <summary>스테이지 총량 최대치입니다.</summary>
    public int MaxTotal => _maxTotal;

    /// <summary>모든 항목의 최소 개수 합입니다.</summary>
    public int SumOfEntryMins
    {
        get
        {
            if (_entries == null) return 0;
            int sum = 0;
            for (int i = 0; i < _entries.Length; ++i)
            {
                sum += _entries[i].MinCount;
            }
            return sum;
        }
    }

    /// <summary>모든 항목의 최대 개수 합입니다.</summary>
    public int SumOfEntryMaxes
    {
        get
        {
            if (_entries == null) return 0;
            int sum = 0;
            for (int i = 0; i < _entries.Length; ++i)
            {
                sum += _entries[i].MaxCount;
            }
            return sum;
        }
    }

    /// <summary>
    /// 엔트리 min/max 합과 스테이지 총량 min/max가 서로 만족 가능한지 검사합니다.
    /// 불가능하면 false와 사유를 반환합니다. (스포너가 스폰 전에 호출)
    /// </summary>
    /// <param name="reason">실패 사유(성공 시 빈 문자열)</param>
    public bool IsSatisfiable(out string reason)
    {
        int minSum = SumOfEntryMins;
        int maxSum = SumOfEntryMaxes;

        // 엔트리 최소 합이 스테이지 총량 최대를 넘으면, 아무리 줄여도 총량 max를 못 지킴
        if (minSum > _maxTotal)
        {
            reason = $"엔트리 최소 합({minSum})이 스테이지 총량 최대({_maxTotal})를 초과합니다.";
            return false;
        }
        // 엔트리 최대 합이 스테이지 총량 최소에 못 미치면, 아무리 늘려도 총량 min을 못 채움
        if (maxSum < _minTotal)
        {
            reason = $"엔트리 최대 합({maxSum})이 스테이지 총량 최소({_minTotal})에 못 미칩니다.";
            return false;
        }

        reason = string.Empty;
        return true;
    }
    #endregion

    #region ─────────────────────────▶ 일괄 적용 ◀─────────────────────────
    // SO 인스펙터 우상단 ⋮ 메뉴에서 실행. _bulkMinCount/_bulkMaxCount를 모든 엔트리에 복사한다.
    [ContextMenu("모든 항목에 일괄 개수 적용")]
    private void ApplyBulkCount()
    {
        if (_entries == null || _entries.Length == 0)
        {
            UDebug.Print("일괄 적용할 스폰 목록이 없습니다.", LogType.Warning);
            return;
        }
        if (_bulkMaxCount < _bulkMinCount)
        {
            UDebug.Print($"일괄 값이 잘못되었습니다. (min {_bulkMinCount} > max {_bulkMaxCount})", LogType.Error);
            return;
        }

        for (int i = 0; i < _entries.Length; ++i)
        {
            _entries[i].SetCount(_bulkMinCount, _bulkMaxCount);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); // 변경 사항 저장 보장
#endif
        UDebug.Print($"모든 항목({_entries.Length}개)에 개수 {_bulkMinCount}~{_bulkMaxCount}를 적용했습니다.");
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);

        if (_type != EDataType.Spawner)
        {
            errorList.Add($"{errorList.Count + 1}. 타입이 스포너가 아닙니다.");
        }
        if (_entries == null || _entries.Length == 0)
        {
            errorList.Add($"{errorList.Count + 1}. 스폰 목록이 비어있습니다.");
            return;
        }
        if (_maxTotal < _minTotal)
        {
            errorList.Add($"{errorList.Count + 1}. 스테이지 총량 최대가 최소보다 작습니다.");
        }

        for (int i = 0; i < _entries.Length; ++i)
        {
            if (_entries[i].Collectible == null)
            {
                errorList.Add($"{errorList.Count + 1}. 스폰 목록 {i}번 항목의 수집품이 비어있습니다.");
            }
            if (_entries[i].MaxCount < _entries[i].MinCount)
            {
                errorList.Add($"{errorList.Count + 1}. 스폰 목록 {i}번 항목의 최대 개수가 최소보다 작습니다.");
            }
        }

        // 총량과 엔트리 범위의 논리적 충돌 검사 (에디터에서 미리 경고)
        if (!IsSatisfiable(out string reason))
        {
            errorList.Add($"{errorList.Count + 1}. 개수 설정 충돌: {reason}");
        }
    }

    protected override void Reset()
    {
        _type = EDataType.Spawner;
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    /// <summary>스폰할 수집품 한 종류와 그 개수 범위(min~max)를 묶는 항목입니다.</summary>
    [System.Serializable]
    public struct SpawnEntry
    {
        [SerializeField] private CCollectibleSO _collectible;
        [SerializeField, Min(0)] private int _minCount;
        [SerializeField, Min(0)] private int _maxCount;

        public CCollectibleSO Collectible => _collectible;
        public int MinCount => _minCount;
        public int MaxCount => _maxCount;

        /// <summary>개수 범위를 일괄 설정합니다. (수집품 참조는 유지)</summary>
        public void SetCount(int min, int max)
        {
            _minCount = min;
            _maxCount = max;
        }
    }
    #endregion
}
