using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리셋 SO 테이블을 참조하여 물고기 군집을 형성하고 제어하는 클래스입니다.
/// 군집의 이동 가능 범위를 월드 좌표계 기준 경계 박스(AABB)로 제한합니다.
/// </summary>
public sealed class CFlockingGroup : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("물고기 테이블 프리셋")]
    [SerializeField] private CFishPresetSO _preset;

    [Header("군집 설정")]
    [SerializeField, Min(1)] private int _numFish = 30;
    [SerializeField, Min(0.1f)] private float _averageSpeed = 2f;

    [Header("이동 범위 (월드 좌표 절대값)")]
    [Tooltip("물고기가 이 범위를 벗어나려 하면 안쪽으로 되돌리는 힘이 작용합니다.")]
    [SerializeField] private Vector3 _boundsMin = new Vector3(-200f, 48f, -200f);
    [SerializeField] private Vector3 _boundsMax = new Vector3(200f, 100f, 200f);

    [Header("구조 설정 (공란 가능)")]
    [SerializeField] private GameObject _fishSchool;
    [SerializeField] private Transform _target;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly List<CFlockingFish> _allFish = new();
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public IReadOnlyList<CFlockingFish> AllFish => _allFish;
    public Transform Target => _target;

    /// <summary>물고기 이동 범위의 최소 좌표(월드)입니다.</summary>
    public Vector3 BoundsMin => _boundsMin;
    /// <summary>물고기 이동 범위의 최대 좌표(월드)입니다.</summary>
    public Vector3 BoundsMax => _boundsMax;
    /// <summary>이동 범위의 중심 좌표입니다.</summary>
    public Vector3 BoundsCenter => (_boundsMin + _boundsMax) * 0.5f;
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

        // 스폰 반경은 경계 박스의 가장 짧은 변의 1/4로 산출해 초기에 뭉치지 않게 합니다.
        Vector3 size = _boundsMax - _boundsMin;
        float spawnRadius = Mathf.Min(size.x, size.y, size.z) * 0.25f;

        for (int i = 0; i < _numFish; ++i)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            spawnPos = ClampToBounds(spawnPos);

            // 프리셋 테이블에 든 물고기 종류들을 쏠림 없이 순차적으로 균등 스폰합니다.
            GameObject selectedPrefab = prefabs[i % prefabCount];

            GameObject fishGo = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            fishGo.transform.SetParent(schoolParent, true);
            fishGo.transform.localScale = Vector3.one * (Random.value * 0.2f + 0.9f);

            if (fishGo.TryGetComponent(out CFlockingFish fish))
            {
                fish.Flock = this;
                fish.AverageSpeed = _averageSpeed;
                _allFish.Add(fish);
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    public void ExecuteUpdateFrame()
    {
        // 목표점 이동은 CFlockingTargetMover가 담당하므로 여기서는 별도 처리가 없습니다.
    }

    /// <summary>주어진 좌표를 경계 박스 안으로 강제로 끌어당깁니다.</summary>
    private Vector3 ClampToBounds(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, _boundsMin.x, _boundsMax.x);
        pos.y = Mathf.Clamp(pos.y, _boundsMin.y, _boundsMax.y);
        pos.z = Mathf.Clamp(pos.z, _boundsMin.z, _boundsMax.z);
        return pos;
    }
    #endregion

    #region ─────────────────────────▶ 기즈모 그리기 ◀─────────────────────────
    private void OnDrawGizmos()
    {
        // 이동 범위 박스(월드 절대 좌표) — 모든 무리가 공유하는 하드 경계.
        Gizmos.color = Color.cyan;
        Vector3 boxCenter = (_boundsMin + _boundsMax) * 0.5f;
        Vector3 boxSize = _boundsMax - _boundsMin;
        Gizmos.DrawWireCube(boxCenter, boxSize);

        // 타겟이 한 스텝에 이동할 수 있는 반경 구.
        // Anchor 모드: 스폰 원점 중심 = 무리의 전체 배회 범위와 동일.
        // Free 모드: 타겟 현재 위치 중심 = 한 스텝 이동 반경(전체 범위는 위의 박스).
        if (TryGetComponent(out CFlockingTargetMover mover))
        {
            Gizmos.color = mover.MoveMode == CFlockingTargetMover.EMoveMode.Anchor
                ? Color.green   // 고정형: 전체 활동 반경
                : Color.yellow; // 자유형: 스텝 반경
            Gizmos.DrawWireSphere(mover.GizmoBasePosition, mover.MoveRange);
        }

        // 타겟 오브젝트의 실제 위치 표시(작은 점).
        if (_target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_target.position, 0.5f);
        }
    }
    #endregion
}
