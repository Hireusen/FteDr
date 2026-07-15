using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리셋 SO 테이블을 참조하여 물고기 군집을 형성하고 제어하는 클래스입니다.
/// </summary>
public sealed class CFlockingGroup : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("물고기 테이블 프리셋")]
    [SerializeField] private CFishPresetSO _preset;

    [Header("군집 설정")]
    [SerializeField, Min(1)] private int _numFish = 30;
    [SerializeField, Min(0.1f)] private float _wanderSize = 7f;
    [SerializeField, Min(0.1f)] private float _averageSpeed = 2f;

    [Header("구조 설정 (공란 가능)")]
    [SerializeField] private GameObject _fishSchool;
    [SerializeField] private Transform _target;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly List<CFlockingFish> _allFish = new();
    private static Vector3 _goalPosition = Vector3.zero;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public IReadOnlyList<CFlockingFish> AllFish => _allFish;
    public float WanderSize => _wanderSize;
    public Transform Target => _target;
    public static Vector3 GoalPosition => _goalPosition;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_preset == null)
        {
            UDebug.Print("CFlockingGroup: 물고기 프리셋 테이블(CFishPresetSO)이 할당되지 않았습니다.", LogType.Error, this);
            return;
        }

        GameObject[] prefabs = _preset.FishPrefabs;
        if (prefabs == null || prefabs.Length == 0)
        {
            UDebug.Print($"CFlockingGroup: 할당된 프리셋({_preset.name}) 테이블이 비어있습니다.", LogType.Warning, this);
            return;
        }

        Transform schoolParent = _fishSchool != null ? _fishSchool.transform : transform;
        int prefabCount = prefabs.Length;

        for (int i = 0; i < _numFish; ++i)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * _wanderSize;

            // 프리셋 테이블에 든 물고기 종류들을 쏠림 현상 없이 순차적으로 균등 스폰합니다.
            GameObject selectedPrefab = prefabs[i % prefabCount];

            GameObject fishGo = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            fishGo.transform.SetParent(schoolParent, true);
            fishGo.transform.localScale = Vector3.one * (Random.value * 0.2f + 0.9f);

            if (fishGo.TryGetComponent(out CFlockingFish fish))
            {
                fish.Flock = this;
                fish.AverageSpeed = _averageSpeed; // 개별 물고기에 평균 속도 주입
                _allFish.Add(fish);
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    public void ExecuteUpdateFrame()
    {
        HandleGoalPos();
    }

    private void HandleGoalPos()
    {
        if (Random.Range(1, 10000) < 50)
        {
            _goalPosition = new Vector3(
                Random.Range(-_wanderSize, _wanderSize),
                Random.Range(-_wanderSize, _wanderSize),
                Random.Range(-_wanderSize, _wanderSize)
            );
        }
    }
    #endregion

    #region ─────────────────────────▶ 기즈모 그리기 ◀─────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _wanderSize);
    }
    #endregion
}
