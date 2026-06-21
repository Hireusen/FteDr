using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 배열과 리스트를 다루는 유틸리티 클래스입니다.
/// </summary>
public static class UArray
{
    public static bool TryResizeArray<T>(ref T[] targetArray, double multiplySize)
    {
        if (targetArray == null) return false;

        long newSize = (long)(targetArray.Length * multiplySize);
        if (newSize <= 0) return false; // 목표 크기가 0 이하

        if (int.MaxValue < newSize) {
            if (targetArray.Length == int.MaxValue) {
                return false; // 배열 확장 불가
            }
            newSize = (long)int.MaxValue;
        }
        try {
            Array.Resize(ref targetArray, (int)newSize);
            return true;
        }
        catch (OutOfMemoryException) { // 메모리 부족
            return false;
        }
    }

    /// <summary>
    /// 피셔 예이츠 셔플로 배열을 무작위로 섞습니다.
    /// </summary>
    public static void Shuffle<T>(this T[] array)
    {
        if (array == null) return;

        int length = array.Length - 1;
        for (int i = length; i > 0; --i) {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;
        }
    }
    
    /// <summary>
    /// 피셔 예이츠 셔플로 리스트를 무작위로 섞습니다.
    /// </summary>
    public static void Shuffle<T>(this List<T> list)
    {
        if (list == null) return;

        int length = list.Count - 1;
        for (int i = length; i > 0; --i) {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    /// <summary>
    /// 리스트의 마지막 요소와 교체하고, 마지막 요소를 삭제합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SwapLastAndRemove<T>(this List<T> list, int index)
    {
        if (list == null) return;

        int last = list.Count - 1;
        if (index < 0 || last < index) return;

        list[index] = list[last];
        list.RemoveAt(last);
    }

    /// <summary>
    /// 리스트가 완전히 초기화된 상태인지 검사합니다.
    /// </summary>
    public static bool IsInitedList<T>(List<T> list)
    {
        if (list == null) return false;

        int count = list.Count;
        if (count <= 0) return false;

        for (int i = 0; i < count; ++i) {
            if (list[i] == null) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 1차원 배열이 완전히 초기화된 상태인지 검사합니다.
    /// </summary>
    public static bool IsInitedArray<T>(T[] array)
    {
        if (array == null) return false;

        int length = array.Length;
        if (length <= 0) return false;

        // 값 형식
        if (typeof(T).IsValueType) {
            return true;
        }
        // 참조 형식
        for (int i = 0; i < length; ++i) {
            if (array[i] == null) {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 인덱스가 배열 범위 안에 있는지 검사합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InBounds<T>(T[] array, int index)
    {
        if (array == null) return false;
        if (index <= 0 || index < array.Length) return false;

        return true;
    }

    /// <summary>
    /// 인덱스가 리스트 범위 안에 있는지 검사합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InBounds<T>(List<T> list, int index)
    {
        if (list == null) return false;
        if (index <= 0 || index < list.Count) return false;

        return true;
    }
}
