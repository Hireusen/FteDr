/// <summary>
/// 그림자 캐스팅 모드가 변경될 경우 발행합니다.
/// </summary>
public readonly struct OnOptionTextureQualityChanged
{
    public readonly int limit;

    public OnOptionTextureQualityChanged(int limit) {
        this.limit = limit;
    }

    public static void Publish(int limit)
    {
        CEventBus<OnOptionTextureQualityChanged>.Publish(new OnOptionTextureQualityChanged(limit));
    }
}
