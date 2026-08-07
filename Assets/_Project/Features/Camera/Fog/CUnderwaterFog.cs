using UnityEngine;

/// <summary>
/// 카메라에 부착해 깊이(카메라로부터의 거리) 기반 수중 안개를 화면 전체에 적용합니다.
/// 포스트 이펙트라 오브젝트 셰이더 종류와 무관하게 지형 포함 모든 픽셀에 적용됩니다.
/// Built-in 렌더 파이프라인 전용이며, Hidden/UnderwaterFog 셰이더와 카메라 Depth Texture가 필요합니다.
/// CUnderwaterEffect(틴트·비네팅)와 별개로 독립 동작하며, 같은 카메라에 함께 붙여도 됩니다.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public sealed class CUnderwaterFog : AMono
{
    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Tooltip("안개 색")]
    [SerializeField] private Color _fogColor = new Color(0.08f, 0.28f, 0.4f, 1f);
    [Tooltip("안개가 시작되는 거리(m). 이 안쪽은 선명")]
    [SerializeField] private float _fogStart = 15f;
    [Tooltip("완전히 안개색이 되는 거리(m)")]
    [SerializeField] private float _fogEnd = 60f;
    [Tooltip("최대 안개 농도(1이면 먼 곳이 완전히 안개색)")]
    [SerializeField, Range(0f, 1f)] private float _fogMaxDensity = 1f;
    #endregion

    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private Material _material;
    private Camera _camera;

    private static readonly int _fogColorID = Shader.PropertyToID("_FogColor");
    private static readonly int _fogStartID = Shader.PropertyToID("_FogStart");
    private static readonly int _fogEndID = Shader.PropertyToID("_FogEnd");
    private static readonly int _fogMaxDensityID = Shader.PropertyToID("_FogMaxDensity");
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnEnable()
    {
        // 이 이펙트는 깊이 정보가 필요하므로 카메라의 Depth Texture 생성을 켠다.
        _camera = GetComponent<Camera>();
        _camera.depthTextureMode |= DepthTextureMode.Depth;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!EnsureMaterial())
        {
            Graphics.Blit(src, dest);
            return;
        }

        _material.SetColor(_fogColorID, _fogColor);
        _material.SetFloat(_fogStartID, _fogStart);
        _material.SetFloat(_fogEndID, _fogEnd);
        _material.SetFloat(_fogMaxDensityID, _fogMaxDensity);

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
    private bool EnsureMaterial()
    {
        if (_material != null) return true;

        Shader shader = Shader.Find("Hidden/UnderwaterFog");
        if (shader == null)
        {
            UDebug.Print("Hidden/UnderwaterFog 셰이더를 찾지 못했습니다. 셰이더 파일이 프로젝트에 있는지 확인하세요.", LogType.Error);
            return false;
        }

        _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        return true;
    }
    #endregion
}
