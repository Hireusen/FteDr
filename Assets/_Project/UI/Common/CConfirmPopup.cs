using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 확인/취소 두 갈래 응답을 받는 모달 팝업입니다.
/// 되돌릴 수 없는 행동(게임 종료·타이틀 복귀) 직전에 유저의 응답을 기다릴 때 사용합니다.
///
/// 자동으로 사라지는 <see cref="CNoticePopup"/>과 달리 응답이 있어야 닫히며, 그동안 뒤쪽 UI의 클릭을 막습니다.
/// CUIWindow(스택/커서/이동잠금)와 무관하게 항상 최상단에 떠야 하므로, CNoticePopup처럼
/// 항상 켜져있는 오브젝트에 붙여서 쓰고 CanvasGroup으로만 보이기/숨기기를 제어합니다.
/// (Canvas의 Sort Order를 CUIManager가 창에 매기는 값보다 크게 잡아야 일시정지 창 위에 뜹니다)
/// </summary>
[DisallowMultipleComponent]
public sealed class CConfirmPopup : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("필수 연결")]
    [Tooltip("팝업 전체를 감싸는 캔버스 그룹 (알파/입력 차단 제어)")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _btnConfirm;
    [SerializeField] private Button _btnCancel;

    [Header("선택 연결 (버튼 라벨을 호출부에서 바꾸고 싶을 때만)")]
    [SerializeField] private TMP_Text _confirmLabel;
    [SerializeField] private TMP_Text _cancelLabel;

    [Header("페이드 설정")]
    [SerializeField] private float _fadeDuration = 0.15f;

    [Header("버튼 인터랙션 자동 장착 (호버 스케일 + 클릭 펀치 + 클릭 SFX)")]
    [SerializeField] private float _buttonHoverScale = 1.08f;
    [SerializeField] private float _buttonHoverDuration = 0.15f;
    [Tooltip("비워두면 클릭 사운드를 재생하지 않습니다.")]
    [SerializeField] private string _buttonClickSfxId = "";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Action _onConfirm;
    private Action _onCancel;
    private Coroutine _fadeRoutine;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>씬에 존재하는 확인 팝업입니다. 없으면 null입니다.</summary>
    public static CConfirmPopup Current { get; private set; }

    /// <summary>지금 응답을 기다리는 중인지 여부입니다.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// 확인 팝업을 띄웁니다. 씬에 팝업이 없으면 아무것도 하지 않고 False를 반환하므로,
    /// 호출부는 팝업 부재 때문에 기능 자체가 막히지 않도록 대비할 수 있습니다.
    /// </summary>
    /// <param name="message">표시할 문구</param>
    /// <param name="onConfirm">확인 버튼을 눌렀을 때 실행할 동작</param>
    /// <param name="onCancel">취소 버튼을 눌렀을 때 실행할 동작 (없으면 닫기만 함)</param>
    /// <param name="confirmLabel">확인 버튼 문구 (비워두면 프리팹 설정 유지)</param>
    /// <param name="cancelLabel">취소 버튼 문구 (비워두면 프리팹 설정 유지)</param>
    /// <returns>팝업을 띄웠다면 True</returns>
    public static bool TryShow(
        string message, Action onConfirm, Action onCancel = null,
        string confirmLabel = null, string cancelLabel = null)
    {
        if (Current == null)
        {
            UDebug.Print("확인 팝업이 씬에 없어 표시하지 못했습니다.", LogType.Error);
            return false;
        }

        Current.Show(message, onConfirm, onCancel, confirmLabel, cancelLabel);
        return true;
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        Hide(true);

        if (_btnConfirm != null) _btnConfirm.onClick.AddListener(OnClickConfirm);
        else UDebug.Print("CConfirmPopup: 확인 버튼이 연결되지 않았습니다.", LogType.Error, gameObject);

        if (_btnCancel != null) _btnCancel.onClick.AddListener(OnClickCancel);
        else UDebug.Print("CConfirmPopup: 취소 버튼이 연결되지 않았습니다.", LogType.Error, gameObject);

        // 다른 창들과 동일한 호버/클릭 연출을 갖도록 맞춘다. (CUIWindow가 해주던 역할)
        UButtonFx.AutoEquip(gameObject, _buttonHoverScale, _buttonHoverDuration, _buttonClickSfxId);
    }

    private void OnEnable()
    {
        if (Current != null && Current != this)
        {
            UDebug.Print("CConfirmPopup이 씬에 중복으로 존재합니다. 마지막에 켜진 것으로 대체합니다.", LogType.Warning, gameObject);
        }
        Current = this;
    }

    private void OnDisable()
    {
        if (Current == this) Current = null;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void Show(string message, Action onConfirm, Action onCancel, string confirmLabel, string cancelLabel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;

        if (_messageText != null) _messageText.text = message;
        if (_confirmLabel != null && !confirmLabel.IsBlank()) _confirmLabel.text = confirmLabel;
        if (_cancelLabel != null && !cancelLabel.IsBlank()) _cancelLabel.text = cancelLabel;

        IsOpen = true;
        SetBlocking(true);
        StartFade(1f);
    }

    private void OnClickConfirm()
    {
        if (!IsOpen) return;

        // 콜백이 또 팝업을 띄울 수 있으므로 먼저 닫고 참조를 비운 뒤 호출한다.
        Action callback = _onConfirm;
        Hide(false);
        callback?.Invoke();
    }

    private void OnClickCancel()
    {
        if (!IsOpen) return;

        Action callback = _onCancel;
        Hide(false);
        callback?.Invoke();
    }

    // immediate: 페이드 없이 즉시 숨김 (Awake 초기화용)
    private void Hide(bool immediate)
    {
        IsOpen = false;
        _onConfirm = null;
        _onCancel = null;

        SetBlocking(false);

        if (immediate)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            return;
        }

        StartFade(0f);
    }

    private void SetBlocking(bool value)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.interactable = value;
        _canvasGroup.blocksRaycasts = value; // 열려있는 동안 뒤쪽 창(일시정지 등)의 클릭을 막는다
    }

    private void StartFade(float to)
    {
        if (_canvasGroup == null) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(CoFade(_canvasGroup.alpha, to));
    }

    // 일시정지(Time.timeScale=0) 상태에서 뜨는 팝업이므로 unscaledDeltaTime을 사용한다. (CUIWindow/CNoticePopup과 동일 관례)
    private IEnumerator CoFade(float from, float to)
    {
        if (_fadeDuration <= 0f)
        {
            _canvasGroup.alpha = to;
            _fadeRoutine = null;
            yield break;
        }

        float time = 0f;
        while (time < _fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, time / _fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = to;

        _fadeRoutine = null;
    }
    #endregion
}
