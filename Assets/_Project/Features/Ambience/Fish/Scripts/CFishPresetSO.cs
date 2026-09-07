using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물고기 프리팹 목록을 보관하는 스포너용 데이터 프리셋입니다.
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
    /// <summary>데이터 유효성을 검사하고 에러 메시지를 수집합니다.</summary>
    protected override void CollectErrorMessage(List<string> errorList)
    {
        // 상위 클래스의 에러 수집 로직 수행
        base.CollectErrorMessage(errorList);

        // 테이블 데이터 검증 및 에러 추가
        if (_fishPrefabs == null || _fishPrefabs.Length == 0)
        {
            errorList.Add($"{errorList.Count + 1}. 프리펩 테이블이 비어있습니다.");
        }
    }

    /// <summary>생성 시 기본 식별자와 데이터 타입을 초기화합니다.</summary>
    protected override void Reset()
    {
        // 식별자 및 타입 강제 지정
        _id = ID;
        _type = EDataType.Spawner;
    }
    #endregion
}
