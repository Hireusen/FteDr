/// <summary>
/// CUIManager가 열고 닫을 수 있는 UI 창의 공통 계약입니다.
/// 이 인터페이스를 구현하면 SetActive 대신 Open/Close가 호출되어, 페이드 연출을 자체적으로 수행할 수 있습니다.
/// </summary>
public interface IUIWindow
{
    /// <summary>창을 활성화하고 등장 연출(페이드 인 등)을 시작합니다.</summary>
    void Open();

    /// <summary>퇴장 연출(페이드 아웃 등)을 마친 뒤 창을 비활성화합니다.</summary>
    void Close();

    /// <summary>이 창이 열려있는 동안 HUD를 숨겨야 하는지 여부입니다.</summary>
    bool HidesHud { get; }
}
