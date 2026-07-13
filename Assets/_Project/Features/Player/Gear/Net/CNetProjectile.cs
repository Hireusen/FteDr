using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그물 발사체입니다. 포물선으로 날아가며(Outbound) 회수 범위(자체 트리거 콜라이더) 안의 일반(비특수) 수집품을 걸고, <br/>
/// 일정 시간/최대 사거리에 도달하면 플레이어 쪽으로 되감기며(Reeling) 걸린 수집품을 데리고 돌아옵니다. 몸에 도착하는 순간 가방에 확정하고(꽉 차면 도착 지점에 떨굼) 그물은 사라집니다. <br/>
/// 회수 폭은 발사 시 자신의 x,z 로컬 스케일에 배율을 곱해 키웁니다. (콜라이더도 함께 커짐)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class CNetProjectile : AMono
{
    private enum EState { Outbound, Reeling }

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("발사 → 되감기 전환")]
    [Tooltip("발사 후 이 시간(초)이 지나면 되감기를 시작합니다.")]
    [SerializeField] private float _outboundDuration = 0.8f;
    [Tooltip("발사 지점에서 이 거리를 넘으면 즉시 되감기를 시작합니다. (0이면 무시, 시간만 사용)")]
    [SerializeField] private float _maxTravelDistance = 0f;

    [Header("되감기")]
    [Tooltip("되감기 시작 속도")]
    [SerializeField] private float _reelStartSpeed = 4f;
    [Tooltip("되감기 가속도(줄에 감기듯 점점 빨라짐)")]
    [SerializeField] private float _reelAccel = 30f;
    [Tooltip("이 거리 안에 들면 몸에 도착한 것으로 간주")]
    [SerializeField] private float _reelArriveDistance = 0.5f;

    [Header("회수 대상")]
    [Tooltip("집게에 잡혀 있는 수집품은 회수 대상에서 제외할지 여부입니다.")]
    [SerializeField] private bool _ignoreHeld = true;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Rigidbody _rb;

    private EState _state;
    private float _launchTime;
    private Vector3 _startPos;
    private Vector3 _prevPos;   // 지난 스텝 위치 (걸린 수집품 따라 이동용)
    private float _reelSpeed;
    private Transform _reelTarget;

    private readonly List<CCollectible> _caught = new();
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 발사체를 초기화하고 발사합니다.
    /// </summary>
    /// <param name="velocity">초기 속도 벡터(방향 × 속력)</param>
    /// <param name="catchScale">발사체 x,z 스케일 배율 (회수 폭)</param>
    /// <param name="reelTarget">되감길 대상(보통 플레이어)</param>
    public void Launch(Vector3 velocity, float catchScale, Transform reelTarget)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();

        // 프리팹 x,z 스케일에 배율을 곱해 회수 폭을 키움 (메시·콜라이더 함께 커짐, y는 유지)
        float k = Mathf.Max(0.01f, catchScale);
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(s.x * k, s.y, s.z * k);

        // 트리거 보정 (프리팹에서 Is Trigger를 켜두는 게 원칙이지만 방어)
        if (TryGetComponent(out Collider col)) col.isTrigger = true;
        else UDebug.Print("그물 발사체에 콜라이더가 없습니다. (BoxCollider, Is Trigger 필요)", LogType.Warning, gameObject);

        _reelTarget = reelTarget;

        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.velocity = velocity; // 중력이 포물선을 만듦

        _state = EState.Outbound;
        _launchTime = Time.time;
        _startPos = _rb.position;
        _prevPos = _rb.position;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void EnterReeling()
    {
        _state = EState.Reeling;
        _reelSpeed = _reelStartSpeed;
        _rb.isKinematic = true; // 물리 낙하 멈추고 직접 끌어당김
    }

    // 되감기 중 그물을 대상 쪽으로 이동시킵니다. 도착하면 회수를 마무리합니다.
    private void ReelStep()
    {
        if (_reelTarget == null)
        {
            Arrive();
            return;
        }

        Vector3 pos = _rb.position;
        Vector3 target = _reelTarget.position;

        if ((target - pos).sqrMagnitude <= _reelArriveDistance * _reelArriveDistance)
        {
            Arrive();
            return;
        }

        _reelSpeed += _reelAccel * Time.fixedDeltaTime;
        Vector3 next = Vector3.MoveTowards(pos, target, _reelSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(next);
    }

    // 걸린 수집품을 그물 이동량만큼 함께 옮깁니다. (자식으로 붙이지 않아 스케일 영향 없음)
    private void MoveCaughtWithNet()
    {
        Vector3 delta = _rb.position - _prevPos;
        _prevPos = _rb.position;

        if (delta.sqrMagnitude <= 0f) return;

        for (int i = 0; i < _caught.Count; ++i)
        {
            if (_caught[i] != null) _caught[i].transform.position += delta;
        }
    }

    // 그물에 수집품을 겁니다. (가방 확정은 도착 시)
    private void AttachCaught(CCollectible c)
    {
        _caught.Add(c);
        if (c.TryGetComponent(out Rigidbody crb)) crb.isKinematic = true; // 물리 간섭 제거
    }

    // 몸에 도착: 걸린 것들을 가방에 확정하고, 안 들어가면 도착 지점에 떨군 뒤 그물 제거.
    private void Arrive()
    {
        CPlayerManager pm = CPlayerManager.Ins;

        for (int i = 0; i < _caught.Count; ++i)
        {
            CCollectible c = _caught[i];
            if (c == null) continue;

            bool bagged = pm != null && c.Data != null && pm.TryAddToBag(c.Data.Id);
            if (bagged)
            {
                c.gameObject.SetActive(false); // 수거 완료 (풀링 도입 시 풀 반환)
            }
            else if (c.TryGetComponent(out Rigidbody crb))
            {
                crb.isKinematic = false; // 가방 꽉 참: 도착 지점에 떨굼 (물리 복구)
            }
        }

        _caught.Clear();
        Destroy(gameObject);
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        MoveCaughtWithNet();

        if (_state == EState.Outbound)
        {
            bool timeUp = Time.time - _launchTime >= _outboundDuration;
            bool tooFar = _maxTravelDistance > 0f
                          && (_rb.position - _startPos).sqrMagnitude >= _maxTravelDistance * _maxTravelDistance;

            if (timeUp || tooFar) EnterReeling();
        }
        else // Reeling
        {
            ReelStep();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 콜라이더가 자식에 있을 수 있으니 부모에서 CCollectible을 찾습니다.
        CCollectible c = other.GetComponentInParent<CCollectible>();
        if (c == null) return;
        if (c.IsSpecial) return;              // 특수 수집품 제외
        if (_ignoreHeld && c.IsHeld) return;
        if (!c.gameObject.activeSelf) return;  // 이미 수거된 것 제외
        if (c.Data == null) return;
        if (_caught.Contains(c)) return;       // 이미 걸린 것 제외

        AttachCaught(c);
    }
    #endregion
}
