using System;
using System.Collections.Generic;
using System.Globalization;
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
        new ResolutionOption { width = 1280, height = 720, label = "1280 × 720 (16:9)" },
        new ResolutionOption { width = 1600, height = 900, label = "1600 × 900 (16:9)" },
        new ResolutionOption { width = 1920, height = 1080, label = "1920 × 1080 (16:9)" },
        new ResolutionOption { width = 1280, height = 800, label = "1280 × 800 (16:10)" },
        new ResolutionOption { width = 1920, height = 1200, label = "1920 × 1200 (16:10)" },
        new ResolutionOption { width = 2560, height = 1080, label = "2560 × 1080 (21:9)" },
    };
    [SerializeField] private TMP_Dropdown _screenModeDropdown;
    [SerializeField]
    private List<ScreenModeOption> _screenModeOptions = new()
    {
        new ScreenModeOption { mode = FullScreenMode.ExclusiveFullScreen, label = "전체화면" },
        new ScreenModeOption { mode = FullScreenMode.FullScreenWindow, label = "테두리 없는 창모드" },
        new ScreenModeOption { mode = FullScreenMode.Windowed, label = "창모드" }
    };

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

    [Header("조작 - 카메라 감도")]
    [Tooltip("감도 슬라이더. 범위는 K.MIN/MAX_CAMERA_SENSITIVITY로 런타임에 맞춰집니다.")]
    [SerializeField] private Slider _cameraSensitivitySlider;
    [Tooltip("감도를 백분율(슬라이더 값 × 100)로 표시/입력하는 인풋 필드")]
    [SerializeField] private TMP_InputField _cameraSensitivityInput;

    [Header("조작 - 조작키 변경")]
    [Tooltip("누르면 조작키 변경창(EUI.KeyMappingWindow)을 엽니다.")]
    [SerializeField] private Button _keyMappingButton;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const float PERCENT_SCALE = 100.0f; // 감도(0~1) ↔ 백분율 변환 배율
    private const int PERCENT_DECIMALS = 2;     // 백분율 표시/입력에 허용하는 소수점 자릿수
    // 필요한 자리만 찍는다. 50 → "50", 49.5 → "49.5", 49.37 → "49.37"
    private const string PERCENT_FORMAT = "0.##";
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        BuildResolutionDropdown();
        BuildScreenModeDropdown();
        BuildFrameLimitDropdown();
        BuildCameraSensitivity();

        if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (_ambienceSlider != null) _ambienceSlider.onValueChanged.AddListener(OnAmbienceChanged);

        if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (_screenModeDropdown != null) _screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);

        if (_frameLimitDropdown != null) _frameLimitDropdown.onValueChanged.AddListener(OnFrameLimitChanged);
        if (_vsyncToggle != null) _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

        if (_cameraSensitivitySlider != null) _cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivitySliderChanged);
        // 타이핑 도중(onValueChanged)이 아니라 입력을 마쳤을 때(엔터/포커스 아웃)만 반영해야 "5"를 치는 중에 5%로 튀지 않는다.
        if (_cameraSensitivityInput != null) _cameraSensitivityInput.onEndEdit.AddListener(OnCameraSensitivityInputEndEdit);

        if (_keyMappingButton != null) _keyMappingButton.onClick.AddListener(OnKeyMappingClicked);
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

    private void BuildScreenModeDropdown()
    {
        if (_screenModeDropdown == null) return;

        _screenModeDropdown.ClearOptions();
        List<string> labels = new(_screenModeOptions.Count);
        for (int i = 0; i < _screenModeOptions.Count; i++)
        {
            labels.Add(_screenModeOptions[i].label);
        }
        _screenModeDropdown.AddOptions(labels);
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

    private void BuildCameraSensitivity()
    {
        if (_cameraSensitivitySlider == null) return;

        // 슬라이더 범위를 상수와 강제로 일치시켜, 프리팹 값이 달라도 인풋 필드의 백분율과 어긋나지 않게 한다.
        _cameraSensitivitySlider.minValue = K.MIN_CAMERA_SENSITIVITY;
        _cameraSensitivitySlider.maxValue = K.MAX_CAMERA_SENSITIVITY;
        _cameraSensitivitySlider.wholeNumbers = false;
    }

    // 창이 열릴 때마다(OnEnable) 현재 저장된 옵션 값으로 UI를 맞춰준다. (리스너가 다시 발동하지 않도록 SetValueWithoutNotify 사용)
    private void RefreshFromCurrentOption()
    {
        OptionData option = CLocalOptionManager.Ins.Option;

        if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(option.masterVolume);
        if (_bgmSlider != null) _bgmSlider.SetValueWithoutNotify(option.bgmVolume);
        if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(option.sfxVolume);
        if (_ambienceSlider != null) _ambienceSlider.SetValueWithoutNotify(option.ambienceVolume);

        if (_screenModeDropdown != null)
        {
            int index = _screenModeOptions.FindIndex(s => s.mode == option.screenMode);
            _screenModeDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        }

        if (_resolutionDropdown != null)
        {
            bool canChangeResolution = (option.screenMode != FullScreenMode.FullScreenWindow);
            _resolutionDropdown.interactable = canChangeResolution;

            if (_resolutionDropdown.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup.alpha = canChangeResolution ? 1.0f : 0.3f;
            }

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

        RefreshCameraSensitivityUI(option.cameraSensitivity);
    }

    // 슬라이더와 인풋 필드를 같은 값으로 동시에 맞춘다. (서로의 리스너를 다시 깨우지 않도록 WithoutNotify 사용)
    private void RefreshCameraSensitivityUI(float sensitivity)
    {
        if (_cameraSensitivitySlider != null)
        {
            _cameraSensitivitySlider.SetValueWithoutNotify(sensitivity);
        }

        if (_cameraSensitivityInput != null)
        {
            _cameraSensitivityInput.SetTextWithoutNotify(
                ToPercent(sensitivity).ToString(PERCENT_FORMAT, CultureInfo.InvariantCulture));
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
        FullScreenMode currentMode = CLocalOptionManager.Ins.Option.screenMode;

        CLocalOptionManager.Ins.SetResolution(selected.width, selected.height, currentMode);
    }

    private void OnScreenModeChanged(int index)
    {
        if (index < 0 || index >= _screenModeOptions.Count) return;

        FullScreenMode selectedMode = _screenModeOptions[index].mode;
        OptionData option = CLocalOptionManager.Ins.Option;

        int targetWidth = option.resolutionWidth;
        int targetHeight = option.resolutionHeight;

        if (selectedMode == FullScreenMode.FullScreenWindow)
        {
            targetWidth = Screen.currentResolution.width;
            targetHeight = Screen.currentResolution.height;
        }

        CLocalOptionManager.Ins.SetResolution(targetWidth, targetHeight, selectedMode);
        RefreshFromCurrentOption();
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

    // 슬라이더는 연속값이라 그대로 두면 인풋 필드에 표시되는(반올림된) 백분율과 미세하게 어긋난다.
    // 표시 자릿수와 같은 단위로 끊어서 적용하면 슬라이더 · 인풋 필드 · 저장값이 항상 같은 숫자를 가리킨다.
    private void OnCameraSensitivitySliderChanged(float value)
    {
        ApplyCameraSensitivity(FromPercent(ToPercent(value)));
    }

    private void OnCameraSensitivityInputEndEdit(string text)
    {
        if (!TryParsePercent(text, out float percent))
        {
            // 숫자로 해석할 수 없으면 입력을 버리고 현재 설정값을 다시 보여준다.
            RefreshCameraSensitivityUI(CLocalOptionManager.Ins.Option.cameraSensitivity);
            return;
        }

        ApplyCameraSensitivity(FromPercent(percent));
    }

    // 설정창은 닫지 않는다. CUIManager가 스택으로 관리하므로 조작키 변경창이 설정창 위에 겹쳐 열린다.
    private void OnKeyMappingClicked()
    {
        OnRequestOpenUI.Publish(EUI.KeyMappingWindow);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 카메라 감도 ◀─────────────────────────
    // 감도를 옵션에 반영한 뒤, 클램프된 최종값으로 두 위젯을 다시 맞춘다.
    // (범위를 벗어난 입력이 들어와도 UI가 실제 저장값과 달라지지 않게 하기 위함)
    private void ApplyCameraSensitivity(float sensitivity)
    {
        CLocalOptionManager.Ins.SetCameraSensitivity(sensitivity);
        RefreshCameraSensitivityUI(CLocalOptionManager.Ins.Option.cameraSensitivity);
    }

    private static float ToPercent(float sensitivity) => RoundPercent(sensitivity * PERCENT_SCALE);

    private static float FromPercent(float percent) => percent / PERCENT_SCALE;

    // 소수점 아래 PERCENT_DECIMALS 자리에서 끊는다. 슬라이더가 이 단위로 스냅되어야
    // 인풋 필드에 찍히는 문자열과 저장값이 정확히 같은 숫자가 된다.
    private static float RoundPercent(float percent)
    {
        return (float)Math.Round(percent, PERCENT_DECIMALS, MidpointRounding.AwayFromZero);
    }

    // "50", " 50 ", "50%", "49.37" 을 모두 허용한다. 두 자리보다 더 긴 소수는 반올림된다.
    private static bool TryParsePercent(string text, out float percent)
    {
        percent = 0.0f;
        if (string.IsNullOrWhiteSpace(text)) return false;

        string trimmed = text.Trim().TrimEnd('%').Trim();
        if (!float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)) return false;

        percent = RoundPercent(parsed);
        return true;
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
    public struct ScreenModeOption
    {
        public FullScreenMode mode;
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
