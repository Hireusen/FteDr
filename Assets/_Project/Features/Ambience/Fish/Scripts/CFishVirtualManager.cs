using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

/// <summary>
/// 씬 내 모든 CFlockingGroup 데이터를 수집하여 단일 Job으로 병렬 연산 및 통합 렌더링을 수행합니다.
/// </summary>
public sealed class CFishVirtualManager : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("렌더링 및 최적화")]
    [SerializeField] private LayerMask _renderingLayer;
    [SerializeField] private float _shadowDistance = 40f;
    [SerializeField] private float _cullRadius = 2f;
    #endregion

    #region ─────────────────────────▶ 내부 구조체 & 클래스 ◀─────────────────────────
    /// <summary>NativeArray 충돌을 방지하기 위해 카메라 평면 6개를 값으로 전달하는 구조체입니다.</summary>
    public struct FrustumPlanes
    {
        public float4 p0, p1, p2, p3, p4, p5;
    }

    /// <summary>물고기의 이동, 회전, 프러스텀 컬링을 병렬로 연산하는 잡 구조체입니다.</summary>
    [BurstCompile]
    private struct UnifiedFishUpdateJob : IJobParallelFor
    {
        public float deltaTime;
        public float3 camPos;
        public FrustumPlanes frustum;
        public float shadowDistanceSq;
        public float cullRadius;

        [Unity.Collections.ReadOnly] public NativeArray<float3> targetPositions;
        [Unity.Collections.ReadOnly] public NativeArray<float> turnSpeeds;
        [Unity.Collections.ReadOnly] public NativeArray<float3> targetOffsets;
        [Unity.Collections.ReadOnly] public NativeArray<float> speeds;
        [Unity.Collections.ReadOnly] public NativeArray<float3> scales;

        public NativeArray<float3> positions;
        public NativeArray<quaternion> rotations;

        [WriteOnly] public NativeArray<Matrix4x4> matrices;
        [WriteOnly] public NativeArray<int> cullStates;

        public void Execute(int i)
        {
            float3 dest = targetPositions[i] + targetOffsets[i];
            float3 dir = dest - positions[i];

            if (math.lengthsq(dir) > 0.1f)
            {
                quaternion lookRot = quaternion.LookRotationSafe(math.normalize(dir), math.up());
                rotations[i] = math.slerp(rotations[i], lookRot, turnSpeeds[i] * deltaTime);
            }

            float3 forward = math.mul(rotations[i], new float3(0, 0, 1));
            float3 pos = positions[i] + forward * (speeds[i] * deltaTime);
            positions[i] = pos;

            matrices[i] = float4x4.TRS(pos, rotations[i], scales[i]);

            bool isVisible = true;
            if (math.dot(frustum.p0.xyz, pos) + frustum.p0.w < -cullRadius) isVisible = false;
            else if (math.dot(frustum.p1.xyz, pos) + frustum.p1.w < -cullRadius) isVisible = false;
            else if (math.dot(frustum.p2.xyz, pos) + frustum.p2.w < -cullRadius) isVisible = false;
            else if (math.dot(frustum.p3.xyz, pos) + frustum.p3.w < -cullRadius) isVisible = false;
            else if (math.dot(frustum.p4.xyz, pos) + frustum.p4.w < -cullRadius) isVisible = false;
            else if (math.dot(frustum.p5.xyz, pos) + frustum.p5.w < -cullRadius) isVisible = false;

            if (!isVisible)
            {
                cullStates[i] = 0;
            }
            else
            {
                cullStates[i] = math.distancesq(camPos, pos) < shadowDistanceSq ? 1 : 2;
            }
        }
    }

    /// <summary>1023개 이하의 단일 렌더링 배치를 구성하는 데이터 및 메모리 블록입니다.</summary>
    private class VirtualBatch : System.IDisposable
    {
        public Mesh mesh;
        public Material material;
        public int count;

        // 데이터 덮어쓰기 오염 방지를 위해 그림자용/비그림자용 분리
        public readonly MaterialPropertyBlock mpbShadow = new();
        public readonly MaterialPropertyBlock mpbNoShadow = new();

        public CFlockingGroup[] parentGroups;

        public NativeArray<float3> targetPositionsNative;
        public NativeArray<float> turnSpeedsNative;
        public NativeArray<float3> targetOffsetsNative;
        public NativeArray<float> speedsNative;
        public NativeArray<float3> scalesNative;
        public NativeArray<float3> positionsNative;
        public NativeArray<quaternion> rotationsNative;
        public NativeArray<Matrix4x4> matricesNative;
        public NativeArray<int> cullStatesNative;

        // NativeArray 고속 복사 및 분류용 관리(Managed) 배열
        public Matrix4x4[] matricesManaged;
        public int[] cullStatesManaged;
        public Vector4[] originalUVs;

        public Matrix4x4[] shadowMatrices;
        public Vector4[] shadowUVs;
        public Matrix4x4[] noShadowMatrices;
        public Vector4[] noShadowUVs;

        public void Dispose()
        {
            if (targetPositionsNative.IsCreated) targetPositionsNative.Dispose();
            if (turnSpeedsNative.IsCreated) turnSpeedsNative.Dispose();
            if (targetOffsetsNative.IsCreated) targetOffsetsNative.Dispose();
            if (speedsNative.IsCreated) speedsNative.Dispose();
            if (scalesNative.IsCreated) scalesNative.Dispose();
            if (positionsNative.IsCreated) positionsNative.Dispose();
            if (rotationsNative.IsCreated) rotationsNative.Dispose();
            if (matricesNative.IsCreated) matricesNative.Dispose();
            if (cullStatesNative.IsCreated) cullStatesNative.Dispose();
        }
    }

    /// <summary>초기화 시 스포너에서 추출한 물고기 생성 데이터를 임시 보관합니다.</summary>
    private struct FishSpawnData
    {
        public CFlockingGroup group;
        public Vector3 offset;
        public float speed;
        public Vector3 scale;
        public Vector4 uv;
    }
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private int _layerIndex;
    private Camera _mainCam;
    private readonly List<VirtualBatch> _batches = new();

    private NativeArray<JobHandle> _jobHandles;
    private Plane[] _cameraPlanes = new Plane[6];
    private static readonly int UV_OFFSET_ID = Shader.PropertyToID("_UVOffset");
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    /// <summary>카메라 참조 캐싱 및 렌더링 레이어 인덱스를 추출합니다.</summary>
    private void Awake()
    {
        _mainCam = Camera.main;
        int mask = _renderingLayer.value;
        if (mask != 0 && (mask & (mask - 1)) == 0) _layerIndex = Mathf.RoundToInt(Mathf.Log(mask, 2));
    }

    /// <summary>씬 내 스포너 데이터를 수집하고 렌더링을 위한 배치를 구성합니다.</summary>
    private void Start()
    {
        CFlockingGroup[] allGroups = FindObjectsOfType<CFlockingGroup>();
        Dictionary<Mesh, List<FishSpawnData>> fishByMesh = new();

        foreach (var group in allGroups)
        {
            if (group.preset == null || group.preset.FishPrefabs.Length == 0) continue;

            GameObject[] prefabs = group.preset.FishPrefabs;
            int prefabCount = prefabs.Length;

            for (int i = 0; i < prefabCount; i++)
            {
                int countForPrefab = group.numFish / prefabCount + (i < group.numFish % prefabCount ? 1 : 0);
                if (countForPrefab == 0) continue;

                Mesh mesh = prefabs[i].GetComponentInChildren<MeshFilter>().sharedMesh;
                if (!fishByMesh.ContainsKey(mesh)) fishByMesh[mesh] = new List<FishSpawnData>();

                for (int j = 0; j < countForPrefab; j++)
                {
                    fishByMesh[mesh].Add(new FishSpawnData
                    {
                        group = group,
                        offset = Random.insideUnitSphere * group.spreadRadius,
                        speed = Random.Range(0.8f, 1.2f) * group.averageSpeed,
                        scale = Vector3.one * Random.Range(0.9f, 1.1f),
                        uv = new Vector4(Random.Range(0, 8) * 0.125f, Random.Range(0, 8) * 0.125f, 0, 0)
                    });
                }
            }
        }

        foreach (var kvp in fishByMesh)
        {
            Mesh mesh = kvp.Key;
            List<FishSpawnData> list = kvp.Value;
            Material mat = list[0].group.preset.FishPrefabs[0].GetComponentInChildren<MeshRenderer>().sharedMaterial;

            for (int startIndex = 0; startIndex < list.Count; startIndex += 1023)
            {
                int count = Mathf.Min(1023, list.Count - startIndex);
                VirtualBatch batch = new VirtualBatch
                {
                    mesh = mesh,
                    material = mat,
                    count = count,
                    parentGroups = new CFlockingGroup[count],
                    targetPositionsNative = new NativeArray<float3>(count, Allocator.Persistent),
                    turnSpeedsNative = new NativeArray<float>(count, Allocator.Persistent),
                    targetOffsetsNative = new NativeArray<float3>(count, Allocator.Persistent),
                    speedsNative = new NativeArray<float>(count, Allocator.Persistent),
                    scalesNative = new NativeArray<float3>(count, Allocator.Persistent),
                    positionsNative = new NativeArray<float3>(count, Allocator.Persistent),
                    rotationsNative = new NativeArray<quaternion>(count, Allocator.Persistent),
                    matricesNative = new NativeArray<Matrix4x4>(count, Allocator.Persistent),
                    cullStatesNative = new NativeArray<int>(count, Allocator.Persistent),

                    matricesManaged = new Matrix4x4[count],
                    cullStatesManaged = new int[count],
                    originalUVs = new Vector4[count],
                    shadowMatrices = new Matrix4x4[count],
                    shadowUVs = new Vector4[count],
                    noShadowMatrices = new Matrix4x4[count],
                    noShadowUVs = new Vector4[count]
                };

                for (int i = 0; i < count; i++)
                {
                    FishSpawnData data = list[startIndex + i];
                    batch.parentGroups[i] = data.group;
                    batch.turnSpeedsNative[i] = data.group.turnSpeed;
                    batch.targetOffsetsNative[i] = data.offset;
                    batch.speedsNative[i] = data.speed;
                    batch.scalesNative[i] = data.scale;

                    Vector3 spawnPos = data.group.transform.position + Random.insideUnitSphere * 5f;
                    spawnPos.x = Mathf.Clamp(spawnPos.x, data.group.BoundsMin.x, data.group.BoundsMax.x);
                    spawnPos.y = Mathf.Clamp(spawnPos.y, data.group.BoundsMin.y, data.group.BoundsMax.y);
                    spawnPos.z = Mathf.Clamp(spawnPos.z, data.group.BoundsMin.z, data.group.BoundsMax.z);
                    batch.positionsNative[i] = spawnPos;

                    batch.rotationsNative[i] = quaternion.identity;
                    batch.originalUVs[i] = data.uv;
                    batch.targetPositionsNative[i] = data.group.Target != null ? data.group.Target.position : data.group.transform.position;
                }
                _batches.Add(batch);
            }
        }
        _jobHandles = new NativeArray<JobHandle>(_batches.Count, Allocator.Persistent);
    }

    /// <summary>할당된 모든 비관리 메모리를 안전하게 해제합니다.</summary>
    private void OnDestroy()
    {
        JobHandle.CompleteAll(_jobHandles);
        foreach (var batch in _batches) batch.Dispose();
        _batches.Clear();
        if (_jobHandles.IsCreated) _jobHandles.Dispose();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>이전 프레임의 연산 결과를 렌더링하고, 현재 프레임의 이동 로직을 비동기 스케줄링합니다.</summary>
    public void ExecuteUpdateFrame()
    {
        if (_mainCam == null) return;

        JobHandle.CompleteAll(_jobHandles);

        for (int i = 0; i < _batches.Count; i++)
        {
            VirtualBatch batch = _batches[i];

            batch.cullStatesNative.CopyTo(batch.cullStatesManaged);
            batch.matricesNative.CopyTo(batch.matricesManaged);

            int sCount = 0, nCount = 0;
            for (int j = 0; j < batch.count; j++)
            {
                int state = batch.cullStatesManaged[j];
                if (state == 1)
                {
                    batch.shadowMatrices[sCount] = batch.matricesManaged[j];
                    batch.shadowUVs[sCount] = batch.originalUVs[j];
                    sCount++;
                }
                else if (state == 2)
                {
                    batch.noShadowMatrices[nCount] = batch.matricesManaged[j];
                    batch.noShadowUVs[nCount] = batch.originalUVs[j];
                    nCount++;
                }
            }

            if (sCount > 0)
            {
                batch.mpbShadow.SetVectorArray(UV_OFFSET_ID, batch.shadowUVs);
                Graphics.DrawMeshInstanced(batch.mesh, 0, batch.material, batch.shadowMatrices, sCount, batch.mpbShadow, ShadowCastingMode.On, true, _layerIndex, null, LightProbeUsage.BlendProbes);
            }
            if (nCount > 0)
            {
                batch.mpbNoShadow.SetVectorArray(UV_OFFSET_ID, batch.noShadowUVs);
                Graphics.DrawMeshInstanced(batch.mesh, 0, batch.material, batch.noShadowMatrices, nCount, batch.mpbNoShadow, ShadowCastingMode.Off, true, _layerIndex, null, LightProbeUsage.BlendProbes);
            }
        }

        GeometryUtility.CalculateFrustumPlanes(_mainCam, _cameraPlanes);
        FrustumPlanes frustumData = new FrustumPlanes
        {
            p0 = new float4(_cameraPlanes[0].normal, _cameraPlanes[0].distance),
            p1 = new float4(_cameraPlanes[1].normal, _cameraPlanes[1].distance),
            p2 = new float4(_cameraPlanes[2].normal, _cameraPlanes[2].distance),
            p3 = new float4(_cameraPlanes[3].normal, _cameraPlanes[3].distance),
            p4 = new float4(_cameraPlanes[4].normal, _cameraPlanes[4].distance),
            p5 = new float4(_cameraPlanes[5].normal, _cameraPlanes[5].distance)
        };

        float dt = Time.deltaTime;
        float3 camPos = _mainCam.transform.position;
        float shadowDistSq = _shadowDistance * _shadowDistance;

        for (int i = 0; i < _batches.Count; i++)
        {
            VirtualBatch batch = _batches[i];

            for (int j = 0; j < batch.count; j++)
            {
                if (batch.parentGroups[j] != null && batch.parentGroups[j].Target != null)
                {
                    batch.targetPositionsNative[j] = batch.parentGroups[j].Target.position;
                }
            }

            UnifiedFishUpdateJob job = new UnifiedFishUpdateJob
            {
                deltaTime = dt,
                camPos = camPos,
                frustum = frustumData,
                shadowDistanceSq = shadowDistSq,
                cullRadius = _cullRadius,
                targetPositions = batch.targetPositionsNative,
                turnSpeeds = batch.turnSpeedsNative,
                targetOffsets = batch.targetOffsetsNative,
                speeds = batch.speedsNative,
                scales = batch.scalesNative,
                positions = batch.positionsNative,
                rotations = batch.rotationsNative,
                matrices = batch.matricesNative,
                cullStates = batch.cullStatesNative
            };

            _jobHandles[i] = job.Schedule(batch.count, 64);
        }

        JobHandle.ScheduleBatchedJobs();
    }
    #endregion
}
