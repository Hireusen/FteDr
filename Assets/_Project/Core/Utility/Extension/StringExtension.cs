using System.Runtime.CompilerServices;

/// <summary>
/// 문자열 확장 메서드를 담는 유틸리티입니다.
/// </summary>
public static class StringExtension
{
    /// <summary>
    /// 문자열이 공백 또는 비어있는지 확인합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBlank(this string str) // 콜스택을 호출을 하면 
    {
        return string.IsNullOrWhiteSpace(str);
    }

    public static void Test()
    {
        //string.IsNullOrWhiteSpace(str)
        // IsBlank → 함수간 이동이 하나 더있어요
    }
    
    /// <summary>
    /// 문자열이 채워져 있는지 확인합니다.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotBlank(this string str)
    {
        return !IsBlank(str);
    }
}
