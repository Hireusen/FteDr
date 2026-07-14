using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 창 프리팹의 루트에 부착하는 공용 컴포넌트입니다.
/// CUIManager가 SetActive 대신 이 컴포넌트의 Open/Close를 호출하여, 모든 창이 동일한 페이드 연출을 갖도록 합니다.
/// Close 버튼은 OnRequestCloseUI를 발행해 CUIManager를 거쳐 닫히므로, 다른 시스템도 창이 닫힘을 일관되게 알 수 있습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CUIWindow : AMono, IUIWindow
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("창 식별")]
    [Tooltip("CUIRegistrySO에 등록된 것과 동일한 타입이어야 합니다. Close 버튼이 이 타입으로 닫기를 요청합니다.")]
    [SerializeField] private EUI _uiType;

    [Header("필수 연결")]
    [Tooltip("창 전체를 감싸는 캔버스 그룹 (알파/입력 차단 제어)")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [Tooltip("모든 창이 공통으로 가져야 하는 닫기 버튼")]
    [SerializeField] private Button _closeButton;

    [Header("페이드 설정")]
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.2f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Coroutine _fadeCoroutine;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>창을 활성화하고 페이드 인으로 등장시킵니다.</summary>
    public void Open()
    {
        gameObject.SetActive(true);

        if (_canvasGroup == null)
        {
            UDebug.Print($"CUIWindow({_uiType}): CanvasGroup이 비어있습니다.", LogType.Error, gameObject);
            return;
        }

        SetInteractable(false);
        StartFade(_canvasGroup.alpha, 1f, _fadeInDuration, onComplete: () => SetInteractable(true));
    }

    /// <summary>페이드 아웃 후 창을 비활성화합니다.</summary>
    public void Close()
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(false);
            return;
        }

        SetInteractable(false);
        StartFade(_canvasGroup.alpha, 0f, _fadeOutDuration, onComplete: () => gameObject.SetActive(false));
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(RequestClose);
        }
        else
        {
            UDebug.Print($"CUIWindow({_uiType}): 닫기 버튼이 연결되지 않았습니다. 모든 창은 닫기 버튼을 가져야 합니다.", LogType.Warning, gameObject);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 닫기 버튼 클릭 시: 스스로 닫지 않고 이벤트를 발행해 CUIManager를 거쳐 닫히게 한다.
    private void RequestClose()
    {
        OnRequestCloseUI.Publish(_uiType);
    }

    private void SetInteractable(bool value)
    {
        _canvasGroup.interactable = value;
        _canvasGroup.blocksRaycasts = value;
    }

    private void StartFade(float from, float to, float duration, Action onComplete)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CoFade(from, to, duration, onComplete));
    }

    // 일시정지(Time.timeScale=0) 상태에서도 페이드가 진행되도록 unscaledDeltaTime 사용 (UFade와 동일 관례)
    private IEnumerator CoFade(float from, float to, float duration, Action onComplete)
    {
        _canvasGroup.alpha = from;

        if (duration <= 0f)
        {
            _canvasGroup.alpha = to;
        }
        else
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        _fadeCoroutine = null;
        onComplete?.Invoke();
    }
    #endregion
}
