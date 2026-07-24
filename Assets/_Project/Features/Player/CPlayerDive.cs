using UnityEngine;

/// <summary>
/// 잠수함에서 나갔을 때 이펙트를 재생합니다.
/// </summary>
public class CPlayerDive : AFrameable, IUpdateFrameable
{
    [SerializeField] private CPlayerController _player;

    private bool _playWaiting;

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        // 잠수함 내부
        if (_player.CurrentState == EPlayerState.OnGround)
        {
            _playWaiting = true;
        }
        // 수영
        else if(_playWaiting)
        {
            _playWaiting = false;
            USound.PlaySfx(Id.SFX_Dive_02);
        }
    }
}
