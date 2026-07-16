using UnityEngine;
using System.Runtime.CompilerServices;

/// <summary>
/// 유니티 오브젝트 확장 메서드를 담는 유틸리티입니다.
/// </summary>
public static class ObjectExtension
{
    /// <summary>
    /// 컴포넌트를 가져오되 없을 경우 부착하여 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        return go.TryGetComponent(out T component) ? component : go.AddComponent<T>();
    }

    /// <summary>
    /// 게임 오브젝트가 해당 마스크에 포함되어있는지 확인합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInLayerMask(this GameObject go, LayerMask mask)
    {
        return (mask.value & (1 << go.layer)) != 0;
    }

    /// <summary>
    /// 레이어가 해당 마스크에 포함되어있는지 확인합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInLayerMask(this int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
