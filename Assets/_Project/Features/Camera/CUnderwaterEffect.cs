using UnityEngine;

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
    [Tooltip("화면 전체에 섞을 물 색상")]
    [SerializeField] private Color _tintColor = new Color(0.1f, 0.4f, 0.55f, 1f);
    [Tooltip("물색을 섞는 정도")]
    [SerializeField, Range(0f, 1f)] private float _tintStrength = 0.3f;

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
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Material _material;

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
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!EnsureMaterial())
        {
            // 셰이더가 없으면 원본을 그대로 통과시킨다.
            Graphics.Blit(src, dest);
            return;
        }

        _material.SetColor(ID_Tint, _tintColor);
        _material.SetFloat(ID_TintStr, _tintStrength);
        _material.SetColor(ID_Vig, _vignetteColor);
        _material.SetFloat(ID_VigStr, _vignetteStrength);
        _material.SetFloat(ID_VigSoft, _vignetteSoftness);
        _material.SetFloat(ID_DistStr, _distortStrength);
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
