/// <summary>
/// 조종석 UI의 닫기 버튼으로 조종석에서 내리기를 요청할 때 발행합니다.
/// 이동키로 일어나는 것과 같은 경로(CPlayerToCockpit.CockpitToPlayer)를 탑니다.
/// </summary>
public readonly struct OnRequestExitCockpit
{
    public static void Publish()
    {
        CEventBus<OnRequestExitCockpit>.Publish(new OnRequestExitCockpit());
    }
}
