using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 마우스 호버/클릭 시 스케일 변화와 색상(틴트) 변화를 DOTween으로 재생하는 공용 반응형 버튼 컴포넌트입니다.
/// 두 효과는 인스펙터에서 각각 독립적으로 켜고 끌 수 있습니다.
///
/// 주의: 스케일 변화는 RectTransform의 Pivot을 기준으로 커지고 작아집니다.
/// Pivot이 (0.5, 0.5)가 아니면 한쪽으로 치우쳐 보이니, 버튼의 Pivot을 (0.5, 0.5)로 맞춰주세요.
/// </summary>
public class CScaleResponsiveButton : AMono, IPointerEnterHandler, IPointerExitHandler
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("스케일 연출")]
    [SerializeField] private bool _useScale = true;
    [SerializeField] private float _hoverScaleFactor = 1.08f;
    [SerializeField] private float _clickScaleFactor = 0.92f;
    [SerializeField] private float _scaleDuration = 0.15f;

    [Header("색상(틴트) 연출")]
    [SerializeField] private bool _useColorTint = false;
    [Tooltip("호버 시 곱해질 색상. 흰색보다 밝게 하려면 RGB를 1보다 크게(예: 1.15) 주세요.")]
    [SerializeField] private Color _hoverTintColor = new Color(1.15f, 1.15f, 1.15f, 1f);
    [SerializeField] private float _tintDuration = 0.15f;

    [Header("클릭 사운드")]
    [Tooltip("비워두면 클릭 사운드를 재생하지 않습니다.")]
    [SerializeField] private string _clickSfxId = "";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private RectTransform _rectTransform;
    private Button _button;
    private Graphic _targetGraphic;   // 틴트를 적용할 대상 (Button.targetGraphic 재사용)
    private Vector3 _originalScale;
    private Color _originalColor;
    private bool _isHovering;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null) _originalScale = _rectTransform.localScale;

        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnClicked);
            _targetGraphic = _button.targetGraphic;
        }

        if (_targetGraphic == null) _targetGraphic = GetComponent<Graphic>();
        if (_targetGraphic != null) _originalColor = _targetGraphic.color;
    }

    private void OnDisable()
    {
        // 비활성화될 때 진행 중이던 트윈을 정리하고 원래 상태로 복구한다.
        _rectTransform?.DOKill();
        _targetGraphic?.DOKill();

        _isHovering = false;
        if (_rectTransform != null) _rectTransform.localScale = _originalScale;
        if (_targetGraphic != null) _targetGraphic.color = _originalColor;
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>외부(UButtonFx 자동 장착 등)에서 파라미터를 세팅할 때 호출합니다.</summary>
    public void Initialize(float scaleFactor, float duration, string clickSfxId = null)
    {
        _hoverScaleFactor = scaleFactor;
        _scaleDuration = duration;
        if (clickSfxId != null) _clickSfxId = clickSfxId;
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;

        if (_useScale && _rectTransform != null)
        {
            _rectTransform.DOKill();
            _rectTransform.DOScale(_originalScale * _hoverScaleFactor, _scaleDuration).SetUpdate(true);
        }

        if (_useColorTint && _targetGraphic != null)
        {
            _targetGraphic.DOKill();
            _targetGraphic.DOColor(_originalColor * _hoverTintColor, _tintDuration).SetUpdate(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;

        if (_useScale && _rectTransform != null)
        {
            _rectTransform.DOKill();
            _rectTransform.DOScale(_originalScale, _scaleDuration).SetUpdate(true);
        }

        if (_useColorTint && _targetGraphic != null)
        {
            _targetGraphic.DOKill();
            _targetGraphic.DOColor(_originalColor, _tintDuration).SetUpdate(true);
        }
    }

    // 클릭 시: 살짝 눌렸다가, 지금 호버 중이면 호버 크기로 아니면 원래 크기로 되돌아오는 펀치 연출 + SFX
    private void OnClicked()
    {
        if (!string.IsNullOrEmpty(_clickSfxId))
        {
            USound.PlaySfx(_clickSfxId);
        }

        if (_useScale && _rectTransform != null)
        {
            Vector3 returnTarget = _isHovering ? _originalScale * _hoverScaleFactor : _originalScale;

            _rectTransform.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true); // Pause(Time.timeScale=0) 위에서도 클릭 펀치가 멈추지 않도록
            seq.Append(_rectTransform.DOScale(_originalScale * _clickScaleFactor, _scaleDuration * 0.5f));
            seq.Append(_rectTransform.DOScale(returnTarget, _scaleDuration * 0.5f));
        }
    }
    #endregion
}
