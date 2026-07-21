using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// 레이어 마스크 관련 확장 메서드 유틸리티입니다.
/// </summary>
public static class LayerExtension
{
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

    /// <summary>
    /// 마스크가 비어있는지(어떤 레이어도 포함하지 않는지) 확인합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this LayerMask mask)
    {
        return mask.value == 0;
    }

    /// <summary>
    /// 마스크가 하나 이상의 레이어를 포함하는지 확인합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotEmpty(this LayerMask mask)
    {
        return mask.value != 0;
    }

    /// <summary>
    /// 마스크가 정확히 하나의 레이어만 포함하는지 확인합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSingleLayer(this LayerMask mask)
    {
        return mask.value != 0 && (mask.value & (mask.value - 1)) == 0;
    }

    /// <summary>
    /// 마스크에 특정 레이어를 추가합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask AddLayer(this LayerMask mask, int layer)
    {
        mask.value |= (1 << layer);
        return mask;
    }

    /// <summary>
    /// 마스크에 이름으로 지정한 레이어를 추가합니다.
    /// </summary>
    public static LayerMask AddLayer(this LayerMask mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            mask.value |= (1 << layer);
        }
        return mask;
    }

    /// <summary>
    /// 마스크에서 특정 레이어를 제거합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask RemoveLayer(this LayerMask mask, int layer)
    {
        mask.value &= ~(1 << layer);
        return mask;
    }

    /// <summary>
    /// 마스크에서 이름으로 지정한 레이어를 제거합니다.
    /// </summary>
    public static LayerMask RemoveLayer(this LayerMask mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
        {
            mask.value &= ~(1 << layer);
        }
        return mask;
    }

    /// <summary>
    /// 마스크에서 특정 레이어의 포함 여부를 토글합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask ToggleLayer(this LayerMask mask, int layer)
    {
        mask.value ^= (1 << layer);
        return mask;
    }

    /// <summary>
    /// 마스크에 포함된 레이어의 개수를 반환합니다.
    /// </summary>
    public static int GetLayerCount(this LayerMask mask)
    {
        int value = mask.value;
        int count = 0;
        while (value != 0)
        {
            value &= value - 1;
            count++;
        }
        return count;
    }

    /// <summary>
    /// 마스크에 포함된 모든 레이어 인덱스를 반환합니다.
    /// </summary>
    public static List<int> GetLayers(this LayerMask mask)
    {
        List<int> layers = new List<int>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                layers.Add(i);
            }
        }
        return layers;
    }
}
