using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// 랜덤 값을 반환하는 유틸리티 클래스입니다.
/// </summary>
public class URandom
{
    /// <summary>
    /// 0 ~ 1 범위 확률로 True를 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Chance(float chance)
    {
        return UnityEngine.Random.value < chance;
    }

    private static readonly Vector3[] _directions = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.up, Vector3.down };
    /// <summary>
    /// 3D 방향 벡터 여섯 가지 중 하나를 랜덤으로 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetAxis()
    {
        return _directions[Random.Range(0, _directions.Length)];
    }

    private static readonly Color[] _colors = { Color.yellow, Color.red, Color.white, Color.blue, Color.green, Color.gray, Color.red, Color.black, Color.cyan, Color.magenta };
    /// <summary>
    /// 유니티에서 기본적으로 제공하는 색깔 중 하나를 랜덤으로 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color GetColor()
    {
        return _colors[Random.Range(0, _colors.Length)];
    }
}
