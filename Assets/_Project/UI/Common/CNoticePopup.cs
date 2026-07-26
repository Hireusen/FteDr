using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// OnRequestNotice를 받아 짧게 떴다 자동으로 사라지는 토스트 팝업입니다.
/// CUIWindow(스택/커서/이동잠금)와 무관한 독립적인 안내 문구라, 항상 켜져있는 오브젝트에 붙여서 씁니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CNoticePopup : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _messageText;

    [Header("타이밍")]
    [SerializeField] private float _fadeInDuration = 0.15f;
    [SerializeField] private float _holdDuration = 1.5f;
    [SerializeField] private float _fadeOutDuration = 0.3f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Coroutine _routine;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    private void OnEnable()
    {
        CEventBus<OnRequestNotice>.Subscribe(NoticeHandler);
    }

    private void OnDisable()
    {
        CEventBus<OnRequestNotice>.Unsubscribe(NoticeHandler);
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void NoticeHandler(OnRequestNotice ctx)
    {
        if (_messageText != null) _messageText.text = ctx.message;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(CoShow());
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private IEnumerator CoShow()
    {
        yield return CoFade(0f, 1f, _fadeInDuration);
        yield return new WaitForSecondsRealtime(_holdDuration); // Pause(Time.timeScale=0) 중에도 정상 노출되도록
        yield return CoFade(1f, 0f, _fadeOutDuration);

        _routine = null;
    }

    private IEnumerator CoFade(float from, float to, float duration)
    {
        if (_canvasGroup == null) yield break;

        _canvasGroup.alpha = from;
        if (duration <= 0f)
        {
            _canvasGroup.alpha = to;
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }
    #endregion
}
