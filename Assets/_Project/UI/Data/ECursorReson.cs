using System;

/// <summary>
/// 커서를 표시(=시선 회전 차단)해야 하는 사유입니다.
/// 여러 사유가 동시에 성립할 수 있으며, 하나라도 켜져 있으면 커서가 보입니다.
/// </summary>
[Flags]
public enum ECursorReason
{
    None = 0,
    Menu = 1 << 0,         // 일시정지/설정 등 메뉴 열림
    FuelDepleted = 1 << 1, // 연료 고갈로 조작 불가
}
