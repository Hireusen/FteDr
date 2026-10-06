using UnityEngine;
using UnityEngine.Video;

public class CEndingVedioToTitle : AMono
{
    #region ─────────────────────────▷ 내부 변수 ◁─────────────────────────
    [Header("영상 연결")]
    [SerializeField] private VideoPlayer _vedioRef;
    #endregion

    #region ─────────────────────────▷ 내부 메서드 ◁─────────────────────────
    private void HandleVedioEnd(VideoPlayer vp)
    {
        UDebug.Print("엔딩 영상이 종료되었습니다.");
        UScene.LoadWithFade(EScene.Title, onProgress: p => OnSceneLoadProgress.Publish(p));
    }
    #endregion

    #region ─────────────────────────▷ 메시지 함수 ◁─────────────────────────
    private void OnEnable()
    {
        if(_vedioRef != null)
        {
            _vedioRef.loopPointReached += HandleVedioEnd;
        }
    }
    private void OnDisable()
    {
        if(_vedioRef != null)
        {
            _vedioRef.loopPointReached -= HandleVedioEnd;
        }
    }
    #endregion
}
