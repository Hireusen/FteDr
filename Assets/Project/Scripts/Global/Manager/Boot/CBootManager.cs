using System.Collections;
using UnityEngine;

/// <summary>
/// 함수를 호출당할 시 매니저를 순서대로 생성합니다.
/// </summary>
public sealed class CBootManager : MonoBehaviour
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Coroutine _coBoot;
    private static bool _isInitialized;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public static bool IsInitialized => _isInitialized;

    // 외부에서 호출할 함수
    public void StartBootSequence(GameObject root)
    {
        if (_coBoot != null)
        {
            UDebug.Print("부트 시퀀스가 중복 호출되었습니다.", LogType.Assert, gameObject);
            return;
        }
        if (_isInitialized)
        {
            UDebug.Print("부트 시퀀스가 이미 실행되었습니다.", LogType.Assert, gameObject);
            return;
        }

        _coBoot = StartCoroutine(CoInitialize(root));
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 진입점
    private IEnumerator CoInitialize(GameObject root)
    {
        UDebug.Print("▷ 매니저 생성을 시작합니다. ◁");

        // 매니저 생성 및 초기화
        ManagerSpawner(root);
        UDebug.Print("▷ 글로벌 프리펩 생성을 시작합니다. ◁");

        // 완료
        yield return null;

        _isInitialized = true;
        _coBoot = null;
        UDebug.Print("▷ 부트 시퀀스가 완료되었습니다. ◁");
    }

    private void ManagerSpawner(GameObject root)
    {
        var inputManager = root.GetOrAddComponent<CInputManager>();
        inputManager.EntryInitialize();
        var frameManager = root.GetOrAddComponent<CFrameManager>();
        frameManager.EntryInitialize();
        var gameManager = root.GetOrAddComponent<CGameManager>();
        gameManager.EntryInitialize();
        var optionManager = root.GetOrAddComponent<CLocalOptionManager>();
        optionManager.EntryInitialize();
        var soundManager = root.GetOrAddComponent<CSoundManager>();
        soundManager.EntryInitialize();
        var progressManager = root.GetOrAddComponent<CProgressManager>();
        progressManager.EntryInitialize();
        var playerManager = root.GetOrAddComponent<CPlayerManager>();
        playerManager.EntryInitialize();
    }
    #endregion

    // 플레이 모드가 종료될 경우 호출
    private void OnApplicationQuit()
    {
        _isInitialized = false;
    }
}
