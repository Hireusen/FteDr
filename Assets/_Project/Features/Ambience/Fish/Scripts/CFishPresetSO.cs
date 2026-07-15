using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 특정 콘셉트의 물고기 프리팹 목록을 모아둔 테이블 프리셋 SO입니다.
/// </summary>
[CreateAssetMenu(fileName = "FishPresetSO_", menuName = "ScriptableObjects/FishPresetSO", order = 1)]
public sealed class CFishPresetSO : ABaseSO
{
    private const string ID = "FishTablePreset";

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("물고기 프리팹 테이블")]
    [SerializeField] private GameObject[] _fishPrefabs;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이 프리셋에 등록된 물고기 프리팹 배열입니다.</summary>
    public GameObject[] FishPrefabs => _fishPrefabs;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void CollectErrorMessage(List<string> errorList)
    {
        base.CollectErrorMessage(errorList);

        if (_fishPrefabs == null || _fishPrefabs.Length == 0)
        {
            errorList.Add($"{errorList.Count + 1}. 프리펩 테이블이 비어있습니다.");
        }
    }

    protected override void Reset()
    {
        _id = ID;
        _type = EDataType.Spawner;
    }
    #endregion
}
