using UnityEngine;
using UnityEditor;

public class SnapToGround : Editor
{
    // 1. 위치와 회전(노멀) 모두 지형에 맞춤 (단축키: Ctrl + G)
    [MenuItem("Tools/Snap To Ground (Align Normal) %g")]
    public static void SnapAndAlign()
    {
        Snap(true);
    }

    // 2. 위치만 지형에 맞춤 - 나무 등 곧게 서 있어야 하는 오브젝트용 (단축키: Ctrl + Shift + G)
    [MenuItem("Tools/Snap To Ground (Position Only) %#g")]
    public static void SnapPositionOnly()
    {
        Snap(false);
    }

    private static void Snap(bool alignNormal)
    {
        // 실행 취소(Ctrl+Z)를 지원하기 위해 상태를 기록합니다.
        Undo.RecordObjects(Selection.transforms, "Snap To Ground");

        foreach (Transform t in Selection.transforms)
        {
            // 현재 오브젝트 위치에서 위로 10 높이에서 아래로 레이캐스트를 쏩니다.
            // (오브젝트가 땅속에 살짝 파묻혀 있을 때를 대비)
            Vector3 rayStart = t.position + Vector3.up * 10f;

            // 물리 충돌체를 가진 바닥(Terrain 등)을 찾습니다.
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit))
            {
                // 위치를 충돌한 표면(바닥)으로 이동
                t.position = hit.point;

                // 지형의 굴곡에 맞춰 회전값 조정
                if (alignNormal)
                {
                    // 기존의 앞(Forward) 방향은 최대한 유지하면서 위(Up) 방향만 바닥의 노멀(Normal)에 맞춥니다.
                    t.rotation = Quaternion.FromToRotation(t.up, hit.normal) * t.rotation;
                }
            }
        }
    }
}
