using System.Runtime.CompilerServices;
using UnityEngine;
/// <summary>
/// 수학 관련 연산을 돕는 유틸리티입니다.
/// </summary>
public class UMath : MonoBehaviour
{
    /// <summary>
    /// 지수 보간한 값(0~1)을 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetSmoothT(float sharpness, float deltatime)
    {
        return 1f - Mathf.Exp(-sharpness * deltatime);
    }

    /// <summary>
    /// 두 벡터의 각 요소를 비교해서 가장 작은 벡터와 가장 큰 벡터를 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector2 minPos, Vector2 maxPos) SortNumericSize(Vector2 pos1, Vector2 pos2)
    {
        if (pos2.x <= pos1.x)
        {
            float tmp = pos1.x;
            pos1.x = pos2.x;
            pos2.x = tmp;
        }
        if (pos2.y <= pos1.y)
        {
            float tmp = pos1.y;
            pos1.y = pos2.y;
            pos2.y = tmp;
        }
        return (pos1, pos2);
    }

    /// <summary>
    /// 두 벡터의 각 요소를 비교해서 가장 작은 벡터와 가장 큰 벡터를 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector2Int minPos, Vector2Int maxPos) SortNumericSize(Vector2Int pos1, Vector2Int pos2)
    {
        if (pos2.x <= pos1.x)
        {
            int tmp = pos1.x;
            pos1.x = pos2.x;
            pos2.x = tmp;
        }
        if (pos2.y <= pos1.y)
        {
            int tmp = pos1.y;
            pos1.y = pos2.y;
            pos2.y = tmp;
        }
        return (pos1, pos2);
    }

    /// <summary>
    /// 두 좌표를 비교해서 가장 작은 좌표와 가장 큰 좌표를 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (float minX, float maxX, float minY, float maxY) SortNumericSize(float x1, float y1, float x2, float y2)
    {
        if (x2 <= x1)
        {
            float tmp = x1;
            x1 = x2;
            x2 = tmp;
        }
        if (y2 <= y1)
        {
            float tmp = y1;
            y1 = y2;
            y2 = tmp;
        }
        return (x1, x2, x1, x2);
    }

    /// <summary>
    /// 두 벡터 사이의 거리의 제곱을 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetDistanceSquare(Vector2 pos1, Vector2 pos2)
    {
        float dx = pos2.x - pos1.x;
        float dy = pos2.y - pos1.y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// 두 좌표 사이의 거리의 제곱을 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetDistanceSquare(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// 두 점이 거리 이내에 있는지 검사합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWithinDistance(Vector2 pos1, Vector2 pos2, float distance)
    {
        if (GetDistanceSquare(pos1, pos2) <= distance * distance)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 원점을 기준으로 벡터를 회전시킵니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Rotate(Vector2 vector, float radian)
    {
        float cos = Mathf.Cos(radian);
        float sin = Mathf.Sin(radian);
        // 2차원 회전 행렬
        float rotateX = vector.x * cos - vector.y * sin;
        float rotateY = vector.x * sin + vector.y * cos;
        vector.x = rotateX;
        vector.y = rotateY;
        return vector;
    }

    /// <summary>
    /// 점이 중심점을 기준으로 회전한 좌표를 반환합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 RotatePoint(Vector2 center, Vector2 pos, float radian)
    {
        // center가 0일때 pos의 값
        float originX = pos.x - center.x;
        float originY = pos.y - center.y;
        // 회전했을 경우의 좌표
        float cos = Mathf.Cos(radian);
        float sin = Mathf.Sin(radian);
        float rotateX = originX * cos - originY * sin;
        float rotateY = originX * sin + originY * cos;
        // 결과
        pos.x = rotateX + center.x;
        pos.y = rotateY + center.y;
        return pos;
    }

    /// <summary>
    /// 클램프한 벡터를 반환합니다.
    /// </summary>
    /// <returns></returns>
    public static Vector2 ClampVector(Vector2 v, Vector2 min, Vector2 max)
    {
        if (v.x < min.x) v.x = min.x;
        if (v.y < min.y) v.y = min.y;
        if (max.x < v.x) v.x = max.x;
        if (max.y < v.y) v.y = max.y;
        return v;
    }

    /// <summary>
    /// 각도를 0° 이상 360° 미만 범위로 정규화합니다.
    /// </summary>
    public static float NormalizeAngle(float degree)
    {
        while (360 <= degree) degree -= 360;
        while (degree < 0) degree += 360;
        return degree;
    }
}
