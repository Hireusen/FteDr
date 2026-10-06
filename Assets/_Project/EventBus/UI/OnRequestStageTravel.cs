/// <summary>
/// 조종석 UI의 지역이동 버튼으로 상승/하강을 요청할 때 발행합니다.
/// 키보드(Q/E) 입력과 완전히 같은 경로를 타도록, CPlayerToCockpit.TryTravel이 이 요청을 처리합니다.
/// </summary>
public readonly struct OnRequestStageTravel
{
    public readonly bool goDeeper;

    public OnRequestStageTravel(bool goDeeper)
    {
        this.goDeeper = goDeeper;
    }

    /// <param name="goDeeper">true면 하강(다음 지역), false면 상승(이전 지역)</param>
    public static void Publish(bool goDeeper)
    {
        CEventBus<OnRequestStageTravel>.Publish(new OnRequestStageTravel(goDeeper));
    }
}
