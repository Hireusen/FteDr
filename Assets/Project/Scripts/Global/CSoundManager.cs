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
    private string _curBgmId;
    private string _curAmbienceId;

    private bool _useUnderwater; // 수중 분위기 전역 토글
    private float _underwaterCutoff = 1500f;

    private const int PREWARM_COUNT = 4;
    private const float BLEND_2D = 0f; // 카메라(거리감 없음)
    private const float BLEND_3D = 1f; // 공간(거리감 있음)
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ─ SFX ◀─────────────────────────
    /// <summary>
    /// 메인 카메라 위치에서 거리감 없는 효과음을 재생합니다.
    /// </summary>
    public void PlaySfx(string id)
    {
        if (!TryGetClip(id, out CSoundSO so)) return;

        Camera cam = Camera.main;
        Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetPosition(pos);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff);
        PlayOnEmitter(emitter, so, BLEND_2D, so.MinDistance, so.MaxDistance);
    }

    /// <summary>지정한 좌표에서 3D 효과음을 재생합니다.</summary>
    public void PlaySfx(string id, Vector3 pos)
    {
        if (!TryGetClip(id, out CSoundSO so)) return;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetPosition(pos);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff);
        PlayOnEmitter(emitter, so, BLEND_3D, so.MinDistance, so.MaxDistance);
    }

    /// <summary>재생 거리를 덮어씌워 지정한 좌표에서 3D 효과음을 재생합니다.</summary>
    public void PlaySfx(string id, Vector3 pos, float minDistance, float maxDistance)
    {
        if (!TryGetClip(id, out CSoundSO so)) return;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetPosition(pos);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff);
        PlayOnEmitter(emitter, so, BLEND_3D, minDistance, maxDistance);
    }

    /// <summary>지정한 대상을 따라다니며 3D 효과음을 재생합니다.</summary>
    public void PlaySfx(string id, Transform target)
    {
        if (!TryGetClip(id, out CSoundSO so)) return;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetFollow(target);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff);
        PlayOnEmitter(emitter, so, BLEND_3D, so.MinDistance, so.MaxDistance);
    }

    /// <summary>재생 거리를 덮어씌워  지정한 대상을 따라다니며 3D 효과음을 재생합니다.</summary>
    public void PlaySfx(string id, Transform target, float minDistance, float maxDistance)
    {
        if (!TryGetClip(id, out CSoundSO so)) return;

        CSoundEmitter emitter = _factory.Rent();
        emitter.SetFollow(target);
        emitter.SetLowPass(_useUnderwater, _underwaterCutoff);
        PlayOnEmitter(emitter, so, BLEND_3D, minDistance, maxDistance);
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
    /// <summary>수중 분위기(로우패스)를 전역으로 켜거나 끕니다. 이후 재생되는 효과음에 적용됩니다.</summary>
    public void SetUnderwater(bool enabled, float cutoffHz = 1500f)
    {
        _useUnderwater = enabled;
        _underwaterCutoff = cutoffHz;
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
        _bgmSource = UObject.Create(K.NAME_BGM_OBJECT, root).GetOrAddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = BLEND_2D;

        // Ambience 소스
        _ambienceSource = UObject.Create(K.NAME_AMBIENCE_OBJECT, root).GetOrAddComponent<AudioSource>();
        _ambienceSource.playOnAwake = false;
        _ambienceSource.loop = true;
        _ambienceSource.spatialBlend = BLEND_2D;

        // SFX 이미터 풀
        _factory = new CSoundEmitterFactory(root, PREWARM_COUNT);
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

    // 볼륨 접근 격리 지점.
    // 실제 볼륨 매니저(예: CDataManager.Ins.Volume)가 정해지면 이 본문만 교체하세요.
    private (float master, float bgm, float sfx, float ambience) GetVolume()
    {
        return (1f, 1f, 1f, 1f); // 임시 고정값
    }
    #endregion
}
