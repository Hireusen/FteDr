using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 마우스 호버 시 크기가 부드럽게 변하고, 클릭 시 살짝 눌리는 펀치 연출 + 클릭 SFX까지 재생하는
/// 공용 반응형 버튼 컴포넌트입니다. (기존 호버 스케일 기능은 그대로 유지)
/// </summary>
public class CScaleResponsiveButton : AMono, IPointerEnterHandler, IPointerExitHandler
{
    #region ─────────────────────────▶ 인펙터 설정 ◀─────────────────────────
    [Header("Scale Settings")]
    [SerializeField] private float _hoverScaleFactor = 1.08f;      // 호버 시 커질 배율
    [SerializeField] private float _transitionDuration = 0.15f;    // 크기 변화에 걸리는 시간 (초)

    [Header("Click Settings")]
    [Tooltip("클릭 시 살짝 줄어드는 배율 (1보다 작은 값)")]
    [SerializeField] private float _clickScaleFactor = 0.92f;
    [SerializeField] private float _clickTransitionDuration = 0.08f;

    [Header("Click Sound")]
    [Tooltip("비워두면 클릭 사운드를 재생하지 않습니다.")]
    [SerializeField] private string _clickSfxId = "";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private RectTransform _rectTransform;                           // 캐싱된 RectTransform
    private Button _button;
    private Vector3 _originalScale;                                 // 초기 크기 저장용
    private Vector3 _targetScale;                                   // 목표 크기 저장용
    private Coroutine _scaleCoroutine;                              // 현재 실행 중인 크기 변경 코루틴
    private bool _isHovering;                                       // 클릭 펀치가 끝난 뒤 되돌아갈 목표(호버 중인지) 판단용
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        // [최적화] GetComponent는 Awake에서 최초 1회만 캐싱하여 런타임 오버헤드 방지
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
        {
            _originalScale = _rectTransform.localScale;
            _targetScale = _originalScale;
        }

        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnClicked);
        }
    }

    private void OnDisable()
    {
        // [예외 처리] 오브젝트가 비활성화될 때 코루틴이 남아있어 발생할 수 있는 오동작 방지
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }

        _isHovering = false;

        // [상태 초기화] 원래 크기로 강제 복구
        if (_rectTransform != null)
        {
            _rectTransform.localScale = _originalScale;
        }
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 외부 컨트롤러(또는 UButtonFx 자동 장착)에서 애니메이션/사운드 설정값을 세팅할 때 호출합니다.
    /// </summary>
    /// <param name="scaleFactor">호버 시 커질 배율</param>
    /// <param name="duration">호버 크기 변화 시간(초)</param>
    /// <param name="clickSfxId">클릭 시 재생할 사운드 ID. null/빈 문자열이면 무음</param>
    public void Initialize(float scaleFactor, float duration, string clickSfxId = null)
    {
        _hoverScaleFactor = scaleFactor;
        _transitionDuration = duration;
        if (clickSfxId != null) _clickSfxId = clickSfxId;
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    // 마우스 포인터가 버튼 안으로 들어왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        StartScaleTransition(_originalScale * _hoverScaleFactor, _transitionDuration);
    }

    // 마우스 포인터가 버튼 밖으로 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        StartScaleTransition(_originalScale, _transitionDuration);
    }

    // 버튼 클릭 시: 살짝 줄어들었다가 원래(또는 호버 중이었으면 호버 크기로) 되돌아오는 펀치 연출 + SFX
    private void OnClicked()
    {
        if (!string.IsNullOrEmpty(_clickSfxId))
        {
            USound.PlaySfx(_clickSfxId);
        }

        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(CoClickPunch());
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void StartScaleTransition(Vector3 newTargetScale, float duration)
    {
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        _scaleCoroutine = StartCoroutine(CoScaleTo(newTargetScale, duration));
    }

    private IEnumerator CoScaleTo(Vector3 destScale, float duration)
    {
        Vector3 startScale = _rectTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            _rectTransform.localScale = Vector3.Lerp(startScale, destScale, t);
            yield return null;
        }

        _rectTransform.localScale = destScale;
        _scaleCoroutine = null;
    }

    // 목표 크기까지 줄었다가, 지금 호버 중이면 호버 크기로 아니면 원래 크기로 되돌아온다.
    private IEnumerator CoClickPunch()
    {
        Vector3 punchTarget = _originalScale * _clickScaleFactor;
        yield return CoScaleTo(punchTarget, _clickTransitionDuration);

        Vector3 returnTarget = _isHovering ? _originalScale * _hoverScaleFactor : _originalScale;
        _scaleCoroutine = StartCoroutine(CoScaleTo(returnTarget, _clickTransitionDuration));
    }
    #endregion
}
