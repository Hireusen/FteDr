using UnityEngine;

/// <summary>
/// 상속받은 클래스는 항상 싱글톤을 보장합니다.
/// IsGlobal 프로퍼티로 씬 전환 시 파괴 여부를 결정할 수 있습니다.
/// </summary>
public abstract class ASingleton<T> : AMono where T : AMono
{
    private static T _instance = null;
    private static bool _isQuitting = false;
    private bool _isInitialized = false;

    public bool IsInitialized => _isInitialized;
    public virtual bool IsGlobal { get; } = true;
    public static bool IsQuitting => _isQuitting;

    public static T Ins
    {
        get
        {
            if (_instance == null)
            {
                // 플레이 모드가 종료 중
                if (_isQuitting)
                {
                    UDebug.Print($"싱글톤({typeof(T).ToString()})이 지연 생성을 요청받았지만 플레이 종료중이므로 무시합니다.");
                    return null;
                }
                // 씬에 있는 싱글톤 컴포넌트를 우선 탐색
                T singleton = FindAnyObjectByType<T>();
                if (singleton != null)
                {
                    _instance = singleton;
                    UDebug.Print($"싱글톤({singleton.gameObject.name}<{typeof(T).ToString()}>)을 탐색하여 등록했습니다.", LogType.Log, _instance);
                }
                // 씬에 배치되어 있지 않으므로 생성
                else
                {
                    GameObject go = new GameObject(typeof(T).ToString());
                    _instance = go.AddComponent<T>();
                    UDebug.Print($"인스턴스가 호출당하여 글로벌 싱글톤({go.name}<{typeof(T).ToString()}>)을 생성했습니다.", LogType.Log, _instance);
                }
                // 초기화 성공
                if (_instance is ASingleton<T> instance)
                {
                    if (instance.IsGlobal)
                    {
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                    instance.EntryInitialize();
                }
                // 초기화 실패
                else
                {
                    UDebug.Print($"타입이 일치하지 않는 알 수 없는 오류가 발생했습니다.", LogType.Error, _instance);
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 인스턴스 생성 시 필요한 초기화 로직을 구현하는 함수
    /// </summary>
    protected abstract void Initialize();

    public void EntryInitialize()
    {
        if (_isInitialized) return;

        Initialize();
        _isInitialized = true;
        UDebug.Print($"싱글톤({gameObject.name}<{typeof(T).ToString()}>)의 초기화 메서드를 실행 완료했습니다.");
    }

    /// <summary>
    /// Singleton Awake Function
    /// </summary>
    private void Awake()
    {
        // 중복 싱글톤 방어
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            UDebug.Print($"중복 싱글톤({gameObject.name}<{typeof(T).ToString()}>)을 삭제했습니다.");
            return;
        }

        // 싱글톤 오브젝트 생성
        _instance = this as T;
        if (IsGlobal)
        {
            DontDestroyOnLoad(gameObject);
        }
        EntryInitialize();

        UDebug.Print($"새로운 싱글톤({gameObject.name}<{typeof(T).ToString()}>)을 생성했습니다.");
    }

    /// <summary>
    /// GlobalSingleton OnDestroy Function
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
            UDebug.Print($"싱글톤 인스턴스({typeof(T).ToString()})가 파괴되었습니다.");
        }
    }

    // 플레이 모드가 종료될 경우 호출
    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }
}
