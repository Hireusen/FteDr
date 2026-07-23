using Cinemachine;
using System.Collections;
using UnityEngine;

/// <summary>
/// 잠수함의 상승·하강 이동과 도착 연출을 제어하는 컴포넌트입니다.
/// </summary>
public class CSubMarineUpDown : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("참조 연결")]
    [SerializeField] private CinemachineVirtualCamera _controlCam;
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private GameObject _timelineUp;
    [SerializeField] private GameObject _timelineDown;

    [Header("필수 정보")]
    [SerializeField] private EScene _firstGameScene = EScene.Stage_1;
    [SerializeField] private EScene _lastGameScene = EScene.Stage_4;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const float MAX_SPEED = 10f;
    private const float ACCELERATION = 5f;

    private bool _moveOn = false;
    private CinemachineVirtualCamera _arriveCam;
    private CPlayerController _playerCtrl;
    private GameObject _activeTimeline; // StartCutScene에서 켠 타임라인. 도착 연출 종료 시 끈다.
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 잠수함 이동/도착 연출이 진행 중인지 여부입니다.</summary>
    public bool IsMoving => _moveOn;

    /// <summary>
    /// 잠수함 이동 연출을 시작하는 진입점입니다. 외부(입력·UI 등)에서 호출합니다.
    /// 방향에 맞는 타임라인을 활성화하면, 타임라인의 Signal이 MoveSubmarine을 자동 호출합니다.
    /// </summary>
    /// <param name="goDeeper">true면 하강, false면 상승</param>
    public void StartCutScene(bool goDeeper)
    {
        if (_moveOn)
        {
            UDebug.Print("이미 잠수함 연출이 진행 중입니다.", LogType.Warning);
            return;
        }

        if (!CanMove(goDeeper))
        {
            UDebug.Print($"현재 씬에서 {(goDeeper ? "하강" : "상승")}할 수 없습니다.", LogType.Warning);
            return;
        }

        // 방향에 맞는 타임라인을 켜면 Play On Awake로 재생되고,
        // 타임라인 내부 Signal이 MoveSubmarine(goDeeper)을 호출한다.
        _activeTimeline = goDeeper ? _timelineDown : _timelineUp;
        if (_activeTimeline == null)
        {
            UDebug.Print($"{(goDeeper ? "_timelineDown" : "_timelineUp")}이 인스펙터에 할당되지 않았습니다.", LogType.Error);
            return;
        }

        UObject.SetActive(_activeTimeline, true);
    }

    /// <summary>
    /// 현재 씬에서 지정한 방향으로 잠수함을 이동시킬 수 있는지 검사합니다.
    /// </summary>
    public bool CanMove(bool goDeeper)
    {
        EScene curScene = UScene.Current;
        if (curScene < _firstGameScene || curScene > _lastGameScene) return false;
        if (goDeeper && curScene >= _lastGameScene) return false;
        if (!goDeeper && curScene <= _firstGameScene) return false;

        return true;
    }

    /// <summary>
    /// 플레이어를 잠수함 내부의 SpawnPoint로 이동시킵니다.
    /// </summary>
    public void SpawnPlayer()
    {
        if (_playerSpawnPoint == null)
        {
            UDebug.Print("_playerSpawnPoint가 인스펙터에 할당되지 않았습니다.", LogType.Error);
            return;
        }

        CPlayerController player = Player;
        if (player == null) return;

        player.Teleport(_playerSpawnPoint);
    }

    /// <summary>
    /// 잠수함을 지정한 방향으로 이동시키고 다음/이전 씬을 로드합니다.
    /// </summary>
    public void MoveSubmarine(bool goDeeper)
    {
        if (_moveOn) return;
        _moveOn = true;

        // 연출 동안 플레이어를 통째로 숨긴다. (플레이어 카메라도 함께 꺼져 연출 카메라만 남는다)
        if (CGameManager.Player != null) CGameManager.Player.SetActive(false);

        StartCoroutine(MoveSubmarineSlowStartCo(goDeeper, 1.5f));
        UFade.FadeOut(1.5f, true);

        if (goDeeper)
            UScene.NextLoad(delay: 2f, onComplete: () => ArriveSubmarine(3f, goDeeper));
        else
            UScene.PrevLoad(delay: 2f, onComplete: () => ArriveSubmarine(3f, goDeeper));
    }

    /// <summary>
    /// 씬 로드 후 잠수함을 시작점에서 도착점으로 이동시키는 도착 연출을 재생합니다.
    /// </summary>
    /// <param name="duration">(초)</param>
    public void ArriveSubmarine(float duration, bool goDeeper = true)
    {
        UFade.FadeIn(2f, true);
        // MoveSubmarine의 가속 코루틴이 끝났을 경우에만 진행
        if (_moveOn)
        {
            UDebug.Print("잠수함이 아직 이동 중이라 도착 연출을 건너뜁니다.", LogType.Warning);
            return;
        }
        _moveOn = true;

        var stage = CStageManager.Current;
        if (stage == null)
        {
            UDebug.Print("현재 씬에 CStageManager가 없습니다. 도착 연출을 중단합니다.", LogType.Error);
            _moveOn = false;
            return;
        }
        if (stage.ArriveCam == null || stage.Dest == null || stage.DownPos == null || stage.UpPos == null)
        {
            UDebug.Print(
                $"잠수함 도착 연출에 필요한 오브젝트를 찾지 못했습니다. " +
                $"(캠:{stage.ArriveCam != null}, 중간점:{stage.Dest != null}, " +
                $"다운 포스:{stage.DownPos != null}, 업 포스:{stage.UpPos != null}) " +
                $"오브젝트 이름 또는 대소문자를 계층과 일치시켜야 합니다.",
                LogType.Error);
            _moveOn = false;
            return;
        }

        _arriveCam = stage.ArriveCam.GetComponent<CinemachineVirtualCamera>();
        if (_arriveCam == null)
        {
            UDebug.Print($"{stage.ArriveCam}에 CinemachineVirtualCamera가 없습니다.", LogType.Error);
            _moveOn = false;
            return;
        }

        if (_controlCam == null)
        {
            UDebug.Print("_controlCam이 인스펙터에 할당되지 않았습니다.", LogType.Error);
            _moveOn = false;
            return;
        }

        _arriveCam.LookAt = transform;
        _arriveCam.Priority = 20;
        _controlCam.Priority = 10;

        // 도착 연출 동안 플레이어를 숨긴다. (첫 진입 경로에서도 확실히 꺼지도록)
        if (CGameManager.Player != null) CGameManager.Player.SetActive(false);

        Vector3 startPos = goDeeper ? stage.UpPos.transform.position : stage.DownPos.transform.position;
        StartCoroutine(MoveStartToDestCo(startPos, stage.Dest.transform.position, duration));
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // CGameManager가 보유한 전역 플레이어에서 컨트롤러를 1회 캐싱해 반환한다.
    private CPlayerController Player
    {
        get
        {
            if (_playerCtrl != null) return _playerCtrl;

            GameObject playerObj = CGameManager.Player;
            if (playerObj == null)
            {
                UDebug.Print("전역 플레이어(CGameManager.Player)가 아직 생성되지 않았습니다.", LogType.Error);
                return null;
            }

            _playerCtrl = playerObj.GetComponent<CPlayerController>();
            if (_playerCtrl == null)
            {
                UDebug.Print("플레이어에 CPlayerController가 없습니다.", LogType.Error);
            }
            return _playerCtrl;
        }
    }

    // 잠수함이 서서히 가속하며 위/아래로 출발하는 연출 코루틴입니다.
    private IEnumerator MoveSubmarineSlowStartCo(bool goDeeper, float duration)
    {
        float timer = 0f;
        float speed = 0f;
        float dir = goDeeper ? -1f : 1f;

        while (timer < duration)
        {
            speed = Mathf.MoveTowards(speed, MAX_SPEED, ACCELERATION * Time.deltaTime);
            transform.position += Vector3.up * (dir * speed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
        _moveOn = false;
    }

    // 시작점에서 도착점까지 잠수함을 이징 이동시키고, 끝나면 카메라 우선순위를 되돌립니다.
    private IEnumerator MoveStartToDestCo(Vector3 startPos, Vector3 destPos, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            float t = timer / duration;
            t = 1f - (1f - t) * (1f - t); // ease-out quad

            transform.position = Vector3.Lerp(startPos, destPos, t);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = destPos;

        if (_arriveCam != null) _arriveCam.Priority = 10;
        if (_controlCam != null) _controlCam.Priority = 20;

        // 연출 종료: 플레이어를 SpawnPoint 위치에 놓고 다시 활성화한다.
        if (_playerSpawnPoint != null && CGameManager.Player != null)
        {
            CGameManager.Player.transform.SetPositionAndRotation(
                _playerSpawnPoint.position, _playerSpawnPoint.rotation);
            CGameManager.Player.SetActive(true);
        }

        // 켜둔 타임라인을 끈다. (다음 재생을 위해 처음부터 다시 시작되도록)
        if (_activeTimeline != null)
        {
            UObject.SetActive(_activeTimeline, false);
            _activeTimeline = null;
        }

        _moveOn = false;
    }
    #endregion
}
