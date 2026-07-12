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

    /// <summary>
    /// XZ 평면 사각형 범위 안의 랜덤한 위치를 반환합니다.
    /// </summary>
    /// <param name="center">사각형 중심</param>
    /// <param name="sizeX">가로(X) 전체 길이</param>
    /// <param name="sizeZ">세로(Z) 전체 길이</param>
    public static Vector3 PointInBox(Vector3 center, float sizeX, float sizeZ)
    {
        float x = Random.Range(-sizeX * 0.5f, sizeX * 0.5f);
        float z = Random.Range(-sizeZ * 0.5f, sizeZ * 0.5f);
        return new Vector3(center.x + x, center.y, center.z + z);
    }

    /// <summary>
    /// XZ 평면 원형 범위 안의 랜덤한 위치를 반환합니다. (y는 center.y 그대로)
    /// 면적 기준 균등 분포(중심 쏠림 없음).
    /// </summary>
    /// <param name="center">원 중심</param>
    /// <param name="radius">반지름</param>
    public static Vector3 PointInCircle(Vector3 center, float radius)
    {
        // sqrt로 반지름을 보정해야 면적 균등 분포가 됨
        float r = radius * Mathf.Sqrt(Random.value);
        float angle = Random.value * Mathf.PI * 2f;
        return new Vector3(
            center.x + Mathf.Cos(angle) * r,
            center.y,
            center.z + Mathf.Sin(angle) * r);
    }

    /// <summary>
    /// 모든 축에 대해 완전 랜덤한 회전을 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Rotation()
    {
        return Random.rotationUniform;
    }

    /// <summary>
    /// Y축(수평 방향)만 랜덤한 회전을 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion RotationYaw()
    {
        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }
}
