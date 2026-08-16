using UnityEngine;

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
        _option.fullScreenMode = fullScreenMode;
        ApplyResolution();
        Save();
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
    #endregion

    /// <summary>현재 옵션을 로컬 파일에 저장합니다.</summary>
    public void Save()
    {
        USaveFile.Save(FILE_NAME, _option);
    }

    /// <summary>로컬 파일에서 옵션을 다시 불러오고 화면/볼륨에 재적용합니다.</summary>
    public void Load()
    {
        _option = USaveFile.Load(FILE_NAME, new OptionData());

        ValidateFrameOption();

        ApplyResolution();
        ApplyFrameAndVSync();
        PublishVolume();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 부모 클래스가 최초 1회 호출합니다.
    protected override void Initialize()
    {
        _option = USaveFile.Load(FILE_NAME, new OptionData());

        ValidateFrameOption();

        ApplyResolution();
        ApplyFrameAndVSync();
        PublishVolume();
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

    // 옵션의 해상도/전체화면 값을 실제 화면에 적용
    private void ApplyResolution()
    {
        Screen.SetResolution(
            _option.resolutionWidth,
            _option.resolutionHeight,
            _option.fullScreenMode);
        OnOptionResolutionChanged.Publish(
            _option.resolutionWidth,
            _option.resolutionHeight,
            _option.fullScreenMode);
    }

    private void ApplyFrameAndVSync()
    {
        QualitySettings.vSyncCount = _option.vSync ? 1 : 0;
        Application.targetFrameRate = _option.targetFrameRate;

        OnOptionFrameSyncChanged.Publish(_option.targetFrameRate, _option.vSync);
    }
    #endregion
}
