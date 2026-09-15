using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 조작키 변경창의 행 하나(액션 이름 · 현재 키 · 변경 버튼 · 되돌리기 버튼)를 담당합니다.
/// 실제 리바인딩은 CKeyMappingController가 CRebindManager를 통해 수행하고,
/// 이 컴포넌트는 표시와 버튼 클릭 전달만 맡습니다.
/// </summary>
public sealed class CKeyMappingRow : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Tooltip("액션의 표시 이름 (예: 앞으로 이동)")]
    [SerializeField] private TMP_Text _labelText;
    [Tooltip("현재 지정된 키를 보여주는 텍스트")]
    [SerializeField] private TMP_Text _keyText;
    [Tooltip("누르면 새 키 입력을 기다리는 버튼")]
    [SerializeField] private Button _rebindButton;
    [Tooltip("이 행만 기본값으로 되돌리는 버튼 (없으면 비워두세요)")]
    [SerializeField] private Button _resetButton;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Action<CKeyMappingRow> _onRebindRequested;
    private Action<CKeyMappingRow> _onResetRequested;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>이 행이 담당하는 액션 이름입니다. (예: "Move")</summary>
    public string ActionName { get; private set; }

    /// <summary>이 행이 담당하는 바인딩 인덱스입니다.</summary>
    public int BindingIndex { get; private set; }

    /// <summary>행을 한 번만 초기화합니다. 생성 직후 컨트롤러가 호출합니다.</summary>
    /// <param name="actionName">액션 이름</param>
    /// <param name="bindingIndex">바인딩 인덱스</param>
    /// <param name="label">UI에 보여줄 표시 이름</param>
    /// <param name="onRebindRequested">변경 버튼 클릭 콜백</param>
    /// <param name="onResetRequested">되돌리기 버튼 클릭 콜백</param>
    public void Setup(
        string actionName,
        int bindingIndex,
        string label,
        Action<CKeyMappingRow> onRebindRequested,
        Action<CKeyMappingRow> onResetRequested)
    {
        ActionName = actionName;
        BindingIndex = bindingIndex;

        _onRebindRequested = onRebindRequested;
        _onResetRequested = onResetRequested;

        if (_labelText != null) _labelText.text = label;

        if (_rebindButton != null)
        {
            _rebindButton.onClick.RemoveAllListeners();
            _rebindButton.onClick.AddListener(() => _onRebindRequested?.Invoke(this));
        }

        if (_resetButton != null)
        {
            _resetButton.onClick.RemoveAllListeners();
            _resetButton.onClick.AddListener(() => _onResetRequested?.Invoke(this));
        }
    }

    /// <summary>현재 키 표시를 갱신합니다.</summary>
    /// <param name="keyDisplay">표시할 문자열 (비어있으면 "-")</param>
    public void SetKeyDisplay(string keyDisplay)
    {
        if (_keyText == null) return;

        _keyText.text = string.IsNullOrEmpty(keyDisplay) ? "-" : keyDisplay;
    }

    /// <summary>다른 행이 리바인딩 중일 때 이 행의 버튼을 잠급니다.</summary>
    /// <param name="value">누를 수 있는지 여부</param>
    public void SetInteractable(bool value)
    {
        if (_rebindButton != null) _rebindButton.interactable = value;
        if (_resetButton != null) _resetButton.interactable = value;
    }
    #endregion
}
