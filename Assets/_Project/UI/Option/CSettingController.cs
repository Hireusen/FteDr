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

    [Header("그래픽")]
    [SerializeField] private Toggle _shadowToggle;
    [SerializeField] private TMP_Dropdown _textureQualityDropdown;
    // 드롭다운에 보이는 순서는 이 목록 순서를 그대로 따른다. (limit 값과 순서는 서로 무관하니 자유롭게 바꿔도 된다)
    [SerializeField]
    private List<TextureQualityOption> _textureQualityOptions = new()
    {
        new TextureQualityOption { limit = 2, label = "낮음" },
        new TextureQualityOption { limit = 1, label = "보통" },
        new TextureQualityOption { limit = 0, label = "높음" }
    };
    [Tooltip("시야각(FOV). 값 범위는 프리팹에 설정한 슬라이더의 Min/Max를 그대로 사용합니다.")]
    [SerializeField] private Slider _fovSlider;
    [Tooltip("시야각을 숫자로 표시/입력하는 인풋 필드. 없어도 동작합니다.")]
    [SerializeField] private TMP_InputField _fovInput;
    [Tooltip("렌더링 거리(Far Clip Plane). 값 범위는 프리팹에 설정한 슬라이더의 Min/Max를 그대로 사용합니다.")]
    [SerializeField] private Slider _renderDistanceSlider;
    [Tooltip("렌더링 거리를 숫자로 표시/입력하는 인풋 필드. 없어도 동작합니다.")]
    [SerializeField] private TMP_InputField _renderDistanceInput;

    [Header("조작 - 카메라 감도")]
    [Tooltip("감도 단계 슬라이더. 범위는 런타임에 0~1로 맞춰집니다. (실제 회전 배율 변환은 CLocalOptionManager.CameraSensitivity)")]
    [SerializeField] private Slider _cameraSensitivitySlider;
    [Tooltip("감도 단계를 백분율(슬라이더 값 × 100)로 표시/입력하는 인풋 필드")]
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
    // 시야각 · 렌더링 거리는 소수점을 보여주지 않는다. 60.4 → "60"
    private const string CAMERA_VALUE_FORMAT = "0";

    // 드롭다운에 실제로 표시되는 해상도 목록. 프리셋(_resolutionOptions)을 복사한 뒤,
    // 프리셋에 없는 해상도(모니터 해상도 등)를 뒤에 덧붙여서 쓴다. 프리셋 원본은 건드리지 않는다.
    private readonly List<ResolutionOption> _shownResolutionOptions = new();
    // 프리셋 개수. 이 뒤에 붙은 항목은 런타임에 덧붙인 것이라 필요할 때마다 지우고 다시 만든다.
    private int _presetResolutionCount = 0;

    private const float ASPECT_RATIO_TOLERANCE = 0.02f;
    // 라벨에 붙일 화면비 표기. 2560×1080처럼 약분으로는 21:9가 나오지 않는 관례 표기가 있어 비율 값으로 찾는다.
    private static readonly (float ratio, string text)[] ASPECT_RATIOS =
    {
        (5.0f / 4.0f, "5:4"),
        (4.0f / 3.0f, "4:3"),
        (16.0f / 10.0f, "16:10"),
        (16.0f / 9.0f, "16:9"),
        (2560.0f / 1080.0f, "21:9"),
        (3440.0f / 1440.0f, "21:9"),
        (32.0f / 9.0f, "32:9"),
    };
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void Awake()
    {
        InitResolutionOptions();
        BuildResolutionDropdown();
        BuildScreenModeDropdown();
        BuildFrameLimitDropdown();
        BuildTextureQualityDropdown();
        BuildCameraSensitivity();

        if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (_ambienceSlider != null) _ambienceSlider.onValueChanged.AddListener(OnAmbienceChanged);

        if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (_screenModeDropdown != null) _screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);

        if (_frameLimitDropdown != null) _frameLimitDropdown.onValueChanged.AddListener(OnFrameLimitChanged);
        if (_vsyncToggle != null) _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

        if (_shadowToggle != null) _shadowToggle.onValueChanged.AddListener(OnShadowToggleChanged);
        if (_textureQualityDropdown != null) _textureQualityDropdown.onValueChanged.AddListener(OnTextureQualityDropdownChanged);
        if (_fovSlider != null) _fovSlider.onValueChanged.AddListener(OnFovSliderChanged);
        if (_renderDistanceSlider != null) _renderDistanceSlider.onValueChanged.AddListener(OnRenderDistanceSliderChanged);
        // 감도 인풋 필드와 같은 이유로, 타이핑 도중이 아니라 입력을 마쳤을 때만 반영한다.
        if (_fovInput != null) _fovInput.onEndEdit.AddListener(OnFovInputEndEdit);
        if (_renderDistanceInput != null) _renderDistanceInput.onEndEdit.AddListener(OnRenderDistanceInputEndEdit);

        if (_cameraSensitivitySlider != null) _cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivitySliderChanged);
        // 타이핑 도중(onValueChanged)이 아니라 입력을 마쳤을 때(엔터/포커스 아웃)만 반영해야 "5"를 치는 중에 5%로 튀지 않는다.
        if (_cameraSensitivityInput != null) _cameraSensitivityInput.onEndEdit.AddListener(OnCameraSensitivityInputEndEdit);

        if (_keyMappingButton != null) _keyMappingButton.onClick.AddListener(OnKeyMappingClicked);
    }

    private void OnEnable()
    {
        RefreshFromCurrentOption();
    }

    private void OnDisable()
    {
        if (CLocalOptionManager.IsQuitting) return;

        // 시야각 · 렌더링 거리 슬라이더는 드래그 중 저장을 건너뛰므로(매 프레임 파일 쓰기 방지)
        // 창이 닫힐 때 한 번 모아서 저장한다.
        CLocalOptionManager.Ins.Save();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 초기화 ◀─────────────────────────
    // 인스펙터에 적어둔 프리셋을 런타임 목록으로 한 번 복사해둔다.
    // 이후 덧붙이는 항목은 런타임 목록에만 들어가므로, 프리팹에 저장된 프리셋은 그대로 유지된다.
    private void InitResolutionOptions()
    {
        _shownResolutionOptions.Clear();
        _shownResolutionOptions.AddRange(_resolutionOptions);
        _presetResolutionCount = _shownResolutionOptions.Count;
    }

    private void BuildResolutionDropdown()
    {
        if (_resolutionDropdown == null) return;

        _resolutionDropdown.ClearOptions();
        List<string> labels = new(_shownResolutionOptions.Count);
        for (int i = 0; i < _shownResolutionOptions.Count; ++i)
        {
            labels.Add(_shownResolutionOptions[i].label);
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

    private void BuildTextureQualityDropdown()
    {
        if (_textureQualityDropdown == null) return;

        _textureQualityDropdown.ClearOptions();
        List<string> labels = new(_textureQualityOptions.Count);
        for (int i = 0; i < _textureQualityOptions.Count; ++i)
        {
            labels.Add(_textureQualityOptions[i].label);
        }
        _textureQualityDropdown.AddOptions(labels);
    }

    private void BuildCameraSensitivity()
    {
        if (_cameraSensitivitySlider == null) return;

        // 슬라이더 범위를 상수와 강제로 일치시켜, 프리팹 값이 달라도 인풋 필드의 백분율과 어긋나지 않게 한다.
        _cameraSensitivitySlider.minValue = 0.0f;
        _cameraSensitivitySlider.maxValue = 1.0f;
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

            int width = option.resolutionWidth;
            int height = option.resolutionHeight;

            // 테두리 없는 창모드는 해상도를 모니터에 맞춰 강제하므로, 저장값이 아니라 실제 모니터 해상도를 보여준다.
            if (!canChangeResolution)
            {
                CLocalOptionManager.GetDisplayResolution(out width, out height);
            }

            _resolutionDropdown.SetValueWithoutNotify(EnsureResolutionOption(width, height));
            // 값이 0에서 0으로 그대로면 SetValueWithoutNotify가 캡션을 갱신하지 않고 빠져나가므로 직접 한 번 갱신한다.
            _resolutionDropdown.RefreshShownValue();
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

        if (_shadowToggle != null)
        {
            _shadowToggle.SetIsOnWithoutNotify(option.useShadow);
        }

        if (_textureQualityDropdown != null)
        {
            int index = _textureQualityOptions.FindIndex(t => t.limit == option.textureQualityLimit);
            _textureQualityDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
            _textureQualityDropdown.RefreshShownValue();
        }

        // 슬라이더의 Min/Max는 프리팹 값을 그대로 쓴다. (시야각 · 렌더링 거리의 허용 범위가 아직 정해지지 않음)
        RefreshCameraOptionUI(option.verticalFOV, option.farClipPlane);

        RefreshCameraSensitivityUI(option.cameraSensitivityLevel);
    }

    // 시야각 · 렌더링 거리의 슬라이더와 인풋 필드를 같은 값으로 동시에 맞춘다.
    // (서로의 리스너를 다시 깨우지 않도록 WithoutNotify 사용. 위젯은 둘 다 없어도 동작해야 한다)
    private void RefreshCameraOptionUI(float verticalFOV, float farClipPlane)
    {
        if (_fovSlider != null) _fovSlider.SetValueWithoutNotify(verticalFOV);
        if (_renderDistanceSlider != null) _renderDistanceSlider.SetValueWithoutNotify(farClipPlane);

        if (_fovInput != null)
        {
            _fovInput.SetTextWithoutNotify(
                verticalFOV.ToString(CAMERA_VALUE_FORMAT, CultureInfo.InvariantCulture));
        }

        if (_renderDistanceInput != null)
        {
            _renderDistanceInput.SetTextWithoutNotify(
                farClipPlane.ToString(CAMERA_VALUE_FORMAT, CultureInfo.InvariantCulture));
        }
    }

    // 슬라이더와 인풋 필드를 같은 값으로 동시에 맞춘다. (서로의 리스너를 다시 깨우지 않도록 WithoutNotify 사용)
    private void RefreshCameraSensitivityUI(float level)
    {
        if (_cameraSensitivitySlider != null)
        {
            _cameraSensitivitySlider.SetValueWithoutNotify(level);
        }

        if (_cameraSensitivityInput != null)
        {
            _cameraSensitivityInput.SetTextWithoutNotify(
                ToPercent(level).ToString(PERCENT_FORMAT, CultureInfo.InvariantCulture));
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
        if (index < 0 || index >= _shownResolutionOptions.Count) return;

        ResolutionOption selected = _shownResolutionOptions[index];
        FullScreenMode currentMode = CLocalOptionManager.Ins.Option.screenMode;

        CLocalOptionManager.Ins.SetResolution(selected.width, selected.height, currentMode);
    }

    private void OnScreenModeChanged(int index)
    {
        if (index < 0 || index >= _screenModeOptions.Count) return;

        FullScreenMode selectedMode = _screenModeOptions[index].mode;

        // 모드만 바꾸고 저장된 해상도는 건드리지 않는다.
        // 테두리 없는 창모드에서 실제로 적용할 해상도는 매니저가 모니터 기준으로 결정한다.
        CLocalOptionManager.Ins.SetScreenMode(selectedMode);
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

    private void OnShadowToggleChanged(bool isOn)
    {
        CLocalOptionManager.Ins.SetShadow(isOn);
    }

    private void OnTextureQualityDropdownChanged(int index)
    {
        if (index < 0 || index >= _textureQualityOptions.Count) return;

        CLocalOptionManager.Ins.SetTextureQuality(_textureQualityOptions[index].limit);
    }

    // 시야각과 렌더링 거리는 매니저에서 한 함수로 묶여 있어, 바꾸지 않는 쪽은 현재 값을 그대로 다시 넘긴다.
    private void OnFovSliderChanged(float value)
    {
        ApplyCameraOption(value, CLocalOptionManager.Ins.Option.farClipPlane);
    }

    private void OnRenderDistanceSliderChanged(float value)
    {
        ApplyCameraOption(CLocalOptionManager.Ins.Option.verticalFOV, value);
    }

    private void OnFovInputEndEdit(string text)
    {
        OptionData option = CLocalOptionManager.Ins.Option;

        if (!TryParseCameraValue(text, out float verticalFOV))
        {
            // 숫자로 해석할 수 없으면 입력을 버리고 현재 설정값을 다시 보여준다.
            RefreshCameraOptionUI(option.verticalFOV, option.farClipPlane);
            return;
        }

        ApplyCameraOption(ClampToSliderRange(_fovSlider, verticalFOV), option.farClipPlane);
    }

    private void OnRenderDistanceInputEndEdit(string text)
    {
        OptionData option = CLocalOptionManager.Ins.Option;

        if (!TryParseCameraValue(text, out float farClipPlane))
        {
            RefreshCameraOptionUI(option.verticalFOV, option.farClipPlane);
            return;
        }

        ApplyCameraOption(option.verticalFOV, ClampToSliderRange(_renderDistanceSlider, farClipPlane));
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
            RefreshCameraSensitivityUI(CLocalOptionManager.Ins.Option.cameraSensitivityLevel);
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

    #region ─────────────────────────▶ 내부 메서드 - 시야각 및 렌더링 거리 ◀─────────────────────────
    // 값을 옵션에 반영한 뒤, 적용된 최종값으로 슬라이더와 인풋 필드를 다시 맞춘다.
    // 인풋 필드가 소수점을 보여주지 않으므로, 적용값도 같은 단위(정수)로 끊어서 표시와 저장값이 어긋나지 않게 한다.
    // 드래그 · 타이핑 중 매 프레임 파일을 쓰지 않도록 저장은 미루고(save: false), 창이 닫힐 때(OnDisable) 한 번만 저장한다.
    private void ApplyCameraOption(float verticalFOV, float farClipPlane)
    {
        float appliedFOV = Mathf.Round(verticalFOV);
        float appliedFarClipPlane = Mathf.Round(farClipPlane);

        CLocalOptionManager.Ins.SetCameraSettings(appliedFOV, appliedFarClipPlane, false);
        RefreshCameraOptionUI(appliedFOV, appliedFarClipPlane);
    }

    // 입력값의 허용 범위는 슬라이더의 Min/Max를 쓴다. 범위를 프리팹 한 곳에서만 관리하기 위함이다.
    // 슬라이더를 연결하지 않았다면 제한할 기준이 없으므로 입력값을 그대로 쓴다.
    private static float ClampToSliderRange(Slider slider, float value)
    {
        if (slider == null) return value;

        return Mathf.Clamp(value, slider.minValue, slider.maxValue);
    }

    // "60", " 60 ", "60.4" 를 모두 허용한다. 단위 표기(m, °)가 섞여 들어오면 입력을 버린다.
    private static bool TryParseCameraValue(string text, out float value)
    {
        value = 0.0f;
        if (string.IsNullOrWhiteSpace(text)) return false;

        return float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 - 카메라 감도 ◀─────────────────────────
    // 감도를 옵션에 반영한 뒤, 클램프된 최종값으로 두 위젯을 다시 맞춘다.
    // (범위를 벗어난 입력이 들어와도 UI가 실제 저장값과 달라지지 않게 하기 위함)
    private void ApplyCameraSensitivity(float level)
    {
        CLocalOptionManager.Ins.SetCameraSensitivityLevel(level);
        RefreshCameraSensitivityUI(CLocalOptionManager.Ins.Option.cameraSensitivityLevel);
    }

    private static float ToPercent(float level) => RoundPercent(level * PERCENT_SCALE);

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

    #region ─────────────────────────▶ 내부 메서드 - 해상도 ◀─────────────────────────
    // 전달된 해상도를 드롭다운이 가리킬 수 있는 인덱스로 바꿔준다.
    // 프리셋에 없는 해상도면 목록 끝에 항목으로 덧붙이므로, 더 이상 첫 항목(1280 × 720)으로 되돌아가지 않는다.
    private int EnsureResolutionOption(int width, int height)
    {
        if (width <= 0 || height <= 0) return 0;

        int index = _shownResolutionOptions.FindIndex(r => r.width == width && r.height == height);
        if (index >= 0) return index;

        // 전에 덧붙인 항목은 지운다. (모드를 왕복하거나 모니터가 바뀌어도 목록이 계속 늘어나지 않게)
        if (_shownResolutionOptions.Count > _presetResolutionCount)
        {
            _shownResolutionOptions.RemoveRange(
                _presetResolutionCount,
                _shownResolutionOptions.Count - _presetResolutionCount);
        }

        _shownResolutionOptions.Add(new ResolutionOption
        {
            width = width,
            height = height,
            label = BuildResolutionLabel(width, height)
        });
        BuildResolutionDropdown();

        return _shownResolutionOptions.Count - 1;
    }

    private static string BuildResolutionLabel(int width, int height)
    {
        string aspect = GetAspectRatioText(width, height);
        return string.IsNullOrEmpty(aspect)
            ? $"{width} × {height}"
            : $"{width} × {height} ({aspect})";
    }

    // 관례 표기를 찾지 못하면 비율 없이 해상도만 보여준다.
    private static string GetAspectRatioText(int width, int height)
    {
        if (width <= 0 || height <= 0) return string.Empty;

        float ratio = (float)width / height;
        for (int i = 0; i < ASPECT_RATIOS.Length; ++i)
        {
            if (Mathf.Abs(ratio - ASPECT_RATIOS[i].ratio) <= ASPECT_RATIO_TOLERANCE)
            {
                return ASPECT_RATIOS[i].text;
            }
        }

        return string.Empty;
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
    public struct TextureQualityOption
    {
        public int limit; // QualitySettings.globalTextureMipmapLimit에 넣는 값 (0 : 원본 / 1 : 절반 / 2 : 4분의 1)
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
