using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;

/// <summary>
/// 붙은 오브젝트의 알파를 최소↔최대 사이로 천천히 오가게 해서 점멸시키는 컴포넌트입니다.
/// 완전히 꺼졌다 켜지는 깜빡임이 아니라, 서서히 어두워졌다 서서히 밝아지는 숨쉬기 연출입니다.
/// (NAV_SYSTEM 상태 표시처럼 "살아있다"는 느낌을 주는 UI에 씁니다)
///
/// 대상을 비워두면 같은 오브젝트에서 CanvasGroup → Graphic(Image/TMP_Text 등) 순으로 찾아 씁니다.
/// 여러 요소를 한꺼번에 점멸시키려면 그 부모에 CanvasGroup을 붙이고 거기에 이 컴포넌트를 두세요.
/// </summary>
[DisallowMultipleComponent]
public sealed class CAlphaPulse : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("대상 (비워두면 같은 오브젝트에서 자동으로 찾습니다)")]
    [Tooltip("여러 요소를 한꺼번에 점멸시킬 때 사용합니다. 비어 있으면 아래 Graphic을 씁니다.")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [Tooltip("단일 이미지/텍스트를 점멸시킬 때 사용합니다.")]
    [SerializeField] private Graphic _graphic;

    [Header("점멸")]
    [Tooltip("가장 어두워졌을 때의 알파")]
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.3f;
    [Tooltip("가장 밝을 때의 알파")]
    [SerializeField, Range(0f, 1f)] private float _maxAlpha = 1f;
    [Tooltip("밝음 → 어두움 한 방향에 걸리는 시간(초). 한 주기는 이 값의 두 배입니다.")]
    [SerializeField, Min(0.01f)] private float _duration = 1.2f;
    [Tooltip("감속 곡선. InOutSine이 가장 자연스러운 숨쉬기 느낌입니다.")]
    [SerializeField] private Ease _ease = Ease.InOutSine;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Tween _tween;
    // DOTween.To가 받는 전용 델리게이트 타입입니다. (System.Func/Action과는 호환되지 않습니다)
    private DOGetter<float> _getter;
    private DOSetter<float> _setter;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        if (!ResolveTarget()) return;

        // 밝은 상태에서 시작해 어두워졌다 다시 밝아지기를 무한 반복한다.
        _setter(_maxAlpha);

        // SetUpdate를 쓰지 않으므로 일시정지(timeScale=0) 중에는 점멸도 같이 멈춘다.
        _tween = DOTween.To(_getter, _setter, _minAlpha, _duration)
            .SetEase(_ease)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _tween = null;

        // 꺼질 때는 밝은 상태로 되돌려, 다시 켰을 때 어두운 채로 보이지 않게 한다.
        _setter?.Invoke(_maxAlpha);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 알파를 읽고 쓰는 방법을 한 번만 정해둔다. CanvasGroup이 있으면 그쪽을 우선한다.
    private bool ResolveTarget()
    {
        if (_canvasGroup == null && _graphic == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _graphic = GetComponent<Graphic>();
        }

        if (_canvasGroup != null)
        {
            _getter = () => _canvasGroup.alpha;
            _setter = value => _canvasGroup.alpha = value;
            return true;
        }

        if (_graphic != null)
        {
            // Graphic.color를 직접 건드린다. (DOTween의 TMP 전용 모듈 없이도 TMP_Text에 그대로 동작)
            _getter = () => _graphic.color.a;
            _setter = value =>
            {
                Color color = _graphic.color;
                color.a = value;
                _graphic.color = color;
            };
            return true;
        }

        UDebug.Print("점멸시킬 대상이 없습니다. CanvasGroup이나 Graphic을 연결해주세요.", LogType.Error, gameObject);
        return false;
    }
    #endregion
}
