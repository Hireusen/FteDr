using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 창 프리팹/씬 오브젝트의 루트에 부착하는 공용 컴포넌트입니다.
/// CUIManager가 SetActive 대신 이 컴포넌트의 Open/Close를 호출하여, 모든 창이 동일한 페이드 연출을 갖도록 합니다.
/// Close 버튼은 OnRequestCloseUI를 발행해 CUIManager를 거쳐 닫히므로, 다른 시스템도 창이 닫힘을 일관되게 알 수 있습니다.
///
/// 인스펙터에서 이동 잠금 사유를 지정하면 열려있는 동안 플레이어 이동을 막습니다.
/// (마우스 커서 표시는 여러 창이 겹칠 때의 참조 계수 문제 때문에 CUIManager가 스택 전체를 보고 중앙에서 관리합니다)
/// </summary>
[DisallowMultipleComponent]
public sealed class CUIWindow : AMono, IUIWindow
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("창 식별")]
    [Tooltip("CUIRegistrySO/씬배치 목록에 등록된 것과 동일한 타입이어야 합니다. Close 버튼이 이 타입으로 닫기를 요청합니다.")]
    [SerializeField] private EUI _uiType;

    [Header("필수 연결")]
    [Tooltip("창 전체를 감싸는 캔버스 그룹 (알파/입력 차단 제어)")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [Tooltip("모든 창이 공통으로 가져야 하는 닫기 버튼")]
    [SerializeField] private Button _closeButton;

    [Header("페이드 설정")]
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.2f;

    [Header("게임플레이 차단 (선택)")]
    [Tooltip("이 창이 열려있는 동안 막을 플레이어 조작 사유. None이면 이동을 막지 않습니다 (예: Pause는 Time.timeScale로 이미 멈추므로 None).")]
    [SerializeField] private EMoveLockReason _moveLockReason = EMoveLockReason.None;
    [Tooltip("이 창이 열려있는 동안 HUD를 숨길지 여부")]
    [SerializeField] private bool _hidesHud = false;

    [Header("씬 전환 시 유지 (선택)")]
    [Tooltip("체크하면 씬이 바뀌어도 이 창이 파괴되지 않고 계속 살아있습니다. Loading처럼 Boot에서 한 번 만들어져 게임 내내 유지되어야 하는 창에만 체크하세요. Pause/Shop/Inventory처럼 씬마다 새로 생기는 게 정상인 창은 체크하지 마세요.")]
    [SerializeField] private bool _isPersistentAcrossScenes = false;

    [Header("버튼 인터랙션 자동 장착 (호버 스케일 + 클릭 펀치 + 클릭 SFX)")]
    [SerializeField] private float _buttonHoverScale = 1.08f;
    [SerializeField] private float _buttonHoverDuration = 0.15f;
    [Tooltip("비워두면 클릭 사운드를 재생하지 않습니다.")]
    [SerializeField] private string _buttonClickSfxId = "";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Coroutine _fadeCoroutine;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public bool HidesHud => _hidesHud;

    /// <summary>창을 활성화하고 페이드 인으로 등장시킵니다.</summary>
    public void Open()
    {
        gameObject.SetActive(true);

        if (_moveLockReason != EMoveLockReason.None)
        {
            OnSetMoveLockReason.Publish(_moveLockReason, true);
        }

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
        if (_moveLockReason != EMoveLockReason.None)
        {
            OnSetMoveLockReason.Publish(_moveLockReason, false);
        }

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
        // 씬이 바뀌어도 파괴되지 않아야 하는 창(Loading 등)은 여기서 살아남도록 표시한다.
        if (_isPersistentAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(RequestClose);
        }
        else
        {
            UDebug.Print($"CUIWindow({_uiType}): 닫기 버튼이 연결되지 않았습니다. 모든 창은 닫기 버튼을 가져야 합니다.", LogType.Warning, gameObject);
        }

        // 이 창 아래의 모든 버튼(닫기 버튼 포함)에 호버/클릭 연출 + 클릭 SFX를 자동으로 붙인다.
        // 특정 버튼만 빼고 싶으면 그 버튼에 CButtonFxExclude를 붙이면 된다.
        UButtonFx.AutoEquip(gameObject, _buttonHoverScale, _buttonHoverDuration, _buttonClickSfxId);
    }

    // CUIManager에 스스로를 등록한다. Awake가 아니라 Start에서 하는 이유: RegisterWindow가 즉시 SetActive(false)를
    // 호출하는데, 같은 오브젝트의 다른 컴포넌트(예: CPauseMenuController)의 Awake가 아직 안 끝났다면
    // 그 시점에 꺼져버려 이후 Awake가 통째로 스킵될 수 있다. Start는 씬의 모든 Awake가 끝난 뒤 호출되므로 안전하다.
    private void Start()
    {
        CUIManager.Ins?.RegisterWindow(_uiType, gameObject);
    }

    // 창이 파괴될 때(씬 전환 등) 등록을 해제하고, 켜둔 이동잠금 사유가 남아버리는 것을 방지한다.
    private void OnDestroy()
    {
        if (CInputManager.IsQuitting) return;

        CUIManager.Ins?.UnregisterWindow(_uiType, gameObject);

        if (_moveLockReason != EMoveLockReason.None)
        {
            OnSetMoveLockReason.Publish(_moveLockReason, false);
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
        // 부모 오브젝트가 비활성 상태면 activeInHierarchy가 false라서 StartCoroutine이 조용히(혹은 에러와 함께) 실패한다.
        // 여기서 미리 감지해서 원인을 바로 알 수 있게 하고, 페이드 없이도 최소한 켜짐/꺼짐은 즉시 반영한다.
        if (!gameObject.activeInHierarchy)
        {
            UDebug.Print($"CUIWindow({_uiType}): 조상 오브젝트 '{FindInactiveAncestorName()}'가 비활성 상태라 페이드를 시작할 수 없습니다. " +
                $"이 오브젝트를 활성화해주세요.", LogType.Error, gameObject);
            _canvasGroup.alpha = to;
            onComplete?.Invoke();
            return;
        }

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CoFade(from, to, duration, onComplete));
    }

    // activeInHierarchy가 false인 원인이 된 첫 번째 비활성 조상의 이름을 찾는다. (자기 자신 포함해서 위로 훑음)
    private string FindInactiveAncestorName()
    {
        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                return current.gameObject.name;
            }
            current = current.parent;
        }
        return "(찾지 못함 - 원인 불명)";
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
