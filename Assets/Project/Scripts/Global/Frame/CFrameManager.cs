using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 매 프레임 실행되는 로직을 총괄하는 매니저
/// </summary>
public sealed class CFrameManager : ASingleton<CFrameManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    // 배열 안에 리스트가 있는 구조
    private List<IUpdateFrameable>[] _updateFrames;
    private List<ILateUpdateFrameable>[] _lateUpdateFrames;
    private List<IFixedUpdateFrameable>[] _fixedUpdateFrames;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    // 부트 매니저에게 호출당할 초기화 메서드
    protected override void Initialize()
    {
        // 열거형 전달하여 리스트 초기화
        _updateFrames = InitFrames<IUpdateFrameable>(typeof(EUpdatePriority));
        _lateUpdateFrames = InitFrames<ILateUpdateFrameable>(typeof(ELateUpdatePriority));
        _fixedUpdateFrames = InitFrames<IFixedUpdateFrameable>(typeof(EFixedUpdatePriority));
    }

    public void Register(IUpdateFrameable frameable)
    {
        TryAddFrame(_updateFrames[(int)frameable.UpdatePriority], frameable);
    }
    public void Register(ILateUpdateFrameable frameable)
    {
        TryAddFrame(_lateUpdateFrames[(int)frameable.LateUpdatePriority], frameable);
    }
    public void Register(IFixedUpdateFrameable frameable)
    {
        TryAddFrame(_fixedUpdateFrames[(int)frameable.FixedUpdatePriority], frameable);
    }

    public void Unregister(IUpdateFrameable frameable)
    {
        TryRemoveFrame(_updateFrames[(int)frameable.UpdatePriority], frameable);
    }
    public void Unregister(ILateUpdateFrameable frameable)
    {
        TryRemoveFrame(_lateUpdateFrames[(int)frameable.LateUpdatePriority], frameable);
    }
    public void Unregister(IFixedUpdateFrameable frameable)
    {
        TryRemoveFrame(_fixedUpdateFrames[(int)frameable.FixedUpdatePriority], frameable);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private List<T>[] InitFrames<T>(Type enumType)
    {
        int length = Enum.GetValues(enumType).Length;
        var arr = new List<T>[length];
        for (int i = 0; i < length; ++i) {
            arr[i] = new List<T>();
        }
        return arr;
    }
    private void TryAddFrame<T>(List<T> list, T frameable)
    {
        if (frameable == null) {
            UDebug.Print($"존재하지 않는 객체가 {typeof(T).Name} 타입 프레임 매니저에 가입을 시도했습니다.", LogType.Error);
            return;
        }
        list.Add(frameable);
    }
    private void TryRemoveFrame<T>(List<T> list, T frameable)
    {
        if (frameable == null) {
            UDebug.Print($"존재하지 않는 객체가 {typeof(T).Name} 타입 프레임 매니저에서 탈퇴를 시도했습니다.", LogType.Error);
            return;
        }
        int index = list.IndexOf(frameable);
        if (index >= 0) {
            UArray.SwapLastAndRemove(list, index); // 스왑 앤 팝
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Update()
    {
        if (!IsInitialized) return;

        int length = _updateFrames.Length;
        for (int i = 0; i < length; ++i) {
            List<IUpdateFrameable> list = _updateFrames[i];
            for (int j = list.Count - 1; j >= 0; --j) {
                list[j].ExecuteUpdateFrame();
            }
        }
    }
    private void FixedUpdate()
    {
        if (!IsInitialized) return;

        int length = _fixedUpdateFrames.Length;
        for (int i = 0; i < length; ++i) {
            List<IFixedUpdateFrameable> list = _fixedUpdateFrames[i];
            for (int j = list.Count - 1; j >= 0; --j) {
                list[j].ExecuteFixedUpdateFrame();
            }
        }
    }
    private void LateUpdate()
    {
        if (!IsInitialized) return;

        int length = _lateUpdateFrames.Length;
        for (int i = 0; i < length; ++i) {
            List<ILateUpdateFrameable> list = _lateUpdateFrames[i];
            for (int j = list.Count - 1; j >= 0; --j) {
                list[j].ExecuteLateUpdateFrame();
            }
        }
    }
    #endregion
}
