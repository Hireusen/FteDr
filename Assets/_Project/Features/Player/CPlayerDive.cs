using UnityEngine;

/// <summary>
/// 잠수함에서 나가(수영 상태로 전환) 잠수를 시작하는 순간의 연출을 재생합니다.
/// 물방울 파티클 생성, 화면 색감/일렁임 펄스, 아래 방향 임펄스를 처리합니다.
/// </summary>
public class CPlayerDive : AFrameable, IUpdateFrameable
{
    [SerializeField] private CPlayerController _player;

    [Header("연출 참조")]
    [Tooltip("잠수 순간 재생할 물방울 파티클 프리팹")]
    [SerializeField] private GameObject _bubblePrefab;
    [Tooltip("플레이어 기준 물방울 생성 오프셋")]
    [SerializeField] private Vector3 _bubbleOffset = new Vector3(0f, 0.5f, 1f);
    [Tooltip("생성된 물방울 오브젝트를 자동 파괴하기까지의 시간(초)")]
    [SerializeField] private float _bubbleLifetime = 3f;
    

    [Header("다이브 힘")]
    [Tooltip("잠수 시작 시 아래로 주는 순간 힘의 크기")]
    [SerializeField] private float _diveForce = 20f;

    private bool _playWaiting;
    private Rigidbody _playerRb;
    private CUnderwaterEffect _underwaterEffect; // 비우면 펄스 생략

    // 실행 우선순위 정의
    public EUpdatePriority UpdatePriority => EUpdatePriority.Last;

    // 프레임 매니저에게 호출당할 함수
    public void ExecuteUpdateFrame()
    {
        // 잠수함 내부: 다음 번 나감을 대기 상태로 둔다.
        if (_player.CurrentState == EPlayerState.OnGround)
        {
            _playWaiting = true;
        }
        // 수영으로 전환된 순간(잠수 시작): 연출 1회 재생.
        else if (_playWaiting)
        {
            _playWaiting = false;
            PlayDive();
        }
    }

    public void InjectUnderwaterEffect(CUnderwaterEffect reference)
    {
        _underwaterEffect = reference;
    }

    // 잠수 시작 연출: 사운드 + 물방울 + 색감 펄스 + 아래 힘.
    private void PlayDive()
    {
#if UNITY_EDITOR
        if(_underwaterEffect == null)
        {
            UDebug.Print("CUnderwaterEffect가 할당되지 않았습니다. 잠수 연출이 생략됩니다.", LogType.Error);
        }
#endif
        USound.PlaySfx(Id.SFX_Dive_02);

        SpawnBubble();

        // 화면 색감/일렁임 펄스
        if (_underwaterEffect != null) _underwaterEffect.PlayDivePulse();

        // 아래로 큰 힘(순간 임펄스). 플레이어 Rigidbody를 직접 조작한다.
        if (_playerRb == null && _player != null) _playerRb = _player.GetComponent<Rigidbody>();
        if (_playerRb != null) _playerRb.AddForce(Vector3.down * _diveForce, ForceMode.Impulse);
    }

    // 플레이어 위치 + 오프셋에 물방울 프리팹을 생성하고 일정 시간 후 파괴한다.
    private void SpawnBubble()
    {
        if (_bubblePrefab == null || _player == null) return;

        Vector3 pos = _player.transform.position + _player.transform.TransformVector(_bubbleOffset);
        GameObject bubble = Instantiate(_bubblePrefab, pos, Quaternion.identity);
        Destroy(bubble, _bubbleLifetime);
    }
}
