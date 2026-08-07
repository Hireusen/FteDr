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

    // 데이터 주입 함수
    public void SetMeshAndMaterial(Mesh mesh, Material material)
    {
        if (mesh != null)
        {
            _instanceMesh = mesh;
        }
        else
        {
            UDebug.Print($"GPU 인스턴싱에 필요한 메시가 빈 채로 전달되었습니다.", LogType.Log, mesh);
        }
        if (material != null)
        {
            _instanceMaterial = material;
        }
        else
        {
            UDebug.Print($"GPU 인스턴싱에 필요한 머티리얼이 빈 채로 전달되었습니다.", LogType.Log, material);
        }
    }
    public void AddMatrix(in Matrix4x4[] batch)
    {
        _matrixBatches.Add(batch);
    }

    // 프레임에이블
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;
    public void ExecuteUpdateFrame()
    {
        if (_instanceMesh == null || _instanceMaterial == null) return;

        var count = _matrixBatches.Count;
        for (int i = 0; i < count; ++i)
        {
            Matrix4x4[] batch = _matrixBatches[i];
            Graphics.DrawMeshInstanced(_instanceMesh, 0, _instanceMaterial, batch, batch.Length);
        }
    }
}
