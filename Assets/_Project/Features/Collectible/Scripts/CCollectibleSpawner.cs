using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 스테이지 진입 시 수집품을 공중에 스폰하고 중력으로 낙하시킨 후 Rigidbody를 제거합니다.
/// </summary>
public sealed class CCollectibleSpawner : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("스폰 데이터")]
    [Tooltip("이 스테이지에서 스폰할 수집품 목록 SO")]
    [SerializeField] private CStageSpawnSO _spawnData;

    [Header("생성 옵션")]
    [Tooltip("씬 시작 시 자동으로 Spawn 호출")]
    [SerializeField] private bool _spawnOnStart = true;
    [Tooltip("생성된 오브젝트를 담을 부모(비우면 이 오브젝트)")]
    [SerializeField] private Transform _container;

    [Header("스폰 범위")]
    [SerializeField] private ESpawnShape _shape = ESpawnShape.Box;
    [Tooltip("범위 중심 (비우면 이 오브젝트 위치)")]
    [SerializeField] private Transform _areaCenter;
    [Tooltip("Box: 가로(X) 길이")]
    [SerializeField] private float _boxSizeX = 10f;
    [Tooltip("Box: 세로(Z) 길이")]
    [SerializeField] private float _boxSizeZ = 10f;
    [Tooltip("Circle: 반지름")]
    [SerializeField] private float _circleRadius = 5f;
    [Tooltip("Custom: 지정 스폰 지점들 (이 지점 중 랜덤 선택)")]
    [SerializeField] private Transform[] _customPoints;

    [Header("낙하")]
    [Tooltip("스폰 높이 오프셋 (범위 y 기준 위로)")]
    [SerializeField] private float _dropHeight = 8f;
    [Tooltip("높이 랜덤 편차(각 오브젝트마다 ±)")]
    [SerializeField] private float _dropHeightJitter = 1f;
    [Tooltip("완전 랜덤 회전(끄면 Y축 회전만)")]
    [SerializeField] private bool _fullRandomRotation = true;

    [Header("안정화 감지")]
    [Tooltip("이 속도(초당) 미만이면 정지로 간주")]
    [SerializeField] private float _sleepSpeed = 0.05f;
    [Tooltip("정지 상태가 이 시간(초) 지속되면 Rigidbody 제거")]
    [SerializeField] private float _sleepHoldTime = 0.4f;
    [Tooltip("이 시간(초)을 넘기면 안정화와 무관하게 강제로 Rigidbody 제거")]
    [SerializeField] private float _maxSettleTime = 6f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private bool _spawned; // 최초 1회 보장
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이미 스폰이 완료(또는 진행)되었는지 여부입니다.</summary>
    public bool HasSpawned => _spawned;

    [ContextMenu("추가 생성")]
    public void AlwaysSpawn()
    {
        _spawned = false;
        Spawn();
    }

    /// <summary>수집품을 스폰합니다. 최초 1회만 실행됩니다.</summary>
    public void Spawn()
    {
        if (_spawned)
        {
            UDebug.Print("CCollectibleSpawner: 이미 스폰되어 재실행을 무시합니다.", LogType.Warning);
            return;
        }
        if (_spawnData == null)
        {
            UDebug.Print("CCollectibleSpawner: 스폰 데이터(CStageSpawnSO)가 없습니다.", LogType.Error);
            return;
        }
        // 개수 설정이 논리적으로 만족 불가능하면 스폰하지 않는다.
        if (!_spawnData.IsSatisfiable(out string reason))
        {
            UDebug.Print($"CCollectibleSpawner: 개수 설정 충돌로 스폰을 중단합니다. ({reason})", LogType.Error);
            return;
        }

        _spawned = true;
        SpawnAll();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 엔트리별 개수를 결정한 뒤 그 수만큼 생성한다.
    private void SpawnAll()
    {
        Transform parent = _container != null ? _container : transform;
        IReadOnlyList<CStageSpawnSO.SpawnEntry> entries = _spawnData.Entries;

        int[] counts = DecideCounts(entries);

        for (int i = 0; i < entries.Count; ++i)
        {
            CCollectibleSO so = entries[i].Collectible;
            if (so == null || so.Prefab == null)
            {
                continue;
            }

            for (int n = 0; n < counts[i]; ++n)
            {
                SpawnOne(so, parent);
            }
        }
    }

    // 엔트리 min/max와 스테이지 총량 min/max를 함께 만족하는 엔트리별 개수를 결정한다.
    private int[] DecideCounts(IReadOnlyList<CStageSpawnSO.SpawnEntry> entries)
    {
        int n = entries.Count;
        int[] counts = new int[n];

        // 1) 각 엔트리를 최소치로 먼저 채운다.
        int assigned = 0;
        for (int i = 0; i < n; ++i)
        {
            counts[i] = entries[i].MinCount;
            assigned += counts[i];
        }

        // 2) 이번 스폰의 총량 목표를 결정한다.
        //    [max(minTotal, 엔트리최소합), min(maxTotal, 엔트리최대합)] 범위 → 만족 가능성 검증을 통과했으므로 유효.
        int lowerTarget = Mathf.Max(_spawnData.MinTotal, _spawnData.SumOfEntryMins);
        int upperTarget = Mathf.Min(_spawnData.MaxTotal, _spawnData.SumOfEntryMaxes);
        int target = Random.Range(lowerTarget, upperTarget + 1); // 상한 포함

        // 3) target에 도달할 때까지, max에 여유가 있는 엔트리에 1개씩 랜덤 분배한다.
        List<int> candidates = new List<int>(n);
        while (assigned < target)
        {
            candidates.Clear();
            for (int i = 0; i < n; ++i)
            {
                if (counts[i] < entries[i].MaxCount)
                {
                    candidates.Add(i);
                }
            }
            // 더 넣을 곳이 없으면 종료 (이론상 target이 유효하면 도달 전에 비지 않음)
            if (candidates.Count == 0)
            {
                break;
            }

            int pick = candidates[Random.Range(0, candidates.Count)];
            ++counts[pick];
            ++assigned;
        }

        return counts;
    }

    // 개별 수집품 하나를 생성하고 낙하 코루틴을 시작한다.
    private void SpawnOne(CCollectibleSO so, Transform parent)
    {
        Vector3 pos = GetSpawnPosition();
        pos.y += _dropHeight + Random.Range(-_dropHeightJitter, _dropHeightJitter);

        Quaternion rot = _fullRandomRotation ? URandom.Rotation() : URandom.RotationYaw();

        GameObject go = Instantiate(so.Prefab, pos, rot, parent);
        go.transform.localScale *= so.GetRandomScale(); // SO의 min~max 범위 랜덤 크기

        // 낙하용 Rigidbody 부착 (질량은 수집품 무게 반영)
        Rigidbody rb = go.GetOrAddComponent<Rigidbody>();
        rb.mass = Mathf.Max(0.01f, so.Weight);
        rb.useGravity = true;

        StartCoroutine(SettleRoutine(go, rb));
    }

    // 형태에 따라 범위 내 랜덤 위치를 구한다.
    private Vector3 GetSpawnPosition()
    {
        Vector3 center = _areaCenter != null ? _areaCenter.position : transform.position;

        switch (_shape)
        {
            case ESpawnShape.Circle:
                return URandom.PointInCircle(center, _circleRadius);

            case ESpawnShape.Custom:
                if (_customPoints != null && _customPoints.Length > 0)
                {
                    Transform p = _customPoints[Random.Range(0, _customPoints.Length)];
                    if (p != null)
                    {
                        return p.position;
                    }
                }
                UDebug.Print("CCollectibleSpawner: Custom 지점이 비어 중심으로 스폰합니다.", LogType.Warning);
                return center;

            case ESpawnShape.Box:
            default:
                return URandom.PointInBox(center, _boxSizeX, _boxSizeZ);
        }
    }

    // 오브젝트가 안정화되면(또는 최대 시간 초과 시) Rigidbody를 제거한다.
    private IEnumerator SettleRoutine(GameObject go, Rigidbody rb)
    {
        float elapsed = 0f;
        float stillTime = 0f;
        float sleepSqr = _sleepSpeed * _sleepSpeed;

        while (go != null && rb != null)
        {
            if (rb.velocity.sqrMagnitude <= sleepSqr)
            {
                stillTime += Time.deltaTime;
                if (stillTime >= _sleepHoldTime)
                {
                    break;
                }
            }
            else
            {
                stillTime = 0f;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= _maxSettleTime)
            {
                break;
            }

            yield return null;
        }

        if (rb != null)
        {
            Destroy(rb);
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_spawnOnStart)
        {
            Spawn();
        }
    }

    // 에디터에서 스폰 범위를 시각화한다.
    private void OnDrawGizmosSelected()
    {
        Vector3 center = _areaCenter != null ? _areaCenter.position : transform.position;
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);

        switch (_shape)
        {
            case ESpawnShape.Box:
                Gizmos.DrawWireCube(center, new Vector3(_boxSizeX, 0.1f, _boxSizeZ));
                break;

            case ESpawnShape.Circle:
                DrawWireCircle(center, _circleRadius);
                break;

            case ESpawnShape.Custom:
                if (_customPoints == null)
                {
                    break;
                }
                for (int i = 0; i < _customPoints.Length; ++i)
                {
                    if (_customPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(_customPoints[i].position, 0.3f);
                    }
                }
                break;
        }
    }

    private void DrawWireCircle(Vector3 center, float radius)
    {
        const int SEG = 32;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= SEG; ++i)
        {
            float a = (i / (float)SEG) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
    #endregion
}
