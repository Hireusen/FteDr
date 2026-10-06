using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 조종석에 앉아있는 동안만 보이는 지역 이동 UI입니다.
/// 목적지를 "이전 - 현재 - 다음" 순서로 선형 표시하고, 지역이동 버튼으로 상승/하강을 요청합니다.
/// (키보드 Q/E도 그대로 동작합니다. 둘 다 CPlayerToCockpit.TryTravel이라는 같은 경로를 탑니다)
///
/// 노드 세 자리는 전부 같은 CStageNodeSlot이고, 이 컨트롤러는 각 자리에 어떤 상태를 넣을지만 정합니다.
/// 첫 스테이지에서는 이전 노드를, 마지막 스테이지에서는 다음 노드를 숨기고 그 구간의 선도 같이 숨깁니다.
/// 숨길 때 오브젝트를 끄지 않고 알파만 0으로 두어, 레이아웃이 재배치되며 현재 노드가 움직이는 것을 막습니다.
/// 아직 연구로 해금되지 않은 다음 목적지는 노드와 연결선을 흐린 색으로 두고(물음표 표기) 이동 버튼도 잠급니다.
///
/// CUIWindow(스택형 창)를 쓰지 않습니다 — 여닫는 창이 아니라 착석 상태에 따라 자동으로 나타나는 패널이라
/// CCockpitControlHint/CPlayerHubController와 같은 성격입니다. 다만 버튼을 눌러야 하므로
/// 착석 중에는 ECursorReason.Cockpit으로 마우스 커서를 띄웁니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CStageTravelController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [Tooltip("패널 전체를 감싸는 캔버스 그룹 (알파/입력 제어)")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.2f;

    [Header("목적지 노드")]
    [Tooltip("왼쪽: 이미 다녀온 이전 해역")]
    [SerializeField] private CStageNodeSlot _prevSlot;
    [Tooltip("가운데: 현재 정박 중인 해역")]
    [SerializeField] private CStageNodeSlot _currentSlot;
    [Tooltip("오른쪽: 다음 해역 (미연구면 물음표)")]
    [SerializeField] private CStageNodeSlot _nextSlot;

    [Header("연결선")]
    [Tooltip("이전 → 현재 선. 이미 지나온 구간이라 항상 해금 색으로 표시됩니다.")]
    [SerializeField] private CStageNodeLink _prevLink;
    [Tooltip("현재 → 다음 선. 아직 연구 전이면 흐린 색으로 표시됩니다.")]
    [SerializeField] private CStageNodeLink _nextLink;

    [Header("버튼")]
    [Tooltip("이전 해역으로 (상승). 키보드 Q와 같은 동작입니다.")]
    [SerializeField] private Button _prevTravelButton;
    [Tooltip("다음 해역으로 (하강). 키보드 E와 같은 동작입니다.")]
    [SerializeField] private Button _nextTravelButton;
    [Tooltip("닫기. 조종석에서 내립니다. (이동키로 일어나는 것과 같은 동작)")]
    [SerializeField] private Button _closeButton;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (_prevTravelButton != null)
        {
            _prevTravelButton.onClick.AddListener(() => OnRequestStageTravel.Publish(false));
        }

        if (_nextTravelButton != null)
        {
            _nextTravelButton.onClick.AddListener(() => OnRequestStageTravel.Publish(true));
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(OnRequestExitCockpit.Publish);
        }

        // 시작할 땐 조종석에 앉아있지 않으므로 즉시 숨김 (연출 없이)
        SetVisible(false, instant: true);
    }

    private void OnEnable()
    {
        CEventBus<OnPlayerCockpitStateChanged>.Subscribe(CockpitStateHandler);
        CEventBus<OnStageResearched>.Subscribe(StageResearchedHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnPlayerCockpitStateChanged>.Unsubscribe(CockpitStateHandler);
        CEventBus<OnStageResearched>.Unsubscribe(StageResearchedHandler);

        _canvasGroup?.DOKill();

        // 패널이 꺼진 채로 커서 사유가 남아 조작이 잠기는 것을 방지한다.
        CInputManager.Ins?.SetCursorReason(ECursorReason.Cockpit, false);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void CockpitStateHandler(OnPlayerCockpitStateChanged ctx)
    {
        if (ctx.isSitting) Refresh(); // 보이기 전에 내용을 먼저 맞춰둔다

        SetVisible(ctx.isSitting, instant: false);

        // 버튼을 누를 수 있도록 착석 중에만 커서를 띄운다.
        CInputManager.Ins?.SetCursorReason(ECursorReason.Cockpit, ctx.isSitting);
    }

    // 조종석 밖(상점)에서 연구가 끝나도, 다시 앉았을 때 물음표가 이름으로 바뀌어 있도록 갱신해둔다.
    private void StageResearchedHandler(OnStageResearched ctx)
    {
        Refresh();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void Refresh()
    {
        // 목적지 이름은 1부터 시작하는 단계로 조회한다. (UPlayer.CurrentStage는 0부터 시작)
        int currentLevel = UPlayer.CurrentStage + 1;
        bool hasPrev = currentLevel > 1;
        bool hasNext = currentLevel < UStageResearch.MaxLevel;
        bool isNextUnlocked = UPlayer.IsStageUnlocked(UPlayer.CurrentStage + 1);

        if (_currentSlot != null)
        {
            _currentSlot.Setup(EStageNodeState.Stationed, UStageResearch.DestinationName(currentLevel));
        }

        // 이전 해역은 이미 지나온 곳이므로 항상 이름을 공개한다.
        SetNodeVisible(_prevSlot, hasPrev);
        if (hasPrev && _prevSlot != null)
        {
            _prevSlot.Setup(EStageNodeState.Visited, UStageResearch.DestinationName(currentLevel - 1));
        }

        // 다음 해역은 연구로 해금되기 전까지 물음표로 가리고, 노드와 연결선 모두 흐리게 둔다.
        SetNodeVisible(_nextSlot, hasNext);
        if (hasNext && _nextSlot != null)
        {
            _nextSlot.Setup(
                isNextUnlocked ? EStageNodeState.Available : EStageNodeState.Locked,
                UStageResearch.DestinationName(currentLevel + 1));
        }

        RefreshLinks(hasPrev, hasNext, isNextUnlocked);

        if (_prevTravelButton != null)
        {
            _prevTravelButton.interactable = hasPrev;
        }

        if (_nextTravelButton != null)
        {
            _nextTravelButton.interactable = hasNext && isNextUnlocked;
        }
    }

    // 노드를 켜고 끈 직후에는 레이아웃 그룹이 아직 자리를 안 잡아 Dot 좌표가 옛 값이다.
    // 캔버스를 한 번 강제로 갱신해 최신 좌표를 만든 뒤에 선을 맞춘다.
    private void RefreshLinks(bool hasPrev, bool hasNext, bool isNextUnlocked)
    {
        if (_prevLink == null && _nextLink == null) return;

        Canvas.ForceUpdateCanvases();

        // 노드가 없는 끝 구간에서는 선도 같이 숨긴다. (선은 노드와 다른 부모에 있어 따로 처리해야 한다)
        if (_prevLink != null)
        {
            _prevLink.SetVisible(hasPrev);
            if (hasPrev) _prevLink.Refresh(locked: false);
        }

        if (_nextLink != null)
        {
            _nextLink.SetVisible(hasNext);
            if (hasNext) _nextLink.Refresh(locked: !isNextUnlocked);
        }
    }

    // 노드를 숨길 때도 레이아웃 자리는 유지해서 현재 노드가 항상 같은 위치에 오게 한다.
    private static void SetNodeVisible(CStageNodeSlot slot, bool visible)
    {
        if (slot != null) slot.SetVisible(visible);
    }

    // 패널을 페이드로 보이거나 숨깁니다. 숨겨진 동안은 입력을 받지 않습니다.
    private void SetVisible(bool visible, bool instant)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.DOKill();

        if (instant)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
        }
        else
        {
            // 일시정지(timeScale=0) 중에는 이 패널의 연출도 같이 멈춘다.
            _canvasGroup.DOFade(visible ? 1f : 0f, _fadeDuration);
        }

        // 지역이동 버튼을 클릭으로 받아야 하므로, 보일 때는 레이캐스트를 막아 입력을 가져간다.
        _canvasGroup.blocksRaycasts = visible;
        _canvasGroup.interactable = visible;
    }
    #endregion
}
