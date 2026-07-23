using System.Collections;
using UnityEngine;

/// <summary>
/// 각 게임플레이 씬에 하나씩 배치되어 스테이지 진입 시 초기화(플레이어 스폰)와
/// 리스폰 연출을 담당하는 씬 소속 컴포넌트입니다.
/// 씬을 넘어 유지되지 않으며, 외부 접근은 static Current로 합니다.
/// 플레이어·잠수함의 씬별 활성/비활성 토글은 CGameManager가 전담합니다.
/// </summary>
public class CStageManager : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Tooltip("플레이어 준비를 기다리는 최대 시간(초). 초과 시 스폰을 포기하고 에러 로그를 남깁니다.")]
    [SerializeField] private float _spawnWaitTimeout = 5f;

    [Header("리스폰 연출")]
    [SerializeField] private CUnderwaterEffect _camEffect;
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

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CSubMarineUpDown _submarine;

    // 리스폰 연출 진행 상태. 재진입 방어와 파괴 시 화면 복구에 사용한다.
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

    /// <summary>
    /// 화면을 어둡게 한 뒤 플레이어를 스폰 지점으로 되돌리고 다시 밝히는 리스폰 연출을 실행합니다.
    /// 사망 처리·디버그 강제 귀환 등 외부에서 호출합니다.
    /// 이미 리스폰 연출이 진행 중이면 무시합니다.
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
            _camEffect.StartDeath();
            UFade.FadeOut(_respawnFadeOutTime, true);
            while (UFade.IsFading) yield return null;

            // 화면이 완전히 가려진 동안 플레이어를 스폰 지점으로 이동시킨다.
            _submarine.SpawnPlayer();
            UPlayer.ResetForNew();

            // 다시 화면을 밝힌다.
            _camEffect.StopDeath();
            UFade.FadeIn(_respawnFadeInTime, true);
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
    // 플레이어 활성화(CGameManager 토글)와 이 코루틴의 순서를 코드가 흡수하기 위한 대기.
    private IEnumerator SpawnPlayerWhenReadyCo()
    {
        float timer = 0f;
        while (!IsPlayerReady())
        {
            if (timer >= _spawnWaitTimeout)
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

        // 잠수함이 이미 연출 이동 중이면(스테이지 간 이동), MoveSubmarine의 onComplete가
        // ArriveSubmarine을 이미 예약해 두었으므로 여기서는 아무것도 하지 않는다.
        if (submarine.IsMoving) yield break;

        // 첫 진입(타이틀 → 첫 스테이지): 연출을 안 탔으므로 도착 연출을 직접 트리거한다.
        // 하강 진입이므로 goDeeper=true로 도착 연출을 재생한다.
        submarine.ArriveSubmarine(3f, true);
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
    // 씬에 진입할 때 자신을 현재 스테이지 매니저로 등록한다.
    // 나중에 활성화된 것이 Current가 되므로, 씬 전환 방향과 자연히 일치한다.
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

    public void ExecuteUpdateFrame()
    {
        if (UPlayer.IsFuelLow)
        {
            _camEffect.SetOxygenCrisis(true);
            if (UPlayer.CurrentFuel > 0) return;

            RespawnPlayer();
        }
        else
        {
            _camEffect.SetOxygenCrisis(false);
        }
    }
    #endregion
}
