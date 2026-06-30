using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 글로벌 UFade 연출과 연동하여, 마스크 영역 내에서 밑에서 위로 올라오는 
/// 엔딩 크레딧 스크롤 및 종료 연출을 제어하는 컨트롤러입니다.
/// </summary>
public class CCreditsController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("UI References")]
    [SerializeField] private RectTransform _creditsTextRect;         // 스크롤할 크레딧 텍스트
    [SerializeField] private RectTransform _maskAreaRect;           // 텍스트가 노출될 마스크 영역 Rect
    [SerializeField] private Button _closeButton;                   // 닫기 버튼

    [Header("Scroll Setting")]
    [SerializeField] private float _scrollSpeed = 80f;              // 초당 이동 거리 (위로 상승)
    [SerializeField] private float _delayBeforeScroll = 3f;         // 화면이 밝아진 후 대기 시간 (3초)
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Coroutine _scrollCoroutine;
    private float _startPositionY;                                  // 마스크 하단 밖 (시작 위치)
    private float _endPositionY;                                    // 텍스트 끝자락 안착 (종료 위치)
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
        // 텍스트가 완전히 밑에서부터 걸쳐 올라오도록 구조 정립
        InitScrollPositions();
        StartCreditsScroll();
    }

    private void OnDisable()
    {
        StopCreditsScroll();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    /// <summary>
    /// 마스크 영역과 텍스트 높이를 기반으로 시작 및 종료 Y 좌표를 동적 계산합니다.
    /// </summary>
    private void InitScrollPositions()
    {
        if (_creditsTextRect == null || _maskAreaRect == null) return;

        float maskHeight = _maskAreaRect.rect.height;
        float textHeight = _creditsTextRect.rect.height;

        // 시작 위치: 텍스트의 상단이 마스크의 하단 경계선 바로 밑에 숨도록 설정
        _startPositionY = -(maskHeight * 0.5f) - (textHeight * 0.5f);

        // 종료 위치: 텍스트의 최하단(끝)이 마스크 영역 중심 혹은 약간 상단에 안착하도록 설정
        // 텍스트 끝이 화면 안에 온전히 남게 만듭니다.
        _endPositionY = (textHeight * 0.5f) - (maskHeight * 0.2f);
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
    /// 페이드 연출 시퀀스와 대기, 스크롤 상승을 제어하는 핵심 코루틴입니다.
    /// </summary>
    private IEnumerator ScrollUpSequenceCO()
    {
        // 초기 위치 세팅 (화면 밑에 숨김)
        Vector2 tempPosition = _creditsTextRect.anchoredPosition;
        tempPosition.y = _startPositionY;
        _creditsTextRect.anchoredPosition = tempPosition;

        // 전체 화면을 검은색으로 먼저 덮은 상태에서 시작 (Flashing 방지)
        UFade.SetColor(Color.black);

        // 0.5초 동안 화면을 투명하게 페이드 인하여 크레딧 창을 드러냄
        bool isFadeInComplete = false;
        UFade.FadeIn(0.5f, blockRaycasts: true, onComplete: () => isFadeInComplete = true);
        yield return new WaitUntil(() => isFadeInComplete);

        // 화면이 다 밝아진 후, 요청하신 대로 3초간 멈춰서 대기
        yield return new WaitForSeconds(_delayBeforeScroll);

        // 목표 위치(_endPositionY)까지 밑에서 위로 스크롤 상승 (Y축 증가)
        while (tempPosition.y < _endPositionY)
        {
            tempPosition.y += _scrollSpeed * Time.deltaTime;
            _creditsTextRect.anchoredPosition = tempPosition;
            yield return null;
        }

        // 완벽하게 안착 후 코루틴 종료
        tempPosition.y = _endPositionY;
        _creditsTextRect.anchoredPosition = tempPosition;
        _scrollCoroutine = null;

        UDebug.Print("[CCreditsController] 크레딧 끝부분이 화면 내에 안착하며 연출이 정상 종료되었습니다.");
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출
    /// </summary>
    private void OnClickClose()
    {
        // 버튼을 누르면 UFade로 화면을 완전히 0.5초간 어둡게 덮은(FadeOut) 후 오브젝트를 안전하게 끕니다.
        UFade.FadeOut(0.5f, blockRaycasts: true, onComplete: () =>
        {
            gameObject.SetActive(false);

            // 필요 시 다음 화면 전환을 위해 페이드를 다시 걷어내 주는 코드 추가 가능
            UFade.FadeIn(0.3f);
        });
    }
    #endregion
}
