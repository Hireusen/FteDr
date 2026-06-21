using UnityEngine;

/// <summary>
/// 게임 시작 전 필요한 초기화 작업을 수행합니다.
/// 씬에 배치할 필요 없으며 즉시 로드됩니다.
/// </summary>
public static class CBootstrapper
{
    // Awake 이전에 호출
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Run()
    {
        // 매니저를 담는 루트 오브젝트
        GameObject root = new GameObject(K.NAME_GLOBAL_MANAGER_ROOT);
        Object.DontDestroyOnLoad(root);
        // 부트 매니저 생성 및 실행
        var bootManager = root.AddComponent<CBootManager>();
        bootManager.StartBootSequence(root);
    }
}
