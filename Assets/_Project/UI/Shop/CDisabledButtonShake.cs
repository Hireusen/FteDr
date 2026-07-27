using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 버튼이 비활성(Interactable = false) 상태일 때 클릭하면 살짝 흔들리는 피드백을 주는 컴포넌트입니다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
public sealed class CDisabledButtonShake : AMono, IPointerClickHandler
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("흔들림 설정")]
    [SerializeField] private float _duration = 0.3f;
    [SerializeField] private float _strength = 8f;
    [SerializeField] private int _vibrato = 20;
    [SerializeField] private float _randomness = 90f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Button _button;
    private RectTransform _rect;
    private Vector2 _originalAnchoredPos;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _button = GetComponent<Button>();
        _rect = GetComponent<RectTransform>();
        _originalAnchoredPos = _rect.anchoredPosition;
    }

    private void OnDisable()
    {
        // 흔들리는 중에 로우/창이 꺼지는 경우를 대비해 트윈을 정리하고 위치를 되돌린다.
        _rect.DOKill();
        _rect.anchoredPosition = _originalAnchoredPos;
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        // 활성 상태(정상적으로 구매 가능한 상태)라면 Button.onClick이 이미 처리하므로 아무것도 안 한다.
        if (_button == null || _button.interactable) return;

        _rect.DOKill();
        _rect.anchoredPosition = _originalAnchoredPos;
        _rect.DOShakeAnchorPos(_duration, _strength, _vibrato, _randomness, false, true);
    }
    #endregion
}
