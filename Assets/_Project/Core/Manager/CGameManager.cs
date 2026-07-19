using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 핵심 오브젝트에 대한 접근과 씬 로드를 지원합니다.
/// </summary>
public sealed class CGameManager : ASingleton<CGameManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    // 게임 상태
    private EScene _curScene;
    private Coroutine _bootCo;

    // 루트 오브젝트
    private static Transform _normalObjectRoot;
    private static Transform _enableObjectRoot;

    // 전역 액터
    private static GameObject _player;
    private static GameObject _submarine;
    private static GameObject _uiManager;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 게임 상태
    public EScene Scene => _curScene;
    public bool IsSceneLoading { get; private set; }

    // 루트 오브젝트
    public static Transform NormalObjectRoot => RootProvider(_normalObjectRoot, K.NAME_NORMAL_OBJECT_ROOT);
    public static Transform PoolingObjectRoot => RootProvider(_enableObjectRoot, K.NAME_POOLING_OBJECT_ROOT);
    public static GameObject Player => _player;
    public static GameObject SubmarineObject => _submarine;
    public static GameObject UIManager => _uiManager;

    /// <summary>
    /// 해당 씬을 동기 로드합니다.
    /// 동일한 이름을 가지는 씬도 있을 수 있기 때문에 표준적으로는 인덱스 사용이 권장됩니다.
    /// </summary>
    /// <param name="index">씬 인덱스</param>
    [Obsolete("비동기 씬 로드를 권장합니다.")]
    public void LoadScene(int index)
    {
        if (!IsValidScene(index))
        {
            return;
        }
        string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
        LoadScene(scenePath); // 경로를 넣어도 씬 매니저에서 알아서 해준다.
    }

    /// <summary>
    /// 해당 씬을 동기 로드합니다.
    /// </summary>
    /// <param name="name">씬 이름</param>
    [Obsolete("비동기 씬 로드를 권장합니다.")]
    public void LoadScene(string name)
    {
        if (!IsValidScene(name))
        {
            return;
        }
        PreProcessing(_curScene, name);
        SceneManager.LoadScene(name, LoadSceneMode.Single);
        PostProcessing(_curScene, name);
    }

    /// <summary>
    /// 해당 씬을 비동기 로드합니다.
    /// 동일한 이름을 가지는 씬도 있을 수 있기 때문에 표준적으로는 인덱스 사용이 권장됩니다.
    /// </summary>
    /// <param name="index">씬 인덱스</param>
    /// <param name="callback">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="delay">씬 로드 시작 전에 대기할 시간(초)</param>
    /// <param name="loadSceneMode">씬 로드 모드</param>
    public void LoadSceneAsync(
        int index, Action callback = null, Action<float> onProgress = null, float delay = 0f,
        LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidScene(index))
        {
            UDebug.Print($"유효하지 않은 씬 인덱스를 사용했습니다!", LogType.Error);
            return;
        }
        string scenePath = SceneUtility.GetScenePathByBuildIndex(index); // 경로를 넣어도 씬 매니저에서 알아서 해준다.
        LoadSceneAsync(scenePath, callback, onProgress, delay, loadSceneMode);
    }

    /// <summary>
    /// 해당 씬을 비동기 로드합니다.
    /// </summary>
    /// <param name="name">씬 이름</param>
    /// <param name="callback">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="delay">씬 로드 시작 전에 대기할 시간(초)</param>
    /// <param name="loadSceneMode">씬 로드 모드</param>
    public void LoadSceneAsync(
        string name, Action callback = null, Action<float> onProgress = null, float delay = 0f,
        LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidScene(name))
        {
            UDebug.Print($"유효하지 않은 씬 이름을 사용했습니다!", LogType.Error);
            return;
        }
        PreProcessing(_curScene, name);
        StartCoroutine(DoLoadSceneAsync(name, callback, onProgress, DelayPrologue(delay), loadSceneMode));
    }

    /// <summary>
    /// 해당 씬을 페이드 효과로 비동기 로드합니다.
    /// 동일한 이름을 가지는 씬도 있을 수 있기 때문에 표준적으로는 인덱스 사용이 권장됩니다.
    /// </summary>
    /// <param name="index">씬 인덱스</param>
    /// <param name="callback">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="delay">씬 로드 시작 전에 대기할 시간(초)</param>
    /// <param name="loadSceneMode">씬 로드 모드</param>
    public void LoadSceneAsyncWithFade(
        int index, float delay = 0f, float fadeOutTime = 0.45f, float fadeInTime = 0.45f,
        Action callback = null, Action<float> onProgress = null,
        LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidScene(index))
        {
            UDebug.Print($"유효하지 않은 씬 인덱스를 사용했습니다!", LogType.Error);
            return;
        }
        string scenePath = SceneUtility.GetScenePathByBuildIndex(index); // 경로를 넣어도 씬 매니저에서 알아서 해준다.
        LoadSceneAsyncWithFade(scenePath, delay, fadeOutTime, fadeInTime, callback, onProgress, loadSceneMode);
    }

    /// <summary>
    /// 해당 씬을 페이드 효과로 비동기 로드합니다.
    /// </summary>
    /// <param name="name">씬 이름</param>
    /// <param name="callback">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="delay">씬 로드 시작 전에 대기할 시간(초)</param>
    /// <param name="loadSceneMode">씬 로드 모드</param>
    public void LoadSceneAsyncWithFade(
        string name, float delay = 0f, float fadeOutTime = 0.45f, float fadeInTime = 0.45f,
        Action callback = null, Action<float> onProgress = null,
        LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidScene(name))
        {
            UDebug.Print($"유효하지 않은 씬 이름을 사용했습니다!", LogType.Error);
            return;
        }
        PreProcessing(_curScene, name);
        StartCoroutine(DoLoadSceneAsyncWithFade
            (name, DelayPrologue(delay), fadeOutTime, fadeInTime, callback, onProgress, loadSceneMode));
    }

    /// <summary>
    /// 선행 코루틴을 끝까지 실행한 뒤 해당 씬을 비동기 로드합니다.
    /// 동일한 이름을 가지는 씬도 있을 수 있기 때문에 표준적으로는 인덱스 사용이 권장됩니다.
    /// </summary>
    /// <param name="index">씬 인덱스</param>
    /// <param name="preRoutine">씬 로드 시작 전에 끝까지 실행할 선행 코루틴</param>
    /// <param name="callback">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="loadSceneMode">씬 로드 모드</param>
    public void LoadSceneAsync(
        int index, IEnumerator preRoutine, Action callback = null, Action<float> onProgress = null,
        LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidScene(index))
        {
            UDebug.Print($"유효하지 않은 씬 인덱스를 사용했습니다!", LogType.Error);
            return;
        }
        string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
        LoadSceneAsync(scenePath, preRoutine, callback, onProgress, loadSceneMode);
    }

    /// <summary>
    /// 선행 코루틴을 끝까지 실행한 뒤 해당 씬을 비동기 로드합니다.
    /// </summary>
    /// <param name="name">씬 이름</param>
    /// <param name="preRoutine">씬 로드 시작 전에 끝까지 실행할 선행 코루틴</param>
    /// <param name="callback">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="loadSceneMode">씬 로드 모드</param>
    public void LoadSceneAsync(
        string name, IEnumerator preRoutine, Action callback = null, Action<float> onProgress = null,
        LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidScene(name))
        {
            UDebug.Print($"유효하지 않은 씬 이름을 사용했습니다!", LogType.Error);
            return;
        }
        PreProcessing(_curScene, name);
        StartCoroutine(DoLoadSceneAsync(name, callback, onProgress, preRoutine, loadSceneMode));
    }

    /// <summary>
    /// 선행 코루틴을 끝까지 실행한 뒤 해당 씬을 페이드 효과로 비동기 로드합니다.
    /// 동일한 이름을 가지는 씬도 있을 수 있기 때문에 표준적으로는 인덱스 사용이 권장됩니다.
    /// </summary>
    /// <param name="index">씬 인덱스</param>
    /// <param name="preRoutine">씬 로드 시작 전에 끝까지 실행할 선행 코루틴</param>
    /// <param name="fadeOutTime">페이드 아웃 시간(초)</param>
    /// <param name="fadeInTime">페이드 인 시간(초)</param>
    /// <param name="callback">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="loadSceneMode">씬 로드 모드</param>
    public void LoadSceneAsyncWithFade(
        int index, IEnumerator preRoutine, float fadeOutTime = 0.45f, float fadeInTime = 0.45f,
        Action callback = null, Action<float> onProgress = null,
        LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidScene(index))
        {
            UDebug.Print($"유효하지 않은 씬 인덱스를 사용했습니다!", LogType.Error);
            return;
        }
        string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
        LoadSceneAsyncWithFade(scenePath, preRoutine, fadeOutTime, fadeInTime, callback, onProgress, loadSceneMode);
    }

    /// <summary>
    /// 선행 코루틴을 끝까지 실행한 뒤 해당 씬을 페이드 효과로 비동기 로드합니다.
    /// </summary>
    /// <param name="name">씬 이름</param>
    /// <param name="preRoutine">씬 로드 시작 전에 끝까지 실행할 선행 코루틴</param>
    /// <param name="fadeOutTime">페이드 아웃 시간(초)</param>
    /// <param name="fadeInTime">페이드 인 시간(초)</param>
    /// <param name="callback">씬 로드 완료 시 호출할 메서드</param>
    /// <param name="onProgress">씬 로드 진행율을 받을 메서드</param>
    /// <param name="loadSceneMode">씬 로드 모드</param>
    public void LoadSceneAsyncWithFade(
        string name, IEnumerator preRoutine, float fadeOutTime = 0.45f, float fadeInTime = 0.45f,
        Action callback = null, Action<float> onProgress = null,
        LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidScene(name))
        {
            UDebug.Print($"유효하지 않은 씬 이름을 사용했습니다!", LogType.Error);
            return;
        }
        PreProcessing(_curScene, name);
        StartCoroutine(DoLoadSceneAsyncWithFade
            (name, preRoutine, fadeOutTime, fadeInTime, callback, onProgress, loadSceneMode));
    }

    /// <summary>
    /// 전역 유지 오브젝트(플레이어·잠수함·UI 매니저)를 Resources에서 로드해 생성합니다.
    /// 부트 시퀀스가 모든 매니저를 초기화한 직후 1회 호출합니다.
    /// 플레이어·잠수함은 비활성 상태로 생성되며 씬별 토글 스크립트가 활성을 관리합니다.
    /// UI 매니저는 활성 상태로 생성하고 이후 관리는 CUIManager가 담당합니다.
    /// </summary>
    public void SpawnGlobalActors()
    {
        if (_player != null || _submarine != null || _uiManager != null)
        {
            UDebug.Print("전역 액터가 이미 생성되었습니다.", LogType.Assert);
            return;
        }

        _player = SpawnGlobalActor(K.RESOURCE_PLAYER_PATH, false);
        _submarine = SpawnGlobalActor(K.RESOURCE_SUBMARINE_PATH, false);
        _uiManager = SpawnGlobalActor(K.RESOURCE_UI_PATH, true);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 초기화 메서드
    protected override void Initialize()
    {
        // 생성 및 초기화
        _curScene = (EScene)SceneManager.GetActiveScene().buildIndex;

        // 씬에 따라 전역 액터를 켜고 끄기 위해 구독한다.
        // FirstLoadCo가 뿌리는 최초 이벤트도 받아야 하므로 코루틴 시작 전에 구독한다.
        CEventBus<OnSceneLoadEnd>.Subscribe(SceneLoadEndHandler);

        // 초기 부팅 시 씬 전환 이벤트 뿌리기
        if (_bootCo == null)
        {
            _bootCo = StartCoroutine(FirstLoadCo(EScene.Boot, _curScene));
        }
        else
        {
            UDebug.Print($"부트 코루틴이 중복 호출되었습니다.", LogType.Assert);
        }
    }

    // 씬 로드 완료 시 게임플레이 씬이면 전역 액터를 켜고, 아니면 끈다.
    private void SceneLoadEndHandler(OnSceneLoadEnd e)
    {
        bool active = e.nextScene.IsGameplay();
        UObject.SetActive(_player, active);
        UObject.SetActive(_submarine, active);
    }

    // Resources에서 프리팹을 로드해 인스턴스화하고 전역 유지시킵니다.
    private GameObject SpawnGlobalActor(string resourcePath, bool active)
    {
        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            UDebug.Print($"전역 액터 프리팹을 찾지 못했습니다: {resourcePath}", LogType.Error);
            return null;
        }

        GameObject instance = Instantiate(prefab);
        instance.name = prefab.name; // "(Clone)" 접미사 제거
        DontDestroyOnLoad(instance);
        UObject.SetActive(instance, active);
        return instance;
    }

    // 루트 오브젝트를 안전하게 가져오고 없으면 새로 생성
    private static Transform RootProvider(Transform root, string name)
    {
        if (root == null)
        {
            GameObject go = GameObject.Find(name);
            if (go == null)
            {
                root = UObject.Create(name).transform;
                UDebug.Print($"{name} 루트를 찾지 못하여 빈 오브젝트를 새로 생성했습니다.");
            }
            else
            {
                root = go.transform;
            }
        }
        return root;
    }

    // 씬 로드 선행처리
    private void PreProcessing(EScene prevScene, string nextScenePath)
    {
        IsSceneLoading = true;
        ClearStaticMember();
        EScene nextScene = (EScene)SceneUtility.GetBuildIndexByScenePath(nextScenePath);
        OnSceneLoadStart.Publish(prevScene, nextScene);
    }
    // 씬 로드 후처리
    private void PostProcessing(EScene prevScene, string nextScenePath)
    {
        EScene nextScene = (EScene)SceneUtility.GetBuildIndexByScenePath(nextScenePath);
        // 루트 생성
        {
            Transform root;
            root = NormalObjectRoot;
            root = PoolingObjectRoot;
        }
        PublishLoadEnd(prevScene, nextScene);
        _curScene = nextScene;
        IsSceneLoading = false;
    }

    // 씬 유효성 검증
    private static bool IsValidScene(int index)
    {
        // 존재할 수 없는 인덱스인지 검사
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            UDebug.Print($"존재하지 않는 씬 인덱스({index})를 호출했습니다.");
            return false;
        }
        return true;
    }
    private static bool IsValidScene(string name)
    {
        // 존재할 수 없는 인덱스인지 검사
        if (name.IsBlank() || !Application.CanStreamedLevelBeLoaded(name))
        {
            UDebug.Print($"존재하지 않는 씬 이름({name})을 호출했습니다.");
            return false;
        }
        return true;
    }

    // delay(초)를 선행 코루틴 형태로 변환. delay가 0 이하면 null(선행 단계 없음).
    private IEnumerator DelayPrologue(float delay)
    {
        if (delay <= 0f) return null;

        return WaitPrologue(delay);
    }

    private IEnumerator WaitPrologue(float delay)
    {
        yield return UCoroutine.GetWait(delay);
    }

    // 비동기 코루틴 (prologue: 로드 전에 끝까지 실행할 선행 코루틴, 없으면 null)
    private IEnumerator DoLoadSceneAsync(
        string name, Action callback, Action<float> onProgress, IEnumerator prologue, LoadSceneMode loadSceneMode)
    {
        // 선행 코루틴이 있으면 끝까지 대기
        if (prologue != null)
        {
            yield return StartCoroutine(prologue);
        }
        // 유니티 기본 로드 함수 (비동기 대기)
        var asyncOperation = SceneManager.LoadSceneAsync(name, loadSceneMode);
        // 유니티 비동기 씬 로드 유틸리티
        yield return UCoroutine.WaitAsyncOperation(asyncOperation, onProgress);
        // 씬 로드 완료 → 콜백 호출
        callback?.Invoke();
        PostProcessing(_curScene, name);
    }

    // 비동기 페이드 코루틴 (prologue: 로드 전에 끝까지 실행할 선행 코루틴, 없으면 null)
    private IEnumerator DoLoadSceneAsyncWithFade(
        string name, IEnumerator prologue, float fadeOutTime, float fadeInTime,
        Action callback, Action<float> onProgress, LoadSceneMode loadSceneMode)
    {
        // 선행 코루틴이 있으면 끝까지 대기
        if (prologue != null)
        {
            yield return StartCoroutine(prologue);
        }
        // 유니티 기본 로드 함수 (비동기 대기)
        var asyncOperation = SceneManager.LoadSceneAsync(name, loadSceneMode);
        asyncOperation.allowSceneActivation = false; // 씬 로드가 완료되어도 대기
        // 페이드 시작
        UFade.FadeOut(fadeOutTime, true);
        // 모두 완료될때까지 대기
        while (asyncOperation.progress < 0.9f || UFade.IsFading)
        {
            onProgress?.Invoke(asyncOperation.progress);
            yield return null;
        }
        asyncOperation.allowSceneActivation = true;
        onProgress?.Invoke(1f);
        // 씬 전환이 완전히 종료될때까지 대기
        while (!asyncOperation.isDone)
        {
            yield return null;
        }
        // 씬 전환 완료
        callback?.Invoke();
        PostProcessing(_curScene, name);
        // 새로운 씬에서 페이드 인
        UFade.FadeIn(fadeInTime, true);
    }

    private void PublishLoadEnd(EScene prevScene, EScene nextScene)
    {
        OnSceneLoadEnd.Publish(prevScene, nextScene);
    }

    // 유니티 에디터 용도, 어느 씬에서 시작하던 그 씬을 로드하는 효과를 내기 위함
    private IEnumerator FirstLoadCo(EScene prevScene, EScene nextScene)
    {
        while (!CBootManager.IsInitialized)
        {
            yield return null;
        }
        PublishLoadEnd(prevScene, nextScene);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ClearObjectRoot()
    {
        _normalObjectRoot = null;
        _enableObjectRoot = null;
    }

    private static void ClearStaticMember()
    {
        ClearObjectRoot();
        _player = null;
        _submarine = null;
        _uiManager = null;
    }

    // 플레이 모드가 종료될 경우 호출
    private void OnApplicationQuit()
    {
        ClearStaticMember();
    }

    protected override void OnDestroy()
    {
        CEventBus<OnSceneLoadEnd>.Unsubscribe(SceneLoadEndHandler);
        base.OnDestroy();
    }
    #endregion
}
