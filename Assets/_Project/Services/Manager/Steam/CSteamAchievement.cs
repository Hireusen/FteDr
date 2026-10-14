#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using UnityEngine;
#if !DISABLESTEAMWORKS
using System.Collections.Generic;
using Steamworks;
#endif

/// <summary>
/// 스팀 도전과제와 통계의 읽기·쓰기를 담당합니다.
/// CSteamManager가 소유하며, SteamAPI 초기화가 성공한 직후 Initialize()를 호출받습니다.
/// 스팀이 꺼져 있거나 지원하지 않는 플랫폼에서는 모든 메서드가 조용히 아무것도 하지 않습니다.
/// </summary>
/// <remarks>
/// 과제 판정은 전부 이 클래스의 이벤트 핸들러에 모읍니다.
/// 게임플레이 코드는 이 클래스를 몰라도 되고, 호출부는 USteam만 사용합니다.
/// </remarks>
public sealed class CSteamAchievement
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
#if !DISABLESTEAMWORKS
    // 체류 시간을 매 프레임 올리면 네트워크 쓰기가 과하므로 이 주기로만 반영한다.
    private const float STORE_INTERVAL_SECONDS = 60f;

    // Callback을 멤버 변수로 붙잡아 두지 않으면 GC가 수거해 콜백이 끊긴다. (Steamworks.NET의 명시적 요구사항)
    private Callback<UserStatsReceived_t> _onUserStatsReceived;
    private Callback<UserAchievementStored_t> _onAchievementStored;

    // 통계 수신 전에 들어온 요청을 보관한다. 같은 과제가 여러 번 요청돼도 HashSet이 걸러준다.
    private readonly HashSet<string> _pendingUnlocks = new HashSet<string>();
    private int _pendingGold;
    private int _pendingSeconds;

    // 아직 통계에 반영하지 않은 체류 시간. 소수점은 다음 주기로 넘긴다.
    private float _unflushedSeconds;
    private bool _isCountingDiveTime;

    private ulong _gameId;
    private bool _isInitialized;
    private bool _isStatsReady;
#endif
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
#if !DISABLESTEAMWORKS
    /// <summary>스팀에서 통계·도전과제 데이터를 받아와 읽고 쓸 수 있는 상태인지 여부입니다.</summary>
    public bool IsStatsReady => _isStatsReady;
#else
    /// <summary>스팀을 지원하지 않는 플랫폼이므로 항상 False입니다.</summary>
    public bool IsStatsReady => false;
#endif

    /// <summary>
    /// 콜백과 이벤트 구독을 등록합니다. SteamAPI.Init()이 성공한 뒤에만 호출해야 합니다.
    /// </summary>
    public void Initialize()
    {
#if !DISABLESTEAMWORKS
        if (_isInitialized) return;

        // GetAppID()는 Init 성공 이후에만 유효하다.
        _gameId = new CGameID(SteamUtils.GetAppID()).m_GameID;

        // Init 직후 스팀이 현재 사용자의 통계를 자동으로 보내준다. 수동 요청 API는 SDK 1.64에서 제거되었다.
        _onUserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
        _onAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);

        // 게임플레이 코드를 건드리지 않기 위해 기존 이벤트 버스를 구독해 과제를 판정한다.
        // 구독을 늘릴 때는 Release의 해제도 반드시 쌍으로 늘려야 한다.
        CEventBus<OnPlayerSubmarineAreaChanged>.Subscribe(HandleSubmarineAreaChanged);
        CEventBus<OnSceneLoadStart>.Subscribe(HandleSceneLoadStart);
        CEventBus<OnStageResearched>.Subscribe(HandleStageResearched);
        CEventBus<OnItemsSold>.Subscribe(HandleItemsSold);

        _isInitialized = true;
        UDebug.Print("도전과제 모듈을 초기화했습니다. 통계 수신을 대기합니다.");
