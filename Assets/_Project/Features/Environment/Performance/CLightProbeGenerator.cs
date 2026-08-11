using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BoxCollider 영역 내에서 지형(Ground)을 감지하여 라이트 프로브를 자동 배치합니다.
/// </summary>
[RequireComponent(typeof(LightProbeGroup))]
public class CLightProbeGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("프로브 간격 (X, Z축)")]
    public float spacing = 10f;

    [Header("바닥에서 띄울 높이 (다중 층 생성)")]
    public float[] heightLevels = { 0.5f, 3.0f, 6.0f };

    [Header("지형 레이어 (이 레이어에 닿은 곳에만 생성)")]
    public LayerMask groundLayer = ~0;

    [ContextMenu("자동 배치 실행 (Generate Probes)")]
    public void GenerateProbes()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        LightProbeGroup probeGroup = GetComponent<LightProbeGroup>();

        Bounds bounds = box.bounds;
        List<Vector3> probePositions = new List<Vector3>();

        float startX = bounds.min.x;
        float endX = bounds.max.x;
        float startZ = bounds.min.z;
        float endZ = bounds.max.z;
        float rayStartY = bounds.max.y;
        float rayDistance = bounds.size.y;

        // X, Z 축을 순회하며 위에서 아래로 레이캐스트(Raycast)를 쏩니다.
        for (float x = startX; x <= endX; x += spacing)
        {
            for (float z = startZ; z <= endZ; z += spacing)
            {
                Vector3 rayOrigin = new Vector3(x, rayStartY, z);

                // 바닥을 향해 레이저를 쏴서 지형이나 바위 등에 맞으면
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
                {
                    // 설정한 높이 층(heightLevels)마다 프로브를 생성합니다.
                    foreach (float h in heightLevels)
                    {
                        Vector3 pos = hit.point + Vector3.up * h;

                        // LightProbeGroup은 로컬 좌표계를 사용하므로 월드->로컬로 변환해서 저장
                        probePositions.Add(transform.InverseTransformPoint(pos));
                    }
                }
            }
        }

        // 완성된 리스트를 LightProbeGroup에 덮어씌웁니다.
        probeGroup.probePositions = probePositions.ToArray();
        UDebug.Print($"{probePositions.Count}개의 라이트 프로브 자동 배치 완료!");
    }
#endif
    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if(box != null) Destroy(box);
        Destroy(this);
    }
}
