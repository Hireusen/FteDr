using Cinemachine;
using System.Collections;
using UnityEngine;

/// <summary>
/// 프레임에이블 클래스의 설계 의도입니다.
/// </summary>
public class CPlayerToCockpit : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [SerializeField] private CSubMarineUpDown _cSubMarineUpDown;
    [SerializeField] private CinemachineVirtualCamera _cockpitCam;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private int _cockpitoriginPriority;
    private Coroutine _camToCutCoroutine;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public int ToCockpitPriority { get; private set; } = 600;
    public bool SitCockpit { get; private set; } = false;
    public CinemachineBrain CineBrain { get; private set; }
    public void MoveToCockpit()
    {
        SitCockpit = true;
        OnPlayerCockpitStateChanged.Publish(true); // 조작 안내 UI 표시
        OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, true);
        CineBrain = Camera.main.GetComponent<CinemachineBrain>();
        CineBrain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 1f);
        _cockpitoriginPriority = _cockpitCam.Priority;
        _cockpitCam.Priority = ToCockpitPriority;
    }
    public void CockpitToPlayer()
    {
        if (_camToCutCoroutine != null) return;

        _cockpitCam.Priority = _cockpitoriginPriority;
        _cockpitCam.Priority = _cockpitoriginPriority;
        _camToCutCoroutine = StartCoroutine(CamToCut());
    }

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv3;

    /// <summary>
    /// 조종석에서 지역 이동(상승/하강)을 시도합니다.
    /// 키보드(Q/E)와 조종석 UI의 지역이동 버튼이 공유하는 단일 경로입니다.
    /// 하강은 다음 지역이 연구로 해금되어 있어야 하고, 출발 전에 가방을 자동 정산합니다.
    /// </summary>
    /// <param name="goDeeper">true면 하강(다음 지역), false면 상승(이전 지역)</param>
    /// <returns>이동 연출을 시작했는지 여부</returns>
    public bool TryTravel(bool goDeeper)
    {
        if (SitCockpit == false) return false;

        if (_cSubMarineUpDown == null)
        {
            UDebug.Print("_cSubMarineUpDown이 인스펙터에 할당되지 않았습니다.", LogType.Error, gameObject);
            return false;
        }

        if (!_cSubMarineUpDown.CanMove(goDeeper)) return false;

        if (goDeeper)
        {
            int nextStage = UPlayer.CurrentStage + 1;
            UDebug.Print($"목표 스테이지 : {nextStage}, 해금된 스테이지 : {UPlayer.UnlockedStage}");

            // 다음 스테이지 해금 안됨
            if (!UPlayer.IsStageUnlocked(nextStage))
            {
                UDebug.Print($"다음 스테이지 해금 안됨 : {nextStage}");
                USound.PlaySfx(Id.SFX_Sonar_Ping);
                return false;
            }

            PlayerRuntimeData runtimeData = CPlayerManager.Ins.Runtime;

            if (runtimeData.bagItems.Count > 0)
            {
                UDebug.Print($"가방에 있는 아이템 갯수 : {runtimeData.bagItems.Count}");
                CShopSellController.ExecuteSellAll(false);
            }
        }

        OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, false);
        _cSubMarineUpDown.StartCutScene(goDeeper);
        SitCockpit = false;
        OnPlayerCockpitStateChanged.Publish(false); // 조작 안내 UI 숨김
        UDebug.Print($"지역 이동 시작 : {(goDeeper ? "하강" : "상승")}");
        return true;
    }

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (SitCockpit == false) return;

        // 프레임 매니저는 Time.timeScale과 무관하게 Update를 돌리므로,
        // 일시정지 창이 떠 있는 동안 키 입력으로 지역 이동이 시작되지 않도록 여기서 막는다.
        if (Time.timeScale == 0f) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryTravel(false);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            TryTravel(true);
        }
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    // 조종석 UI의 지역이동 버튼 요청. 키 입력과 동일하게 처리한다.
    private void StageTravelHandler(OnRequestStageTravel ctx)
    {
        TryTravel(ctx.goDeeper);
    }

    // 조종석 UI의 닫기 버튼 요청. 이동키로 일어나는 것과 동일하게 처리한다.
    private void ExitCockpitHandler(OnRequestExitCockpit ctx)
    {
        if (SitCockpit == false) return;

        CockpitToPlayer();
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    protected override void OnEnable()
    {
        base.OnEnable();
        CEventBus<OnRequestStageTravel>.Subscribe(StageTravelHandler);
        CEventBus<OnRequestExitCockpit>.Subscribe(ExitCockpitHandler);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        CEventBus<OnRequestStageTravel>.Unsubscribe(StageTravelHandler);
        CEventBus<OnRequestExitCockpit>.Unsubscribe(ExitCockpitHandler);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private IEnumerator CamToCut()
    {
        yield return UCoroutine.GetWait(1f);
        CineBrain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
        SitCockpit = false;
        OnPlayerCockpitStateChanged.Publish(false); // 조작 안내 UI 숨김
        OnSetMoveLockReason.Publish(EMoveLockReason.Submarine, false);
        UDebug.Print("벗어남 사유 : 캠투컷");
        _camToCutCoroutine = null;
    }
    #endregion
}
