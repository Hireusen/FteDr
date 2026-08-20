using UnityEngine;

/// <summary>
/// 효과음을 재생하고, 재생이 끝나면 스스로 풀에 반납되는 이미터입니다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class CSoundEmitter : AFrameable, ILateUpdateFrameable
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private AudioSource _source;
    private AudioLowPassFilter _lowPass; // 수중 분위기용
    private Transform _followTarget; // 추적 대상
    private bool _isActive;

    // 개별(SO) 로우패스 설정 보존 (전역 변경 시 중첩 재계산용)
    private bool _soLowPassOn;
    private float _soLowPassCutoff;

    private float _baseVolume; // SO 원본 볼륨 (실시간 갱신 기준값)

    // 페이드 아웃
    private bool _isFading;
    private float _fadeStartVolume;
    private float _fadeDuration;
    private float _fadeElapsed;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>재생용 오디오 소스입니다.</summary>
    public AudioSource Source => _source;

    /// <summary>SO 원본 볼륨입니다. (볼륨 실시간 재계산 기준)</summary>
    public float BaseVolume => _baseVolume;

    /// <summary>페이드 아웃 진행 중인지 여부입니다.</summary>
    public bool IsFading => _isFading;

    /// <summary>프레임 매니저 실행 순서. 추종 갱신은 늦게 처리합니다.</summary>
    public ELateUpdatePriority LateUpdatePriority => ELateUpdatePriority.Last;

    /// <summary>
    /// 이미터를 1회 초기화합니다. (풀 생성 시 호출)
    /// </summary>
    public void Setup()
    {
        _source = gameObject.GetOrAddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.rolloffMode = AudioRolloffMode.Linear;

        // 수중 로우패스 필터
        _lowPass = gameObject.GetOrAddComponent<AudioLowPassFilter>();
        _lowPass.enabled = false;
    }

    /// <summary>
    /// 고정 좌표에서 재생하도록 위치를 설정합니다.
    /// </summary>
    public void SetPosition(Vector3 pos)
    {
        _followTarget = null;
        transform.position = pos;
    }

    /// <summary>
    /// 지정한 대상을 따라다니며 재생하도록 설정합니다.
    /// </summary>
    public void SetFollow(Transform target)
    {
        _followTarget = target;
        if (target == null) return;

        transform.position = target.position;
    }

    /// <summary>
    /// 전역·개별 로우패스를 중첩하여 필터를 갱신합니다.
    /// 둘 중 하나라도 켜져 있으면 활성화되며, 차단 주파수는 더 낮은(더 먹먹한) 쪽을 채택합니다.
    /// </summary>
    /// <param name="globalOn">전역 로우패스 활성 여부</param>
    /// <param name="globalCutoff">전역 차단 주파수(Hz)</param>
    /// <param name="soOn">개별(SO) 로우패스 활성 여부</param>
    /// <param name="soCutoff">개별(SO) 차단 주파수(Hz)</param>
    public void SetLowPass(bool globalOn, float globalCutoff, bool soOn, float soCutoff)
    {
        // 개별 설정 보존: 이후 전역 변경 시 RefreshLowPass로 중첩 재계산
        _soLowPassOn = soOn;
        _soLowPassCutoff = soCutoff;

        Apply(globalOn, globalCutoff);
    }

    /// <summary>
    /// 전역 로우패스가 바뀌었을 때, 보존된 개별 설정과 중첩하여 필터를 다시 계산합니다.
    /// (SetUnderwater 즉시 반영용)
    /// </summary>
    /// <param name="globalOn">전역 로우패스 활성 여부</param>
    /// <param name="globalCutoff">전역 차단 주파수(Hz)</param>
    public void RefreshLowPass(bool globalOn, float globalCutoff)
    {
        Apply(globalOn, globalCutoff);
    }

    // 전역+보존된 개별 설정으로 필터를 갱신합니다.
    private void Apply(bool globalOn, float globalCutoff)
    {
        if (_lowPass == null) return;

        bool on = globalOn || _soLowPassOn;
        _lowPass.enabled = on;
        if (!on) return;

        _lowPass.cutoffFrequency = USound.ResolveCutoff(globalOn, globalCutoff, _soLowPassOn, _soLowPassCutoff);
    }

    /// <summary>
    /// 클립을 지정하고 재생을 시작합니다. 풀에서 꺼낸 직후 호출됩니다.
    /// </summary>
    /// <param name="clip">재생할 클립</param>
    /// <param name="baseVolume">SO 원본 볼륨</param>
    /// <param name="finalVolume">사용자 볼륨을 반영한 최종 볼륨</param>
    /// <param name="spatialBlend">0이면 2D(거리감 없음), 1이면 완전 3D</param>
    /// <param name="minDistance">최대 음량이 유지되는 최소 거리 (3D일 때만 의미)</param>
    /// <param name="maxDistance">소리가 거의 안 들리게 되는 최대 거리 (3D일 때만 의미)</param>
    public void Play(
        AudioClip clip, float baseVolume, float finalVolume, float spatialBlend,
        float minDistance, float maxDistance)
    {
        _isActive = true;
        _isFading = false;
        _fadeElapsed = 0f;
        _baseVolume = baseVolume;
        _source.spatialBlend = spatialBlend;
        _source.minDistance = minDistance;
        _source.maxDistance = maxDistance;
        _source.clip = clip;
        _source.volume = finalVolume;
        _source.Play();
    }

    /// <summary>
    /// 현재 볼륨을 즉시 갱신합니다. (옵션 실시간 반영용)
    /// 페이드 중인 이미터는 무시합니다.
    /// </summary>
    public void ApplyVolume(float finalVolume)
    {
        if (!_isActive || _isFading) return;
        _source.volume = finalVolume;
    }

    /// <summary>
    /// 재생을 즉시 중단하고 반납합니다.
    /// </summary>
    public void StopImmediate()
    {
        if (!_isActive) return;

        _isActive = false;
        _isFading = false; // 페이드 진행 중이었어도 취소
        _source.Stop();
        CSoundManager.Ins.ReturnEmitter(this);
    }

    /// <summary>
    /// 지정한 시간 동안 페이드 아웃한 뒤 반납합니다.
    /// </summary>
    /// <param name="duration">페이드 시간(초).</param>
    public void FadeOutAndReturn(float duration)
    {
        if (!_isActive) return;

        if (duration <= 0f)
        {
            StopImmediate();
            return;
        }
        _isFading = true;
        _fadeStartVolume = _source.volume;
        _fadeDuration = duration;
        _fadeElapsed = 0f;
    }

    /// <summary>
    /// 프레임 매니저가 매 프레임 호출합니다.
    /// </summary>
    public void ExecuteLateUpdateFrame()
    {
        if (!_isActive) return;

        // 페이드 아웃 진행
        if (_isFading)
        {
            _fadeElapsed += Time.deltaTime;
            float t = _fadeElapsed / _fadeDuration;
            if (t >= 1f)
            {
                StopImmediate();
                return;
            }
            _source.volume = Mathf.Lerp(_fadeStartVolume, 0f, t);
            if (_followTarget != null)
            {
                transform.position = _followTarget.position;
            }
            return;
        }

        // 아직 재생 중이면 추종 대상 위치 갱신
        if (_source.isPlaying)
        {
            if (_followTarget != null)
            {
                transform.position = _followTarget.position;
            }
            return;
        }

        // 재생 종료 → 풀에 반납
        _isActive = false;
        CSoundManager.Ins.ReturnEmitter(this);
    }
    #endregion
}
