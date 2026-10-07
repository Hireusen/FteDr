using UnityEngine;

/// <summary>
/// 현재 장소에 따라 플레이어 주변에 파티클을 생성한다.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class CDynamicParticle : AFrameable, ILateUpdateFrameable
{
    #region ─────────────────────────▷ 내부 변수 ◁─────────────────────────
    [Header("추적 대상")]
    [SerializeField] private Transform _targetTr;

    [Header("파티클 재생 불가 영역")]
    [SerializeField] private LayerMask _norWaterLayer;
    [SerializeField] private bool _alwaysPlay = false;

    [Header("파티클 연결")]
    [SerializeField] private ParticleSystem _particle;

    [Header("파티클 위치")]
    [SerializeField] private Vector3 _offset;
    #endregion

    #region ─────────────────────────▷ 위치 조정 ◁─────────────────────────
    public ELateUpdatePriority LateUpdatePriority => ELateUpdatePriority.Last;

    public void ExecuteLateUpdateFrame()
    {
        if (_targetTr == null)
        {
            GameObject player = CGameManager.Player;
            if (player == null) return;

            transform.position = player.transform.position + _offset;
        }
        else
        {
            transform.position = _targetTr.position + _offset;
        }

    }
    #endregion

    #region ─────────────────────────▷ 영역에 따라 온오프 ◁─────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer.IsInLayerMask(_norWaterLayer))
        {
            if (_particle == null) return;
            _particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            UDebug.Print("스테이지 파티클을 비활성화했습니다.", LogType.Log, this);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer.IsInLayerMask(_norWaterLayer))
        {
            if (_particle == null) return;
            _particle.Play();
            UDebug.Print("스테이지 파티클을 활성화했습니다.", LogType.Log, this);
        }
    }
    #endregion

    private void Awake()
    {
        if (UDebug.IsNull(_particle)) return;
        if (_particle == null) return;

        if (_alwaysPlay)
        {
            _particle.Play();
            UDebug.Print("초기 스테이지 파티클을 활성화했습니다.", LogType.Log, this);
        }
        else
        {
            _particle.Stop();
            UDebug.Print("초기 스테이지 파티클을 비활성화했습니다.", LogType.Log, this);
        }
    }
}
