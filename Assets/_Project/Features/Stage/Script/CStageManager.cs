using System.Collections;
using UnityEngine;

/// <summary>
/// 각 게임플레이 씬에 하나씩 배치되는 매니저
/// </summary>
public class CStageManager : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("참조 연결")]
    [SerializeField] private GameObject _arriveCam;
    [SerializeField] private GameObject _dest;
    [SerializeField] private GameObject _downPos;
    [SerializeField] private GameObject _upPos;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const float SPAWN_WAIT_TIMEOUT = 1.5f;
    private const float RESPAWN_FADEOUT_TIME = 1.5f;
    private const float RESPAWN_FADEIN_TIME = 1.5f;

    // 리스폰 연출 진행 상태. 재진입 방어와 파괴 시 화면 복구에 사용한다.
    private CSubMarineUpDown _submarine;
    private bool _isRespawning = false;
    private Coroutine _respawnRoutine;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 씬에 배치된 스테이지 매니저입니다. 게임플레이 씬이 아니면 null입니다.</summary>
    public static CStageManager Current { get; private set; }

    public GameObject ArriveCam => _arriveCam;
    public GameObject Dest => _dest;
    public GameObject DownPos => _downPos;
    public GameObject UpPos => _upPos;
    public CSubMarineUpDown SubMarine => _submarine;

    public EUpdatePriority UpdatePriority => EUpdatePriority.First;

    public void ExecuteUpdateFrame()
    {
        if (UPlayer.CurrentFuel <= 0)
        {
            RespawnPlayer();
        }
    }

    /// <summary>
    /// 화면을 어둡게 한 뒤 플레이어를 스폰 지점으로 되돌리고 다시 밝히는 리스폰 연출을 실행합니다.
    /// </summary>
    public void RespawnPlayer()
    {
        if (_isRespawning)
        {
            UDebug.Print("리스폰 연출이 이미 진행 중이라 요청을 무시합니다.", LogType.Warning);
            return;
        }
        _respawnRoutine = StartCoroutine(RespawnCo());
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 페이드 아웃 → 스폰 지점 이동 → 페이드 인 순서의 리스폰 연출 코루틴입니다.
    private IEnumerator RespawnCo()
    {
        _isRespawning = true;
        try
        {
            if (_submarine == null) _submarine = GetSubmarine();
            if (_submarine == null) yield break;

            // 화면을 어둡게 덮는다. 완료될 때까지 대기.
            UFade.FadeOut(RESPAWN_FADEOUT_TIME, true);
            while (UFade.IsFading) yield return null;

            // 화면이 완전히 가려진 동안 플레이어를 스폰 지점으로 이동시킨다.
            _submarine.SpawnPlayer();
            UPlayer.ResetForNew();

            // 다시 화면을 밝힌다.
            UFade.FadeIn(RESPAWN_FADEIN_TIME, true);
            while (UFade.IsFading) yield return null;
        }
        finally
        {
            // 정상 완료·중단 무관하게 상태를 정리한다.
            _isRespawning = false;
            _respawnRoutine = null;
        }
    }

    // 전역 플레이어가 활성화·준비될 때까지 기다린 뒤, 잠수함을 통해 스폰을 요청한다.
    private IEnumerator SpawnPlayerWhenReadyCo()
    {
        float timer = 0f;
        while (!IsPlayerReady())
        {
            if (timer >= SPAWN_WAIT_TIMEOUT)
            {
                UDebug.Print(
                    "플레이어가 제한 시간 내에 준비되지 않아 스폰을 포기합니다. " +
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

        // 잠수함 위치를 이 씬의 도착점으로 스냅한 뒤 플레이어를 스폰한다.
        if (_dest != null) CGameManager.Submarine.transform.position = _dest.transform.position;
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
    protected override void OnEnable()
    {
        base.OnEnable();
        Current = this;
    }

    private void Start()
    {
        StartCoroutine(SpawnPlayerWhenReadyCo());
    }

    private void OnDestroy()
    {
        // 리스폰 연출 도중 파괴되면(씬 전환 등) 화면이 검게 덮인 채 남을 수 있으므로 복구한다.
        if (_isRespawning)
        {
            if (_respawnRoutine != null) StopCoroutine(_respawnRoutine);
            UFade.StopFade();
            _isRespawning = false;
        }

        // 자신이 현재 등록된 매니저일 때만 해제한다.
        if (Current == this) Current = null;
    }
    #endregion
}
