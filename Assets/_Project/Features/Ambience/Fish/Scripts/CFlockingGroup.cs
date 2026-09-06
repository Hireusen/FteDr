using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Random = UnityEngine.Random;

/// <summary>
/// 잡 시스템으로 가상 물고기를 연산하고, 1프레임 지연 렌더링 기법으로 메인 스레드 대기 시간을 제거합니다.
/// </summary>
public sealed class CFlockingGroup : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("물고기 테이블 프리셋")]
    [SerializeField] private CFishPresetSO _preset;

    [Header("렌더링 레이어")]
    [SerializeField] private LayerMask _renderingLayer;

    [Header("군집 설정")]
    [SerializeField, Min(1)] private int _numFish = 3000;
    [SerializeField, Min(0.1f)] private float _averageSpeed = 2f;
    [SerializeField, Range(0.5f, 12f)] private float _turnSpeed = 3f;
    [SerializeField, Tooltip("타겟 주변으로 흩어질 반경입니다.")] private float _spreadRadius = 3f;

    [Header("이동 범위 및 타겟")]
    [SerializeField] private Vector3 _boundsMin = new Vector3(-200f, 48f, -200f);
    [SerializeField] private Vector3 _boundsMax = new Vector3(200f, 100f, 200f);
    [SerializeField] private Transform _target;
    #endregion

    #region ─────────────────────────▶ 내부 구조체 & 클래스 ◀─────────────────────────
    /// <summary>병렬 연산으로 위치와 회전을 갱신하고 TRS 행렬을 산출합니다.</summary>
    [BurstCompile]
    private struct FishUpdateJob : IJobParallelFor
    {
        [ReadOnly] public float3 targetPosition;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float turnSpeed;

        public NativeArray<float3> positions;
        public NativeArray<quaternion> rotations;
        [ReadOnly] public NativeArray<float3> scales;
        [ReadOnly] public NativeArray<float3> targetOffsets;
        [ReadOnly] public NativeArray<float> speeds;
        [WriteOnly] public NativeArray<Matrix4x4> matrices;

        public void Execute(int i)
        {
            float3 dest = targetPosition + targetOffsets[i];
            float3 dir = dest - positions[i];

            if (math.lengthsq(dir) > 0.1f)
            {
                quaternion lookRot = quaternion.LookRotationSafe(math.normalize(dir), math.up());
                rotations[i] = math.slerp(rotations[i], lookRot, turnSpeed * deltaTime);
            }

            float3 forward = math.mul(rotations[i], new float3(0, 0, 1));
            positions[i] += forward * (speeds[i] * deltaTime);

            matrices[i] = float4x4.TRS(positions[i], rotations[i], scales[i]);
        }
    }

    /// <summary>Native 데이터와 DrawMeshInstanced용 관리 배열을 함께 보유하는 배치 단위입니다.</summary>
    private class VirtualBatch : System.IDisposable
    {
        public Mesh mesh;
        public Material material;
        public int count;
        public readonly MaterialPropertyBlock mpb = new();

        public NativeArray<float3> positions;
        public NativeArray<quaternion> rotations;
        public NativeArray<float3> scales;
        public NativeArray<float3> targetOffsets;
        public NativeArray<float> speeds;

        public NativeArray<Matrix4x4> matricesNative;
        public Matrix4x4[] matricesManaged; // GPU 송신용 고속 복사 대상

        public void Dispose()
        {
            if (positions.IsCreated) positions.Dispose();
            if (rotations.IsCreated) rotations.Dispose();
            if (scales.IsCreated) scales.Dispose();
            if (targetOffsets.IsCreated) targetOffsets.Dispose();
            if (speeds.IsCreated) speeds.Dispose();
            if (matricesNative.IsCreated) matricesNative.Dispose();
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private int _layerIndex;
    private readonly List<VirtualBatch> _batches = new();
    private NativeArray<JobHandle> _jobHandles;
    private static readonly int UV_OFFSET_ID = Shader.PropertyToID("_UVOffset");
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public Transform Target => _target;
    public Vector3 BoundsMin => _boundsMin;
    public Vector3 BoundsMax => _boundsMax;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        int mask = _renderingLayer.value;
        if (mask != 0 && (mask & (mask - 1)) == 0)
        {
            _layerIndex = Mathf.RoundToInt(Mathf.Log(mask, 2));
        }
    }

    private void Start()
    {
        if (_preset == null || _preset.FishPrefabs == null || _preset.FishPrefabs.Length == 0) return;

        GameObject[] prefabs = _preset.FishPrefabs;
        int prefabCount = prefabs.Length;
        Vector3 size = _boundsMax - _boundsMin;
        float spawnRadius = Mathf.Min(size.x, size.y, size.z) * 0.25f;

        for (int i = 0; i < prefabCount; i++)
        {
            int totalCountForPrefab = _numFish / prefabCount + (i < _numFish % prefabCount ? 1 : 0);
            if (totalCountForPrefab == 0) continue;

            Mesh mesh = prefabs[i].GetComponentInChildren<MeshFilter>().sharedMesh;
            Material mat = prefabs[i].GetComponentInChildren<MeshRenderer>().sharedMaterial;

            for (int startIndex = 0; startIndex < totalCountForPrefab; startIndex += 1023)
            {
                int count = Mathf.Min(1023, totalCountForPrefab - startIndex);

                VirtualBatch batch = new VirtualBatch
                {
                    mesh = mesh,
                    material = mat,
                    count = count,
                    positions = new NativeArray<float3>(count, Allocator.Persistent),
                    rotations = new NativeArray<quaternion>(count, Allocator.Persistent),
                    scales = new NativeArray<float3>(count, Allocator.Persistent),
                    targetOffsets = new NativeArray<float3>(count, Allocator.Persistent),
                    speeds = new NativeArray<float>(count, Allocator.Persistent),
                    matricesNative = new NativeArray<Matrix4x4>(count, Allocator.Persistent),
                    matricesManaged = new Matrix4x4[count]
                };

                Vector4[] tempUvs = new Vector4[count];

                for (int j = 0; j < count; j++)
                {
                    Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
                    batch.positions[j] = ClampToBounds(spawnPos);
                    batch.rotations[j] = quaternion.identity;
                    batch.scales[j] = new float3(1, 1, 1) * Random.Range(0.9f, 1.1f);
                    batch.targetOffsets[j] = Random.insideUnitSphere * _spreadRadius;
                    batch.speeds[j] = Random.Range(0.8f, 1.2f) * _averageSpeed;

                    tempUvs[j] = new Vector4(Random.Range(0, 8) * 0.125f, Random.Range(0, 8) * 0.125f, 0, 0);
                }

                batch.mpb.SetVectorArray(UV_OFFSET_ID, tempUvs);
                _batches.Add(batch);
            }
        }

        _jobHandles = new NativeArray<JobHandle>(_batches.Count, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        JobHandle.CompleteAll(_jobHandles);

        foreach (var batch in _batches) batch.Dispose();
        _batches.Clear();

        if (_jobHandles.IsCreated) _jobHandles.Dispose();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>이전 프레임의 연산 결과를 렌더링하고 새로운 연산을 비동기 스케줄링합니다.</summary>
    public void ExecuteUpdateFrame()
    {
        if (_target == null) return;

        JobHandle.CompleteAll(_jobHandles);

        for (int i = 0; i < _batches.Count; i++)
        {
            VirtualBatch batch = _batches[i];

            // Native 블록 데이터를 Managed 배열로 고속 복사
            batch.matricesNative.CopyTo(batch.matricesManaged);

            Graphics.DrawMeshInstanced(
                batch.mesh,
                0,
                batch.material,
                batch.matricesManaged,
                batch.count,
                batch.mpb,
                UnityEngine.Rendering.ShadowCastingMode.On,
                true,
                _layerIndex
            );
        }

        float dt = Time.deltaTime;
        float3 targetPos = _target.position;

        for (int i = 0; i < _batches.Count; i++)
        {
            VirtualBatch batch = _batches[i];
            FishUpdateJob job = new FishUpdateJob
            {
                targetPosition = targetPos,
                deltaTime = dt,
                turnSpeed = _turnSpeed,
                positions = batch.positions,
                rotations = batch.rotations,
                scales = batch.scales,
                targetOffsets = batch.targetOffsets,
                speeds = batch.speeds,
                matrices = batch.matricesNative
            };

            _jobHandles[i] = job.Schedule(batch.count, 64);
        }

        JobHandle.ScheduleBatchedJobs();
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, _boundsMin.x, _boundsMax.x);
        pos.y = Mathf.Clamp(pos.y, _boundsMin.y, _boundsMax.y);
        pos.z = Mathf.Clamp(pos.z, _boundsMin.z, _boundsMax.z);
        return pos;
    }
    #endregion
}
