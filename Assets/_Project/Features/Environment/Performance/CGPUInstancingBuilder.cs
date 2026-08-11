#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// 씬이 로드/빌드될 때 오브젝트를 삭제하고 GPU 인스턴싱으로 대체합니다.
/// </summary>
public class CGPUInstancingBuilder : IProcessSceneWithReport
{
    #region ─────────────────────────▷ 내부 변수 ◁─────────────────────────
    private static Transform _root;
    private const string MANAGER_NAME = "GPU Instancing Manager";
    private const float GRID_SIZE = 50f;
    private const float BOUNDS_EXPAND = 5f; // 팝인 현상 방지
    private const int MIN_INSTANCE_COUNT = 50;
    #endregion

    #region ─────────────────────────▷ 공개 멤버 ◁─────────────────────────
    // 인터페이스가 요구하는 콜백 순서
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // 빌드나 플레이 모드일 때만 동작
        if (!Application.isPlaying && report == null) return;

        // 씬 내의 모든 타겟 수집
        var targets = UObject.FindComponents<CGPUInstancingTarget>();
        if (targets.Length == 0) return;

        // 동일한 메시, 머티리얼끼리 그룹화
        var groupedTargets = new Dictionary<(Mesh, Material, int, Vector3Int), InstancingGroup>();
        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null) continue;

            var filter = target.GetComponent<MeshFilter>();
            var renderer = target.GetComponent<MeshRenderer>();
            // 필터나 렌더러 누락 검사
            if (filter == null || renderer == null) continue;

            Vector3 pos = target.transform.position;
            // 3D 그리드 인덱스
            Vector3Int gridPos = new Vector3Int(
                Mathf.FloorToInt(pos.x / GRID_SIZE),
                Mathf.FloorToInt(pos.y / GRID_SIZE),
                Mathf.FloorToInt(pos.z / GRID_SIZE)
            );

            int layer = target.gameObject.layer;
            // 튜플 키 만들어서 넣기
            var key = (filter.sharedMesh, renderer.sharedMaterial, layer, gridPos);
            if (!groupedTargets.ContainsKey(key))
            {
                groupedTargets[key] = new InstancingGroup();
            }
            groupedTargets[key].matrices.Add(target.transform.localToWorldMatrix);
            groupedTargets[key].positions.Add(target.transform.position);
            groupedTargets[key].originalTargets.Add(target);
        }

        {

        }

        // 그룹별로 매니저 오브젝트 및 렌더러 생성
        int managerIndex = 0;
        foreach (var kvp in groupedTargets)
        {
            var group = kvp.Value;
            // 개수가 적을 시 인스턴싱 생략
            if (group.matrices.Count < MIN_INSTANCE_COUNT)
            {
                foreach (var target in group.originalTargets)
                {
                    if (target != null) Object.DestroyImmediate(target); // 컴포넌트만 제거
                }
                continue;
            }

            // GPU 인스턴싱할 오브젝트 청소
            foreach (var target in group.originalTargets)
            {
                if (target == null) continue;

                // 콜라이더 존재 여부에 따라 삭제 범위 결정
                if (target.GetComponent<Collider>() != null)
                {
                    // 콜라이더 존재 시 렌더러, 필터, 타겟만 삭제
                    var renderer = target.GetComponent<Renderer>();
                    var filter = target.GetComponent<MeshFilter>();

                    if (renderer != null) Object.DestroyImmediate(renderer);
                    if (filter != null) Object.DestroyImmediate(filter);
                    Object.DestroyImmediate(target);
                }
                else
                {
                    // 콜라이더 없을 시 전부 삭제
                    Object.DestroyImmediate(target.gameObject);
                }
            }

            // 변수 준비
            Mesh targetMesh = kvp.Key.Item1;
            Material targetMat = kvp.Key.Item2;
            int targetLayer = kvp.Key.Item3;

            // 매니저 오브젝트 생성
            if (_root == null)
            {
                _root = UObject.Create(MANAGER_NAME).transform;
            }
            GameObject managerObj = UObject.Create($"{MANAGER_NAME}_{managerIndex++}", _root);

            // 렌더러 설정
            var instancingRenderer = managerObj.AddComponent<GPUInstancingRenderer>();
            instancingRenderer.SetMeshAndMaterial(targetMesh, targetMat, targetLayer);

            // 행렬 분할 저장
            for (int i = 0; i < group.matrices.Count; i += 1023)
            {
                int length = Mathf.Min(1023, group.matrices.Count - i);
                // 배치 생성
                Matrix4x4[] batch = new Matrix4x4[length];
                Vector3[] posBatch = new Vector3[length];
                group.matrices.CopyTo(i, batch, 0, length);
                group.positions.CopyTo(i, posBatch, 0, length);

                // 바운딩 박스 생성
                Bounds checkBounds = new Bounds(posBatch[0], Vector3.zero);
                for (int j = 0; j < length; ++j)
                {
                    checkBounds.Encapsulate(posBatch[j]);
                }
                checkBounds.Expand(BOUNDS_EXPAND); // 애니메이션과 메시 크기 고려 → 여유 공간 확보

                // 빛 정보 추출
                var mpb = BuildMPB(posBatch, length);

                // 완성된 데이터 주입
                instancingRenderer.AddMatrix(batch, mpb, checkBounds);
            }
        }
    }
    #endregion

    #region ─────────────────────────▷ 내부 메서드 ◁─────────────────────────
    // 빛 정보 추출하기
    private MaterialPropertyBlock BuildMPB(in Vector3[] posBatch, int length)
    {
        SphericalHarmonicsL2[] lightProbes = new SphericalHarmonicsL2[length]; // 3차원 공간 조명 정보
        LightProbes.CalculateInterpolatedLightAndOcclusionProbes(posBatch, lightProbes, null);

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.CopySHCoefficientArraysFrom(lightProbes);
        return mpb;
    }

    // 매트릭스와 빛 연산을 위한 좌표를 저장
    private class InstancingGroup
    {
        public List<Matrix4x4> matrices = new();
        public List<Vector3> positions = new();
        public List<CGPUInstancingTarget> originalTargets = new();
    }
    #endregion
}
#endif
