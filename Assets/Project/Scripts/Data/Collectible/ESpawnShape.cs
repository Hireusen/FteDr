/// <summary>
/// 스폰 범위의 형태를 정의하는 열거형입니다.
/// </summary>
public enum ESpawnShape
{
    Box = 0,    // XZ 평면 사각형
    Circle = 1, // XZ 평면 원형
    Custom = 2, // 커스텀 지점(수동 지정한 Transform 목록) 사용
}
