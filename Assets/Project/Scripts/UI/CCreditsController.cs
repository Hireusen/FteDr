using System.Collections;
using UnityEngine;

/// <summary>
/// 클래스의 설계 의도입니다.
/// </summary>
public class CCreditsController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("Scroll Setting")]
    [SerializeField] private RectTransform _creditsTextRect;             // 스크롤할 텍스트 RectTransform
    [SerializeField] private float _scrollSpeed = 80f;                  // 초당 이동 거리
    [SerializeField] private float _startPositionY = 800f;              // 시작 Y 좌표
    [SerializeField] private float _endPositionY = -800f;               // 종료 Y 좌표
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Coroutine _scrollCoroutine;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────

    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────

    /// <summary>
    /// 스크롤을 롤링 연출합니다.
    /// </summary>
    private void StartCreditsScroll()
    {
        StopCreditsScroll();

        if (_creditsTextRect != null)
        {
            _scrollCoroutine = StartCoroutine(ScrollDownCO());
        }
        else
        {
            UDebug.Print("스크롤할 Credits Text RectTransform이 지정되지 않았습니다.");
        }
    }

    /// <summary>
    /// 실행 중인 스크롤 코루틴을 중지합니다.
    /// </summary>
    private void StopCreditsScroll()
    {
        if (_scrollCoroutine != null)
        {
            StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = null;
        }
    }

    private IEnumerator ScrollDownCO()
    {
        // 크레딧 텍스트의 anchoredPosition 위치를 받아와 초기 Y 설정
        Vector2 tempPosition = _creditsTextRect.anchoredPosition;
        tempPosition.y = _startPositionY;
        _creditsTextRect.anchoredPosition = tempPosition;

        // 현재 Y 좌표가 종료 좌표보다 위에 있을 동안 지속적으로 하강
        while (tempPosition.y > _endPositionY)
        {
            tempPosition.y -= _scrollSpeed * Time.deltaTime;
            _creditsTextRect.anchoredPosition = tempPosition;
            yield return null;
        }

        // 목표 위치에 완벽하게 안착 후 마무리
        tempPosition.y = _endPositionY;
        _creditsTextRect.anchoredPosition = tempPosition;
        _scrollCoroutine = null;

        UDebug.Print("[CCreditsController] 크레딧 스크롤 연출이 성공적으로 완료되었습니다.");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        StartCreditsScroll();
    }

    private void OnDisable()
    {
        StopCreditsScroll();
    }

    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────

    #endregion
}
