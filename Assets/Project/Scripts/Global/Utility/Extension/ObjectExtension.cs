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
    /// 자식 오브젝트를 모두 파괴합니다.
    /// </summary>
    public static void DestroyChildren(this Transform parent)
    {
        int length = parent.childCount;
        for (int i = length - 1; i >= 0; --i) {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
