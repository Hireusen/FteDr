/// <summary>
/// 씬 로드가 시작되었을 때
/// </summary>
public readonly struct OnSceneLoadStart
{
    public readonly EScene prevScene;
    public readonly EScene nextScene;

    public OnSceneLoadStart(EScene prevScene, EScene nextScene)
    {
        this.prevScene = prevScene;
        this.nextScene = nextScene;
    }

    /// <param name="prevScene">이전 씬 ID</param>
    /// <param name="nextScene">다음 씬 ID</param>
    public static void Publish(EScene prevScene, EScene nextScene)
    {
        CEventBus<OnSceneLoadStart>.Publish(new OnSceneLoadStart(prevScene, nextScene));
    }
}
