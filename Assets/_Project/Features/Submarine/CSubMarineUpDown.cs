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

    [Header("필수 정보")]
    [SerializeField] private EScene _firstGameScene = EScene.Stage_1;
    [SerializeField] private EScene _lastGameScene = EScene.Stage_6;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const string NAME_DESTINATION = "Destination";
    private const string NAME_UPPOS = "Uppos";
    private const string NAME_DOWNPOS = "Downpos";
    private const string NAME_ARRIVECAM = "ArriveCamera";

    private const float MAX_SPEED = 10f;
    private const float ACCELERATION = 5f;

    private bool _moveOn = false;
    private CinemachineVirtualCamera _arriveCam;
    private CPlayerController _playerCtrl;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
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

        // 연출 동안 플레이어를 잠수함에 종속시켜 함께 이동하도록 한다.
        Player?.AttachTo(transform);

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

        GameObject arriveCamObj = GameObject.Find(NAME_ARRIVECAM);
        GameObject dest = GameObject.Find(NAME_DESTINATION);
        GameObject downPos = GameObject.Find(NAME_DOWNPOS);
        GameObject upPos = GameObject.Find(NAME_UPPOS);

        // 씬에 배치된 기준 오브젝트를 이름으로 찾는다. 하나라도 없으면 연출을 중단한다.
        if (arriveCamObj == null || dest == null || downPos == null || upPos == null)
        {
            UDebug.Print(
                $"잠수함 도착 연출에 필요한 오브젝트를 찾지 못했습니다. " +
                $"({NAME_ARRIVECAM}:{arriveCamObj != null}, {NAME_DESTINATION}:{dest != null}, " +
                $"{NAME_DOWNPOS}:{downPos != null}, {NAME_UPPOS}:{upPos != null}) " +
                $"오브젝트 이름 또는 대소문자를 계층과 일치시켜야 합니다.",
                LogType.Error);
            _moveOn = false;
            return;
        }

        _arriveCam = arriveCamObj.GetComponent<CinemachineVirtualCamera>();
        if (_arriveCam == null)
        {
            UDebug.Print($"{NAME_ARRIVECAM}에 CinemachineVirtualCamera가 없습니다.", LogType.Error);
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

        Vector3 startPos = goDeeper ? upPos.transform.position : downPos.transform.position;
        StartCoroutine(MoveStartToDestCo(startPos, dest.transform.position, duration));
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

        // 연출 종료: 플레이어를 SpawnPoint에 정확히 앉히고 종속을 해제한다.
        CPlayerController player = Player;
        if (player != null)
        {
            if (_playerSpawnPoint != null) player.Teleport(_playerSpawnPoint);
            player.Detach();
        }

        _moveOn = false;
    }
    #endregion
}
