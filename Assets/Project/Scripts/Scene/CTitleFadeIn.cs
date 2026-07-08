using System.Collections;
using UnityEngine;

/// <summary>
/// 타이틀 씬 전용. 씬 진입 시 어두운 화면에서 시작해 서서히 밝게 엽니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class CTitleFadeIn : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("연출")]
    [Tooltip("페이드 인 시작 전 어둠 유지 시간(초)")]
    [SerializeField] private float _startDelay = 0f;
    [Tooltip("화면이 밝아지는 시간(초)")]
    [SerializeField] private float _fadeInDuration = 1.4f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CanvasGroup _group;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 1f; // 진입 시 어두운 상태

        StartCoroutine(CoFadeIn());
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private IEnumerator CoFadeIn()
    {
        if (_startDelay > 0f)
        {
            yield return new WaitForSeconds(_startDelay);
        }

        float t = 0f;
        while (t < _fadeInDuration)
        {
            t += Time.deltaTime;
            _group.alpha = Mathf.Lerp(1f, 0f, t / _fadeInDuration);
            yield return null;
        }

        _group.alpha = 0f;
        _group.blocksRaycasts = false; // 다 열렸으면 입력 통과시킴
    }
    #endregion
}
