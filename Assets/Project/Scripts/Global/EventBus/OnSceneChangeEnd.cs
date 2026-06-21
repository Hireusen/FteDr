/// <summary>
/// 씬 로드가 완료되었을 때
/// </summary>
public readonly struct OnSceneLoadEnd
{
    public readonly EScene prevScene;
    public readonly EScene nextScene;

    public OnSceneLoadEnd(EScene prevScene, EScene nextScene)
    {
        this.prevScene = prevScene;
        this.nextScene = nextScene;
    }

    /// <param name="prevScene">이전 씬 ID</param>
    /// <param name="nextScene">다음 씬 ID</param>
    public static void Publish(EScene prevScene, EScene nextScene)
    {
        CEventBus<OnSceneLoadEnd>.Publish(new OnSceneLoadEnd(prevScene, nextScene));
    }
}
