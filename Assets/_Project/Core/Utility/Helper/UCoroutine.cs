using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 코루틴 기능을 제공하는 유틸리티입니다.
/// </summary>
public static class UCoroutine
{
    private static readonly Dictionary<float, WaitForSeconds> _intervals = new();

    /// <summary>
    /// 캐싱된 WaitForSeconds를 반환합니다.
    /// </summary>
    /// <param name="seconds">시간(초)</param>
    /// <returns></returns>
    public static WaitForSeconds GetWait(float seconds)
    {
        if (!_intervals.TryGetValue(seconds, out WaitForSeconds wfs)) {
            wfs = new WaitForSeconds(seconds);
            _intervals.Add(seconds, wfs);
        }
        return wfs;
    }

    /// <summary>
    /// 동기 작업으로 특정 시간이 지난 후 액션을 실행합니다.
    /// </summary>
    /// <param name="delay">대기할 시간(초)</param>
    /// <param name="callback">대기 후 실행할 메서드</param>
    /// <returns></returns>
    public static IEnumerator DelayAction(float delay, Action callback)
    {
        float remain = delay;
        while (remain > 0f) {
            yield return null;
            remain -= Time.deltaTime;
        }
        callback?.Invoke();
    }

    /// <summary>
    /// 비동기 작업의 진행 상황을 콜백으로 받으며 대기합니다.
    /// </summary>
    /// <param name="asyncOp">비동기 클래스</param>
    /// <param name="onProgress">진행율을 받을 메서드</param>
    /// <returns></returns>
    public static IEnumerator WaitAsyncOperation(AsyncOperation asyncOp, Action<float> onProgress = null)
    {
        if (asyncOp == null) {
            yield break;
        }
        // 비동기 완료까지 반복
        while (!asyncOp.isDone) {
            yield return null;
            onProgress?.Invoke(asyncOp.progress);
        }
        onProgress?.Invoke(1f);
    }
}
