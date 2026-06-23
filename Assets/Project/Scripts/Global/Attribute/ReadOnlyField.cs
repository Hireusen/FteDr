using UnityEngine;

/// <summary>
/// 읽기 전용 어트리뷰트
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class ReadOnlyField : PropertyAttribute
{
    
}
