using System.Collections;
using UnityEngine;

/// <summary>
/// 수집품의 물리를 동적으로 깨우고 재우는 컴포넌트입니다. (CCollectible과 같은 루트에 부착)
///
/// [설계 배경]
/// 수집품은 평소 Rigidbody 없이 정적 상태로 둔다(다수의 물체 성능 최적화).
/// 하지만 특정 순간에는 물리가 필요하다:
///   - 인형뽑기: 집게가 건드리거나 밀 때
///   - 본 게임: 벽/다른 물체에 부딪혀 떨어져야 할 때
/// 이때 WakePhysics()로 Rigidbody를 붙여 물리를 켜고,
/// 잠잠해지면(속도가 낮게 유지) 자동으로 SleepPhysics()로 제거해 다시 정적 상태로 돌린다.
///
/// 스포너의 낙하-안정화와 같은 원리이되, 언제든 다시 깨울 수 있는 재사용 형태다.
/// </summary>
[RequireComponent(typeof(CCollectible))]
public sealed class CCollectiblePhysics : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("깨우기")]
    [Tooltip("깨울 때 중력 사용 여부")]
    [SerializeField] private bool _useGravity = true;

    [Header("자동 재우기")]
    [Tooltip("이 속도(초당) 미만이면 정지로 간주")]
    [SerializeField] private float _sleepSpeed = 0.05f;
    [Tooltip("정지 상태가 이 시간(초) 지속되면 Rigidbody 제거")]
    [SerializeField] private float _sleepHoldTime = 0.6f;
    [Tooltip("false면 자동으로 재우지 않음(수동 SleepPhysics 호출 필요)")]
    [SerializeField] private bool _autoSleep = true;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CCollectible _collectible;
    private Coroutine _sleepRoutine;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>현재 물리가 깨어있는지(Rigidbody 존재) 여부입니다.</summary>
    public bool IsAwake => TryGetComponent(out Rigidbody _);

    /// <summary>현재 Rigidbody입니다. 잠든 상태면 null.</summary>
    public Rigidbody Body => TryGetComponent(out Rigidbody rb) ? rb : null;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ 깨우기 / 재우기 ◀─────────────────────────
    /// <summary>
    /// 물리를 깨웁니다. Rigidbody가 없으면 붙이고, 질량을 수집품 무게로 설정합니다.
    /// autoSleep이 켜져 있으면 잠잠해질 때 자동으로 재웁니다.
    /// </summary>
    public Rigidbody WakePhysics()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.mass = Mathf.Max(0.01f, _collectible.Weight);
        rb.useGravity = _useGravity;
        rb.WakeUp();

        // 잡혀 있는 동안엔 자동 재우기를 걸지 않음 (집게가 관리)
        if (_autoSleep && !_collectible.IsHeld)
        {
            RestartSleepWatch(rb);
        }
        return rb;
    }

    /// <summary>물리를 재웁니다. Rigidbody를 제거해 정적 상태로 되돌립니다.</summary>
    public void SleepPhysics()
    {
        StopSleepWatch();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        _collectible = GetComponent<CCollectible>();
    }

    // 다른 물체가 세게 부딪히면 물리를 깨운다. (기획: 벽/충돌 시 떨어지도록)
    private void OnCollisionEnter(Collision collision)
    {
        // 이미 깨어있으면 스포너/기존 물리가 처리 중이므로 무시
        if (IsAwake) return;

        WakePhysics();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 정지 감시 코루틴을 재시작한다.
    private void RestartSleepWatch(Rigidbody rb)
    {
        StopSleepWatch();
        _sleepRoutine = StartCoroutine(CoSleepWatch(rb));
    }

    private void StopSleepWatch()
    {
        if (_sleepRoutine != null)
        {
            StopCoroutine(_sleepRoutine);
            _sleepRoutine = null;
        }
    }

    // 속도가 임계값 미만으로 일정 시간 유지되면 재운다. (잡혀 있으면 대기)
    private IEnumerator CoSleepWatch(Rigidbody rb)
    {
        float still = 0f;
        float sleepSqr = _sleepSpeed * _sleepSpeed;

        while (rb != null)
        {
            // 집게에 잡혀 있는 동안은 재우지 않음
            if (_collectible.IsHeld)
            {
                still = 0f;
                yield return null;
                continue;
            }

            if (rb.velocity.sqrMagnitude <= sleepSqr)
            {
                still += Time.deltaTime;
                if (still >= _sleepHoldTime) break;
            }
            else
            {
                still = 0f;
            }
            yield return null;
        }

        _sleepRoutine = null;
        if (!_collectible.IsHeld)
        {
            SleepPhysics();
        }
    }
    #endregion
}
