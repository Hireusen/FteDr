using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물고기 군집 그룹 전체의 스폰 및 타겟 이동 범위 등을 총괄하는 클래스입니다.
/// </summary>
public sealed class CFlockingGroup : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("스폰 설정")]
    [SerializeField] private GameObject[] _fishPrefabs;
    [SerializeField] private GameObject _fishSchool;
    [SerializeField] private int _numFish = 30;
    [SerializeField] private float _wanderSize = 7f;

    [Header("타겟")]
    [SerializeField] private Transform _target;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly List<CFlockingFish> _allFish = new();
    private static Vector3 _goalPosition = Vector3.zero;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    /// <summary>현재 군집 그룹에 생성된 모든 물고기 인스턴스의 캐싱 목록입니다. (읽기 전용)</summary>
    public IReadOnlyList<CFlockingFish> AllFish => _allFish;

    /// <summary>물고기들이 활동할 구형 범위의 반지름 크기입니다.</summary>
    public float WanderSize => _wanderSize;

    /// <summary>물고기들이 목표로 하는 중앙 타겟 트랜스폼입니다.</summary>
    public Transform Target => _target;

    /// <summary>공유용 글로벌 목표 구역 좌표입니다.</summary>
    public static Vector3 GoalPosition => _goalPosition;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void Start()
    {
        if (_fishPrefabs == null || _fishPrefabs.Length == 0)
        {
            UDebug.Print("스폰할 물고기 프리팹이 할당되지 않았습니다.", LogType.Error, this);
            return;
        }

        Transform schoolParent = _fishSchool != null ? _fishSchool.transform : transform;

        for (int i = 0; i < _numFish; ++i)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * _wanderSize;
            GameObject selectedPrefab = _fishPrefabs[Random.Range(0, _fishPrefabs.Length)];

            GameObject fishGo = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            fishGo.transform.SetParent(schoolParent, true);
            fishGo.transform.localScale = Vector3.one * (Random.value * 0.2f + 0.9f);

            if (fishGo.TryGetComponent(out CFlockingFish fish))
            {
                fish.Flock = this;
                _allFish.Add(fish);
            }
            else
            {
                UDebug.Print($"스폰된 오브젝트에 CFlockingFish 스크립트가 누락되었습니다: {fishGo.name}", LogType.Warning);
            }
        }
    }

    /// <summary>
    /// 프레임 매니저에 의해 매 프레임 호출되는 로직입니다.
    /// </summary>
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
