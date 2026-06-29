using UnityEngine;

/// <summary>
/// 옵션(볼륨/해상도)을 인스펙터에서 테스트하는 스크립트입니다.
/// 컴포넌트 우측 상단 ⋮ 메뉴(또는 우클릭)의 항목을 눌러 실행합니다.
/// 개발용이며, 빌드에서는 제거하거나 비활성화하세요.
///
/// 사용법: 볼륨/해상도 값은 아래 필드에서 정하고, 실행은 컨텍스트 메뉴 버튼으로 합니다.
/// </summary>
public sealed class CTestOption : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("적용할 볼륨 값 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float _master = 1f;
    [SerializeField, Range(0f, 1f)] private float _bgm = 1f;
    [SerializeField, Range(0f, 1f)] private float _sfx = 1f;
    [SerializeField, Range(0f, 1f)] private float _ambience = 1f;

    [Header("적용할 해상도")]
    [SerializeField] private Vector2Int _resolution = new(1920, 1080);
    [SerializeField] private FullScreenMode _fullScreenMode = FullScreenMode.FullScreenWindow;

    [Header("표시")]
    [SerializeField] private bool _showOnGUI = true;
    #endregion

    #region ─────────────────────────▶ 볼륨 ◀─────────────────────────
    [ContextMenu("볼륨/위 필드 값으로 모두 적용")]
    private void ApplyAllVolumes()
    {
        CLocalOptionManager mgr = CLocalOptionManager.Ins;
        mgr.SetMasterVolume(_master);
        mgr.SetBgmVolume(_bgm);
        mgr.SetSfxVolume(_sfx);
        mgr.SetAmbienceVolume(_ambience);
        UDebug.Print($"[Test] 볼륨 적용 M{_master:F2} B{_bgm:F2} S{_sfx:F2} A{_ambience:F2}");
    }

    [ContextMenu("볼륨/마스터만 적용")]
    private void ApplyMaster()
    {
        CLocalOptionManager.Ins.SetMasterVolume(_master);
        UDebug.Print($"[Test] 마스터 볼륨 → {_master:F2}");
    }
    #endregion

    #region ─────────────────────────▶ 해상도 ◀─────────────────────────
    [ContextMenu("해상도/위 필드 값으로 적용")]
    private void ApplyResolution()
    {
        CLocalOptionManager.Ins.SetResolution(_resolution.x, _resolution.y, _fullScreenMode);
        UDebug.Print($"[Test] 해상도 → {_resolution.x}x{_resolution.y} / {_fullScreenMode}");
    }
    #endregion

    #region ─────────────────────────▶ 저장 / 불러오기 ◀─────────────────────────
    [ContextMenu("저장소/지금 저장")]
    private void Save()
    {
        CLocalOptionManager.Ins.Save();
        UDebug.Print("[Test] 옵션을 저장했습니다.");
    }

    [ContextMenu("저장소/파일에서 다시 불러오기")]
    private void Load()
    {
        CLocalOptionManager.Ins.Load();
        UDebug.Print("[Test] 옵션을 파일에서 다시 불러왔습니다.");
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnGUI()
    {
        if (!_showOnGUI) return;

        OptionData opt = CLocalOptionManager.Ins.Option;
        GUILayout.BeginArea(new Rect(340, 10, 300, 160), GUI.skin.box);
        GUILayout.Label("<b>[ 현재 옵션 ]</b>");
        GUILayout.Label($"Master {opt.masterVolume:F2} / BGM {opt.bgmVolume:F2}");
        GUILayout.Label($"SFX {opt.sfxVolume:F2} / Ambience {opt.ambienceVolume:F2}");
        GUILayout.Label($"해상도: {opt.resolutionWidth}x{opt.resolutionHeight}");
        GUILayout.Label($"모드: {opt.fullScreenMode}");
        GUILayout.EndArea();
    }
    #endregion
}
