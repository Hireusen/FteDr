using UnityEngine;

/// <summary>
/// 집게(_clawObject)에 부착하는 충돌 감지 컴포넌트입니다.
/// 잡고 있는 동안 다른 물체에 부딪히면 CPlayerGrab.ForceRelease()를 호출하고,
/// FixedJoint가 breakForce로 끊어졌을 때도 해제 처리를 합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CClawCollision : MonoBehaviour
{
    private CPlayerGrab _owner;

    /// <summary>
    /// CPlayerGrab.OnEnable에서 호출되어 소유자를 연결합니다.
    /// </summary>
    public void Init(CPlayerGrab owner)
    {
        _owner = owner;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_owner == null) return;

        // 잡고 있던 아이템 자신과의 충돌은 무시할 수 있음 (필요 시 태그/레이어로 필터)
        _owner.ForceRelease();
    }

    // FixedJoint가 breakForce를 초과해 끊어지면 호출됨
    private void OnJointBreak(float breakForce)
    {
        if (_owner == null) return;
        _owner.ForceRelease();
    }
}
