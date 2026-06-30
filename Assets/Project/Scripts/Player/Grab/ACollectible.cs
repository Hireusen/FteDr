using UnityEngine;

/// <summary>
/// 임시로 사용하는 수집품 추상 클래스 입니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class ACollectible : AMono
{
    protected Rigidbody _rb;

    public Rigidbody Rb => _rb;

    /// <summary>
    /// true면 인벤토리에 저장되는 일반 수집품,
    /// false면 집게(손)에 들고 다녀야 하는 특수 수집품.
    /// </summary>
    public abstract bool CanStoreInInventory { get; }

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// 집게에 잡혔을 때 호출됩니다. (사운드·이펙트 등은 하위 클래스에서)
    /// </summary>
    public virtual void OnGrabbed()
    {
        UDebug.Print($"[수집품] {name} 잡힘");
    }

    /// <summary>
    /// 집게에서 풀렸을 때 호출됩니다.
    /// </summary>
    public virtual void OnReleased()
    {
        UDebug.Print($"[수집품] {name} 풀림");
    }
}
