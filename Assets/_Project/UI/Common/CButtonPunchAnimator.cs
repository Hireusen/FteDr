using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 트리거/전환 없이 "재생하면 한 번 통통 튀는" 단일 상태짜리 Animator Controller
/// (예: Btn_Go, Btn_LargePlayYellow2, Btn_Start의 Zoom 클립)를 위한 재생기입니다.
/// 마우스 진입(호버)과 클릭, 둘 다에서 애니메이션을 처음부터 다시 재생시켜 "팡" 하는 펀치 효과를 냅니다.
/// (ScaleResponsiveButton과 달리 스케일을 코드로 계산하지 않고, 이미 있는 Animator 클립을 그대로 재사용)
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class CButtonPunchAnimator : AMono, IPointerEnterHandler
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Tooltip("비워두면 클릭 사운드를 재생하지 않습니다.")]
    [SerializeField] private string _clickSfxId = "";
    [Tooltip("마우스가 올라갔을 때도 재생할지 여부. 끄면 클릭할 때만 재생합니다.")]
    [SerializeField] private bool _playOnHover = true;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Animator _animator;
    private int _defaultStateHash; // 상태 이름과 무관하게, 처음 물려있던 기본 상태를 그대로 다시 재생하기 위해 저장
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator != null)
        {
            _defaultStateHash = _animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        }

        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClicked);
        }
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>UButtonFx가 자동 장착할 때 클릭 사운드 ID를 주입합니다.</summary>
    public void Initialize(string clickSfxId)
    {
        _clickSfxId = clickSfxId;
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_playOnHover) PlayPunch();
    }

    private void OnClicked()
    {
        PlayPunch();

        if (!string.IsNullOrEmpty(_clickSfxId))
        {
            USound.PlaySfx(_clickSfxId);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void PlayPunch()
    {
        if (_animator == null) return;

        // 0번째 프레임부터 강제로 다시 재생 → 이미 재생 중이어도 처음부터 "팡" 하고 다시 튀게 만든다.
        _animator.Play(_defaultStateHash, 0, 0f);
    }
    #endregion
}
