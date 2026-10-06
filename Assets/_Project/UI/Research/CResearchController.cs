using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 연구 창의 내용과 분석 연출을 담당하는 컴포넌트입니다.
/// 특수 수집품을 가방에 담은 채로 상점과 상호작용하면 상점창 대신 이 창이 열리고,
/// 창이 열린 직후부터 프로그레스 바가 채워지다가 다 차는 순간 연구가 완료되어 다음 스테이지가 해금됩니다.
/// (버튼으로 시작하지 않습니다. 창이 열리는 것 자체가 연구 시작입니다)
///
/// 창의 열기/닫기/페이드/닫기 버튼은 같은 오브젝트의 CUIWindow(EUI.ResearchWindow)가 담당하고,
/// 해금 규칙과 수집품 소모는 UStageResearch가 전담합니다. 이 컴포넌트는 표시와 연출 타이밍만 책임집니다.
/// 분석이 끝나기 전에 창을 닫으면 연구는 적용되지 않습니다. (수집품도 그대로 남아 다시 시도할 수 있습니다)
/// </summary>
[DisallowMultipleComponent]
public sealed class CResearchController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("분석 대상 표시 (가방의 특수 수집품)")]
    [SerializeField] private Image _specialIcon;
    [Tooltip("아이콘 원본 비율을 유지할지 여부")]
    [SerializeField] private bool _preserveIconAspect = true;
    [SerializeField] private TMP_Text _specialNameText;
    [SerializeField] private TMP_Text _specialDescriptionText;

    [Header("분석 진행 표시")]
    [Tooltip("분석 진행도를 보여줄 슬라이더 (0 → 1로 채워집니다)")]
    [SerializeField] private Slider _progressSlider;
    [Tooltip("진행률 퍼센트 텍스트 (선택)")]
    [SerializeField] private TMP_Text _progressPercentText;
    [Tooltip("창이 열린 뒤 연구가 완료될 때까지의 시간(초)")]
    [SerializeField, Min(0f)] private float _analysisDuration = 2.5f;

    [Header("결과 표시")]
    [Tooltip("새로 해금된 목적지 이름을 표시할 텍스트")]
    [SerializeField] private TMP_Text _destinationText;
    [Tooltip("연구가 완료될 때까지 숨겨두고, 완료되면 깜빡이며 나타나는 문구 (예: 회수한 유물에서 새로운 단서를 발견했습니다.)")]
    [SerializeField] private TMP_Text _completeMessageText;
    [Tooltip("완료 문구가 깜빡이는 횟수")]
    [SerializeField, Min(1)] private int _blinkCount = 3;
    [Tooltip("한 번 깜빡이는 데 걸리는 시간(초). 절반은 켜짐, 절반은 꺼짐입니다.")]
    [SerializeField, Min(0.02f)] private float _blinkInterval = 0.18f;
    [Tooltip("분석이 끝나기 전까지 목적지 자리에 보여줄 문구")]
    [SerializeField] private string _pendingDestinationText = "???";
    [Tooltip("확인(닫기) 버튼. 분석이 끝날 때까지 눌리지 않게 잠급니다. (선택)")]
    [SerializeField] private Button _confirmButton;

    [Header("표시 형식")]
    [SerializeField] private string _percentFormat = "{0}%";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Coroutine _analysisRoutine;
    private Coroutine _blinkRoutine;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        // 분석이 끝나는 순간 수집품이 소모되므로, 표시용 데이터는 열릴 때 한 번만 잡아둔다.
        // (소모 후 다시 조회하면 가방이 비어 아이콘/이름이 사라져 버린다)
        string specialId = UPlayer.SpecialInBag;
        BindSpecial(specialId.IsBlank() ? null : UData.Collectible(specialId));

        SetProgress(0f);
        SetDestination(_pendingDestinationText);
        SetConfirmInteractable(false);
        SetCompleteMessageVisible(false);

        _analysisRoutine = StartCoroutine(AnalysisCo());
    }

    private void OnDisable()
    {
        // 분석 도중 창이 닫히면(ESC 등) 연구는 적용하지 않고 중단한다. 다시 열면 처음부터 진행된다.
        if (_analysisRoutine != null)
        {
            StopCoroutine(_analysisRoutine);
            _analysisRoutine = null;
        }

        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 프로그레스 바를 채우고, 다 찬 시점에 연구를 실제로 적용한다.
    // 일시정지 등으로 timeScale이 0이 되어도 멈추지 않도록 unscaledDeltaTime을 쓴다. (CUIWindow 페이드와 동일 관례)
    private IEnumerator AnalysisCo()
    {
        float time = 0f;
        while (time < _analysisDuration)
        {
            time += Time.unscaledDeltaTime;
            SetProgress(_analysisDuration > 0f ? Mathf.Clamp01(time / _analysisDuration) : 1f);
            yield return null;
        }
        SetProgress(1f);

        _analysisRoutine = null;
        CompleteResearch();
    }

    // 실제 소모/해금은 UStageResearch가 전담하고, 이 창은 결과 표시와 실패 안내만 담당한다.
    private void CompleteResearch()
    {
        if (!UStageResearch.TryResearch(out string failMessage))
        {
            if (!failMessage.IsBlank())
            {
                OnRequestNotice.Publish(failMessage);
            }
            UDebug.Print($"[연구] 분석을 마쳤지만 연구에 실패했습니다. ({failMessage})", LogType.Warning, gameObject);

            SetDestination(string.Empty);
            SetConfirmInteractable(true);
            return;
        }

        // 해금 직후의 CurrentLevel이 곧 새로 도달할 수 있게 된 스테이지다.
        SetDestination(UStageResearch.DestinationName(UStageResearch.CurrentLevel));
        SetConfirmInteractable(true);

        _blinkRoutine = StartCoroutine(BlinkCompleteMessageCo());
    }

    // 완료 문구를 몇 번 깜빡인 뒤 켜진 상태로 남긴다.
    private IEnumerator BlinkCompleteMessageCo()
    {
        float halfInterval = _blinkInterval * 0.5f;

        for (int i = 0; i < _blinkCount; ++i)
        {
            SetCompleteMessageVisible(true);
            yield return WaitUnscaledCo(halfInterval);
            SetCompleteMessageVisible(false);
            yield return WaitUnscaledCo(halfInterval);
        }

        SetCompleteMessageVisible(true);
        _blinkRoutine = null;
    }

    // 일시정지(timeScale=0)와 겹쳐도 연출이 멈추지 않도록 스케일되지 않은 시간으로 대기한다.
    private static IEnumerator WaitUnscaledCo(float seconds)
    {
        float time = 0f;
        while (time < seconds)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetProgress(float ratio)
    {
        if (_progressSlider != null)
        {
            _progressSlider.value = ratio;
        }

        if (_progressPercentText != null)
        {
            _progressPercentText.text = string.Format(_percentFormat, Mathf.RoundToInt(ratio * 100f));
        }
    }

    private void SetDestination(string destinationName)
    {
        if (_destinationText != null)
        {
            _destinationText.text = destinationName;
        }
    }

    private void SetConfirmInteractable(bool value)
    {
        if (_confirmButton != null)
        {
            _confirmButton.interactable = value;
        }
    }

    // 오브젝트를 켜고 끄는 대신 알파만 조절한다. (레이아웃 그룹 안에서 다른 요소가 밀리지 않도록)
    private void SetCompleteMessageVisible(bool value)
    {
        if (_completeMessageText != null)
        {
            _completeMessageText.alpha = value ? 1f : 0f;
        }
    }

    // 가방에 특수 수집품이 없으면 아이콘/이름/설명을 비운다. (정상 흐름에서는 항상 있다)
    private void BindSpecial(CCollectibleSO special)
    {
        if (_specialIcon != null)
        {
            _specialIcon.sprite = special != null ? special.Icon : null;
            _specialIcon.enabled = special != null && special.Icon != null;
            _specialIcon.preserveAspect = _preserveIconAspect;
        }

        if (_specialNameText != null)
        {
            _specialNameText.text = special != null ? special.Name : string.Empty;
        }

        if (_specialDescriptionText != null)
        {
            _specialDescriptionText.text = special != null ? special.Description : string.Empty;
        }
    }
    #endregion
}
