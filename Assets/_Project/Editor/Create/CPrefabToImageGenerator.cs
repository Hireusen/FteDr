using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class CPrefabToImageGenerator : EditorWindow
{
    private Vector2Int _imageResolution = new Vector2Int(512, 512);
    private Vector3 _cameraAngle = new Vector3(30, 225, 0);
    private float _zoom = 1.2f;
    private Color _backgroundColor = Color.clear;
    private float _lightIntensity = 1.5f;

    private GameObject _targetPrefab;
    private PreviewRenderUtility _previewUtility;
    private Texture2D _previewTexture;

    [MenuItem("Tools/Prefab To Image Generator")]
    public static void ShowWindow()
    {
        GetWindow<CPrefabToImageGenerator>("Prefab to Image");
    }

    private void OnEnable()
    {
        if (_previewUtility == null)
        {
            _previewUtility = new PreviewRenderUtility();
        }
    }

    private void OnDisable()
    {
        CleanUp();
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        if (_previewUtility != null)
        {
            _previewUtility.Cleanup();
            _previewUtility = null;
        }
        if (_previewTexture != null)
        {
            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("출력 설정", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        _targetPrefab = (GameObject)EditorGUILayout.ObjectField("대상 프리팹", _targetPrefab, typeof(GameObject), false);

        GUILayout.Space(10);
        _imageResolution = EditorGUILayout.Vector2IntField("해상도 (가로 x 세로)", _imageResolution);
        _imageResolution.x = Mathf.Max(64, _imageResolution.x);
        _imageResolution.y = Mathf.Max(64, _imageResolution.y);

        _cameraAngle = EditorGUILayout.Vector3Field("카메라 각도 (X, Y, Z)", _cameraAngle);
        _zoom = EditorGUILayout.Slider("줌 (Zoom)", _zoom, 0.1f, 5.0f);

        GUILayout.Space(10);
        GUILayout.Label("렌더링 설정", EditorStyles.boldLabel);

        _backgroundColor = EditorGUILayout.ColorField("배경색", _backgroundColor);
        _lightIntensity = EditorGUILayout.Slider("조명 강도", _lightIntensity, 0f, 3f);

        bool isSettingsChanged = EditorGUI.EndChangeCheck();

        // 설정이 변경되었거나, 프리팹이 등록되었는데 미리보기가 없는 경우 갱신
        if (isSettingsChanged && _targetPrefab != null || (_targetPrefab != null && _previewTexture == null))
        {
            UpdatePreview();
        }
        else if (_targetPrefab == null && _previewTexture != null)
        {
            // 대상이 비워지면 미리보기 삭제
            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }

        GUILayout.Space(20);

        if (_previewTexture != null)
        {
            GUILayout.Label("실시간 미리보기", EditorStyles.boldLabel);

            // UI 영역에 맞춰 비율 유지하며 그리기
            float aspect = (float)_imageResolution.x / _imageResolution.y;
            Rect previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true));
            float drawWidth = Mathf.Min(previewRect.width, previewRect.height * aspect);
            float drawHeight = drawWidth / aspect;

            Rect centerRect = new Rect(
                previewRect.x + (previewRect.width - drawWidth) * 0.5f,
                previewRect.y + (previewRect.height - drawHeight) * 0.5f,
                drawWidth,
                drawHeight
            );

            // 투명 배경이 잘 보이도록 체커보드로 렌더링
            EditorGUI.DrawTextureTransparent(centerRect, _previewTexture, ScaleMode.ScaleToFit);
        }

        GUILayout.Space(20);

        using (new EditorGUI.DisabledScope(_previewTexture == null))
        {
            if (GUILayout.Button("PNG 이미지로 저장", GUILayout.Height(40)))
            {
                SaveImage();
            }
        }
    }

    private void UpdatePreview()
    {
        if (_previewUtility == null)
            _previewUtility = new PreviewRenderUtility();

        GameObject instance = null;
        try
        {
            // 1. 프리팹을 격리된 Preview 씬에 생성
            instance = Instantiate(_targetPrefab);
            _previewUtility.AddSingleGO(instance);

            // 2. 바운딩 박스 계산
            Bounds bounds = CalculateBounds(instance);

            // 3. 카메라 설정
            Camera cam = _previewUtility.camera;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _backgroundColor;
            cam.fieldOfView = 30f;

            // 카메라 위치 연산 (보내주신 안전한 거리 공식을 적용)
            float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
            float distance = (radius * (1f / Mathf.Max(_zoom, 0.01f))) / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

            cam.transform.rotation = Quaternion.Euler(_cameraAngle);
            cam.transform.position = bounds.center - cam.transform.forward * distance;
            cam.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
            cam.farClipPlane = distance + radius * 2f;

            // 4. 조명 설정
            _previewUtility.lights[0].intensity = _lightIntensity;
            _previewUtility.lights[0].transform.rotation = Quaternion.Euler(30f, 40f, 0f); // 기본 주광 각도
            _previewUtility.ambientColor = new Color(0.5f, 0.5f, 0.5f);

            // 5. 렌더링 실행
            _previewUtility.BeginStaticPreview(new Rect(0, 0, _imageResolution.x, _imageResolution.y));
            _previewUtility.Render(true, true);

            // 6. 결과물을 Texture2D로 추출
            RenderTexture rt = _previewUtility.camera.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = rt;

            if (_previewTexture != null) DestroyImmediate(_previewTexture);

            _previewTexture = new Texture2D(_imageResolution.x, _imageResolution.y, TextureFormat.RGBA32, false);
            _previewTexture.ReadPixels(new Rect(0, 0, _imageResolution.x, _imageResolution.y), 0, 0);
            _previewTexture.Apply();

            RenderTexture.active = prevActive;

            // PreviewUtility 내부 메모리 정리
            Texture2D discard = _previewUtility.EndStaticPreview();
            if (discard != null) DestroyImmediate(discard);
        }
        catch (Exception e)
        {
            Debug.LogError($"미리보기 생성 실패: {e.Message}");
        }
        finally
        {
            // 메모리 누수 방지: 추출이 끝난 인스턴스는 즉시 파괴
            if (instance != null)
            {
                DestroyImmediate(instance);
            }
        }
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private void SaveImage()
    {
        if (_previewTexture == null || _targetPrefab == null) return;

        byte[] bytes = _previewTexture.EncodeToPNG();
        string defaultName = _targetPrefab.name + "_Icon.png";
        string path = EditorUtility.SaveFilePanel("이미지 저장", Application.dataPath, defaultName, "png");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log($"[성공] 이미지 저장 완료: {path}");
        }
    }
}
