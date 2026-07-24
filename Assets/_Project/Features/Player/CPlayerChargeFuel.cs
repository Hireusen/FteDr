using UnityEngine;

/// <summary>
/// 잠수함에 들어왔을 때 연료량을 모두 채워주고, 충전 순간 물방울 파티클을 재생합니다.
/// (다이브와 달리 색감/일렁임 펄스는 없습니다.)
/// </summary>
public class CPlayerChangeFuel : AFrameable, IUpdateFrameable
{
    [SerializeField] private CPlayerController _player;
    [SerializeField] private Transform _playerCenter;

    [Header("연출 참조")]
    [Tooltip("충전 순간 재생할 물방울 파티클 프리팹(다이브와 공용)")]
    [SerializeField] private GameObject _bubblePrefab;
    [Tooltip("플레이어 기준 물방울 생성 오프셋")]
    [SerializeField] private Vector3 _bubbleOffset = new Vector3(0f, 0.5f, 1f);
    [Tooltip("생성된 물방울 오브젝트를 자동 파괴하기까지의 시간(초)")]
    [SerializeField] private float _bubbleLifetime = 3f;

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        if (_player.CurrentState != EPlayerState.OnGround) return;
        if (UPlayer.CurrentFuel >= UPlayer.MaxFuel) return;

        // 실제로 충전이 일어나는 순간에만 1회 실행된다(다음 프레임부턴 위 조건에서 반환).
        UPlayer.RecoverFuel(UPlayer.MaxFuel);
        USound.PlaySfx(Id.SFX_Jump_03);
        SpawnBubble();
    }

    // 플레이어 위치 + 오프셋에 물방울 프리팹을 생성하고 일정 시간 후 파괴한다.
    private void SpawnBubble()
    {
        if (_bubblePrefab == null || _playerCenter == null) return;

        Vector3 pos = _playerCenter.position + _playerCenter.TransformVector(_bubbleOffset);
        GameObject bubble = Instantiate(_bubblePrefab, pos, Quaternion.identity);
        Destroy(bubble, _bubbleLifetime);
    }
}
