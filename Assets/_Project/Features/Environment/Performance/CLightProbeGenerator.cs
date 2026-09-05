using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BoxCollider 영역 내 지형을 감지하여 라이트 프로브를 다층 배치합니다.
/// </summary>
[RequireComponent(typeof(LightProbeGroup))]
public class CLightProbeGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("프로브 생성 설정")]
    [SerializeField] private float _spacing = 10f;
    [SerializeField] private float[] _heightLevels = { 0.5f, 3.0f, 6.0f };
    [SerializeField] private LayerMask _groundLayer = ~0;

    [Header("충돌 방어 설정")]
    [SerializeField] private float _checkRadius = 0.2f;

    [ContextMenu("자동 배치 실행 (Generate Probes)")]
    public void GenerateProbes()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        LightProbeGroup probeGroup = GetComponent<LightProbeGroup>();

        Bounds bounds = box.bounds;
        List<Vector3> probePositions = new List<Vector3>();

        float topY = bounds.max.y;
        float bottomY = bounds.min.y;

        // 속이 빈 메시 콜라이더 내부를 관통하기 위한 물리 설정 임시 변경
        bool originalBackfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;

        try
        {
            for (float x = bounds.min.x; x <= bounds.max.x; x += _spacing)
            {
                for (float z = bounds.min.z; z <= bounds.max.z; z += _spacing)
                {
                    float currentY = topY;

                    while (currentY > bottomY)
                    {
                        Vector3 rayOrigin = new Vector3(x, currentY, z);
                        float rayDistance = currentY - bottomY;

                        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, _groundLayer))
                        {
                            // 윗면(바닥) 타격 시에만 프로브 생성 평가
                            if (hit.normal.y > 0.1f)
                            {
                                foreach (float h in _heightLevels)
                                {
                                    Vector3 pos = hit.point + Vector3.up * h;

                                    Vector3 checkStart = hit.point + Vector3.up * 0.05f;
                                    float checkDistance = h - 0.05f;

                                    bool isBlockedByCeiling = Physics.Raycast(checkStart, Vector3.up, checkDistance, _groundLayer);
                                    bool isInsideCollider = Physics.CheckSphere(pos, _checkRadius, _groundLayer);

                                    if (!isBlockedByCeiling && !isInsideCollider && pos.y <= topY)
                                    {
                                        probePositions.Add(transform.InverseTransformPoint(pos));
                                    }
                                }
                            }

                            // 관통 후 다음 레이캐스트 시작점 갱신
                            currentY = hit.point.y - 0.05f;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
        finally
        {
            Physics.queriesHitBackfaces = originalBackfaces;
        }

        probeGroup.probePositions = probePositions.ToArray();
        UDebug.Print($"{probePositions.Count}개의 라이트 프로브 자동 배치 완료!");
    }
#endif

    private void Awake()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) Destroy(box);
        Destroy(this);
    }
}
