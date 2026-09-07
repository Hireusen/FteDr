using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬의 모든 물고기를 수집해 렌더러를 비활성화하고 일괄 렌더링(Instancing)을 수행합니다.
/// </summary>
public sealed class CFishInstancingManager : AFrameable, ILateUpdateFrameable
{
    #region ─────────────────────────▶ 내부 클래스 ◀─────────────────────────
    /// <summary>동일한 메시를 공유하는 물고기들을 묶어 관리하는 배치(Batch) 단위입니다.</summary>
    private class InstancedBatch
    {
        // 렌더링 기본 정보
        public Mesh mesh;
        public Material material;
        public int count;

        // 렌더링 연산용 캐싱 데이터
        public readonly List<Transform> transforms = new();
        public readonly MaterialPropertyBlock mpb = new();
        public Matrix4x4[] matrices;
        public Vector4[] uvOffsets;
    }
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly List<InstancedBatch> _batches = new();                      // 생성된 전체 렌더링 배치 리스트
    private static readonly int UV_OFFSET_ID = Shader.PropertyToID("_UVOffset"); // 셰이더 프로퍼티 ID 캐싱
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public ELateUpdatePriority LateUpdatePriority => ELateUpdatePriority.Lv5;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    /// <summary>시작 시 물고기 렌더러를 수집 및 비활성화하고 배치를 구성합니다.</summary>
    private System.Collections.IEnumerator Start()
    {
        yield return null;

        // 씬 내 물고기 객체를 수집하여 메시(Mesh)를 기준으로 그룹화
        CFlockingFish[] allFish = FindObjectsOfType<CFlockingFish>();
        Dictionary<Mesh, List<CFlockingFish>> fishByMesh = new();

        foreach (CFlockingFish fish in allFish)
        {
            MeshFilter mf = fish.GetComponentInChildren<MeshFilter>();
            MeshRenderer mr = fish.GetComponentInChildren<MeshRenderer>();

            // 렌더링 컴포넌트가 온전한 개체만 필터링하여 딕셔너리에 추가
            if (mf != null && mf.sharedMesh != null && mr != null)
            {
                Mesh mesh = mf.sharedMesh;

                if (!fishByMesh.ContainsKey(mesh))
                {
                    fishByMesh[mesh] = new List<CFlockingFish>();
                }
                fishByMesh[mesh].Add(fish);

                // 유니티 기본 렌더링 사이클에서 제외 (오버헤드 및 간섭 제거)
                mr.enabled = false;
            }
        }

        // 수집된 물고기들을 DrawMeshInstanced 최대치(1023개) 단위로 분할하여 배치 생성
        foreach (var kvp in fishByMesh)
        {
            Mesh mesh = kvp.Key;
            List<CFlockingFish> fishList = kvp.Value;
            Material sharedMat = fishList[0].GetComponentInChildren<MeshRenderer>().sharedMaterial;

            // 1023개씩 청크(Chunk)를 나누어 데이터 배열 할당
            for (int startIndex = 0; startIndex < fishList.Count; startIndex += 1023)
            {
                int count = Mathf.Min(1023, fishList.Count - startIndex);

                InstancedBatch batch = new InstancedBatch
                {
                    mesh = mesh,
                    material = sharedMat,
                    count = count,
                    matrices = new Matrix4x4[count],
                    uvOffsets = new Vector4[count]
                };

                // 개별 물고기의 트랜스폼 참조 및 무작위 아틀라스 색상(UV) 지정
                for (int i = 0; i < count; i++)
                {
                    CFlockingFish targetFish = fishList[startIndex + i];
                    batch.transforms.Add(targetFish.transform);

                    float offsetX = Random.Range(0, 8) * 0.125f;
                    float offsetY = Random.Range(0, 8) * 0.125f;
                    batch.uvOffsets[i] = new Vector4(offsetX, offsetY, 0, 0);
                }

                // 완성된 UV 배열을 인스턴싱 버퍼에 일괄 등록 후 리스트에 추가
                batch.mpb.SetVectorArray(UV_OFFSET_ID, batch.uvOffsets);
                _batches.Add(batch);
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>매 프레임 이동이 끝난 후 변환 행렬을 갱신하고 GPU에 렌더링을 요청합니다.</summary>
    public void ExecuteLateUpdateFrame()
    {
        // 캐싱된 배열을 재사용하여 GC(가비지 컬렉션) 없이 각 배치를 순회 렌더링
        foreach (InstancedBatch batch in _batches)
        {
            // 이번 프레임의 물고기 위치/회전/크기 데이터를 행렬로 갱신
            for (int i = 0; i < batch.count; i++)
            {
                batch.matrices[i] = batch.transforms[i].localToWorldMatrix;
            }

            // 준비된 데이터로 렌더러를 거치지 않고 GPU에 직접 그리기 명령 하달
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
    #endregion
}
