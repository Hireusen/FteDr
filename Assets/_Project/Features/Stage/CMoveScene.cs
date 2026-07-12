using System.Collections;
using UnityEngine;

/// <summary>
/// 호출되면 다음 씬으로 페이드 효과와 함께 전환하는 컴포넌트입니다.
/// 씬 전환/페이드는 프로젝트의 UScene.NextLoadWithFade + UFade 시스템을 사용합니다.
///
/// - 인스펙터로 페이드 사용 여부, 색/이미지, 지속 시간, 자동 전환 여부를 설정.
/// - 자동 전환 시 "모든 오브젝트의 Start가 끝난 뒤" 넘어가도록 한 프레임 대기 옵션 제공.
/// - 수동으로 넘기려면 다른 코드에서 Move()를 호출.
/// </summary>
public sealed class CMoveScene : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("자동 전환")]
    [Tooltip("씬 시작 시 자동으로 다음 씬으로 넘어감")]
    [SerializeField] private bool _moveOnStart = false;
    [Tooltip("모든 오브젝트의 Start가 끝난 뒤 넘어가도록 한 프레임 대기")]
    [SerializeField] private bool _waitForAllStart = true;
    [Tooltip("자동 전환 전 추가 대기 시간(초)")]
    [SerializeField] private float _delay = 0f;

    [Header("페이드")]
    [Tooltip("페이드 효과 사용 여부")]
    [SerializeField] private bool _useFade = true;
    [Tooltip("페이드 색깔")]
    [SerializeField] private Color _fadeColor = Color.black;
    [Tooltip("페이드 이미지")]
    [SerializeField] private Sprite _fadeSprite;
    [Tooltip("페이드 아웃(어두워짐) 시간(초)")]
    [SerializeField] private float _fadeOutDuration = 0.45f;
    [Tooltip("페이드 인(밝아짐) 시간(초)")]
    [SerializeField] private float _fadeInDuration = 0.45f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private bool _moving; // 중복 호출 방지
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>다음 씬으로 전환합니다. (인스펙터 설정에 따라 페이드 적용)</summary>
    public void Move()
    {
        if (_moving) return;
        _moving = true;

        ApplyFadeVisual();

        if (_useFade)
        {
            bool started = UScene.NextLoadWithFade(0f, _fadeOutDuration, _fadeInDuration);
            if (!started)
            {
                UDebug.Print("다음 씬이 빌드 세팅 범위를 벗어났습니다.", LogType.Warning);
                _moving = false;
            }
        }
        else
        {
            bool started = UScene.NextLoad();
            if (!started)
            {
                UDebug.Print("다음 씬이 빌드 세팅 범위를 벗어났습니다.", LogType.Warning);
                _moving = false;
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Start()
    {
        if (_moveOnStart) StartCoroutine(CoAutoMove());
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 자동 전환: (옵션) 모든 Start 완료 대기 → (옵션) 추가 지연 → 전환
    private IEnumerator CoAutoMove()
    {
        // 이 프레임의 모든 Start가 끝난 뒤 실행 (프레임 끝까지 대기)
        if (_waitForAllStart) yield return new WaitForEndOfFrame();

        if (_delay > 0f) yield return new WaitForSeconds(_delay);

        Move();
    }

    // 페이드 색/이미지를 UFade에 반영한다.
    private void ApplyFadeVisual()
    {
        if (!_useFade) return;

        if (_fadeSprite != null)
        {
            UFade.SetSprite(_fadeSprite, _fadeColor);
        }
        else
        {
            UFade.SetColor(_fadeColor);
        }
    }
    #endregion
}
