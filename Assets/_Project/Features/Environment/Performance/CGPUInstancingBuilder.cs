#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 씬이 로드/빌드될 때 오브젝트를 삭제하고 GPU 인스턴싱으로 대체합니다.
/// </summary>
public class CGPUInstancingBuilder : IProcessSceneWithReport
{
    // 인터페이스가 요구하는 콜백 순서
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // 빌드나 플레이 모드일 때만 동작
        if (!Application.isPlaying && report == null) return;

        // 씬 내의 모든 타겟 수집
        var targets = UObject.FindComponents<CGPUInstancingTarget>();
        if (targets.Length == 0) return;

        // 동일한 Mesh와 Material을 사용하는 오브젝트끼리 그룹화
        var groupedTargets = new Dictionary<(Mesh, Material), List<Matrix4x4>>();
        for (int i = 0; i < targets.Length; i++)
        {
            var target = targets[i];
            if (target == null) continue;

            var filter = target.GetComponent<MeshFilter>();
            var renderer = target.GetComponent<MeshRenderer>();
            // 필터나 렌더러 누락 검사
            if (filter == null || renderer == null) continue;

            // 튜플 키 만들어서 넣기
            var key = (filter.sharedMesh, renderer.sharedMaterial);
            if (!groupedTargets.ContainsKey(key))
            {
                groupedTargets[key] = new List<Matrix4x4>();
            }
            groupedTargets[key].Add(target.transform.localToWorldMatrix);

            // 콜라이더 존재 여부에 따라 삭제 범위 결정
            if (target.GetComponent<Collider>() != null)
            {
                // 콜라이더 존재 시 렌더러, 필터, 타겟만 삭제
                Object.DestroyImmediate(renderer);
                Object.DestroyImmediate(filter);
                Object.DestroyImmediate(target);
            }
            else
            {
                // 콜라이더 없을 시 전부 삭제
                Object.DestroyImmediate(target.gameObject);
            }
        }

        // 그룹별로 매니저 오브젝트 및 렌더러 생성
        int managerIndex = 0;
        foreach (var kvp in groupedTargets)
        {
            // 변수 준비
            Mesh targetMesh = kvp.Key.Item1;
            Material targetMat = kvp.Key.Item2;
            List<Matrix4x4> matrices = kvp.Value;

            // 매니저 오브젝트 생성
            GameObject managerObj = new GameObject($"GPU_Instancing_Manager_{managerIndex++}");

            // 렌더러 설정
            var instancingRenderer = managerObj.AddComponent<GPUInstancingRenderer>();
            instancingRenderer.SetMeshAndMaterial(targetMesh, targetMat);

            // 행렬 분할 저장
            for (int i = 0; i < matrices.Count; i += 1023)
            {
                int length = Mathf.Min(1023, matrices.Count - i);
                Matrix4x4[] batch = new Matrix4x4[length];
                matrices.CopyTo(i, batch, 0, length);
                instancingRenderer.AddMatrix(batch);
            }
        }
    }
}
#endif
