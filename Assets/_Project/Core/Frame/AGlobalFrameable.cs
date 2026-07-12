/// <summary>
/// 상속받은 클래스는 항상 싱글톤을 보장합니다.
/// </summary>
public abstract class AGlobalFrameable<T> : ASingleton<T> where T : AMono
{
    // 프레임 매니저에 자동으로 가입
    protected virtual void OnEnable()
    {
        var frame = CFrameManager.Ins;
        if (frame != null) {
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
    }

    // 프레임 매니저에서 자동으로 탈퇴
    protected virtual void OnDisable()
    {
        var frame = CFrameManager.Ins;
        if (frame != null) {
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
}
