using UnityEngine;

/// <summary>
/// 타이틀 보내기
/// </summary>
public class CToBeContinue : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("페이드 설정")]
    [SerializeField] private float _durationFadeIn = 4f;
    [SerializeField] private float _durationFadeOut = 4f;
    #endregion

    private void FadeInHandler()
    {
        if (UFade.IsFading) UFade.StopFade();
        UScene.LoadWithFade(EScene.Title, _durationFadeOut);
    }

    private void Start()
    {
        if (UFade.IsFading) UFade.StopFade();
        UFade.FadeIn(_durationFadeIn, true, onComplete : FadeInHandler);
    }

    private void Update()
    {
        CGameManager.Player?.SetActive(false);
        CGameManager.Submarine?.SetActive(false);
    }
}
