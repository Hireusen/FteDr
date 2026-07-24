using UnityEngine;

/// <summary>
/// 잠수함에 들어왔을 때 연료량을 모두 채워줍니다.
/// </summary>
public class CPlayerChangeFuel : AFrameable, IUpdateFrameable
{
    [SerializeField] private CPlayerController _player;

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (_player.CurrentState != EPlayerState.OnGround) return;
        if(UPlayer.CurrentFuel >= UPlayer.MaxFuel) return;

        UPlayer.RecoverFuel(UPlayer.MaxFuel);
        USound.PlaySfx(Id.SFX_Jump_03);
    }
}
