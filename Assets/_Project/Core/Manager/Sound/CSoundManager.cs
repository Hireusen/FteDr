using UnityEngine;

/// <summary>
/// 사운드 재생의 실제 로직을 담당하는 싱글톤 클래스입니다.
/// 외부에서는 진입점인 USound를 통해 접근해주세요.
/// </summary>
public sealed class CSoundManager : ASingleton<CSoundManager>
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private CSoundEmitterFactory _factory; // SFX 이미터 풀

    private AudioSource _bgmSource; // 단일 BGM 소스
    private AudioSource _ambienceSource; // 독립 환경음 소스
    private AudioLowPassFilter _bgmLowPass; // BGM 로우패스
    private AudioLowPassFilter _ambienceLowPass; // 환경음 로우패스
    private string _curBgmId;
    private string _curAmbienceId;

    // BGM/Ambience 로우패스 오버라이드 (설정 시 SO·전역 대신 강제 적용)
    private bool _bgmLowPassOverride;
    private bool _bgmLowPassOverrideOn;
    private float _bgmLowPassOverrideCutoff;
    private bool _ambienceLowPassOverride;
    private bool _ambienceLowPassOverrideOn;
    private float _ambienceLowPassOverrideCutoff;

    private bool _useUnderwater; // 수중 분위기 전역 토글
    private float _underwaterCutoff = 750f;

    private const int PREWARM_COUNT = 4;
    private const float BLEND_2D = 0f; // 카메라(거리감 없음)
    private const float BLEND_3D = 1f; // 공간(거리감 있음)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ SFX ◀─────────────────────────
    /// <summary>
    /// 메인 카메라 위치에서 거리감 없는 효과음을 재생합니다.
    /// </summary>
    public CSoundEmitter PlaySfx(string id)
    {
        if (!TryGetClip(id, out CSoundSO so)) return null;

        Camera cam = Camera.main;
        Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetPosition(pos);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff, so.UseLowPass, so.LowPassCutoff);
        PlayOnEmitter(emitter, so, BLEND_2D, so.MinDistance, so.MaxDistance);
        return emitter;
    }

    /// <summary>지정한 좌표에서 3D 효과음을 재생합니다.</summary>
    public CSoundEmitter PlaySfx(string id, Vector3 pos)
    {
        if (!TryGetClip(id, out CSoundSO so)) return null;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetPosition(pos);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff, so.UseLowPass, so.LowPassCutoff);
        PlayOnEmitter(emitter, so, BLEND_3D, so.MinDistance, so.MaxDistance);
        return emitter;
    }

    /// <summary>재생 거리를 덮어씌워 지정한 좌표에서 3D 효과음을 재생합니다.</summary>
    public CSoundEmitter PlaySfx(string id, Vector3 pos, float minDistance, float maxDistance)
    {
        if (!TryGetClip(id, out CSoundSO so)) return null;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetPosition(pos);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff, so.UseLowPass, so.LowPassCutoff);
        PlayOnEmitter(emitter, so, BLEND_3D, minDistance, maxDistance);
        return emitter;
    }

    /// <summary>지정한 대상을 따라다니며 3D 효과음을 재생합니다.</summary>
    public CSoundEmitter PlaySfx(string id, Transform target)
    {
        if (!TryGetClip(id, out CSoundSO so)) return null;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetFollow(target);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff, so.UseLowPass, so.LowPassCutoff);
        PlayOnEmitter(emitter, so, BLEND_3D, so.MinDistance, so.MaxDistance);
        return emitter;
    }

    /// <summary>재생 거리를 덮어씌워  지정한 대상을 따라다니며 3D 효과음을 재생합니다.</summary>
    public CSoundEmitter PlaySfx(string id, Transform target, float minDistance, float maxDistance)
    {
        if (!TryGetClip(id, out CSoundSO so)) return null;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetFollow(target);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff, so.UseLowPass, so.LowPassCutoff);
        PlayOnEmitter(emitter, so, BLEND_3D, minDistance, maxDistance);
        return emitter;
    }

    ///<summary> 재생 중인 모든 효과음을 즉시 중단하고 반납합니다.</summary>
    public void StopAllSfx()
    {
        var active = _factory.Active;
        // 역순 순회: StopImmediate가 Return을 호출해 활성 목록을 수정하므로
        for (int i = active.Count - 1; i >= 0; --i)
        {
            active[i].StopImmediate();
        }
    }

    /// <summary>
    /// 재생 중인 모든 효과음을 페이드 아웃한 뒤 반납합니다. (카메라 SFX 포함)
    /// </summary>
    /// <param name="duration">페이드 시간(초)</param>
    public void StopAllSfx(float duration)
    {
        if (duration <= 0f)
        {
            StopAllSfx();
            return;
        }
        var active = _factory.Active;
        for (int i = active.Count - 1; i >= 0; --i)
        {
            active[i].FadeOutAndReturn(duration);
        }
    }

    /// <summary>이미터가 재생을 마쳤을 때 풀에 반납합니다. (CSoundEmitter가 호출)</summary>
    public void ReturnEmitter(CSoundEmitter emitter)
    {
        _factory.Return(emitter);
    }
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ BGM ◀─────────────────────────
    /// <summary>배경음을 설정하고 재생합니다. 한 번에 하나만 재생되며, 같은 ID면 무시합니다.</summary>
    public void PlayBgm(string id)
    {
        if (id.IsBlank()) return;
        if (_curBgmId == id && _bgmSource.isPlaying) return;
        if (!TryGetClip(id, out CSoundSO so)) return;

        var v = GetVolume();
        _bgmSource.clip = so.Clip;
        _bgmSource.volume = so.Volume * v.bgm * v.master;
        _bgmSource.Play();
        _curBgmId = id;

        RefreshBgmLowPass(so); // SO·전역 반영
    }

    /// <summary>배경음을 중단합니다.</summary>
    public void StopBgm()
    {
        _bgmSource.Stop();
        _curBgmId = string.Empty;
    }

    /// <summary>배경음 재생 여부를 반환합니다.</summary>
    public bool IsPlayingBgm() => _bgmSource != null && _bgmSource.isPlaying;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ Ambience ◀─────────────────────────
    /// <summary>환경음을 설정하고 재생합니다. BGM과 독립 채널이라 동시 재생됩니다.</summary>
    public void PlayAmbience(string id)
    {
        if (id.IsBlank()) return;
        if (_curAmbienceId == id && _ambienceSource.isPlaying) return;
        if (!TryGetClip(id, out CSoundSO so)) return;

        var v = GetVolume();
        _ambienceSource.clip = so.Clip;
        _ambienceSource.volume = so.Volume * v.ambience * v.master;
        _ambienceSource.Play();
        _curAmbienceId = id;

        RefreshAmbienceLowPass(so); // SO·전역 반영
    }

    /// <summary>환경음을 중단합니다.</summary>
    public void StopAmbience()
    {
        _ambienceSource.Stop();
        _curAmbienceId = string.Empty;
    }

    /// <summary>환경음 재생 여부를 반환합니다.</summary>
    public bool IsPlayingAmbience() => _ambienceSource != null && _ambienceSource.isPlaying;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ 수중 / 볼륨 ◀─────────────────────────
    /// <summary>
    /// 수중 분위기(로우패스)를 전역으로 켜거나 끕니다.
    /// 재생 중인 BGM·Ambience·SFX 전부에 즉시 반영되며, 이후 재생분에도 적용됩니다.
    /// </summary>
    public void SetUnderwater(bool enabled, float cutoffHz = 750f)
    {
        _useUnderwater = enabled;
        _underwaterCutoff = cutoffHz;

        RefreshAllLowPass();
    }

    /// <summary>
    /// BGM 로우패스를 SO·전역과 무관하게 강제로 지정합니다. (오버라이드)
    /// 자동 규칙으로 되돌리려면 ClearBgmLowPassOverride를 호출하세요.
    /// </summary>
    /// <param name="enabled">강제 활성 여부</param>
    /// <param name="cutoffHz">강제 차단 주파수(Hz)</param>
    public void SetBgmLowPass(bool enabled, float cutoffHz = 750f)
    {
        _bgmLowPassOverride = true;
        _bgmLowPassOverrideOn = enabled;
        _bgmLowPassOverrideCutoff = cutoffHz;
        RefreshBgmLowPass();
    }

    /// <summary>BGM 로우패스 오버라이드를 해제하고 SO·전역 자동 규칙으로 되돌립니다.</summary>
    public void ClearBgmLowPassOverride()
    {
        _bgmLowPassOverride = false;
        RefreshBgmLowPass();
    }

    /// <summary>
    /// 환경음 로우패스를 SO·전역과 무관하게 강제로 지정합니다. (오버라이드)
    /// 자동 규칙으로 되돌리려면 ClearAmbienceLowPassOverride를 호출하세요.
    /// </summary>
    /// <param name="enabled">강제 활성 여부</param>
    /// <param name="cutoffHz">강제 차단 주파수(Hz)</param>
    public void SetAmbienceLowPass(bool enabled, float cutoffHz = 750f)
    {
        _ambienceLowPassOverride = true;
        _ambienceLowPassOverrideOn = enabled;
        _ambienceLowPassOverrideCutoff = cutoffHz;
        RefreshAmbienceLowPass();
    }

    /// <summary>환경음 로우패스 오버라이드를 해제하고 SO·전역 자동 규칙으로 되돌립니다.</summary>
    public void ClearAmbienceLowPassOverride()
    {
        _ambienceLowPassOverride = false;
        RefreshAmbienceLowPass();
    }

    /// <summary>
    /// 볼륨 설정 변경 시 현재 재생 중인 BGM/Ambience/SFX 전부에 즉시 반영합니다.
    /// </summary>
    public void RefreshVolume()
    {
        var v = GetVolume();

        // BGM
        if (!_curBgmId.IsBlank() && TryGetClip(_curBgmId, out CSoundSO bgmSo))
        {
            _bgmSource.volume = bgmSo.Volume * v.bgm * v.master;
        }
        // Ambience
        if (!_curAmbienceId.IsBlank() && TryGetClip(_curAmbienceId, out CSoundSO ambSo))
        {
            _ambienceSource.volume = ambSo.Volume * v.ambience * v.master;
        }
        // SFX 이미터 (페이드 중인 이미터는 ApplyVolume 내부에서 무시)
        var active = _factory.Active;
        int count = active.Count;
        float sfxFactor = v.sfx * v.master;
        for (int i = 0; i < count; ++i)
        {
            CSoundEmitter emitter = active[i];
            emitter.ApplyVolume(emitter.BaseVolume * sfxFactor);
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 부모 클래스가 최초 1회 호출합니다.
    protected override void Initialize()
    {
        Transform root = transform;

        // BGM 소스
        GameObject bgmGo = UObject.Create(K.NAME_BGM_OBJECT, root);
        _bgmSource = bgmGo.GetOrAddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = BLEND_2D;
        _bgmLowPass = bgmGo.GetOrAddComponent<AudioLowPassFilter>();
        _bgmLowPass.enabled = false;

        // Ambience 소스
        GameObject ambGo = UObject.Create(K.NAME_AMBIENCE_OBJECT, root);
        _ambienceSource = ambGo.GetOrAddComponent<AudioSource>();
        _ambienceSource.playOnAwake = false;
        _ambienceSource.loop = true;
        _ambienceSource.spatialBlend = BLEND_2D;
        _ambienceLowPass = ambGo.GetOrAddComponent<AudioLowPassFilter>();
        _ambienceLowPass.enabled = false;

        // SFX 이미터 풀
        _factory = new CSoundEmitterFactory(root, PREWARM_COUNT);

        // 옵션 볼륨 변경 구독
        CEventBus<OnOptionVolumeChanged>.Subscribe(OnOptionVolumeChangedHandler);
    }

    // 싱글톤 파괴 시 이벤트 구독 해제 (부모의 OnDestroy 확장)
    protected override void OnDestroy()
    {
        CEventBus<OnOptionVolumeChanged>.Unsubscribe(OnOptionVolumeChangedHandler);
        base.OnDestroy();
    }

    private void PlayOnEmitter(
        CSoundEmitter emitter, CSoundSO so, float spatialBlend, float minDistance, float maxDistance)
    {
        var v = GetVolume();
        emitter.Play(so.Clip, so.Volume, so.Volume * v.sfx * v.master, spatialBlend, minDistance, maxDistance);
    }

    private bool TryGetClip(string id, out CSoundSO so)
    {
        so = UData.Sound(id);
        if (so == null) return false;

        return true;
    }

    // 볼륨 접근 격리 지점. 로컬 옵션 매니저에서 현재 볼륨을 읽어옵니다.
    private (float master, float bgm, float sfx, float ambience) GetVolume()
    {
        OptionData opt = CLocalOptionManager.Ins.Option;
        return (opt.masterVolume, opt.bgmVolume, opt.sfxVolume, opt.ambienceVolume);
    }

    // 옵션 볼륨 변경 이벤트 구독 핸들러: 재생 중인 사운드에 즉시 반영
    private void OnOptionVolumeChangedHandler(OnOptionVolumeChanged ctx)
    {
        RefreshVolume();
    }

    // 전역 로우패스 변경 시 재생 중인 BGM·Ambience·SFX 필터를 모두 재계산합니다.
    private void RefreshAllLowPass()
    {
        RefreshBgmLowPass();
        RefreshAmbienceLowPass();

        // 재생 중인 SFX 이미터도 전역+개별 중첩으로 즉시 갱신
        var active = _factory.Active;
        int count = active.Count;
        for (int i = 0; i < count; ++i)
        {
            active[i].RefreshLowPass(_useUnderwater, _underwaterCutoff);
        }
    }

    // 현재 BGM SO를 조회해 BGM 로우패스를 재계산합니다.
    private void RefreshBgmLowPass()
    {
        CSoundSO so = null;
        if (!_curBgmId.IsBlank()) TryGetClip(_curBgmId, out so);
        RefreshBgmLowPass(so);
    }

    // 주어진 SO로 BGM 로우패스를 재계산합니다. (오버라이드 우선)
    private void RefreshBgmLowPass(CSoundSO so)
    {
        if (_bgmLowPass == null) return;

        if (_bgmLowPassOverride)
        {
            _bgmLowPass.enabled = _bgmLowPassOverrideOn;
            if (_bgmLowPassOverrideOn) _bgmLowPass.cutoffFrequency = _bgmLowPassOverrideCutoff;
            return;
        }

        bool soOn = so != null && so.UseLowPass;
        float soCutoff = so != null ? so.LowPassCutoff : 22000f;
        ApplyFilter(_bgmLowPass, soOn, soCutoff);
    }

    // 현재 Ambience SO를 조회해 환경음 로우패스를 재계산합니다.
    private void RefreshAmbienceLowPass()
    {
        CSoundSO so = null;
        if (!_curAmbienceId.IsBlank()) TryGetClip(_curAmbienceId, out so);
        RefreshAmbienceLowPass(so);
    }

    // 주어진 SO로 환경음 로우패스를 재계산합니다. (오버라이드 우선)
    private void RefreshAmbienceLowPass(CSoundSO so)
    {
        if (_ambienceLowPass == null) return;

        if (_ambienceLowPassOverride)
        {
            _ambienceLowPass.enabled = _ambienceLowPassOverrideOn;
            if (_ambienceLowPassOverrideOn) _ambienceLowPass.cutoffFrequency = _ambienceLowPassOverrideCutoff;
            return;
        }

        bool soOn = so != null && so.UseLowPass;
        float soCutoff = so != null ? so.LowPassCutoff : 22000f;
        ApplyFilter(_ambienceLowPass, soOn, soCutoff);
    }

    // 전역+개별 중첩 규칙으로 필터를 적용합니다. (BGM·Ambience 공용)
    private void ApplyFilter(AudioLowPassFilter filter, bool soOn, float soCutoff)
    {
        bool on = _useUnderwater || soOn;
        filter.enabled = on;
        if (!on) return;

        filter.cutoffFrequency = USound.ResolveCutoff(_useUnderwater, _underwaterCutoff, soOn, soCutoff);
    }
    #endregion
}
