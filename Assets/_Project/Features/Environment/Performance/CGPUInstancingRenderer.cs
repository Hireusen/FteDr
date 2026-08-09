#pragma warning disable IDE0052
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 연결된 오브젝트의 메시를 코드로 렌더링합니다.
/// </summary>
public class GPUInstancingRenderer : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▷ 내부 변수 ◁─────────────────────────
    // 매니저가 다루는 인스턴싱 개수 표시 용도
    [ReadOnly][SerializeField] private int _count;

    // 외부에서 주입할 값
    private Mesh _instanceMesh;
    private Material _instanceMaterial;
    private List<Matrix4x4[]> _matrixBatches = new();

    private int _renderLayer = 0;
    private List<MaterialPropertyBlock> _mpbBatches = new();

    private List<Bounds> _boundsBatches = new();
    private Plane[] _frustumPlanes = new Plane[6];
    #endregion

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
        _count += batch.Length;
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

    #region ─────────────────────────▷ 에디터 편의성 ◁─────────────────────────
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 초기 방어
        if (_matrixBatches == null || _matrixBatches.Count == 0) return;

        // 준비
        const float DECAY_COUNT = 1f / 50f;
        const float DRAWLINE_MIN_LENGTH = 1.5f;
        const float DRAWLINE_MAX_LENGTH = 20f;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);

        // 레이 길이 계산
        float t = UMath.GetSmoothT(_count, DECAY_COUNT);
        float lineLength = Mathf.Lerp(DRAWLINE_MAX_LENGTH, DRAWLINE_MIN_LENGTH, t);

        // 모든 렌더러 순회
        foreach (var batch in _matrixBatches)
        {
            int length = batch.Length;

            // 배치 안에 속한 모든 오브젝트 레이 그리기
            for (int i = 0; i < length; ++i)
            {
                Vector3 objPos = batch[i].GetPosition();
                UDebug.UpRay(objPos, lineLength);
            }
        }
    }
#endif
    #endregion
}
