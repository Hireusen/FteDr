using System;
using UnityEngine;

/// <summary>
/// 특정 타입 이벤트의 구독/해제/발행을 담당하는 클래스
/// </summary>
public static class CEventBus<T> where T : struct
{
    private static event Action<T> _event;

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>
    /// 메서드가 해당 타입을 구독합니다.
    /// </summary>
    /// <param name="action">구독시킬 메서드</param>
    public static void Subscribe(Action<T> action)
    {
        _event += action;
        UDebug.Print($"메서드({action.Method.Name})가 이벤트 버스({typeof(T).Name})를 구독했습니다.");
    }

    /// <summary>
    /// 메서드가 해당 타입을 구독 해제합니다.
    /// </summary>
    /// <param name="action">구독 해제할 메서드</param>
    public static void Unsubscribe(Action<T> action)
    {
        _event -= action;
        UDebug.Print($"메서드({action.Method.Name})가 이벤트 버스({typeof(T).Name})를 구독 해제했습니다.");
    }

    /// <summary>
    /// 해당 타입 이벤트를 구독한 모든 메서드를 호출합니다.
    /// </summary>
    /// <param name="data">데이터를 담은 구조체</param>
    public static void Publish(T data)
    {
        _event?.Invoke(data);
    }

    // 유니티엔진 플레이 모드 종료 시 static 변수 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        _event = null;
    }
    #endregion
}
