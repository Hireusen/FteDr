using UnityEngine;

/// <summary>
/// 페이드 유틸리티 기능을 테스트하는 스크립트입니다.
/// </summary>
public class CTestFade : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("키")]
    [SerializeField] private KeyCode _startKey = KeyCode.O;
    [SerializeField] private KeyCode _endKey = KeyCode.P;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Update()
    {
        if (Input.GetKeyDown(_startKey))
        {
            UFade.FadeOut(1.1f, true);
            UDebug.Print("페이드 인 실행");
        }
        if (Input.GetKeyDown(_endKey))
        {
            UFade.FadeIn(1.1f, true);
            UDebug.Print("페이드 아웃 실행");
        }
    }
    #endregion
}
