using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Setting_Canvas의 실제 내용(볼륨 슬라이더, 해상도 변경)을 담당합니다.
/// 창의 열기/닫기/페이드/닫기버튼은 CUIWindow가 전담하므로 여기서는 다루지 않습니다.
/// </summary>
public sealed class CSettingController : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("볼륨 슬라이더 (0~1)")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _ambienceSlider;

    [Header("해상도")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField]
    private List<ResolutionOption> _resolutionOptions = new()
    {
        new ResolutionOption { width = 1920, height = 1080, label = "1920 x 1080" },
        new ResolutionOption { width = 2560, height = 1080, label = "2560 x 1080" },
    };
    [SerializeField] private Toggle _fullscreenToggle;

    [Header("프레임 및 수직동기화")]
    [SerializeField] private TMP_Dropdown _frameLimitDropdown;
    [SerializeField]
    private List<FrameOption> _frameOptions = new()
    {
        new FrameOption { frameRate = 30, label = "30 FPS" },
        new FrameOption { frameRate = 60, label = "60 FPS" },
        new FrameOption { frameRate = 120, label = "120 FPS" },
        new FrameOption { frameRate = 144, label = "144 FPS" },
        new FrameOption { frameRate = -1, label = "제한 없음" }
    };
    [SerializeField] private Toggle _vsyncToggle;
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        BuildResolutionDropdown();
        BuildFrameLimitDropdown();

        if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (_ambienceSlider != null) _ambienceSlider.onValueChanged.AddListener(OnAmbienceChanged);

        if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (_fullscreenToggle != null) _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        if (_frameLimitDropdown != null) _frameLimitDropdown.onValueChanged.AddListener(OnFrameLimitChanged);
        if (_vsyncToggle != null) _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
    }

    private void OnEnable()
    {
        RefreshFromCurrentOption();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 초기화 ◀─────────────────────────
    private void BuildResolutionDropdown()
    {
        if (_resolutionDropdown == null) return;

        _resolutionDropdown.ClearOptions();
        List<string> labels = new(_resolutionOptions.Count);
        for (int i = 0; i < _resolutionOptions.Count; ++i)
        {
            labels.Add(_resolutionOptions[i].label);
        }
        _resolutionDropdown.AddOptions(labels);
    }

    private void BuildFrameLimitDropdown()
    {
        if (_frameLimitDropdown == null) return;

        _frameLimitDropdown.ClearOptions();
        List<string> labels = new(_frameOptions.Count);
        for (int i = 0; i < _frameOptions.Count; ++i)
        {
            labels.Add(_frameOptions[i].label);
        }
        _frameLimitDropdown.AddOptions(labels);
    }

    // 창이 열릴 때마다(OnEnable) 현재 저장된 옵션 값으로 UI를 맞춰준다. (리스너가 다시 발동하지 않도록 SetValueWithoutNotify 사용)
    private void RefreshFromCurrentOption()
    {
        OptionData option = CLocalOptionManager.Ins.Option;

        if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(option.masterVolume);
        if (_bgmSlider != null) _bgmSlider.SetValueWithoutNotify(option.bgmVolume);
        if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(option.sfxVolume);
        if (_ambienceSlider != null) _ambienceSlider.SetValueWithoutNotify(option.ambienceVolume);

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.SetIsOnWithoutNotify(option.fullScreenMode != FullScreenMode.Windowed);
        }

        if (_resolutionDropdown != null)
        {
            int index = _resolutionOptions.FindIndex(r => r.width == option.resolutionWidth && r.height == option.resolutionHeight);
            _resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        }

        if (_vsyncToggle != null)
        {
            _vsyncToggle.SetIsOnWithoutNotify(option.vSync);
        }

        if (_frameLimitDropdown != null)
        {
            int index = _frameOptions.FindIndex(f => f.frameRate == option.targetFrameRate);
            _frameLimitDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        }
    }
    #endregion

    #region ─────────────────────────▶ 이벤트 핸들러 ◀─────────────────────────
    private void OnMasterChanged(float value) => CLocalOptionManager.Ins.SetMasterVolume(value);
    private void OnBgmChanged(float value) => CLocalOptionManager.Ins.SetBgmVolume(value);
    private void OnSfxChanged(float value) => CLocalOptionManager.Ins.SetSfxVolume(value);
    private void OnAmbienceChanged(float value) => CLocalOptionManager.Ins.SetAmbienceVolume(value);

    private void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= _resolutionOptions.Count) return;

        ResolutionOption selected = _resolutionOptions[index];
        FullScreenMode mode = (_fullscreenToggle != null && _fullscreenToggle.isOn)
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        CLocalOptionManager.Ins.SetResolution(selected.width, selected.height, mode);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        OptionData option = CLocalOptionManager.Ins.Option;
        FullScreenMode mode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        CLocalOptionManager.Ins.SetResolution(option.resolutionWidth, option.resolutionHeight, mode);
    }

    private void OnFrameLimitChanged(int index)
    {
        if (index < 0 || index >= _frameOptions.Count) return;

        int targetFPS = _frameOptions[index].frameRate;
        CLocalOptionManager.Ins.SetTargetFrameRate(targetFPS);
    }

    private void OnVSyncChanged(bool isOn)
    {
        CLocalOptionManager.Ins.SetVSync(isOn);
    }
    #endregion

    #region ─────────────────────────▶ 중첩 타입 ◀─────────────────────────
    [Serializable]
    public struct ResolutionOption
    {
        public int width;
        public int height;
        public string label;
    }

    [Serializable]
    public struct FrameOption
    {
        public int frameRate; // -1일 경우 제한 없음
        public string label;
    }
    #endregion
}
