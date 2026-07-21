using System.Collections;
using UnityEngine;

/// <summary>
/// 각 스테이지 씬에 배치되어 스테이지 진입 시 초기화를 담당하는 컴포넌트입니다.
/// </summary>
public class CStageManager : ASingleton<CStageManager>
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Tooltip("플레이어 준비를 기다리는 최대 시간(초). 초과 시 스폰을 포기하고 에러 로그를 남깁니다.")]
    [SerializeField] private float _spawnWaitTimeout = 5f;

    [Header("리스폰 연출")]
    [Tooltip("리스폰 시 화면이 어두워지는 시간(초)")]
    [SerializeField] private float _respawnFadeOutTime = 0.5f;
    [Tooltip("리스폰 시 화면이 다시 밝아지는 시간(초)")]
    [SerializeField] private float _respawnFadeInTime = 0.5f;

    [Header("참조 연결")]
    [SerializeField] private GameObject _arriveCam;
    [SerializeField] private GameObject _dest;
    [SerializeField] private GameObject _downPos;
    [SerializeField] private GameObject _upPos;
    #endregion

    private CSubMarineUpDown _submarine;

    #region ─────────────────────────▶ 공개 함수 ◀─────────────────────────
    public GameObject ArriveCam => _arriveCam;
    public GameObject Dest => _dest;
    public GameObject DownPos => _downPos;
    public GameObject UpPos => _upPos;
    public override bool IsGlobal => false;
    /// <summary>
    /// 화면을 어둡게 한 뒤 플레이어를 스폰 지점으로 되돌리고 다시 밝히는 리스폰 연출을 실행합니다.
    /// 사망 처리·디버그 강제 귀환 등 외부에서 호출합니다.
    /// </summary>
    public void RespawnPlayer()
    {
        StartCoroutine(RespawnCo());
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void Initialize()
    {
        var player = CGameManager.Player;
        EScene curScene = CGameManager.Ins.Scene;
        if (curScene.IsGameplay())
        {
            UDebug.Print("게임 플레이 씬입니다!!!");
            CGameManager.Player.SetActive(true);
            CGameManager.Submarine.SetActive(true);
        }
        else
        {
            UDebug.Print($"이외 씬입니다!!! {curScene}");
        }
    }

    // 페이드 아웃 → 스폰 지점 이동 → 페이드 인 순서의 리스폰 연출 코루틴입니다.
    private IEnumerator RespawnCo()
    {
        if (_submarine == null) _submarine = GetSubmarine();
        if (_submarine == null) yield break;

        // 화면을 어둡게 덮는다. 완료될 때까지 대기.
        UFade.FadeOut(_respawnFadeOutTime, true);
        while (UFade.IsFading) yield return null;

        // 화면이 완전히 가려진 동안 플레이어를 스폰 지점으로 이동시킨다.
        _submarine.SpawnPlayer();
        UPlayer.ResetForNew();

        // 다시 화면을 밝힌다.
        UFade.FadeIn(_respawnFadeInTime, true);
        while (UFade.IsFading) yield return null;
    }

    // 전역 플레이어가 활성화·준비될 때까지 기다린 뒤, 잠수함을 통해 스폰을 요청한다.
    // 플레이어 활성화(CGameManager 토글)와 이 코루틴의 순서를 코드가 흡수하기 위한 대기.
    private IEnumerator SpawnPlayerWhenReadyCo()
    {
        float timer = 0f;
        while (!IsPlayerReady())
        {
            if (timer >= _spawnWaitTimeout)
            {
                UDebug.Print(
                    "플레이어가 제한 시간 내에 준비되지 않아 스폰을 포기합니다." +
                    "게임플레이 씬에서 전역 플레이어가 활성화되는지 확인하세요.",
                    LogType.Error);
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        CSubMarineUpDown submarine = GetSubmarine();
        if (submarine == null) yield break;
        _submarine = submarine;

        // 잠수함 위치 스냅
        CGameManager.Submarine.transform.position = _dest.transform.position;
        submarine.SpawnPlayer();
    }

    // 전역 플레이어가 존재하고 활성화되어 조작 컴포넌트까지 준비되었는지 검사한다.
    private bool IsPlayerReady()
    {
        GameObject playerObj = CGameManager.Player;
        if (playerObj == null) return false;
        if (!playerObj.activeInHierarchy) return false;
        return playerObj.GetComponent<CPlayerController>() != null;
    }

    // 전역 잠수함에서 CSubMarineUpDown을 가져온다.
    private CSubMarineUpDown GetSubmarine()
    {
        GameObject submarineObj = CGameManager.Submarine;
        if (submarineObj == null)
        {
            UDebug.Print("전역 잠수함(CGameManager.Submarine)이 존재하지 않습니다.", LogType.Error);
            return null;
        }

        CSubMarineUpDown submarine = submarineObj.GetComponent<CSubMarineUpDown>();
        if (submarine == null)
        {
            UDebug.Print("잠수함에 CSubMarineUpDown이 없습니다.", LogType.Error);
        }
        return submarine;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        StartCoroutine(SpawnPlayerWhenReadyCo());
    }
    #endregion
}
