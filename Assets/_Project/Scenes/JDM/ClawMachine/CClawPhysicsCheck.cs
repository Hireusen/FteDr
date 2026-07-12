using UnityEngine;

/// <summary>
/// [1단계 검증용] 인형뽑기 기계 안 수집품들의 물리 설정이 올바른지 점검하는 헬퍼입니다.
/// ClawMachine 루트에 붙이면, 재생 시 자식 수집품들의 Rigidbody/Convex 설정을 검사해 리포트합니다.
/// 물리가 깨질 만한 설정(Rigidbody 없음, MeshCollider가 non-Convex)을 콘솔로 알려줍니다.
/// </summary>
public sealed class CClawPhysicsCheck : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("검사 옵션")]
    [Tooltip("재생 시작 시 자동 검사")]
    [SerializeField] private bool _checkOnStart = true;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_checkOnStart)
        {
            Check();
        }
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>자식 수집품들의 물리 설정을 검사하고 리포트합니다.</summary>
    [ContextMenu("물리 설정 검사")]
    public void Check()
    {
        CCollectible[] items = GetComponentsInChildren<CCollectible>(true);

        if (items.Length == 0)
        {
            UDebug.Print("CClawPhysicsCheck: 하위에서 CCollectible을 찾지 못했습니다. 인형을 이 오브젝트 아래에 두세요.", LogType.Warning);
            return;
        }

        int okCount = 0;
        int problemCount = 0;

        for (int i = 0; i < items.Length; ++i)
        {
            CCollectible item = items[i];
            bool ok = InspectOne(item, out string problem);

            if (ok)
            {
                ++okCount;
            }
            else
            {
                ++problemCount;
                UDebug.Print($"[물리 문제] {item.name}: {problem}", LogType.Warning, item);
            }
        }

        UDebug.Print($"CClawPhysicsCheck 완료: 정상 {okCount}개, 문제 {problemCount}개 (총 {items.Length}개).");
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 수집품 하나의 물리 설정을 검사한다. 문제 없으면 true.
    // [설계 전제] 수집품은 평소 Rigidbody 없이 정적 상태다. (필요 시 동적으로 붙였다 뗌)
    // 따라서 Rigidbody 유무는 검사하지 않고, 콜라이더/Convex만 본다.
    private bool InspectOne(CCollectible item, out string problem)
    {
        // 1) 콜라이더 확인 (자식 Visual 포함)
        Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            problem = "콜라이더가 하나도 없음 (통과함)";
            return false;
        }

        // 2) MeshCollider가 있으면 Convex 여부 확인
        //    (동적으로 Rigidbody를 붙이는 순간 non-Convex면 물리 에러 → 미리 Convex로)
        for (int i = 0; i < colliders.Length; ++i)
        {
            if (colliders[i] is MeshCollider mesh && !mesh.convex)
            {
                problem = $"MeshCollider '{mesh.name}'가 non-Convex (동적 Rigidbody 부착 시 충돌 안 됨)";
                return false;
            }
        }

        problem = "";
        return true;
    }
    #endregion
}
