using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스 호버 시 크기가 부드럽게 변하는 공용 반응형 버튼 컴포넌트입니다 (코루틴 버전).
/// </summary>
public class ScaleResponsiveButton : AMono, IPointerEnterHandler, IPointerExitHandler
{
    #region ─────────────────────────▶ 인펙터 설정 ◀─────────────────────────
    [Header("Scale Settings")]
    [SerializeField] private float _hoverScaleFactor = 1.08f;      // 호버 시 커질 배율
    [SerializeField] private float _transitionDuration = 0.15f;    // 크기 변화에 걸리는 시간 (초)
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private RectTransform _rectTransform;                           // 캐싱된 RectTransform
    private Vector3 _originalScale;                                 // 초기 크기 저장용
    private Vector3 _targetScale;                                   // 목표 크기 저장용
    private Coroutine _scaleCoroutine;                              // 현재 실행 중인 크기 변경 코루틴
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
    }

    private void OnDisable()
    {
        // [예외 처리] 오브젝트가 비활성화될 때 코루틴이 남아있어 발생할 수 있는 오동작 방지
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = null;
        }

        // [상태 초기화] 원래 크기로 강제 복구
        if (_rectTransform != null)
        {
            _rectTransform.localScale = _originalScale;
        }
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 외부 컨트롤러에서 애니메이션 설정값을 실시간으로 세팅할 때 호출합니다.
    /// </summary>
    public void Initialize(float scaleFactor, float duration)
    {
        _hoverScaleFactor = scaleFactor;
        _transitionDuration = duration;
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    // 마우스 포인터가 버튼 안으로 들어왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetScale = _originalScale * _hoverScaleFactor;
        StartScaleTransition(_targetScale);
    }

    // 마우스 포인터가 버튼 밖으로 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _originalScale;
        StartScaleTransition(_targetScale);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void StartScaleTransition(Vector3 newTargetScale)
    {
        // 기존 실행 중인 연출 중단 (Overlap 방지)
        if (_scaleCoroutine != null)
        {
            StopCoroutine(_scaleCoroutine);
        }

        // Lerp 보간 코루틴 시작
        _scaleCoroutine = StartCoroutine(CoScaleTo(newTargetScale));
    }

    private IEnumerator CoScaleTo(Vector3 destScale)
    {
        Vector3 startScale = _rectTransform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < _transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / _transitionDuration);

            // 선형 보간 적용
            _rectTransform.localScale = Vector3.Lerp(startScale, destScale, t);
            yield return null;
        }

        _rectTransform.localScale = destScale;
        _scaleCoroutine = null;
    }
    #endregion
}
