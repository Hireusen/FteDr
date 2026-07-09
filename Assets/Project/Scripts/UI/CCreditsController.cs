using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 첫 줄이 상단에 배치된 상태에서 시작하여, 3초 대기 후 
/// 정방향(+)으로 위로 스크롤되어 마지막 줄이 안착하면 멈추는 크레딧 컨트롤러입니다.
/// </summary>
public class CCreditsController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("UI References")]
    [SerializeField] private RectTransform _creditsTextRect;         // 스크롤할 크레딧 텍스트 (Anchor: Top-Center, Pivot: Top)
    [SerializeField] private RectTransform _maskAreaRect;           // 텍스트가 노출될 마스크 영역 Rect (Pivot: Center)
    [SerializeField] private Button _closeButton;                   // 닫기 버튼

    [Header("Scroll Setting")]
    [SerializeField] private float _scrollSpeed = 80f;              // 초당 이동 거리 (위로 상승)
    [SerializeField] private float _delayBeforeScroll = 2.0f;       // 첫 화면 노출 후 대기 시간 (2초)
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Coroutine _scrollCoroutine;
    private float _startPositionY;                                  // 첫 줄 상단 배치 위치 (시작 Y)
    private float _endPositionY;                                    // 마지막 줄 하단 안착 위치 (종료 Y)
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(OnClickClose);
        }
    }

    private void OnEnable()
    {
        StartCreditsScroll();
    }

    private void OnDisable()
    {
        StopCreditsScroll();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    /// <summary>
    /// Pivot.y = 1 (Top) 기준 정방향(+) 위로 올라가는 스크롤 좌표를 계산합니다.
    /// </summary>
    private void InitScrollPositions()
    {
        if (_creditsTextRect == null || _maskAreaRect == null) return;

        // 하위 텍스트 컴포넌트 크기 강제 동기화
        var tmpro = _creditsTextRect.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpro != null)
        {
            _creditsTextRect.sizeDelta = new Vector2(_creditsTextRect.sizeDelta.x, tmpro.preferredHeight);
        }
        else
        {
            var normalText = _creditsTextRect.GetComponentInChildren<Text>();
            if (normalText != null)
            {
                _creditsTextRect.sizeDelta = new Vector2(_creditsTextRect.sizeDelta.x, normalText.preferredHeight);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_creditsTextRect);

        float maskHeight = _maskAreaRect.rect.height;
        float textHeight = _creditsTextRect.rect.height;

        // 시작 위치
        _startPositionY = _creditsTextRect.anchoredPosition.y;

        // 종료 위치
        float finalPadding = 100f;
        _endPositionY = textHeight - maskHeight + finalPadding;

        // 안전장치
        if (textHeight <= maskHeight)
        {
            _endPositionY = _startPositionY;
        }

        UDebug.Print($"[Credits 조정] 시작 Y: {_startPositionY} | 종료 Y: {_endPositionY} | 텍스트 높이: {textHeight} | 마스크 높이: {maskHeight}");
    }

    private void StartCreditsScroll()
    {
        StopCreditsScroll();

        if (_creditsTextRect != null && _maskAreaRect != null)
        {
            _scrollCoroutine = StartCoroutine(ScrollUpSequenceCO());
        }
    }

    private void StopCreditsScroll()
    {
        if (_scrollCoroutine != null)
        {
            StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = null;
        }
    }

    /// <summary>
    /// 정방향(+)으로 Y값을 더해가며 위로 끌어올리는 올바른 스크롤 루틴입니다.
    /// </summary>
    private IEnumerator ScrollUpSequenceCO()
    {
        // UI 캔버스 크기 계산 타이밍 찢기
        yield return null;

        InitScrollPositions();

        Vector2 tempPosition = _creditsTextRect.anchoredPosition;
        tempPosition.y = _startPositionY;
        _creditsTextRect.anchoredPosition = tempPosition;

        // 전체 화면을 검은색으로 먼저 덮기
        UFade.SetColor(Color.black);

        // 0.5초 동안 화면을 밝혀서 첫 줄이 있는 크레딧 창을 노출
        bool isFadeInComplete = false;
        UFade.FadeIn(0.3f, blockRaycasts: true, onComplete: () => isFadeInComplete = true);
        yield return new WaitUntil(() => isFadeInComplete);

        // 정지 상태에서 2초간 대기
        yield return new WaitForSeconds(_delayBeforeScroll);

        // 위로 올라가려면 Y값이 증가해야 하므로, 종료 Y가 시작 Y보다 큽니다 (_endPositionY > _startPositionY)
        float totalScrollDistance = _endPositionY - _startPositionY;
        float currentScrolledDistance = 0f;

        // 이동한 거리가 가야 할 총 거리에 도달할 때까지 정직하게 더해줍니다(+)
        while (currentScrolledDistance < totalScrollDistance)
        {
            float moveDelta = _scrollSpeed * Time.deltaTime;
            currentScrolledDistance += moveDelta;

            if (currentScrolledDistance > totalScrollDistance)
            {
                moveDelta -= (currentScrolledDistance - totalScrollDistance);
                currentScrolledDistance = totalScrollDistance;
            }

            tempPosition.y += moveDelta;
            _creditsTextRect.anchoredPosition = tempPosition;

            yield return null;
        }

        // 최종 위치 세팅 후 마무리
        tempPosition.y = _endPositionY;
        _creditsTextRect.anchoredPosition = tempPosition;
        _scrollCoroutine = null;

        UDebug.Print("[CCreditsController] 정방향 상승 스크롤 연출이 완전히 안착하여 종료되었습니다.");
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출
    /// </summary>
    private void OnClickClose()
    {
        _closeButton.interactable = false;

        UFade.FadeOut(0.3f, blockRaycasts: true, onComplete: () =>
        {
            gameObject.SetActive(false);
            _closeButton.interactable = true;
            UFade.FadeIn(0.3f);
        });
    }
    #endregion
}
