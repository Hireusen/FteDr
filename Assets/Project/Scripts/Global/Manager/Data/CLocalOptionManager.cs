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
    /// <summary>마스터 볼륨을 설정하고 저장 및 변경 이벤트를 발행합니다.</summary>
    /// <param name="value">0~1 범위의 볼륨 값</param>
    public void SetMasterVolume(float value)
    {
        _option.masterVolume = Mathf.Clamp01(value);
        OnVolumeUpdated();
    }

    /// <summary>배경음 볼륨을 설정하고 저장 및 변경 이벤트를 발행합니다.</summary>
    /// <param name="value">0~1 범위의 볼륨 값</param>
    public void SetBgmVolume(float value)
    {
        _option.bgmVolume = Mathf.Clamp01(value);
        OnVolumeUpdated();
    }

    /// <summary>효과음 볼륨을 설정하고 저장 및 변경 이벤트를 발행합니다.</summary>
    /// <param name="value">0~1 범위의 볼륨 값</param>
    public void SetSfxVolume(float value)
    {
        _option.sfxVolume = Mathf.Clamp01(value);
        OnVolumeUpdated();
    }

    /// <summary>환경음 볼륨을 설정하고 저장 및 변경 이벤트를 발행합니다.</summary>
    /// <param name="value">0~1 범위의 볼륨 값</param>
    public void SetAmbienceVolume(float value)
    {
        _option.ambienceVolume = Mathf.Clamp01(value);
        OnVolumeUpdated();
    }
    #endregion

    #region ─────────────────────────▶ 화면 ◀─────────────────────────
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
        ApplyResolution();
        PublishVolume();
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 부모 클래스가 최초 1회 호출합니다.
    protected override void Initialize()
    {
        _option = USaveFile.Load(FILE_NAME, new OptionData());
        ApplyResolution();
        PublishVolume();
    }

    // 볼륨 변경 공통 처리: 저장 후 이벤트 발행
    private void OnVolumeUpdated()
    {
        Save();
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
    }
    #endregion
}
