using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameObject를 생성하지 않고, 데이터 배열(순수 C#)로 가상의 물고기들을 연산하여 GPU에 직접 렌더링합니다.
/// </summary>
public sealed class CFlockingGroup : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("물고기 테이블 프리셋")]
    [SerializeField] private CFishPresetSO _preset;

    [Header("군집 설정")]
    [SerializeField, Min(1)] private int _numFish = 100;
    [SerializeField, Min(0.1f)] private float _averageSpeed = 2f;
    [SerializeField, Range(0.5f, 12f)] private float _turnSpeed = 3f;
    [SerializeField, Tooltip("타겟 주변으로 흩어질 반경입니다.")] private float _spreadRadius = 3f;

    [Header("이동 범위 및 타겟")]
    [SerializeField] private Vector3 _boundsMin = new Vector3(-200f, 48f, -200f);
    [SerializeField] private Vector3 _boundsMax = new Vector3(200f, 100f, 200f);
    [SerializeField] private Transform _target;
    #endregion

    #region ─────────────────────────▶ 내부 클래스 ◀─────────────────────────
    /// <summary>동일한 메시를 공유하는 가상 물고기들의 데이터를 담는 배치 단위입니다.</summary>
    private class VirtualBatch
    {
        public Mesh mesh;
        public Material material;
        public int count;
        public readonly MaterialPropertyBlock mpb = new();

        // 가상 물고기의 이동 로직을 위한 순수 데이터 배열
        public Vector3[] positions;
        public Quaternion[] rotations;
        public Vector3[] scales;
        public Vector3[] targetOffsets;
        public float[] speeds;

        // GPU 렌더링을 위한 데이터 배열
        public Matrix4x4[] matrices;
        public Vector4[] uvOffsets;
    }
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly List<VirtualBatch> _batches = new();
    private static readonly int UV_OFFSET_ID = Shader.PropertyToID("_UVOffset");
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public Transform Target => _target;
    public Vector3 BoundsMin => _boundsMin;
    public Vector3 BoundsMax => _boundsMax;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    /// <summary>프리팹을 스폰하지 않고 메시 정보만 추출하여 가상 데이터 배열을 초기화합니다.</summary>
    private void Start()
    {
        if (_preset == null || _preset.FishPrefabs == null || _preset.FishPrefabs.Length == 0) return;

        GameObject[] prefabs = _preset.FishPrefabs;
        int prefabCount = prefabs.Length;
        Vector3 size = _boundsMax - _boundsMin;
        float spawnRadius = Mathf.Min(size.x, size.y, size.z) * 0.25f;

        // 등록된 프리팹 종류별로 균등하게 가상 물고기 마릿수를 분배하여 배치 생성
        for (int i = 0; i < prefabCount; i++)
        {
            // 이 프리팹이 담당할 물고기 마릿수 계산
            int countForThisPrefab = _numFish / prefabCount + (i < _numFish % prefabCount ? 1 : 0);
            if (countForThisPrefab == 0) continue;

            // 프리팹에서 메시와 머티리얼 레퍼런스만 도출 (Instantiate 안 함)
            Mesh mesh = prefabs[i].GetComponentInChildren<MeshFilter>().sharedMesh;
            Material mat = prefabs[i].GetComponentInChildren<MeshRenderer>().sharedMaterial;

            VirtualBatch batch = new VirtualBatch
            {
                mesh = mesh,
                material = mat,
                count = countForThisPrefab,
                positions = new Vector3[countForThisPrefab],
                rotations = new Quaternion[countForThisPrefab],
                scales = new Vector3[countForThisPrefab],
                targetOffsets = new Vector3[countForThisPrefab],
                speeds = new float[countForThisPrefab],
                matrices = new Matrix4x4[countForThisPrefab],
                uvOffsets = new Vector4[countForThisPrefab]
            };

            // 개별 가상 물고기의 초기 데이터 난수 설정
            for (int j = 0; j < countForThisPrefab; j++)
            {
                Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
                batch.positions[j] = ClampToBounds(spawnPos);
                batch.rotations[j] = Quaternion.identity;
                batch.scales[j] = Vector3.one * Random.Range(0.9f, 1.1f);
                batch.targetOffsets[j] = Random.insideUnitSphere * _spreadRadius;
                batch.speeds[j] = Random.Range(0.8f, 1.2f) * _averageSpeed;

                float offsetX = Random.Range(0, 8) * 0.125f;
                float offsetY = Random.Range(0, 8) * 0.125f;
                batch.uvOffsets[j] = new Vector4(offsetX, offsetY, 0, 0);
            }

            batch.mpb.SetVectorArray(UV_OFFSET_ID, batch.uvOffsets);
            _batches.Add(batch);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>매 프레임 C# 배열로만 이동 연산을 마친 뒤 GPU에 직접 일괄 렌더링을 요청합니다.</summary>
    public void ExecuteUpdateFrame()
    {
        if (_target == null) return;

        Vector3 targetPos = _target.position;
        float dt = Time.deltaTime;

        // 배치별 순회: GameObejct Transform 접근 없이 순수 CPU 수학 연산
        foreach (VirtualBatch batch in _batches)
        {
            for (int i = 0; i < batch.count; i++)
            {
                // 타겟 오프셋을 향한 방향 벡터 및 부드러운 회전 산출
                Vector3 dest = targetPos + batch.targetOffsets[i];
                Vector3 dir = dest - batch.positions[i];

                if (dir.sqrMagnitude > 0.1f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
                    batch.rotations[i] = Quaternion.Slerp(batch.rotations[i], lookRot, _turnSpeed * dt);
                }

                // 현재 바라보는 방향(로컬 Z축)으로 전진
                batch.positions[i] += batch.rotations[i] * Vector3.forward * (batch.speeds[i] * dt);

                // GPU 인스턴싱에 전달할 TRS 행렬 구성
                batch.matrices[i] = Matrix4x4.TRS(batch.positions[i], batch.rotations[i], batch.scales[i]);
            }

            // 완성된 행렬 배열을 GPU로 송신
            Graphics.DrawMeshInstanced(
                batch.mesh,
                0,
                batch.material,
                batch.matrices,
                batch.count,
                batch.mpb,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false
            );
        }
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
}
