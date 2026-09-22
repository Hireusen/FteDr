using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// KeyMapping_Canvas의 실제 내용(액션 목록 생성, 리바인딩 요청, 현재 키 표시)을 담당합니다.
/// 창의 열기/닫기/페이드/닫기버튼은 CUIWindow가 전담하므로 여기서는 다루지 않습니다.
///
/// 리바인딩 자체는 CRebindManager가 처리하고, 이 컨트롤러는 "어떤 행을 보여줄지"와
/// "표시 갱신"만 맡습니다.
/// </summary>
public sealed class CKeyMappingController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("행 생성")]
    [Tooltip("행이 쌓일 부모 (보통 Scroll View/Viewport/Content)")]
    [SerializeField] private RectTransform _rowParent;
    [Tooltip("복제해서 쓸 행 템플릿. 캔버스 안의 오브젝트(비활성 권장)와 프리팹 에셋 모두 연결할 수 있습니다.")]
    [SerializeField] private CKeyMappingRow _rowTemplate;

    [Header("안내 / 전체 초기화")]
    [Tooltip("\"새 키를 누르세요\" 같은 안내 문구를 띄울 텍스트 (없으면 비워두세요)")]
    [SerializeField] private TMP_Text _guideText;
    [SerializeField] private Button _resetAllButton;

    [Header("리바인딩 대상")]
    [Tooltip("바인딩 인덱스를 찾기 위한 구조 조회용 에셋입니다. Assets/_Project/Input/InputActions.inputactions를 넣으세요. " +
        "실제 리바인딩과 현재 키 표시는 CRebindManager가 들고 있는 런타임 사본을 통해 이뤄집니다.")]
    [SerializeField] private InputActionAsset _inputActions;
    [Tooltip("리바인딩할 Control Scheme 이름")]
    [SerializeField] private string _controlScheme = "PC";

    [Tooltip("조작키 변경창에 노출할 액션 목록입니다. 순서가 곧 화면 순서입니다.")]
    [SerializeField]
    private List<ActionEntry> _actionEntries = new()
    {
        new ActionEntry { actionName = "Move", partName = "up",    label = "앞으로 이동" },
        new ActionEntry { actionName = "Move", partName = "down",  label = "뒤로 이동" },
        new ActionEntry { actionName = "Move", partName = "left",  label = "왼쪽으로 이동" },
        new ActionEntry { actionName = "Move", partName = "right", label = "오른쪽으로 이동" },
        new ActionEntry { actionName = "Jump",               label = "상승" },
        new ActionEntry { actionName = "Descent",            label = "하강" },
        new ActionEntry { actionName = "Grab",               label = "집게" },
        new ActionEntry { actionName = "Collect",            label = "수집" },
        new ActionEntry { actionName = "Net",                label = "그물" },
        new ActionEntry { actionName = "RotateTwizerLeft",   label = "집게 좌회전" },
        new ActionEntry { actionName = "RotateTwizerRight",  label = "집게 우회전" },
        new ActionEntry { actionName = "Inventory",          label = "인벤토리" },
        new ActionEntry { actionName = "ToggleHud",          label = "HUD 표시" },
    };

    [Header("안내 문구")]
    [SerializeField] private string _idleGuide = "변경할 키의 버튼을 누르세요.";
    [SerializeField] private string _waitingGuide = "새로 사용할 키를 누르세요. (ESC: 취소)";
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private readonly List<CKeyMappingRow> _rows = new();
    private readonly Dictionary<string, int> _keyUseCounts = new(); // 중복 검사용 (키 표시 문자열 → 사용 행 수)
    private bool _isBuilt;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        if (_resetAllButton != null) _resetAllButton.onClick.AddListener(OnResetAllClicked);
    }

    private void OnEnable()
    {
        // 행 생성을 Awake가 아니라 여기서 하는 이유: 바인딩 인덱스 조회는 CInputManager가 CRebindManager에
        // 에셋을 넘긴 뒤에야 성공한다. 창이 열리는 시점이면 부팅이 끝나 있으므로 안전하다. (최초 1회만 생성)
        BuildRows();

        // 창을 다시 열 때는 저장된 오버라이드가 이미 적용된 상태이므로 표시만 맞춰준다.
        RefreshAllKeyDisplay();
        SetRowsInteractable(true);
        SetGuide(_idleGuide);
    }

    private void OnDisable()
    {
        // 키 입력을 기다리는 도중에 창이 닫히면 오퍼레이션이 남아 다음 입력을 삼켜버린다.
        if (CRebindManager.Ins != null && CRebindManager.Ins.IsRebinding)
        {
            CRebindManager.Ins.CancelRebind();
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 초기화 ◀─────────────────────────
    // 템플릿을 복제해 액션 목록만큼 행을 만든다. (Awake에서 한 번만)
    private void BuildRows()
    {
        if (_isBuilt) return;
        if (_rowTemplate == null || _rowParent == null)
        {
            UDebug.Print("CKeyMappingController: 행 템플릿 또는 부모가 연결되지 않았습니다.", LogType.Error, gameObject);
            return;
        }
        if (_inputActions == null)
        {
            UDebug.Print("CKeyMappingController: 조회용 InputActionAsset이 연결되지 않았습니다.", LogType.Error, gameObject);
            return;
        }

        // 템플릿이 캔버스 안에 배치된 오브젝트일 때만 숨긴다.
        // 프리팹 에셋을 연결한 경우 SetActive를 부르면 에디터에서 에셋 원본이 꺼진 채로 저장되어 버린다.
        if (_rowTemplate.gameObject.scene.IsValid())
        {
            _rowTemplate.gameObject.SetActive(false);
        }

        for (int i = 0; i < _actionEntries.Count; ++i)
        {
            ActionEntry entry = _actionEntries[i];

            int bindingIndex = FindBindingIndex(entry);
            if (bindingIndex < 0)
            {
                UDebug.Print($"CKeyMappingController: '{entry.actionName}'{FormatPart(entry.partName)}의 " +
                    $"{_controlScheme} 바인딩을 찾지 못해 건너뜁니다.", LogType.Warning, gameObject);
                continue;
            }

            CKeyMappingRow row = Instantiate(_rowTemplate, _rowParent);
            row.gameObject.SetActive(true);
            row.Setup(entry.actionName, bindingIndex, entry.label, OnRebindRequested, OnResetRequested);

            _rows.Add(row);
        }

        // 한 행도 못 만들었다면 아직 준비가 안 된 것으로 보고 다음에 열릴 때 다시 시도한다.
        _isBuilt = _rows.Count > 0;
    }

    /// <summary>
    /// 표시할 바인딩의 인덱스를 찾습니다.
    /// 단일 키는 CRebindManager가 제공하는 조회를 그대로 쓰고,
    /// Move처럼 composite로 묶인 키는 부분 이름(up/down/left/right)까지 봐야 하므로 직접 찾습니다.
    /// </summary>
    private int FindBindingIndex(ActionEntry entry)
    {
        if (string.IsNullOrEmpty(entry.partName))
        {
            return CRebindManager.Ins.FindBindingIndex(entry.actionName, _controlScheme);
        }

        InputAction action = _inputActions.FindAction(entry.actionName);
        if (action == null) return -1;

        for (int i = 0; i < action.bindings.Count; ++i)
        {
            InputBinding binding = action.bindings[i];

            if (!binding.isPartOfComposite) continue;
            if (!string.Equals(binding.name, entry.partName, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrEmpty(binding.groups) || !binding.groups.Contains(_controlScheme)) continue;

            return i;
        }
        return -1;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 표시 갱신 ◀─────────────────────────
    private void RefreshAllKeyDisplay()
    {
        for (int i = 0; i < _rows.Count; ++i)
        {
            RefreshKeyDisplay(_rows[i]);
        }
        RefreshDuplicateWarnings();
    }

    // 행 하나의 키가 바뀌면 다른 행의 중복 여부도 달라지므로(겹침이 생기거나 풀림) 경고는 항상 전체를 다시 계산한다.
    private void RefreshRowAndWarnings(CKeyMappingRow row)
    {
        RefreshKeyDisplay(row);
        RefreshDuplicateWarnings();
    }

    private void RefreshKeyDisplay(CKeyMappingRow row)
    {
        row.SetKeyDisplay(CRebindManager.Ins.GetBindingDisplay(row.ActionName, row.BindingIndex));
    }

    // 같은 키가 두 행 이상에 지정되어 있으면 해당 행들의 라벨을 경고 색으로 바꾼다. (막지는 않는다)
    // 같은 물리 키는 표시 문자열도 같으므로 표시 문자열로 비교한다.
    private void RefreshDuplicateWarnings()
    {
        _keyUseCounts.Clear();
        for (int i = 0; i < _rows.Count; ++i)
        {
            string key = _rows[i].KeyDisplay;
            if (string.IsNullOrEmpty(key)) continue;

            _keyUseCounts.TryGetValue(key, out int count);
            _keyUseCounts[key] = count + 1;
        }

        for (int i = 0; i < _rows.Count; ++i)
        {
            string key = _rows[i].KeyDisplay;
            bool isDuplicated = !string.IsNullOrEmpty(key) && _keyUseCounts[key] > 1;
            _rows[i].SetDuplicated(isDuplicated);
        }
    }

    // 리바인딩 중에는 다른 행을 건드리지 못하게 막는다. (오퍼레이션이 겹치면 앞의 것이 취소된다)
    private void SetRowsInteractable(bool value)
    {
        for (int i = 0; i < _rows.Count; ++i)
        {
            _rows[i].SetInteractable(value);
        }
    }

    private void SetGuide(string message)
    {
        if (_guideText == null) return;

        _guideText.text = message;
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void OnRebindRequested(CKeyMappingRow row)
    {
        if (CRebindManager.Ins.IsRebinding) return;

        // 클릭한 버튼이 선택 상태로 남아 있으면, 새 키로 Space/Enter를 누르는 순간 UI의 Submit으로도 처리되어
        // 리바인딩이 끝나자마자 같은 버튼이 다시 눌리고 입력 대기가 또 시작된다. 선택을 풀어서 막는다.
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        SetRowsInteractable(false);
        SetGuide(_waitingGuide);
        row.SetWaiting();

        // 완료 콜백은 성공/취소 양쪽 모두에서 불리므로, 여기서 잠금을 풀고 표시를 다시 맞춘다.
        CRebindManager.Ins.StartRebind(row.ActionName, row.BindingIndex, () =>
        {
            RefreshRowAndWarnings(row);
            SetRowsInteractable(true);
            SetGuide(_idleGuide);
        });
    }

    private void OnResetRequested(CKeyMappingRow row)
    {
        if (CRebindManager.Ins.IsRebinding) return;

        CRebindManager.Ins.ResetBinding(row.ActionName, row.BindingIndex);
        RefreshRowAndWarnings(row);
    }

    private void OnResetAllClicked()
    {
        if (CRebindManager.Ins.IsRebinding) return;

        CRebindManager.Ins.ResetAll();
        RefreshAllKeyDisplay();
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    [Serializable]
    public struct ActionEntry
    {
        [Tooltip("InputActions 에셋의 Action 이름과 정확히 일치해야 합니다.")]
        public string actionName;
        [Tooltip("Move처럼 composite로 묶인 액션의 부분 이름 (up/down/left/right). 단일 키면 비워두세요.")]
        public string partName;
        [Tooltip("UI에 보여줄 표시 이름")]
        public string label;
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    private static string FormatPart(string partName)
    {
        return string.IsNullOrEmpty(partName) ? string.Empty : $"({partName})";
    }
    #endregion
}
