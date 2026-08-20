/// <summary>
/// 씬 비동기 로드 진행률이 갱신될 때 발행합니다. (0~1)
/// UScene.Load(...)의 onProgress 콜백에서 이 이벤트를 발행해줘야 로딩 화면이 값을 받을 수 있습니다.
/// </summary>
public readonly struct OnSceneLoadProgress
{
    public readonly float progress;

    public OnSceneLoadProgress(float progress)
    {
        this.progress = progress;
    }

    /// <param name="progress">0~1 사이의 진행률</param>
    public static void Publish(float progress)
    {
        CEventBus<OnSceneLoadProgress>.Publish(new OnSceneLoadProgress(progress));
    }
}
