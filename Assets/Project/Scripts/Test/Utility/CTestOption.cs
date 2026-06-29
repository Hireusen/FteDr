using UnityEngine;

/// <summary>
/// 옵션(볼륨/해상도)을 인스펙터에서 테스트하는 스크립트입니다.
/// </summary>
public sealed class CTestOption : AFrameable, IUpdateFrameable
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("테스트 트리거")]
    [SerializeField] private bool _testApplyAllVolumes;
    [SerializeField] private bool _testApplyMaster;
    [SerializeField] private bool _testApplyResolution;
    [SerializeField] private bool _testSave;
    [SerializeField] private bool _testLoad;

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

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public EUpdatePriority UpdatePriority => EUpdatePriority.Lv5;
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    public void ExecuteUpdateFrame()
    {
        if (_testApplyAllVolumes) { _testApplyAllVolumes = false; ApplyAllVolumes(); }
        if (_testApplyMaster) { _testApplyMaster = false; ApplyMaster(); }
        if (_testApplyResolution) { _testApplyResolution = false; ApplyResolution(); }
        if (_testSave) { _testSave = false; Save(); }
        if (_testLoad) { _testLoad = false; Load(); }
    }

    private void ApplyAllVolumes()
    {
        CLocalOptionManager mgr = CLocalOptionManager.Ins;
        mgr.SetMasterVolume(_master);
        mgr.SetBgmVolume(_bgm);
        mgr.SetSfxVolume(_sfx);
        mgr.SetAmbienceVolume(_ambience);
        UDebug.Print($"[Test] 볼륨 적용 M{_master:F2} B{_bgm:F2} S{_sfx:F2} A{_ambience:F2}");
    }

    private void ApplyMaster()
    {
        CLocalOptionManager.Ins.SetMasterVolume(_master);
        UDebug.Print($"[Test] 마스터 볼륨 → {_master:F2}");
    }

    private void ApplyResolution()
    {
        CLocalOptionManager.Ins.SetResolution(_resolution.x, _resolution.y, _fullScreenMode);
        UDebug.Print($"[Test] 해상도 → {_resolution.x}x{_resolution.y} / {_fullScreenMode}");
    }

    private void Save()
    {
        CLocalOptionManager.Ins.Save();
        UDebug.Print("[Test] 옵션을 저장했습니다.");
    }

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
