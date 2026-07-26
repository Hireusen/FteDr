using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 튜토리얼 단계 하나의 데이터입니다. 인스펙터에서 리스트로 원하는 만큼 채워 넣습니다.
/// </summary>
[Serializable]
public struct TutorialStepData
{
    [TextArea(2, 5)] public string message;
    [Tooltip("선택 사항. 비워두면 그 단계에서는 삽화가 숨겨집니다.")]
    public Sprite illustration;
}

/// <summary>
/// 단계별 순차 안내(슬라이드) 튜토리얼입니다. "다음" 버튼을 누르면 현재 페이지가 왼쪽으로 슬라이드 아웃되면서
/// 페이드 아웃되고, 동시에 다음 페이지가 오른쪽에서 슬라이드 인되며 페이드 인됩니다.
/// 마지막 단계에서 다음을 누르거나 "건너뛰기"를 누르면 창을 닫습니다.
/// Tutorial_Canvas 루트에 CUIWindow(_uiType=TutorialWindow, _moveLockReason=Tutorial)와 함께 부착하세요.
/// </summary>
public sealed class CTutorialController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("단계 데이터")]
    [SerializeField] private List<TutorialStepData> _steps = new();

    [Header("필수 연결")]
    [Tooltip("페이지 내용(텍스트+삽화)을 전부 감싸는 RectTransform. 이게 슬라이드되는 대상입니다.")]
    [SerializeField] private RectTransform _pageRoot;
    [SerializeField] private CanvasGroup _pageCanvasGroup;
    [SerializeField] private TMP_Text _messageText;
    [Tooltip("선택 사항")]
    [SerializeField] private Image _illustrationImage;
    [SerializeField] private Button _btnNext;
    [Tooltip("첫 페이지에서는 자동으로 비활성화됩니다.")]
    [SerializeField] private Button _btnPrev;
    [SerializeField] private Button _btnSkip;

    [Header("선택 연결")]
    [SerializeField] private TMP_Text _stepCounterText;
    [SerializeField] private TMP_Text _nextButtonLabelText;

    [Header("표시 형식")]
    [SerializeField] private string _stepCounterFormat = "{0} / {1}";
    [SerializeField] private string _nextLabelDefault = "다음";
    [SerializeField] private string _nextLabelLastStep = "시작하기";

    [Header("페이지 전환 연출")]
    [Tooltip("페이지가 옆으로 밀려나는 거리(px)")]
    [SerializeField] private float _slideDistance = 120f;
    [SerializeField] private float _transitionDuration = 0.25f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private int _currentIndex;
    private Vector2 _pageOriginalPos;
    private bool _isTransitioning;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (_btnNext != null) _btnNext.onClick.AddListener(OnClickNext);
        if (_btnPrev != null) _btnPrev.onClick.AddListener(OnClickPrev);
        if (_btnSkip != null) _btnSkip.onClick.AddListener(OnClickSkip);
        if (_pageRoot != null) _pageOriginalPos = _pageRoot.anchoredPosition;
    }

    private void OnEnable()
    {
        _currentIndex = 0;
        _isTransitioning = false;

        if (_pageRoot != null) _pageRoot.anchoredPosition = _pageOriginalPos;
        if (_pageCanvasGroup != null) _pageCanvasGroup.alpha = 1f;

        RefreshStep();
    }

    private void OnDisable()
    {
        _pageRoot?.DOKill();
        _pageCanvasGroup?.DOKill();
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void OnClickNext()
    {
        if (_isTransitioning) return;

        bool isLastStep = _steps == null || _currentIndex >= _steps.Count - 1;
        if (isLastStep)
        {
            CloseTutorial();
            return;
        }

        PlayPageTransition(forward: true, onMidpoint: () =>
        {
            _currentIndex++;
            RefreshStep();
        });
    }

    private void OnClickPrev()
    {
        if (_isTransitioning) return;
        if (_currentIndex <= 0) return; // 첫 페이지에서는 아무것도 하지 않음

        PlayPageTransition(forward: false, onMidpoint: () =>
        {
            _currentIndex--;
            RefreshStep();
        });
    }

    private void OnClickSkip()
    {
        CloseTutorial();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void CloseTutorial()
    {
        OnRequestCloseUI.Publish(EUI.TutorialWindow);
    }

    // forward=true(다음): 현재 페이지가 왼쪽으로 나가고, 다음 페이지가 오른쪽에서 들어온다.
    // forward=false(이전): 현재 페이지가 오른쪽으로 나가고, 이전 페이지가 왼쪽에서 들어온다.
    private void PlayPageTransition(bool forward, Action onMidpoint)
    {
        if (_pageRoot == null || _pageCanvasGroup == null)
        {
            onMidpoint?.Invoke();
            return;
        }

        _isTransitioning = true;

        Vector2 outDirection = forward ? Vector2.left : Vector2.right;
        Vector2 inDirection = forward ? Vector2.right : Vector2.left;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true); // Pause(Time.timeScale=0) 위에서 열려도 트랜지션이 멈추지 않도록
        seq.Append(_pageRoot.DOAnchorPos(_pageOriginalPos + outDirection * _slideDistance, _transitionDuration).SetEase(Ease.InCubic));
        seq.Join(_pageCanvasGroup.DOFade(0f, _transitionDuration));
        seq.AppendCallback(() =>
        {
            onMidpoint?.Invoke();
            _pageRoot.anchoredPosition = _pageOriginalPos + inDirection * _slideDistance;
        });
        seq.Append(_pageRoot.DOAnchorPos(_pageOriginalPos, _transitionDuration).SetEase(Ease.OutCubic));
        seq.Join(_pageCanvasGroup.DOFade(1f, _transitionDuration));
        seq.OnComplete(() => _isTransitioning = false);
    }

    private void RefreshStep()
    {
        if (_steps == null || _steps.Count == 0)
        {
            UDebug.Print("CTutorialController: 등록된 튜토리얼 단계가 없습니다.", LogType.Warning, gameObject);
            return;
        }

        TutorialStepData step = _steps[_currentIndex];
        bool isLastStep = _currentIndex == _steps.Count - 1;

        if (_messageText != null) _messageText.text = step.message;

        if (_illustrationImage != null)
        {
            _illustrationImage.sprite = step.illustration;
            _illustrationImage.enabled = step.illustration != null;
        }

        if (_stepCounterText != null)
        {
            _stepCounterText.text = string.Format(_stepCounterFormat, _currentIndex + 1, _steps.Count);
        }

        if (_nextButtonLabelText != null)
        {
            _nextButtonLabelText.text = isLastStep ? _nextLabelLastStep : _nextLabelDefault;
        }

        if (_btnPrev != null)
        {
            _btnPrev.interactable = _currentIndex > 0;
        }
    }
    #endregion
}
