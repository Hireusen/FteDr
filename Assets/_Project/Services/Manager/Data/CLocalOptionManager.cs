using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 사용자 옵션을 메모리에 보유하는 매니저입니다.
/// </summary>
public sealed class CLocalOptionManager : ASingleton<CLocalOptionManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private const string FILE_NAME = "option"; // 저장 파일명
    private OptionData _option;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public override bool IsGlobal => true;

    /// <summary>현재 옵션 데이터에 대한 읽기 접근입니다.</summary>
    public OptionData Option => _option;

    /// <summary>
    /// 감도 단계(0~1)를 카메라가 곱해 쓰는 실제 회전 배율로 변환한 값입니다.
    /// 단계가 같은 만큼 오를 때마다 배율이 같은 비율로 늘어나 체감 변화가 균일합니다. (K.CAMERA_SENSITIVITY_* 참고)
    /// </summary>
    public float CameraSensitivity => LevelToCameraSensitivity(_option.cameraSensitivityLevel);

    #region ─────────────────────────▶ 볼륨 ◀─────────────────────────
    /// <summary>마스터 볼륨을 설정하고 변경 이벤트를 발행합니다.</summary>
    /// <param name="value">0~1 범위의 볼륨 값</param>
    /// <param name="save">true면 파일에 즉시 저장합니다. 드래그 중 저장 지연 시 false를 넘기세요.</param>
    public void SetMasterVolume(float value, bool save = true)
    {
        _option.masterVolume = Mathf.Clamp01(value);
        OnVolumeUpdated(save);
    }

    /// <summary>배경음 볼륨을 설정하고 변경 이벤트를 발행합니다.</summary>
    /// <param name="value">0~1 범위의 볼륨 값</param>
    /// <param name="save">true면 파일에 즉시 저장합니다. 드래그 중 저장 지연 시 false를 넘기세요.</param>
    public void SetBgmVolume(float value, bool save = true)
    {
        _option.bgmVolume = Mathf.Clamp01(value);
        OnVolumeUpdated(save);
    }

    /// <summary>효과음 볼륨을 설정하고 변경 이벤트를 발행합니다.</summary>
    /// <param name="value">0~1 범위의 볼륨 값</param>
    /// <param name="save">true면 파일에 즉시 저장합니다. 드래그 중 저장 지연 시 false를 넘기세요.</param>
    public void SetSfxVolume(float value, bool save = true)
    {
        _option.sfxVolume = Mathf.Clamp01(value);
        OnVolumeUpdated(save);
    }

    /// <summary>환경음 볼륨을 설정하고 변경 이벤트를 발행합니다.</summary>
    /// <param name="value">0~1 범위의 볼륨 값</param>
    /// <param name="save">true면 파일에 즉시 저장합니다. 드래그 중 저장 지연 시 false를 넘기세요.</param>
    public void SetAmbienceVolume(float value, bool save = true)
    {
        _option.ambienceVolume = Mathf.Clamp01(value);
        OnVolumeUpdated(save);
    }
    #endregion

    #region ─────────────────────────▶ 화면 및 그래픽 ◀─────────────────────────
    /// <summary>해상도와 전체화면 모드를 설정하고 화면에 적용한 뒤 저장합니다.</summary>
    /// <param name="width">가로 해상도</param>
    /// <param name="height">세로 해상도</param>
    /// <param name="fullScreenMode">전체화면 모드</param>
    public void SetResolution(int width, int height, FullScreenMode fullScreenMode)
    {
        _option.resolutionWidth = width;
        _option.resolutionHeight = height;
        _option.screenMode = fullScreenMode;
        ApplyResolution();
        Save();
    }

    /// <summary>저장된 해상도는 그대로 두고 전체화면 모드만 설정한 뒤 화면에 적용하고 저장합니다.</summary>
    /// <remarks>
    /// 테두리 없는 창모드처럼 해상도를 모니터에 맞춰 강제하는 모드로 갈 때 쓴다.
    /// 이 모드에서 SetResolution으로 모니터 해상도를 덮어쓰면, 창모드로 돌아왔을 때
    /// 사용자가 고른 해상도가 사라지고 창이 모니터 크기로 열린다.
    /// </remarks>
    /// <param name="fullScreenMode">전체화면 모드</param>
    public void SetScreenMode(FullScreenMode fullScreenMode)
    {
        _option.screenMode = fullScreenMode;
        ApplyResolution();
        Save();
    }

    /// <summary>창이 올라가 있는 모니터의 해상도를 가져옵니다.</summary>
    /// <remarks>
    /// Screen.currentResolution은 에디터에서 Game 뷰 크기를 돌려주므로 모니터 해상도로는 쓸 수 없다.
    /// 디스플레이 정보를 지원하지 않는 플랫폼에서는 Screen.currentResolution으로 대체한다.
    /// </remarks>
    public static void GetDisplayResolution(out int width, out int height)
    {
        DisplayInfo displayInfo = Screen.mainWindowDisplayInfo;
        width = displayInfo.width;
        height = displayInfo.height;

        if (width <= 0 || height <= 0)
        {
            width = Screen.currentResolution.width;
            height = Screen.currentResolution.height;
        }
    }

    public void SetTargetFrameRate(int frameRate)
    {
        _option.targetFrameRate = frameRate;
        ApplyFrameAndVSync();
        Save();
    }

    public void SetVSync(bool vSync)
    {
        _option.vSync = vSync;
        ApplyFrameAndVSync();
        Save();
    }

    /// <summary>
    /// 그림자 사용 여부 설정
    /// </summary>
    public void SetShadow(bool useShadow, bool save = true)
    {
        _option.useShadow = useShadow;
        PublishShadow();

        if (save) Save();
    }

    /// <summary>
    /// 텍스처 품질 제한 설정
    /// </summary>
    public void SetTextureQuality(int limit, bool save = true)
    {
        _option.textureQualityLimit = Mathf.Clamp(limit, 0, 3);
        ApplyTextureQuality(limit);

        if (save) Save();
    }

    /// <summary>
    /// 카메라 시야 설정
    /// </summary>
    public void SetCameraSettings(float fov, float clipPlane, bool save = true)
    {
        _option.verticalFOV = fov;
        _option.farClipPlane = clipPlane;
        PublishCamera();

        if (save) Save();
    }
    #endregion

    #region ─────────────────────────▶ 조작 ◀─────────────────────────
    /// <summary>카메라 감도 단계를 설정하고 변경 이벤트를 발행합니다.</summary>
    /// <param name="level">0~1 범위의 감도 단계 (슬라이더 값)</param>
    /// <param name="save">true면 파일에 즉시 저장합니다. 드래그 중 저장 지연 시 false를 넘기세요.</param>
    public void SetCameraSensitivityLevel(float level, bool save = true)
    {
        _option.cameraSensitivityLevel = Mathf.Clamp01(level);

        if (save) Save();
        PublishCameraSensitivity();
    }
    #endregion

    /// <summary>현재 옵션을 로컬 파일에 저장하고 성공 여부를 반환합니다.</summary>
    public bool Save()
    {
        return USaveFile.Save(FILE_NAME, _option);
    }

    /// <summary>로컬 파일에서 옵션을 다시 불러오고 화면/볼륨에 재적용합니다.</summary>
    public void Load()
    {
        _option = USaveFile.Load(FILE_NAME, new OptionData());

        ValidateFrameOption();
        ValidateControlOption();

        ApplyResolution();
        ApplyFrameAndVSync();
        ApplyTextureQuality(_option.textureQualityLimit);
        PublishVolume();
        PublishCameraSensitivity();
        // 그림자 · 시야각 · 렌더링 거리는 씬의 적용 스크립트가 받아 처리한다.
        // 구독보다 이 호출이 먼저일 수 있어, 적용 스크립트도 자기 Start에서 현재 옵션을 직접 한 번 읽는다.
        PublishShadow();
        PublishCamera();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 부모 클래스가 최초 1회 호출합니다.
    protected override void Initialize()
    {
        _option = USaveFile.Load(FILE_NAME, new OptionData());

        ValidateFrameOption();
        ValidateControlOption();

        ApplyResolution();
        ApplyFrameAndVSync();
        ApplyTextureQuality(_option.textureQualityLimit);
        PublishVolume();
        PublishCameraSensitivity();
        // 그림자 · 시야각 · 렌더링 거리는 씬의 적용 스크립트가 받아 처리한다.
        // 구독보다 이 호출이 먼저일 수 있어, 적용 스크립트도 자기 Start에서 현재 옵션을 직접 한 번 읽는다.
        PublishShadow();
        PublishCamera();
    }

    private void ValidateFrameOption()
    {
        if (_option.targetFrameRate == 0)
        {
            _option.targetFrameRate = K.DEFAULT_TARGET_FRAME_RATE;
            _option.vSync = K.DEFAULT_VSYNC;
            Save();
        }
    }

    // 손으로 고친 저장 파일 등으로 범위를 벗어난 값이 들어와도 0~1 안으로 되돌린다.
    private void ValidateControlOption()
    {
        _option.cameraSensitivityLevel = Mathf.Clamp01(_option.cameraSensitivityLevel);
    }

    // 단계 0 → BASE / RANGE, 0.5 → BASE, 1 → BASE * RANGE 로 지수 보간한다.
    private static float LevelToCameraSensitivity(float level)
    {
        return K.CAMERA_SENSITIVITY_BASE * Mathf.Pow(K.CAMERA_SENSITIVITY_RANGE, level * 2.0f - 1.0f);
    }

    // 볼륨 변경 공통 처리: (선택적) 저장 후 이벤트 발행
    private void OnVolumeUpdated(bool save)
    {
        if (save) Save();
        PublishVolume();
    }

    // 현재 옵션의 볼륨 값으로 변경 이벤트를 발행
    private void PublishVolume()
    {
        OnOptionVolumeChanged.Publish(
            _option.masterVolume,
            _option.sfxVolume,
            _option.bgmVolume,
            _option.ambienceVolume);
    }

    // 현재 옵션의 감도로 변경 이벤트를 발행 (구독자가 바로 곱해 쓸 수 있도록 변환된 배율을 싣는다)
    private void PublishCameraSensitivity()
    {
        OnOptionCameraSensitivityChanged.Publish(CameraSensitivity);
    }

    // 현재 옵션의 그림자 사용 여부로 변경 이벤트를 발행
    private void PublishShadow()
    {
        ShadowCastingMode mode = _option.useShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
        OnOptionShadowChanged.Publish(mode);
    }

    // 현재 옵션의 시야각 · 렌더링 거리로 변경 이벤트를 발행
    private void PublishCamera()
    {
        OnOptionCameraChanged.Publish(_option.verticalFOV, _option.farClipPlane);
    }

    // 옵션의 해상도/전체화면 값을 실제 화면에 적용
    private void ApplyResolution()
    {
        int width = _option.resolutionWidth;
        int height = _option.resolutionHeight;

        // 테두리 없는 창모드는 모니터 해상도로 띄운다.
        // 더 낮은 해상도를 넘기면 그만큼만 렌더해서 화면에 늘려 보여주기 때문에 흐려진다.
        if (_option.screenMode == FullScreenMode.FullScreenWindow)
        {
            GetDisplayResolution(out width, out height);
        }

        Screen.SetResolution(width, height, _option.screenMode);
        // 구독자가 실제 화면 크기를 기준으로 계산할 수 있도록, 저장값이 아니라 적용한 값을 싣는다.
        OnOptionResolutionChanged.Publish(width, height, _option.screenMode);
    }

    private void ApplyFrameAndVSync()
    {
        QualitySettings.vSyncCount = _option.vSync ? 1 : 0;
        Application.targetFrameRate = _option.targetFrameRate;

        OnOptionFrameSyncChanged.Publish(_option.targetFrameRate, _option.vSync);
    }

    private void ApplyTextureQuality(int limit)
    {
        QualitySettings.globalTextureMipmapLimit = _option.textureQualityLimit;
        OnOptionTextureQualityChanged.Publish(_option.textureQualityLimit);
    }
    #endregion
}
