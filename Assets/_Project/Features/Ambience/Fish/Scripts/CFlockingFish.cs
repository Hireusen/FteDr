using UnityEngine;

/// <summary>
/// 군집 이동(Flocking) 행동을 하는 개별 물고기 클래스입니다.
/// </summary>
public sealed class CFlockingFish : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("이동 설정")]
    [SerializeField] private float _averageSpeed = 2f;
    [SerializeField] private int _performance = 4;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private float _speed;
    private float _neighborDistance = 3.0f;
    private bool _isTurning = false;
    private CFlockingGroup _flock;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;

    public CFlockingGroup Flock
    {
        get => _flock;
        set => _flock = value;
    }

    /// <summary>외부 초기화 시 할당받을 평균 속도 프로퍼티입니다.</summary>
    public float AverageSpeed
    {
        get => _averageSpeed;
        set => _averageSpeed = value;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private void Start()
    {
        _speed = Random.Range(0.5f, 1.5f) * _averageSpeed;
    }

    public void ExecuteUpdateFrame()
    {
        ApplyTankBoundary();

        if (_isTurning)
        {
            if (_flock != null && _flock.Target != null)
            {
                Vector3 direction = _flock.Target.position + Vector3.up * Random.Range(-2f, 2f) - transform.position;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction),
                        GetTurnSpeed() * Time.deltaTime
                    );
                }
            }
        }
        else
        {
            if (Random.Range(0, _performance + 1) < 1)
            {
                ApplyRules();
            }
        }

        transform.Translate(0f, 0f, Time.deltaTime * _speed);
    }

    private void ApplyTankBoundary()
    {
        if (_flock == null || _flock.Target == null) return;

        float distance = Vector3.Distance(transform.position, _flock.Target.position);
        _isTurning = distance >= _flock.WanderSize;
    }

    private void ApplyRules()
    {
        if (_flock == null || _flock.Target == null) return;

        var allFish = _flock.AllFish;
        int fishCount = allFish.Count;

        _speed = Random.Range(0.5f, 1.5f) * _averageSpeed;

        Vector3 vCenter = _flock.Target.position;
        Vector3 vAvoid = Vector3.zero;
        float gSpeed = 0f;
        Vector3 goalPos = _flock.Target.position;

        int groupSize = 0;

        for (int i = 0; i < fishCount; ++i)
        {
            CFlockingFish otherFish = allFish[i];
            if (otherFish == this || otherFish == null) continue;

            float dist = Vector3.Distance(otherFish.transform.position, transform.position);
            if (dist <= _neighborDistance)
            {
                vCenter += otherFish.transform.position;
                groupSize++;

                if (dist < 0.75f)
                {
                    vAvoid += (transform.position - otherFish.transform.position);
                }

                gSpeed += otherFish._speed;
            }
        }

        if (groupSize > 0)
        {
            vCenter = vCenter / groupSize + (goalPos - transform.position);
            _speed = gSpeed / groupSize;

            Vector3 direction = (vCenter + vAvoid) - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    GetTurnSpeed() * Time.deltaTime
                );
            }
        }
    }

    private float GetTurnSpeed()
    {
        return Random.Range(0.2f, 0.4f) * _speed;
    }
    #endregion
}
