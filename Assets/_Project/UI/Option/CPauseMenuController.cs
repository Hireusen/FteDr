using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일시정지 창(Resume / Setting / Title / Quit)을 담당하는 컨트롤러입니다.
/// ESC 입력으로 열고 닫기를 토글하며, 시간 정지는 창의 활성/비활성에 자동으로 연동됩니다.
///
/// 타이틀 복귀·게임 종료는 되돌릴 수 없으므로 [가방 확인 → 저장 → 실행] 순서를 거칩니다.
/// 가방에 아이템이 남아있으면 확인 팝업으로 되묻고, 저장에 실패하면 행동을 취소하고 안내만 합니다.
/// </summary>
public class CPauseMenuController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("Pause Buttons")]
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnTutorial;
    [SerializeField] private Button _btnTitle;
    [SerializeField] private Button _btnQuit;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private bool _isExiting;                     // 나가기 확정 후 중복 클릭 방지
    private CPlayerController _cachedController; // 플레이어 상태(잠수함 안/수중) 판정용
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_btnResume != null)
        {
            _btnResume.onClick.AddListener(() => OnRequestCloseUI.Publish(EUI.PauseMenuWindow));
        }

        if (_btnOptions != null)
        {
            _btnOptions.onClick.AddListener(() => OnRequestOpenUI.Publish(EUI.SettingsWindow));
        }

        if (_btnTutorial != null)
        {
            _btnTutorial.onClick.AddListener(() => OnRequestOpenUI.Publish(EUI.TutorialWindow));
        }

        if (_btnTitle != null)
        {
            _btnTitle.onClick.AddListener(OnClickTitle);
        }

        if (_btnQuit != null)
        {
            _btnQuit.onClick.AddListener(OnClickQuit);
        }
    }

    private void OnEnable()
    {
        // 이 창은 전역 UI(DontDestroyOnLoad)에 속해 씬이 바뀌어도 파괴되지 않는다.
        // 타이틀로 나간 뒤 다시 스테이지에 들어오면 플래그가 남아 나가기가 영구히 막히므로, 창을 열 때마다 초기화한다.
        _isExiting = false;

        // 이 창이 열려있는 동안에는 게임 시간을 멈춘다. (버튼으로 열든 ESC로 열든 동일하게 적용)
        Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void OnClickTitle() => RequestExit(DoLoadTitle);

    private void OnClickQuit() => RequestExit(DoQuit);

    private void DoLoadTitle()
    {
        Time.timeScale = 1f; // 씬 전환 전에 명시적으로 복구 (안전장치)
        UScene.LoadWithFade(EScene.Title, onProgress: p => OnSceneLoadProgress.Publish(p));
    }

    private void DoQuit()
    {
        Time.timeScale = 1f; // 종료 전에 명시적으로 복구 (에디터에서 다음 플레이에 남는 것 방지)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 가방에 아이템이 남아있으면 확인 팝업으로 되묻고, 아니면 바로 진행한다.
    private void RequestExit(Action onExit)
    {
        if (_isExiting || UScene.IsLoading) return; // 이미 처리 중이면 무시

        string warning = BuildBagWarning();

        // 경고할 게 없거나 확인 팝업이 씬에 없으면 바로 진행한다. (팝업 부재로 나가기 자체가 막히면 안 된다)
        if (warning == null || !CConfirmPopup.TryShow(warning, () => ProceedExit(onExit), confirmLabel: "나가기"))
        {
            ProceedExit(onExit);
        }
    }

    // 저장을 마친 뒤 실제로 나간다. 저장에 실패하면 아무것도 파괴하지 않고 안내만 한 뒤 중단한다.
    private void ProceedExit(Action onExit)
    {
        // 드롭보다 먼저 저장해서, 저장이 불가능한 상황이면 가방을 잃지 않고 되돌아갈 수 있게 한다.
        if (!USave.TrySaveAll(out string failedTarget))
        {
            OnRequestNotice.Publish($"{failedTarget} 저장에 실패하여 중단했습니다.\n잠시 후 다시 시도해 주세요.", 3f);
            UDebug.Print($"[일시정지] {failedTarget} 저장 실패로 나가기를 취소했습니다.", LogType.Error, gameObject);
            return;
        }

        // 수중에서 나가는 것은 사망과 동일하게 취급한다. 떨어뜨린 수집품을 반영하기 위해 다시 저장한다.
        // 여기서 실패하더라도 위의 저장이 이미 남아있으므로(가방은 원래 영속 데이터가 아님) 나가기는 계속 진행한다.
        if (!IsInSubmarine())
        {
            DropBagItems();
            USave.TrySaveAll(out _);
        }

        _isExiting = true;
        onExit();
    }

    // 가방이 비어있으면 null, 아니면 현재 위치에 맞는 경고 문구를 반환한다.
    private string BuildBagWarning()
    {
        int count = UPlayer.BagItems.Count;
        if (count <= 0) return null;

        return IsInSubmarine()
            ? $"판매하지 않은 아이템이 {count}개 있습니다.\n그래도 나가시겠습니까?"
            : $"지금 나가면 가방에 든 아이템 {count}개를 잃습니다.\n그래도 나가시겠습니까?";
    }

    // 플레이어가 잠수함 안(마른 땅)에 있는지 여부. 판정할 수 없으면 아이템을 잃지 않도록 True로 간주한다.
    private bool IsInSubmarine()
    {
        CPlayerController controller = GetController();
        return controller == null || controller.CurrentState == EPlayerState.OnGround;
    }

    // 사망 처리(CStageManager.RespawnPlayer)와 동일하게 가방의 아이템을 현재 위치에 떨어뜨린다.
    private void DropBagItems()
    {
        GameObject player = CGameManager.Player;
        if (player == null) return;

        CPlayerDropConfig dropConfig = player.GetComponent<CPlayerDropConfig>();
        CPlayerManager.Ins.DropAllBagItems(player.transform.position, dropConfig);
    }

    // 플레이어는 전역 액터라 씬마다 새로 찾을 필요가 없으므로 한 번만 캐싱한다.
    private CPlayerController GetController()
    {
        if (_cachedController == null && CGameManager.Player != null)
        {
            _cachedController = CGameManager.Player.GetComponentInChildren<CPlayerController>(true);
        }
        return _cachedController;
    }
    #endregion
}
