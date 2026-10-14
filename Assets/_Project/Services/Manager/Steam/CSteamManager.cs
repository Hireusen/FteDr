#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// SteamAPI의 초기화·콜백 처리·종료만 담당하는 매니저입니다.
/// 도전과제 같은 기능별 로직은 이 클래스에 넣지 않고 별도 클래스로 분리합니다.
/// 스팀을 지원하지 않는 플랫폼에서는 내용이 비워지고 IsSteamRunning이 항상 false가 됩니다.
/// </summary>
public sealed class CSteamManager : ASingleton<CSteamManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CSteamAchievement _achievement;

#if !DISABLESTEAMWORKS
    // SteamAPI.Init()은 한 프로세스에서 단 한 번만 호출해야 한다.
    // 인스턴스가 파괴된 뒤 재생성될 수 있으므로 인스턴스 필드가 아닌 static으로 추적한다.
    private static bool _everInitialized = false;
    private bool _isSteamRunning = false;
    // 델리게이트가 GC에 수거되면 네이티브 쪽이 죽은 포인터를 호출하므로 필드로 붙잡아 둔다.
    private SteamAPIWarningMessageHook_t _warningMessageHook;
#endif
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>스팀웍스 파트너 사이트에서 발급받은 AppID입니다.</summary>
    public const uint APP_ID = 5432770;

    public override bool IsGlobal => true;

#if !DISABLESTEAMWORKS
    /// <summary>SteamAPI 초기화에 성공해 스팀 기능을 사용할 수 있는 상태인지 여부입니다.</summary>
    public bool IsSteamRunning => _isSteamRunning;
#else
    /// <summary>스팀을 지원하지 않는 플랫폼이므로 항상 False입니다.</summary>
    public bool IsSteamRunning => false;
#endif

    /// <summary>도전과제·통계 모듈입니다. 스팀 상태와 무관하게 null이 아닙니다.</summary>
    public CSteamAchievement Achievement => _achievement;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
#if !DISABLESTEAMWORKS
    // ASingleton의 Awake가 private이므로 이 클래스에 Awake를 선언하면 싱글톤 등록이 깨진다.
    // 초기화 로직은 반드시 이 메서드에만 작성한다.
    protected override void Initialize()
    {
        // 스팀 상태와 무관하게 먼저 만들어 둔다. 모든 메서드가 안전하게 no-op이라 호출부가 null을 걱정할 필요가 없다.
        _achievement = new CSteamAchievement();

        if (_everInitialized)
        {
            UDebug.Print("SteamAPI가 이미 이 세션에서 초기화되었습니다. 중복 초기화를 건너뜁니다.", LogType.Error, gameObject);
            return;
        }

        if (!Packsize.Test())
        {
            UDebug.Print("Packsize 검사에 실패했습니다. 이 플랫폼에 맞지 않는 Steamworks.NET이 실행되고 있습니다.", LogType.Error, gameObject);
        }
        if (!DllCheck.Test())
        {
            UDebug.Print("DllCheck 검사에 실패했습니다. steam_api 바이너리의 버전이 맞지 않습니다.", LogType.Error, gameObject);
        }

        // 스팀을 거치지 않고 실행된 경우 스팀 클라이언트로 재실행시킨다. (간이 DRM)
        try
        {
            if (SteamAPI.RestartAppIfNecessary((AppId_t)APP_ID))
            {
                UDebug.Print("스팀을 통한 재실행이 필요하므로 종료합니다.", LogType.Warning, gameObject);
                Application.Quit();
                return;
            }
        }
        catch (System.DllNotFoundException e)
        {
            UDebug.Print($"steam_api 바이너리를 찾을 수 없습니다. 플러그인 위치를 확인하세요.\n{e}", LogType.Error, gameObject);
            Application.Quit();
            return;
        }

        // 실패하는 주요 원인은 스팀 클라이언트 미실행, steam_appid.txt 누락, 해당 AppID 라이선스 미보유다.
        _isSteamRunning = SteamAPI.Init();
        if (!_isSteamRunning)
        {
            UDebug.Print("SteamAPI.Init()에 실패했습니다. 스팀 클라이언트 실행 여부와 steam_appid.txt를 확인하세요.", LogType.Error, gameObject);
            return;
        }
        _everInitialized = true;

        // 실행 인자에 -debug_steamapi 가 있을 때만 경고가 전달된다.
        _warningMessageHook = new SteamAPIWarningMessageHook_t(OnSteamAPIDebugText);
        SteamClient.SetWarningMessageHook(_warningMessageHook);

        UDebug.Print($"SteamAPI를 초기화했습니다. (AppID: {APP_ID})", LogType.Log, gameObject);

        _achievement.Initialize();
    }

    // 스팀 클라이언트의 콜백은 매 프레임 직접 펌프해 주어야 전달된다.
    private void Update()
    {
        if (!_isSteamRunning) return;

        SteamAPI.RunCallbacks();

        // 체류 시간 누적. 내부에서 주기적으로만 통계에 반영한다.
        _achievement.Tick(Time.deltaTime);
    }

    // OnApplicationQuit은 SteamAPI를 종료하기엔 너무 이른 시점이라 여기서 처리한다.
    protected override void OnDestroy()
    {
        // 베이스가 static 인스턴스를 비우는 역할을 하므로 반드시 먼저 호출해야 한다.
        base.OnDestroy();

        // 스팀이 꺼져 있어도 이벤트 버스 구독은 해제해야 하므로 아래 조기 반환보다 위에서 처리한다.
        _achievement?.Release();

        if (!_isSteamRunning) return;

        _isSteamRunning = false;
        SteamAPI.Shutdown();
        UDebug.Print("SteamAPI를 종료했습니다.");
    }

    // 스팀이 전달하는 경고 메시지를 유니티 콘솔로 옮긴다.
    [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
    private static void OnSteamAPIDebugText(int severity, System.Text.StringBuilder debugText)
    {
        UDebug.Print(debugText, LogType.Warning);
    }

    // 도메인 리로드를 끈 경우에도 플레이 모드 진입 시 static이 초기화되도록 보장한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _everInitialized = false;
    }
#else
    // 스팀 미지원 플랫폼에서는 ASingleton의 추상 메서드만 채우고 아무것도 하지 않는다.
    protected override void Initialize()
    {
        _achievement = new CSteamAchievement();
        UDebug.Print("스팀을 지원하지 않는 플랫폼이므로 SteamAPI를 초기화하지 않습니다.", LogType.Warning, gameObject);
    }
#endif
    #endregion
}
