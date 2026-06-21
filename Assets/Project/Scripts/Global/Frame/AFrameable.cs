/// <summary>
/// 프레임 매니저에 가입하게 해주는 추상화 클래스
/// </summary>
public abstract class AFrameable : AMono
{
    // 프레임 매니저에 자동으로 가입
    protected virtual void OnEnable()
    {
        var frame = CFrameManager.Ins;
        if (frame == null) {
            UDebug.Print($"현재 프레임 매니저가 존재하지 않습니다.", UnityEngine.LogType.Assert, gameObject);
            return;
        }

        if (this is IUpdateFrameable update) {
            frame.Register(update);
        }
        if (this is IFixedUpdateFrameable fixedUpdate) {
            frame.Register(fixedUpdate);
        }
        if (this is ILateUpdateFrameable lateUpdate) {
            frame.Register(lateUpdate);
        }
    }

    // 프레임 매니저에서 자동으로 탈퇴
    protected virtual void OnDisable()
    {
        var frame = CFrameManager.Ins;
        if (frame == null) {
            UDebug.Print($"현재 프레임 매니저가 존재하지 않습니다.", UnityEngine.LogType.Assert, gameObject);
            return;
        }

        if (this is IUpdateFrameable update) {
            frame.Unregister(update);
        }
        if (this is IFixedUpdateFrameable fixedUpdate) {
            frame.Unregister(fixedUpdate);
        }
        if (this is ILateUpdateFrameable lateUpdate) {
            frame.Unregister(lateUpdate);
        }
    }
}
