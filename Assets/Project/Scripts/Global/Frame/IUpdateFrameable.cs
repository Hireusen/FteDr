/// <summary>
/// FrameManager에게 가입당할 스크립트가 준수하는 인터페이스
/// </summary>
public interface IUpdateFrameable
{
    EUpdatePriority UpdatePriority { get; }
    void ExecuteUpdateFrame();
}
