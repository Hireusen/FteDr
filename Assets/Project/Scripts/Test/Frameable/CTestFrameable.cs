using UnityEngine;

/// <summary>
/// Frameable을 상속받는 테스트용 컴포넌트
/// </summary>
public class CTestFrameable : AFrameable, IUpdateFrameable
{
    private int _totalExecuteCount = 0;
    private int _curExecuteCount = 0;
    private int _lastFrame = -1;

    public int AutoDestroyCondition { get; set; } // 1 이상일 경우 해당 횟수만큼 프레임 경과 시 파괴
    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv1;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        int curFrame = Time.frameCount;
        // 이번 프레임 시작
        if (_lastFrame != curFrame) {
            _curExecuteCount = 0;
            _lastFrame = curFrame;
        }

        _curExecuteCount++;
        _totalExecuteCount++;
        // 동일 프레임에 2회 이상 호출
        if (_curExecuteCount > 1) {
            UDebug.Print($"동일 프레임에 2회 이상 호출되는 문제가 발생했습니다!", LogType.Assert, gameObject);
        }

        if (AutoDestroyCondition <= 0) return;
        // 자동 파괴
        if(_totalExecuteCount > AutoDestroyCondition) {
            Destroy(gameObject);
        }
    }
}
