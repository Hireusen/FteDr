using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 조종석 지역 이동 UI의 목적지 노드 하나를 표현하는 컴포넌트입니다.
/// 상태 라벨(VISITED/STATIONED/LOCKED) + 노드 원 + 해역 이름이 한 묶음이며,
/// 이전/현재/다음 세 자리에 같은 프리팹을 배치하고 상태만 다르게 넣어 씁니다.
///
/// 어떤 상태를 넣을지는 CStageTravelController가 정하고, 이 컴포넌트는 그 상태를
/// 문구와 색으로 옮기는 일만 합니다. 문구와 색은 전부 인스펙터에서 조절합니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class CStageNodeSlot : AMono
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CanvasGroup _canvasGroup;

    // RequireComponent는 컴포넌트를 '새로 붙일 때'만 CanvasGroup을 같이 추가하므로,
    // 이 스크립트보다 먼저 만들어져 있던 오브젝트에는 없을 수 있다. 없으면 여기서 붙여 준다.
    private CanvasGroup Group => _canvasGroup != null ? _canvasGroup : (_canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>());
    #endregion

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("표시 요소")]
    [Tooltip("노드 위의 상태 라벨 (VISITED / STATIONED / LOCKED)")]
    [SerializeField] private TMP_Text _statusText;
    [Tooltip("노드 아래의 해역 이름")]
    [SerializeField] private TMP_Text _nameText;
    [Tooltip("노드 원 이미지")]
    [SerializeField] private Image _nodeImage;
    [Tooltip("현재 정박 중일 때만 켜지는 강조 오브젝트 (글로우 링 등). 선택 사항입니다.")]
    [SerializeField] private GameObject _stationedHighlight;

    [Header("상태 라벨 문구")]
    [SerializeField] private string _visitedStatus = "VISITED";
    [SerializeField] private string _stationedStatus = "STATIONED";
    [SerializeField] private string _availableStatus = "AVAILABLE";
    [SerializeField] private string _lockedStatus = "LOCKED";

    [Header("이름 표시 형식")]
    [Tooltip("현재 정박 중인 해역의 이름 형식. {0}이 해역 이름입니다.")]
    [SerializeField] private string _stationedNameFormat = "{0} (현재)";
    [Tooltip("아직 연구하지 않은 해역에 이름 대신 표시할 문구")]
    [SerializeField] private string _lockedName = "? ? ? (분석 필요)";

    [Header("상태별 색")]
    [Tooltip("연결된 상태 라벨/이름/노드 원에 함께 적용됩니다.")]
    [SerializeField] private Color _visitedColor = new Color(0.45f, 0.72f, 0.80f, 1f);
    [SerializeField] private Color _stationedColor = new Color(0.20f, 0.85f, 0.95f, 1f);
    [Tooltip("해금되었지만 아직 가보지 않은 해역")]
    [SerializeField] private Color _availableColor = new Color(0.35f, 0.90f, 0.85f, 1f);
    [Tooltip("아직 연구하지 않은 해역. 알파를 낮춰 흐리게 둡니다.")]
    [SerializeField] private Color _lockedColor = new Color(0.45f, 0.48f, 0.52f, 0.35f);
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 노드를 보이거나 숨깁니다.
    /// 오브젝트를 끄지 않고 알파만 0으로 두는 이유는, 레이아웃 그룹이 남은 노드를 다시 배치하면서
    /// 현재 노드 위치가 해역마다 달라지는 것을 막기 위해서입니다. (자리는 그대로 차지합니다)
    /// </summary>
    /// <param name="visible">표시 여부</param>
    public void SetVisible(bool visible)
    {
        CanvasGroup group = Group;
        if (group == null) return;

        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    /// <summary>
    /// 노드를 지정한 상태와 해역 이름으로 갱신합니다.
    /// Locked 상태에서는 <paramref name="destinationName"/>을 무시하고 물음표 문구를 표시합니다.
    /// </summary>
    /// <param name="state">노드 상태</param>
    /// <param name="destinationName">표시할 해역 이름 (Locked면 쓰이지 않음)</param>
    public void Setup(EStageNodeState state, string destinationName)
    {
        SetText(_statusText, GetStatusLabel(state));
        SetText(_nameText, GetDisplayName(state, destinationName));

        Color color = GetColor(state);
        SetColor(_statusText, color);
        SetColor(_nameText, color);
        if (_nodeImage != null) _nodeImage.color = color;

        UObject.SetActive(_stationedHighlight, state == EStageNodeState.Stationed);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private string GetStatusLabel(EStageNodeState state)
    {
        switch (state)
        {
            case EStageNodeState.Visited: return _visitedStatus;
            case EStageNodeState.Stationed: return _stationedStatus;
            case EStageNodeState.Available: return _availableStatus;
            case EStageNodeState.Locked: return _lockedStatus;
            default: return string.Empty;
        }
    }

    private string GetDisplayName(EStageNodeState state, string destinationName)
    {
        switch (state)
        {
            case EStageNodeState.Stationed: return string.Format(_stationedNameFormat, destinationName);
            case EStageNodeState.Locked: return _lockedName;
            default: return destinationName;
        }
    }

    private Color GetColor(EStageNodeState state)
    {
        switch (state)
        {
            case EStageNodeState.Visited: return _visitedColor;
            case EStageNodeState.Stationed: return _stationedColor;
            case EStageNodeState.Available: return _availableColor;
            case EStageNodeState.Locked: return _lockedColor;
            default: return _lockedColor;
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }

    private static void SetColor(TMP_Text target, Color color)
    {
        if (target != null) target.color = color;
    }
    #endregion
}
