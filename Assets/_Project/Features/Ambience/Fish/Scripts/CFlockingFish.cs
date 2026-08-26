using UnityEngine;

/// <summary>
/// 무거운 군집(Boids) 알고리즘을 제거하고 타겟만을 쫓아 이동하는 초경량 물고기 클래스입니다.
/// </summary>
public sealed class CFlockingFish : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("이동 설정")]
    [SerializeField] private float _averageSpeed = 2f;
    [SerializeField, Range(0.5f, 12f)] private float _turnSpeed = 3f;
    [SerializeField, Tooltip("타겟 주변으로 흩어질 반경입니다.")] private float _spreadRadius = 3f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CFlockingGroup _flock; // 소속 군집 참조
    private Vector3 _targetOffset; // 타겟 중심점으로부터 흩어질 고유 로컬 오프셋
    private float _speed;          // 개체별 고정 이동 속도
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public CFlockingGroup Flock
    {
        get => _flock;
        set => _flock = value;
    }

    /// <summary>외부 초기화 시 할당받을 평균 속도입니다.</summary>
    public float AverageSpeed
    {
        get => _averageSpeed;
        set => _averageSpeed = value;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    /// <summary>시작 시 고유 속도와 타겟 주변의 목표 오프셋을 설정합니다.</summary>
    private void Start()
    {
        // 약간의 속도 차이를 주어 획일적인 움직임 방지
        _speed = Random.Range(0.8f, 1.2f) * _averageSpeed;

        // 타겟 중심에 겹치지 않도록 반경 내 무작위 좌표를 오프셋으로 지정
        _targetOffset = Random.insideUnitSphere * _spreadRadius;
    }

    /// <summary>매 프레임 무리 중심을 향해 회전하고 직진합니다.</summary>
    public void ExecuteUpdateFrame()
    {
        if (_flock == null || _flock.Target == null) return;

        // 개체의 최종 목적지 산출 (타겟 위치 + 고유 오프셋)
        Vector3 destination = _flock.Target.position + _targetOffset;
        Vector3 direction = destination - transform.position;

        // 목적지와의 거리가 아주 가까우면 회전을 생략하여 떨림(Jitter) 방지
        if (direction.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _turnSpeed * Time.deltaTime);
        }

        // 로컬 Z축(정면)으로 지속 전진
        transform.Translate(0f, 0f, _speed * Time.deltaTime);
    }
    #endregion
}
