using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카메라에 부착해 화면 전체에 수중 포스트 이펙트(물색 틴트·비네팅·물결 왜곡·채도)를 적용합니다.
/// Built-in 렌더 파이프라인 전용이며, Hidden/UnderwaterEffect 셰이더가 필요합니다.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public sealed class CUnderwaterEffect : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("물색 틴트")]
    [Tooltip("화면 전체에 섞을 물 색상 (깊이 자동 조정을 끄면 이 값 사용)")]
    [SerializeField] private Color _tintColor = new Color(0.1f, 0.4f, 0.55f, 1f);
    [Tooltip("물색을 섞는 정도 (깊이 자동 조정을 끄면 이 값 사용)")]
    [SerializeField, Range(0f, 1f)] private float _tintStrength = 0.3f;

    [Header("깊이(Y) 자동 틴트")]
    [Tooltip("켜면 카메라 y값에 따라 얕은/깊은 틴트를 자동 보간한다")]
    [SerializeField] private bool _depthTintEnabled = true;
    [Tooltip("이 y값 이상이면 얕은 물로 간주")]
    [SerializeField] private float _maxY = 0f;
    [Tooltip("이 y값 이하이면 깊은 물로 간주")]
    [SerializeField] private float _minY = -50f;
    [Tooltip("얕은 물 색")]
    [SerializeField] private Color _shallowTintColor = new Color(0.2f, 0.55f, 0.65f, 1f);
    [Tooltip("얕은 물 틴트 강도")]
    [SerializeField, Range(0f, 1f)] private float _shallowTintStrength = 0.2f;
    [Tooltip("깊은 물 색")]
    [SerializeField] private Color _deepTintColor = new Color(0.02f, 0.1f, 0.25f, 1f);
    [Tooltip("깊은 물 틴트 강도")]
    [SerializeField, Range(0f, 1f)] private float _deepTintStrength = 0.6f;

    [Header("비네팅")]
    [Tooltip("화면 가장자리를 덮는 색")]
    [SerializeField] private Color _vignetteColor = new Color(0f, 0.05f, 0.1f, 1f);
    [Tooltip("비네팅 강도")]
    [SerializeField, Range(0f, 2f)] private float _vignetteStrength = 0.6f;
    [Tooltip("비네팅이 시작되는 부드러움(클수록 넓게 퍼짐)")]
    [SerializeField, Range(0.01f, 1f)] private float _vignetteSoftness = 0.5f;

    [Header("물결 왜곡 (0이면 끔)")]
    [Tooltip("화면이 일렁이는 정도. 어지러우면 0으로")]
    [SerializeField, Range(0f, 0.02f)] private float _distortStrength = 0.003f;
    [Tooltip("일렁임 속도")]
    [SerializeField, Range(0f, 5f)] private float _distortSpeed = 1f;
    [Tooltip("일렁임 촘촘함")]
    [SerializeField, Range(1f, 30f)] private float _distortScale = 8f;

    [Header("색감")]
    [Tooltip("채도 (1 미만이면 탈색되어 수중 느낌)")]
    [SerializeField, Range(0f, 2f)] private float _saturation = 0.9f;

    [Header("죽음 연출 (눈 감기/뜨기)")]
    [Tooltip("죽음 시 목표 비네팅 강도")]
    [SerializeField, Range(0f, 2f)] private float _deathVignetteStrength = 1.8f;
    [Tooltip("비네팅 강도·색 보간 속도(클수록 빠름)")]
    [SerializeField] private float _deathBlendSpeed = 2f;

    [Header("피격 연출 (이미지 페이드)")]
    [Tooltip("피격 시 페이드할 화면 이미지(붉은 오버레이 등). 색은 이미지 자체에서 지정")]
    [SerializeField] private Image _hitImage;
    [SerializeField] private Canvas _hitCanvas;
    [Tooltip("피격 순간(최고조)의 이미지 알파")]
    [SerializeField, Range(0f, 1f)] private float _hitPeakAlpha = 1f;

    [Header("산소 위기 연출 (붉은 맥동)")]
    [Tooltip("위기 시 테두리 색")]
    [SerializeField] private Color _crisisColor = new Color(0.5f, 0f, 0f, 1f);
    [Tooltip("위기 맥동의 최소 강도")]
    [SerializeField, Range(0f, 2f)] private float _crisisMinStrength = 0.4f;
    [Tooltip("위기 맥동의 최대 강도")]
    [SerializeField, Range(0f, 2f)] private float _crisisMaxStrength = 0.9f;
    [Tooltip("위기 맥동 속도")]
    [SerializeField] private float _crisisPulseSpeed = 4f;

    [Header("다이브 펄스 (잠수 순간 연출)")]
    [Tooltip("펄스 최고조에서 틴트 강도에 더해지는 양")]
    [SerializeField, Range(0f, 1f)] private float _divePulseTint = 0.4f;
    [Tooltip("펄스 최고조에서 왜곡 강도에 더해지는 양")]
    [SerializeField, Range(0f, 0.05f)] private float _divePulseDistort = 0.01f;
    [Tooltip("펄스가 최고조에서 원래대로 돌아오는 시간(초)")]
    [SerializeField] private float _divePulseDuration = 1.2f;
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    /// <summary>죽음 연출을 시작합니다. 비네팅이 서서히 깊어져 눈을 감는 듯한 효과를 냅니다.</summary>
    public void StartDeath() => _deathActive = true;

    /// <summary>죽음 연출을 해제합니다. 비네팅이 서서히 얕아져 눈을 뜨는 듯한 효과를 냅니다.</summary>
    public void StopDeath() => _deathActive = false;

    /// <summary>
    /// 피격 연출: 연결된 이미지의 알파가 즉시 최고조로 올랐다가 <paramref name="duration"/>초에 걸쳐 0으로 사라집니다.
    /// </summary>
    /// <param name="duration">최고조에서 완전히 사라질 때까지의 시간(초)</param>
    public void PlayHit(float duration)
    {
        // 최고조(1)에서 시작해 Update에서 서서히 0으로 감쇠한다.
        _hitPulse = 1f;
        _hitPulseDuration = Mathf.Max(0.01f, duration);
        SetHitImageActive(true); // 쓸 때만 켠다.
        ApplyHitAlpha();         // 튀어오름은 즉시 반영(다음 프레임까지 기다리지 않음).
    }

    /// <summary>산소 위기 연출을 켜거나 끕니다. 켜져 있는 동안 테두리가 붉게 맥동합니다.</summary>
    /// <param name="active">켤지(true) 끌지(false)</param>
    public void SetOxygenCrisis(bool active) => _crisisActive = active;

    /// <summary>
    /// 잠수 순간 연출: 틴트·왜곡이 확 진해졌다가 원래대로 돌아오는 일회성 펄스를 재생합니다.
    /// </summary>
    public void PlayDivePulse()
    {
        // 최고조(1)에서 시작해 Update에서 서서히 0으로 감쇠한다.
        _divePulse = 1f;
    }
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Material _material;

    private bool _deathActive = false;
    private bool _crisisActive = false;
    private float _divePulse = 0f;                // 다이브 펄스 진행도(1=최고조 → 0=없음)
    private float _hitPulse = 0f;                 // 피격 펄스 진행도(1=최고조 → 0=없음)
    private float _hitPulseDuration = 0.5f;       // 피격 펄스가 완전히 사라질 때까지의 시간(초)

    // 비네팅 강도·색의 현재 보간값. 죽음/위기/평상시의 목표값을 향해 매 프레임 부드럽게 수렴한다.
    private float _curVignetteStrength;
    private Color _curVignetteColor;

    // OnRenderImage에서 셰이더로 넘길 최종 계산값. Update가 여기에만 쓰고 인스펙터 필드는 건드리지 않는다.
    private Color _computedTintColor;
    private float _computedTintStrength;
    private Color _computedVignetteColor;
    private float _computedVignetteStrength;

    // 셰이더 프로퍼티 ID 캐싱
    private static readonly int ID_Tint = Shader.PropertyToID("_TintColor");
    private static readonly int ID_TintStr = Shader.PropertyToID("_TintStrength");
    private static readonly int ID_Vig = Shader.PropertyToID("_VignetteColor");
    private static readonly int ID_VigStr = Shader.PropertyToID("_VignetteStrength");
    private static readonly int ID_VigSoft = Shader.PropertyToID("_VignetteSoftness");
    private static readonly int ID_DistStr = Shader.PropertyToID("_DistortStrength");
    private static readonly int ID_DistSpd = Shader.PropertyToID("_DistortSpeed");
    private static readonly int ID_DistScl = Shader.PropertyToID("_DistortScale");
    private static readonly int ID_Sat = Shader.PropertyToID("_Saturation");
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        // 비네팅 보간 시작값을 인스펙터 평상시 값으로 초기화.
        _curVignetteStrength = _vignetteStrength;
        _curVignetteColor = _vignetteColor;

        // 계산값도 인스펙터 값으로 초기화(첫 프레임 렌더가 Update보다 먼저 불릴 경우 대비).
        _computedTintColor = _tintColor;
        _computedTintStrength = _tintStrength;
        _computedVignetteColor = _vignetteColor;
        _computedVignetteStrength = _vignetteStrength;

        // 피격 이미지는 시작 시 꺼두고 투명하게(플레이 중에만 강제; 에디터 프리뷰는 건드리지 않음).
        if (Application.isPlaying)
        {
            _hitPulse = 0f;
            ApplyHitAlpha();
            SetHitImageActive(false);
        }
    }

    private void Update()
    {
        // 1) 틴트 계산: 깊이 자동이 켜져 있으면 y로 얕은/깊은 값을 보간, 아니면 인스펙터 값 그대로.
        if (_depthTintEnabled)
        {
            // y가 maxY(얕음)~minY(깊음). depth01: 0=얕음, 1=깊음.
            float depth01 = Mathf.InverseLerp(_maxY, _minY, transform.position.y);
            _computedTintColor = Color.Lerp(_shallowTintColor, _deepTintColor, depth01);
            _computedTintStrength = Mathf.Lerp(_shallowTintStrength, _deepTintStrength, depth01);
        }
        else
        {
            _computedTintColor = _tintColor;
            _computedTintStrength = _tintStrength;
        }

        // 1-b) 다이브 펄스: 진행도를 서서히 0으로 감쇠시키고, 그만큼 틴트 강도를 추가로 얹는다.
        if (_divePulse > 0f)
        {
            _divePulse = Mathf.MoveTowards(_divePulse, 0f, Time.deltaTime / Mathf.Max(0.01f, _divePulseDuration));
            _computedTintStrength = Mathf.Clamp01(_computedTintStrength + _divePulseTint * _divePulse);
        }

        // 1-c) 피격 이미지 페이드: 튀어오름은 PlayHit에서 즉시, 여기선 0으로 서서히 감쇠만 한다.
        if (_hitPulse > 0f)
        {
            _hitPulse = Mathf.MoveTowards(_hitPulse, 0f, Time.deltaTime / _hitPulseDuration);
            ApplyHitAlpha();
            // 완전히 사라지면 이미지를 꺼서 렌더 비용을 없앤다.
            if (_hitPulse <= 0f) SetHitImageActive(false);
        }

        // 2) 비네팅 계산: 죽음 > 위기 > 평상시 목표값을 정해 강도·색을 부드럽게 보간.
        float vigTarget;
        Color vigColorTarget;

        if (_deathActive)
        {
            vigTarget = _deathVignetteStrength;
            vigColorTarget = _vignetteColor;
        }
        else if (_crisisActive)
        {
            float pulse = (Mathf.Sin(Time.time * _crisisPulseSpeed) + 1f) * 0.5f; // 0~1
            vigTarget = Mathf.Lerp(_crisisMinStrength, _crisisMaxStrength, pulse);
            vigColorTarget = _crisisColor;
        }
        else
        {
            vigTarget = _vignetteStrength;
            vigColorTarget = _vignetteColor;
        }

        float t = 1f - Mathf.Exp(-_deathBlendSpeed * Time.deltaTime);
        _curVignetteStrength = Mathf.Lerp(_curVignetteStrength, vigTarget, t);
        _curVignetteColor = Color.Lerp(_curVignetteColor, vigColorTarget, t);

        _computedVignetteStrength = _curVignetteStrength;
        _computedVignetteColor = _curVignetteColor;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!EnsureMaterial())
        {
            // 셰이더가 없으면 원본을 그대로 통과시킨다.
            Graphics.Blit(src, dest);
            return;
        }

        _material.SetColor(ID_Tint, _computedTintColor);
        _material.SetFloat(ID_TintStr, _computedTintStrength);
        _material.SetColor(ID_Vig, _computedVignetteColor);
        _material.SetFloat(ID_VigStr, _computedVignetteStrength);
        _material.SetFloat(ID_VigSoft, _vignetteSoftness);
        _material.SetFloat(ID_DistStr, _distortStrength + _divePulseDistort * _divePulse);
        _material.SetFloat(ID_DistSpd, _distortSpeed);
        _material.SetFloat(ID_DistScl, _distortScale);
        _material.SetFloat(ID_Sat, _saturation);

        Graphics.Blit(src, dest, _material);
    }

    private void OnDisable()
    {
        if (_material != null)
        {
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
            _material = null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 피격 펄스 진행도를 이미지 알파에 반영한다(RGB는 건드리지 않음).
    private void ApplyHitAlpha()
    {
        if (_hitImage == null) return;
        Color c = _hitImage.color;
        c.a = _hitPeakAlpha * _hitPulse;
        _hitImage.color = c;
    }

    // 피격 이미지 GameObject를 켜고 끈다. 이미 원하는 상태면 아무 것도 하지 않는다.
    private void SetHitImageActive(bool active)
    {
        if (_hitCanvas == null) return;

        GameObject go = _hitCanvas.gameObject;
        if (go.activeSelf != active) go.SetActive(active);
    }

    // 셰이더로부터 머티리얼을 1회 생성한다. 셰이더를 못 찾으면 false.
    private bool EnsureMaterial()
    {
        if (_material != null) return true;

        Shader shader = Shader.Find("Hidden/UnderwaterEffect");
        if (shader == null)
        {
            UDebug.Print("Hidden/UnderwaterEffect 셰이더를 찾지 못했습니다. 셰이더 파일이 프로젝트에 있는지 확인하세요.", LogType.Error);
            return false;
        }

        _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        return true;
    }
    #endregion
}