#endif
    }

    /// <summary>
    /// 콜백과 이벤트 구독을 해제합니다. 스팀 상태와 무관하게 반드시 호출해야 합니다.
    /// </summary>
    public void Release()
    {
#if !DISABLESTEAMWORKS
        if (!_isInitialized) return;

        // 종료 직전까지 쌓인 체류 시간을 마지막으로 반영한다.
        FlushUnflushedSeconds();

        // 이벤트 버스의 델리게이트가 static이므로 해제하지 않으면 죽은 인스턴스가 계속 호출된다.
        CEventBus<OnPlayerSubmarineAreaChanged>.Unsubscribe(HandleSubmarineAreaChanged);
        CEventBus<OnSceneLoadStart>.Unsubscribe(HandleSceneLoadStart);
        CEventBus<OnStageResearched>.Unsubscribe(HandleStageResearched);
        CEventBus<OnItemsSold>.Unsubscribe(HandleItemsSold);

        _onUserStatsReceived?.Dispose();
        _onAchievementStored?.Dispose();
        _onUserStatsReceived = null;
        _onAchievementStored = null;

        _pendingUnlocks.Clear();
        _pendingGold = 0;
        _pendingSeconds = 0;
        _unflushedSeconds = 0f;
        _isCountingDiveTime = false;
        _isStatsReady = false;
        _isInitialized = false;
        UDebug.Print("도전과제 모듈을 해제했습니다.");
#endif
    }

    /// <summary>
    /// 체류 시간을 누적합니다. CSteamManager가 매 프레임 호출합니다.
    /// </summary>
    /// <param name="deltaTime">경과 시간(초)</param>
    public void Tick(float deltaTime)
    {
#if !DISABLESTEAMWORKS
        if (!_isInitialized) return;

        // 잠수함을 벗어나 실제로 수영하는 동안만 누적한다.
        // 씬 전환 시 측정을 끊으므로 타이틀 화면에서는 쌓이지 않고,
        // 일시정지는 Time.timeScale = 0이라 deltaTime이 0이 되어 자연히 제외된다.
        if (!_isCountingDiveTime) return;

        _unflushedSeconds += deltaTime;
        if (_unflushedSeconds < STORE_INTERVAL_SECONDS) return;

        FlushUnflushedSeconds();
#endif
    }

    /// <summary>
    /// 도전과제를 해금합니다. 이미 해금된 과제는 아무 일도 하지 않습니다.
    /// 통계 수신 전에 호출하면 수신 완료 시점까지 보류한 뒤 자동으로 처리됩니다.
    /// </summary>
    /// <param name="apiName">파트너 사이트에 등록한 API Name (K.ACH_*)</param>
    public void Unlock(string apiName)
    {
#if !DISABLESTEAMWORKS
        // 스팀이 없을 때 큐에 쌓으면 영원히 비워지지 않으므로 그냥 버린다.
        if (!_isInitialized) return;

        if (!_isStatsReady)
        {
            _pendingUnlocks.Add(apiName);
            UDebug.Print($"통계 수신 전이므로 도전과제({apiName}) 해금을 보류합니다.");
            return;
        }

        // 중복 StoreStats(네트워크 쓰기)와 팝업 재표시를 막는다.
        if (!SteamUserStats.GetAchievement(apiName, out bool achieved))
        {
            UDebug.Print($"도전과제({apiName})를 찾을 수 없습니다. API Name이 파트너 사이트와 일치하는지 확인하세요.", LogType.Error);
            return;
        }
        if (achieved) return;

        if (!SteamUserStats.SetAchievement(apiName))
        {
            UDebug.Print($"도전과제({apiName}) 설정에 실패했습니다.", LogType.Error);
            return;
        }

        // StoreStats를 호출하지 않으면 해금이 커밋되지 않고 팝업도 뜨지 않는다.
        SteamUserStats.StoreStats();
        UDebug.Print($"도전과제({apiName})를 해금했습니다.");
#endif
    }

    /// <summary>해당 도전과제가 이미 해금되어 있는지 확인합니다. 통계 수신 전에는 항상 False입니다.</summary>
    public bool IsUnlocked(string apiName)
    {
#if !DISABLESTEAMWORKS
        if (!_isStatsReady) return false;
        if (!SteamUserStats.GetAchievement(apiName, out bool achieved)) return false;

        return achieved;
#else
        return false;
#endif
    }

    /// <summary>누적 통계값을 덮어씁니다. 누적 가산이 필요하면 TryGetStat으로 읽은 뒤 더해서 넣으세요.</summary>
    public void SetStat(string apiName, int value)
    {
#if !DISABLESTEAMWORKS
        if (!_isStatsReady) return;

        if (!SteamUserStats.SetStat(apiName, value))
        {
            UDebug.Print($"통계({apiName}) 설정에 실패했습니다.", LogType.Error);
            return;
        }
        SteamUserStats.StoreStats();
#endif
    }

    /// <summary>누적 통계값을 읽습니다.</summary>
    /// <returns>읽기에 성공했다면 True</returns>
    public bool TryGetStat(string apiName, out int value)
    {
        value = 0;
#if !DISABLESTEAMWORKS
        if (!_isStatsReady) return false;

        return SteamUserStats.GetStat(apiName, out value);
#else
        return false;
#endif
    }

    /// <summary>
    /// 진행도 토스트를 띄웁니다. 이 호출만으로는 해금되지 않으므로,
    /// 임계값에 도달하면 Unlock()을 따로 호출해야 합니다.
    /// </summary>
    /// <remarks>
    /// 파트너 사이트에서 과제의 Progress Stat을 연결해두면 오버레이가 진행도를
    /// 자동으로 보여주므로, 보통은 이 메서드를 직접 부를 필요가 없습니다.
    /// </remarks>
    public void IndicateProgress(string apiName, int current, int max)
    {
#if !DISABLESTEAMWORKS
        if (!_isStatsReady) return;
        if (current <= 0 || max <= 0) return;

        SteamUserStats.IndicateAchievementProgress(apiName, (uint)current, (uint)max);
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 모든 통계와 도전과제를 초기화합니다. 테스트 전용이며 에디터에서만 컴파일됩니다.
    /// </summary>
    public void ResetAll()
    {
#if !DISABLESTEAMWORKS
        if (!_isStatsReady) return;

        SteamUserStats.ResetAllStats(true);
        SteamUserStats.StoreStats();
        UDebug.Print("모든 통계와 도전과제를 초기화했습니다.", LogType.Warning);
#endif
    }
#endif
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
#if !DISABLESTEAMWORKS
    // Init 이후 스팀이 비동기로 통계를 보내주면 호출된다. 이 시점부터 과제 상태를 읽고 쓸 수 있다.
    private void OnUserStatsReceived(UserStatsReceived_t param)
    {
        // 다른 게임의 통계가 전달될 수 있으므로 반드시 걸러낸다.
        if (param.m_nGameID != _gameId) return;

        if (param.m_eResult != EResult.k_EResultOK)
        {
            UDebug.Print($"통계 수신에 실패했습니다. (결과: {param.m_eResult})", LogType.Error);
            return;
        }

        _isStatsReady = true;
        UDebug.Print("통계를 수신했습니다. 도전과제를 사용할 수 있습니다.");

        FlushPending();
    }

    // 보류된 해금과 누적치를 한 번에 처리한다.
    private void FlushPending()
    {
        // 해금부터 처리한다. StoreStats는 마지막에 한 번만 호출해 네트워크 쓰기를 줄인다.
        if (_pendingUnlocks.Count > 0)
        {
            bool isDirty = false;
            foreach (string apiName in _pendingUnlocks)
            {
                if (!SteamUserStats.GetAchievement(apiName, out bool achieved))
                {
                    UDebug.Print($"보류된 도전과제({apiName})를 찾을 수 없습니다. API Name을 확인하세요.", LogType.Error);
                    continue;
                }
                if (achieved) continue;

                if (SteamUserStats.SetAchievement(apiName))
                {
                    isDirty = true;
                    UDebug.Print($"보류된 도전과제({apiName})를 해금했습니다.");
                }
            }
            _pendingUnlocks.Clear();

            if (isDirty)
            {
                SteamUserStats.StoreStats();
            }
        }

        // 누적치는 보류분을 비운 뒤 다시 넣어야 재보류 루프에 빠지지 않는다.
        if (_pendingGold > 0)
        {
            int gold = _pendingGold;
            _pendingGold = 0;
            AddGold(gold);
        }
        if (_pendingSeconds > 0)
        {
            int seconds = _pendingSeconds;
            _pendingSeconds = 0;
            AddDiveSeconds(seconds);
        }
    }

    // 스팀 서버에 쓰기가 반영되면 호출된다. 해금이 실제로 커밋됐는지 확인하는 용도다.
    private void OnAchievementStored(UserAchievementStored_t param)
    {
        if (param.m_nGameID != _gameId) return;

        UDebug.Print($"도전과제({param.m_rgchAchievementName})가 스팀에 저장되었습니다.");
    }

    // 쌓인 체류 시간 중 정수 초만 통계에 반영하고 소수점은 남겨 둔다.
    private void FlushUnflushedSeconds()
    {
        int whole = (int)_unflushedSeconds;
        if (whole <= 0) return;

        _unflushedSeconds -= whole;
        AddDiveSeconds(whole);
    }

    // 누적 획득 골드를 더하고 기준값 도달 여부를 판정한다.
    private void AddGold(int amount)
    {
        if (amount <= 0) return;

        if (!_isStatsReady)
        {
            _pendingGold += amount;
            return;
        }

        if (!TryAddStat(K.STAT_TOTAL_MONEY, amount, out int total)) return;

        // 한 번의 판매로 두 단계를 같이 넘을 수 있으므로 셋 다 검사한다.
        if (total >= K.ACH_TOTAL_MONEY_BRONZE_THRESHOLD) Unlock(K.ACH_TOTAL_MONEY_BRONZE);
        if (total >= K.ACH_TOTAL_MONEY_SILVER_THRESHOLD) Unlock(K.ACH_TOTAL_MONEY_SILVER);
        if (total >= K.ACH_TOTAL_MONEY_GOLD_THRESHOLD) Unlock(K.ACH_TOTAL_MONEY_GOLD);
    }

    // 누적 체류 시간을 더하고 기준값 도달 여부를 판정한다.
    private void AddDiveSeconds(int seconds)
    {
        if (seconds <= 0) return;

        if (!_isStatsReady)
        {
            _pendingSeconds += seconds;
            return;
        }

        if (!TryAddStat(K.STAT_DIVE_SECONDS, seconds, out int total)) return;

        if (total >= K.ACH_DIVE_TIME_BRONZE_THRESHOLD_SECONDS) Unlock(K.ACH_DIVE_TIME_BRONZE);
        if (total >= K.ACH_DIVE_TIME_SILVER_THRESHOLD_SECONDS) Unlock(K.ACH_DIVE_TIME_SILVER);
        if (total >= K.ACH_DIVE_TIME_GOLD_THRESHOLD_SECONDS) Unlock(K.ACH_DIVE_TIME_GOLD);
    }

    // 통계는 스팀이 보관하는 값이 기준이므로, 읽어서 더한 뒤 다시 쓴다.
    private bool TryAddStat(string apiName, int delta, out int newTotal)
    {
        newTotal = 0;

        if (!SteamUserStats.GetStat(apiName, out int current))
        {
            UDebug.Print($"통계({apiName})를 읽을 수 없습니다. 파트너 사이트에 통계가 등록되어 있는지 확인하세요.", LogType.Error);
            return false;
        }

        newTotal = current + delta;
        if (!SteamUserStats.SetStat(apiName, newTotal))
        {
            UDebug.Print($"통계({apiName}) 쓰기에 실패했습니다.", LogType.Error);
            return false;
        }

        SteamUserStats.StoreStats();
        return true;
    }

    // 스테이지 번호(1-based)를 과제 API Name으로 바꾼다.
    // 스테이지 1은 연구 없이 처음부터 열려 있으므로 대응하는 과제가 없다.
    private static string GetStageAchievement(int stage)
    {
        switch (stage)
        {
            case 2: return K.ACH_STAGE2_UNLOCKED;
            case 3: return K.ACH_STAGE3_UNLOCKED;
            case 4: return K.ACH_STAGE4_UNLOCKED;
            case 5: return K.ACH_STAGE5_UNLOCKED;
            case 6: return K.ACH_STAGE6_UNLOCKED;
            case 7: return K.ACH_STAGE7_UNLOCKED;
            default: return null;
        }
    }
#endif
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
#if !DISABLESTEAMWORKS
    // 이벤트 버스 구조체와 이름이 겹치지 않도록 핸들러는 Handle 접두사를 쓴다.

    // 첫 잠수 + 체류 시간 측정 구간을 결정한다.
    private void HandleSubmarineAreaChanged(OnPlayerSubmarineAreaChanged e)
    {
        if (!e.isInsideSubmarine)
        {
            // 잠수함 트리거를 벗어난 순간이 곧 잠수 시작이다.
            Unlock(K.ACH_FIRST_DIVE);
            _isCountingDiveTime = true;
            return;
        }

        // 잠수함으로 돌아오면 측정을 끊고 남은 시간을 반영한다.
        _isCountingDiveTime = false;
        FlushUnflushedSeconds();
    }

    // 엔딩 진입 판정 + 수영 측정 종료.
    private void HandleSceneLoadStart(OnSceneLoadStart e)
    {
        // 엔딩 씬 로드가 곧 엔딩 연출의 시작이다. 스테이지 7은 실제로 이동하는 해역이 아니라,
        // 조종석에서 하강을 누르면 이 씬으로 넘어오면서 연출이 재생된다.
        // 이 시점엔 플레이어가 조종석에 있어 수영 측정이 꺼져 있으므로,
        // 아래 조기 반환보다 반드시 위에서 처리해야 한다.
        if (e.nextScene == EScene.Ending)
        {
            Unlock(K.ACH_ENDING);
        }

        // 잠수함 밖에 있는 상태로 타이틀 등으로 나가면 트리거 이탈 이벤트가 오지 않아
        // 측정이 켜진 채 남고, 매니저가 DontDestroyOnLoad라 다음 씬까지 계속 쌓인다.
        if (!_isCountingDiveTime) return;

        _isCountingDiveTime = false;
        FlushUnflushedSeconds();
    }

    // 첫 연구 + 스테이지별 해금.
    private void HandleStageResearched(OnStageResearched e)
    {
        // 연구 자체가 처음인 경우. Unlock이 멱등하므로 매번 호출해도 첫 회에만 해금된다.
        Unlock(K.ACH_FIRST_UNLOCKED);

        // 주의: e.unlockedStage는 스테이지 번호가 아니라 "기본 스테이지 외에 추가로 해금한 수"다.
        //       UStageResearch.CurrentLevel과 같은 1-based 번호로 맞추려면 +1 해야 한다.
        //       (첫 연구 → unlockedStage 1 → 스테이지 2 도달 가능)
        int stage = e.unlockedStage + 1;

        string apiName = GetStageAchievement(stage);
        if (apiName == null)
        {
            UDebug.Print($"스테이지({stage})에 대응하는 도전과제가 없습니다. KDefine.Achievement에 상수를 추가하세요.", LogType.Warning);
            return;
        }

        Unlock(apiName);
    }

    // 누적 획득 골드. 판매 수익만 집계되므로 치트 골드와 업그레이드 환불은 제외된다.
    private void HandleItemsSold(OnItemsSold e)
    {
        AddGold(e.totalGold);
    }
#endif
    #endregion
}
