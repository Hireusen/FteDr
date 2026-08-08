using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 연결된 오브젝트의 메시를 코드로 렌더링합니다.
/// </summary>
public class GPUInstancingRenderer : AFrameable, IUpdateFrameable
{
    // 외부에서 주입할 값
    private Mesh _instanceMesh;
    private Material _instanceMaterial;
    private List<Matrix4x4[]> _matrixBatches = new();

    private int _renderLayer = 0;
    private List<MaterialPropertyBlock> _mpbBatches = new();

    private List<Bounds> _boundsBatches = new();
    private Plane[] _frustumPlanes = new Plane[6];

    #region ─────────────────────────▷ 공개 멤버 ◁─────────────────────────
    // 데이터 주입 함수
    public void SetMeshAndMaterial(Mesh mesh, Material material, int layer)
    {
        if (mesh != null) _instanceMesh = mesh;
        else UDebug.Print($"GPU 인스턴싱에 필요한 메시가 빈 채로 전달되었습니다.", LogType.Log, mesh);

        if (material != null) _instanceMaterial = material;
        else UDebug.Print($"GPU 인스턴싱에 필요한 머티리얼이 빈 채로 전달되었습니다.", LogType.Log, material);

        _renderLayer = layer;
    }
    public void AddMatrix(in Matrix4x4[] batch, MaterialPropertyBlock mpb, Bounds bounds)
    {
        _matrixBatches.Add(batch);
        _mpbBatches.Add(mpb);
        _boundsBatches.Add(bounds);
    }

    // 프레임에이블
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;
    public void ExecuteUpdateFrame()
    {
        Camera mainCam = UCamera.Main;
        // 초기 방어
        if (mainCam == null) return;
        if (_instanceMesh == null || _instanceMaterial == null) return;

        // 카메라의 시야 평면 추출
        GeometryUtility.CalculateFrustumPlanes(mainCam, _frustumPlanes);

        // 렌더링
        int count = _matrixBatches.Count;
        for (int i = 0; i < count; ++i)
        {
            // 각 청크의 바운딩 박스가 카메라 시야에 들어오는가?
            if (!GeometryUtility.TestPlanesAABB(_frustumPlanes, _boundsBatches[i])) continue;

            Matrix4x4[] batch = _matrixBatches[i];
            Graphics.DrawMeshInstanced(_instanceMesh, 0, _instanceMaterial, batch, batch.Length,
                _mpbBatches[i], UnityEngine.Rendering.ShadowCastingMode.Off, true, _renderLayer, // 빛 정보
                null, UnityEngine.Rendering.LightProbeUsage.CustomProvided); // 모든 카메라 대상, MPB 강제 적용
        }
    }
    #endregion
}
