using UnityEngine;

/// <summary>
/// 프레임 매니저의 안정성을 테스트하는 스크립트
/// </summary>
public class CTestFrameManager : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("설정")]
    [SerializeField] private int _count = 500; // 샘플 생성 개수
    [SerializeField] private int _autoDestroyCondition = 240; // 파괴까지 반복할 프레임 수

    [Header("버튼")]
    [SerializeField] private bool _start = false; // 샘플 생성 및 시작
    [SerializeField] private bool _clear = false; // 모든 샘플 청소
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (_start) {
            CreateFrameableObjects();
            _start = false;
        }
        if (_clear) {
            DestroyFrameableObjects();
            _clear = false;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void CreateFrameableObjects()
    {
        for (int i = 0; i < _count; ++i) {
            var comp = UObject.Create("FrameableObject").AddComponent<CTestFrameable>();
            comp.AutoDestroyCondition = _autoDestroyCondition;
        }
    }

    private void DestroyFrameableObjects()
    {
        CTestFrameable[] objs = UObject.FindComponents<CTestFrameable>(true);
        int length = objs.Length;
        for (int i = 0; i < length; ++i) {
            Destroy(objs[i]);
        }
    }
    #endregion
}
