/// <summary>
/// HUD 요소(가방/그물 버튼, 잠수함 레이더 마커) 토글 키가 눌렸을 때 발행합니다.
/// 1인칭 플레이 중엔 마우스 커서가 없어서 버튼 클릭이 불가능하므로, 키보드로도 토글할 수 있어야 합니다.
/// </summary>
public readonly struct OnInputToggleHud
{
    public static void Publish()
    {
        CEventBus<OnInputToggleHud>.Publish(new OnInputToggleHud());
    }
}
