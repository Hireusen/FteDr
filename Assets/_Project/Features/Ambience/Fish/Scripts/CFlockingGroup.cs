using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 개체들을 스폰하여 물고기 군집을 형성하고 이동 가능한 월드 경계를 관리합니다.
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
    private readonly List<CFlockingFish> _allFish = new(); // 스폰된 전체 물고기 개체 리스트
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
    /// <summary>프리셋을 검증하고 지정된 범위 내에 물고기들을 스폰합니다.</summary>
    private void Start()
    {
        // 프리셋 및 프리팹 유효성 검사
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

        // 초기 스폰 반경 및 부모 트랜스폼 설정
        Transform schoolParent = _fishSchool != null ? _fishSchool.transform : transform;
        int prefabCount = prefabs.Length;
        Vector3 size = _boundsMax - _boundsMin;
        float spawnRadius = Mathf.Min(size.x, size.y, size.z) * 0.25f;

        // 지정된 마릿수만큼 물고기 개체 생성 및 초기화
        for (int i = 0; i < _numFish; ++i)
        {
            // 경계 내 안전한 랜덤 스폰 위치 산출
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            spawnPos = ClampToBounds(spawnPos);

            // 프리팹 순차 선택 및 씬 생성
            GameObject selectedPrefab = prefabs[i % prefabCount];
            GameObject fishGo = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

            // 크기 및 부모 지정
            fishGo.transform.SetParent(schoolParent, true);
            fishGo.transform.localScale = Vector3.one * (Random.value * 0.2f + 0.9f);

            // 개별 물고기 초기화 및 리스트 등록
            if (fishGo.TryGetComponent(out CFlockingFish fish))
            {
                fish.Flock = this;
                fish.AverageSpeed = _averageSpeed;
                fish.FishIndex = i; // 프레임 분산용 고유 인덱스 할당
                _allFish.Add(fish);
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>매 프레임 호출되는 로직입니다. (현재 타겟 이동은 분리되어 처리됨)</summary>
    public void ExecuteUpdateFrame()
    {
        // 목표점 이동은 CFlockingTargetMover가 담당하므로 여기서는 별도 처리가 없습니다.
    }

    /// <summary>주어진 좌표를 경계 박스 내부로 강제 클램프합니다.</summary>
    private Vector3 ClampToBounds(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, _boundsMin.x, _boundsMax.x);
        pos.y = Mathf.Clamp(pos.y, _boundsMin.y, _boundsMax.y);
        pos.z = Mathf.Clamp(pos.z, _boundsMin.z, _boundsMax.z);
        return pos;
    }
    #endregion

    #region ─────────────────────────▶ 기즈모 그리기 ◀─────────────────────────
    /// <summary>에디터 상에서 군집의 한계 경계선과 타겟 반경을 가시화합니다.</summary>
    private void OnDrawGizmos()
    {
        // 이동 가능한 최대 범위 박스 그리기
        Gizmos.color = Color.cyan;
        Vector3 boxCenter = (_boundsMin + _boundsMax) * 0.5f;
        Vector3 boxSize = _boundsMax - _boundsMin;
        Gizmos.DrawWireCube(boxCenter, boxSize);

        // 이동 모드에 따른 타겟 이동 구역 그리기
        if (TryGetComponent(out CFlockingTargetMover mover))
        {
            Gizmos.color = mover.MoveMode == CFlockingTargetMover.EMoveMode.Anchor
                ? Color.green
                : Color.yellow;
            Gizmos.DrawWireSphere(mover.GizmoBasePosition, mover.MoveRange);
        }

        // 타겟의 현재 위치 표시
        if (_target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_target.position, 0.5f);
        }
    }
    #endregion
}
