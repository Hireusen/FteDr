using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 한 스테이지에서 스폰할 수집품 목록을 정의하는 SO 클래스입니다.
/// 각 항목은 자체 min/max 개수를 갖고, 스테이지 전체도 총량 min/max를 갖습니다.
/// 실제 개수 결정과 배치는 CCollectibleSpawner가 수행합니다.
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
    }
    #endregion
}
