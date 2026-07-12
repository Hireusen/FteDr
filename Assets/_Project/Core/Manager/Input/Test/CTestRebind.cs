#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// CRebindManager 사용 예제입니다. (IMGUI 데모)
/// 씬의 빈 오브젝트에 붙이면, 각 액션의 현재 키를 보여주고 버튼으로 리바인딩할 수 있습니다.
///
/// [핵심 사용법 요약]
///   1) 현재 키 표시:  UCamera 없이 CRebindManager.Ins.GetBindingDisplay(액션, 인덱스)
///   2) 인덱스 찾기:    CRebindManager.Ins.FindBindingIndex(액션, "PC"/"Gamepad")
///   3) 리바인딩 시작:  CRebindManager.Ins.StartRebind(액션, 인덱스, 완료콜백)
///   4) 기본값 복원:    ResetBinding(액션, 인덱스) 또는 ResetAll()
/// </summary>
public sealed class CTestRebind : MonoBehaviour
{
    // 리바인딩할 액션 이름들 (에셋의 Action 이름과 정확히 일치해야 함)
    private static readonly string[] _actions = { "Jump", "Esc", "Grab" };

    private bool _open = true;
    private string _status = "F2로 패널 토글";

    private void Update()
    {
        // 데모용 토글 (구식 Input이 막혀 있으면 이 부분은 무시됨)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            _open = !_open;
        }
    }

    private void OnGUI()
    {
        if (!_open) return;

        GUILayout.BeginArea(new Rect(20, 20, 360, 400), GUI.skin.box);
        GUILayout.Label("── 키 리바인딩 데모 ──");
        GUILayout.Label(_status);
        GUILayout.Space(6);

        // 리바인딩 중에는 다른 버튼을 못 누르게 막기
        bool busy = CRebindManager.Ins.IsRebinding;

        for (int i = 0; i < _actions.Length; ++i)
        {
            DrawActionRow(_actions[i], busy);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("전체 기본값으로 복원"))
        {
            CRebindManager.Ins.ResetAll();
            _status = "모든 키를 기본값으로 되돌렸습니다.";
        }

        GUILayout.EndArea();
    }

    // 액션 하나에 대한 UI 행: [액션명] [현재 키] [PC 변경] [게임패드 변경]
    private void DrawActionRow(string actionName, bool busy)
    {
        // PC 스킴의 바인딩 인덱스를 이름으로 찾는다 (손으로 세지 않음)
        int pcIndex = CRebindManager.Ins.FindBindingIndex(actionName, "PC");
        int padIndex = CRebindManager.Ins.FindBindingIndex(actionName, "Gamepad");

        GUILayout.BeginHorizontal();
        GUILayout.Label(actionName, GUILayout.Width(60));

        // 현재 PC 키 표시
        string pcKey = pcIndex >= 0 ? CRebindManager.Ins.GetBindingDisplay(actionName, pcIndex) : "-";
        GUILayout.Label(pcKey, GUILayout.Width(90));

        // PC 키 변경 버튼
        GUI.enabled = !busy && pcIndex >= 0;
        if (GUILayout.Button("PC 변경", GUILayout.Width(80)))
        {
            _status = $"{actionName}: 새 키를 누르세요...";
            // 리바인딩 시작. 완료되면 상태 문구 갱신 (표시는 다음 OnGUI에서 자동 반영)
            CRebindManager.Ins.StartRebind(actionName, pcIndex, () =>
            {
                _status = $"{actionName} 키가 변경되었습니다.";
            });
        }

        // 게임패드 변경 버튼 (마우스 제외는 게임패드엔 의미 없지만 기본값 유지)
        GUI.enabled = !busy && padIndex >= 0;
        if (GUILayout.Button("패드 변경", GUILayout.Width(80)))
        {
            _status = $"{actionName}: 게임패드 버튼을 누르세요...";
            CRebindManager.Ins.StartRebind(actionName, padIndex, () =>
            {
                _status = $"{actionName} 패드 버튼이 변경되었습니다.";
            });
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }
}
#endif
