using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 키 리바인딩(커스텀 키 설정)을 담당하는 싱글톤입니다.
/// CInputManager가 쓰는 InputActionAsset에 대해 대화형 리바인딩, 기본값 복원,
/// 오버라이드 저장/복원을 제공합니다.
///
/// [저장] 바인딩 오버라이드를 JSON으로 뽑아 USaveFile("rebind")에 저장합니다.
///        (볼륨/해상도 저장과 같은 로컬 세이브 도메인)
///
/// [다중 장치/플랫폼] 리바인딩은 InputActionAsset 전체에 작용하므로,
///        에셋에 키보드/게임패드 등 여러 Binding·Control Scheme을 추가해 두면
///        같은 코드로 장치 구분 없이 동작합니다. (에셋 설정은 에디터에서)
/// </summary>
public sealed class CRebindManager : ASingleton<CRebindManager>
{
    #region ─────────────────────────▶ 상수 ◀─────────────────────────
    private const string FILE_NAME = "rebind";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private InputActionAsset _asset;                    // 리바인딩 대상 에셋
    private InputActionRebindingExtensions.RebindingOperation _op; // 진행 중인 리바인딩
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override bool IsGlobal => true;

    /// <summary>현재 리바인딩이 진행 중인지 여부입니다.</summary>
    public bool IsRebinding => _op != null;

    /// <summary>
    /// 리바인딩 대상 InputActionAsset을 등록합니다.
    /// CInputManager가 자신의 디스패처 에셋(_input.asset)을 넘겨 호출합니다.
    /// </summary>
    /// <param name="asset">입력 액션 에셋</param>
    public void SetAsset(InputActionAsset asset)
    {
        _asset = asset;
        Load(); // 등록 시 저장된 오버라이드를 즉시 복원
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ 리바인딩 ◀─────────────────────────
    /// <summary>
    /// 특정 액션의 특정 바인딩을 대화형으로 다시 설정합니다.
    /// 호출 후 플레이어가 누르는 다음 입력이 새 바인딩이 됩니다.
    /// </summary>
    /// <param name="actionName">액션 이름 (예: "Jump")</param>
    /// <param name="bindingIndex">바인딩 인덱스 (단일 키면 0)</param>
    /// <param name="onComplete">완료 콜백 (성공/취소 공통, 완료 후 호출)</param>
    /// <param name="excludeMouse">마우스 입력을 리바인딩 대상에서 제외할지</param>
    public void StartRebind(string actionName, int bindingIndex, Action onComplete, bool excludeMouse = true)
    {
        if (_asset == null)
        {
            UDebug.Print("CRebindManager: 에셋이 등록되지 않았습니다. SetAsset 먼저 호출하세요.", LogType.Error);
            onComplete?.Invoke();
            return;
        }

        InputAction action = _asset.FindAction(actionName);
        if (action == null)
        {
            UDebug.Print($"CRebindManager: '{actionName}' 액션을 찾을 수 없습니다.", LogType.Error);
            onComplete?.Invoke();
            return;
        }

        // 진행 중인 리바인딩이 있으면 정리
        CancelRebind();

        // 리바인딩 중에는 해당 액션을 비활성화해야 함(입력이 즉시 소비되는 것 방지)
        action.Disable();

        _op = action.PerformInteractiveRebinding(bindingIndex);
        if (excludeMouse)
        {
            _op = _op.WithControlsExcluding("<Mouse>/position")
                     .WithControlsExcluding("<Mouse>/delta");
        }

        _op = _op.OnComplete(operation =>
                 {
                     action.Enable();
                     DisposeOp();
                     Save();
                     onComplete?.Invoke();
                 })
                 .OnCancel(operation =>
                 {
                     action.Enable();
                     DisposeOp();
                     onComplete?.Invoke();
                 });

        _op.Start();
    }

    /// <summary>진행 중인 리바인딩을 취소합니다.</summary>
    public void CancelRebind()
    {
        if (_op == null) return;

        _op.Cancel();
        DisposeOp();
    }

    /// <summary>특정 액션의 특정 바인딩을 기본값으로 되돌립니다.</summary>
    /// <param name="actionName">액션 이름</param>
    /// <param name="bindingIndex">바인딩 인덱스</param>
    public void ResetBinding(string actionName, int bindingIndex)
    {
        if (_asset == null) return;

        InputAction action = _asset.FindAction(actionName);
        if (action == null) return;

        action.RemoveBindingOverride(bindingIndex);
        Save();
    }

    /// <summary>모든 바인딩을 기본값으로 되돌립니다.</summary>
    public void ResetAll()
    {
        if (_asset == null) return;

        foreach (InputActionMap map in _asset.actionMaps)
        {
            map.RemoveAllBindingOverrides();
        }
        Save();
    }

    /// <summary>
    /// 특정 바인딩의 현재 표시 문자열을 반환합니다. (UI에 "현재 키" 표시용)
    /// </summary>
    /// <param name="actionName">액션 이름</param>
    /// <param name="bindingIndex">바인딩 인덱스</param>
    public string GetBindingDisplay(string actionName, int bindingIndex)
    {
        if (_asset == null) return "";

        InputAction action = _asset.FindAction(actionName);
        if (action == null || bindingIndex >= action.bindings.Count) return "";

        return action.GetBindingDisplayString(bindingIndex);
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ 저장 / 복원 ◀─────────────────────────
    /// <summary>현재 바인딩 오버라이드를 로컬 파일에 저장합니다.</summary>
    public void Save()
    {
        if (_asset == null) return;

        RebindData data = new RebindData
        {
            overridesJson = _asset.SaveBindingOverridesAsJson()
        };
        USaveFile.Save(FILE_NAME, data);
    }

    /// <summary>저장된 바인딩 오버라이드를 불러와 적용합니다.</summary>
    public void Load()
    {
        if (_asset == null) return;

        RebindData data = USaveFile.Load(FILE_NAME, new RebindData());
        if (!string.IsNullOrEmpty(data.overridesJson))
        {
            _asset.LoadBindingOverridesFromJson(data.overridesJson);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    protected override void Initialize()
    {
        // 에셋은 CInputManager가 SetAsset으로 주입합니다.
    }

    // 진행 중 리바인딩 오퍼레이션 정리
    private void DisposeOp()
    {
        if (_op == null) return;

        _op.Dispose();
        _op = null;
    }
    #endregion
}
