using UnityEngine;

/// <summary>
/// 페이드 유틸리티 기능을 테스트하는 스크립트입니다.
/// </summary>
public class CTestFade : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("테스트 트리거")]
    [SerializeField] private bool _testFadeOut;
    [SerializeField] private bool _testFadeIn;

    [Header("설정")]
    [SerializeField] private Sprite _image;
    [SerializeField] private Color _color;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    public void ExecuteUpdateFrame()
    {
        if (_testFadeOut)
        {
            _testFadeOut = false;
            UFade.SetColor(_color);
            UFade.FadeOut(1.1f, true);
            UDebug.Print("페이드 인 실행");
        }

        if (_testFadeIn)
        {
            _testFadeIn = false;
            UFade.SetColor(_color);
            UFade.FadeIn(1.1f, true);
            UDebug.Print("페이드 아웃 실행");
        }
    }
    #endregion
}
