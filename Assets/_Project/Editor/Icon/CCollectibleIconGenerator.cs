#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 3D 프리팹 아이콘 렌더러 및 아틀라스 자동 등록 에디터 윈도우
public class CollectibleIconGenerator : EditorWindow
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private string _outputFolder = K.ITEM_ICON_EXPORT_PATH;

    private readonly int[] _resolutions = { 256, 512, 1024 };
    private readonly string[] _resolutionLabels = { "256 x 256", "512 x 512", "1024 x 1024" };
    private int _resolutionIndex = 1;

    private float _pitch = 25f;
    private float _yaw = 35f;
    private float _padding = 1.35f;
    private bool _orthographic = true;
    private bool _onlySelected = false;
    private Vector3 _cameraOffset = Vector3.zero;

    private float _keyIntensity = 1.6f;
    private float _keyPitch = 30f;
    private float _keyYaw = 40f;
    private Color _keyColor = Color.white;

    private float _fillIntensity = 1.2f;
    private float _fillPitch = -25f;
    private float _fillYaw = 220f;
    private Color _fillColor = new Color(0.9f, 0.9f, 1f);

    private Color _ambientColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private UnityEngine.U2D.SpriteAtlas _atlasToRepack;

    private readonly List<string> _failLog = new();
    private Vector2 _failScroll;

    private Texture2D _previewTexture;
    private string _previewLabel;
    private bool _livePreview = false;

    private CIconGeneratorPreset _preset;
    #endregion

    #region ─────────────────────────▶ 메뉴 진입점 ◀─────────────────────────
    [MenuItem("Tools/Create/아이템 아이콘 생성기")]
    private static void Open()
    {
        var window = GetWindow<CollectibleIconGenerator>("아이템 아이콘 생성기");
        window.minSize = new Vector2(380, 750);
    }

    private void OnDisable()
    {
        // 메모리 릭 방지: 미리보기 텍스처 해제
        if (_previewTexture != null)
        {
            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }
    }
    #endregion

    #region ─────────────────────────▶ 프리셋 ◀─────────────────────────
    private void DrawPresetSection()
    {
        EditorGUILayout.LabelField("프리셋", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _preset = (CIconGeneratorPreset)EditorGUILayout.ObjectField(
            "현재 프리셋", _preset, typeof(CIconGeneratorPreset), false);

        // 프리셋 변경 시 자동 로드
        if (EditorGUI.EndChangeCheck() && _preset != null)
        {
            LoadFromPreset(_preset);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_preset == null))
            {
                if (GUILayout.Button("현재 값을 이 프리셋에 저장"))
                {
                    SaveToPreset(_preset);
                }
                if (GUILayout.Button("이 프리셋 불러오기"))
                {
                    LoadFromPreset(_preset);
                }
            }
        }

        if (GUILayout.Button("새 프리셋으로 저장..."))
        {
            SaveAsNewPreset();
        }

        EditorGUILayout.Space();
    }

    // 현재 설정을 프리셋 에셋에 덮어쓰기
    private void SaveToPreset(CIconGeneratorPreset preset)
    {
        if (preset == null) return;

        preset.resolutionIndex = _resolutionIndex;

        preset.pitch = _pitch;
        preset.yaw = _yaw;
        preset.padding = _padding;
        preset.orthographic = _orthographic;
        preset.cameraOffset = _cameraOffset;

        preset.keyIntensity = _keyIntensity;
        preset.keyPitch = _keyPitch;
        preset.keyYaw = _keyYaw;
        preset.keyColor = _keyColor;

        preset.fillIntensity = _fillIntensity;
        preset.fillPitch = _fillPitch;
        preset.fillYaw = _fillYaw;
        preset.fillColor = _fillColor;

        preset.ambientColor = _ambientColor;

        EditorUtility.SetDirty(preset);
        AssetDatabase.SaveAssets();
        Debug.Log($"프리셋 '{preset.name}'에 현재 설정을 저장했습니다.");
    }

    // 프리셋 에셋에서 설정 불러오기
    private void LoadFromPreset(CIconGeneratorPreset preset)
    {
        if (preset == null) return;

        _resolutionIndex = Mathf.Clamp(preset.resolutionIndex, 0, _resolutions.Length - 1);

        _pitch = preset.pitch;
        _yaw = preset.yaw;
        _padding = preset.padding;
        _orthographic = preset.orthographic;
        _cameraOffset = preset.cameraOffset;

        _keyIntensity = preset.keyIntensity;
        _keyPitch = preset.keyPitch;
        _keyYaw = preset.keyYaw;
        _keyColor = preset.keyColor;

        _fillIntensity = preset.fillIntensity;
        _fillPitch = preset.fillPitch;
        _fillYaw = preset.fillYaw;
        _fillColor = preset.fillColor;

        _ambientColor = preset.ambientColor;

        if (_livePreview)
        {
            RefreshPreview();
        }
        Repaint();
    }

    // 새 프리셋 에셋 파일 생성 및 저장
    private void SaveAsNewPreset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "새 프리셋 저장",
            "IconPreset",
            "asset",
            "프리셋을 저장할 위치와 이름을 지정하세요.");

        if (string.IsNullOrEmpty(path)) return;

        CIconGeneratorPreset newPreset = ScriptableObject.CreateInstance<CIconGeneratorPreset>();
        AssetDatabase.CreateAsset(newPreset, path);

        SaveToPreset(newPreset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _preset = newPreset;
        EditorGUIUtility.PingObject(newPreset);
        Debug.Log($"새 프리셋을 저장했습니다: {path}");
    }
    #endregion

    #region ─────────────────────────▶ GUI ◀─────────────────────────
    private void OnGUI()
    {
        DrawPresetSection();

        EditorGUILayout.LabelField("출력 설정", EditorStyles.boldLabel);
        _outputFolder = EditorGUILayout.TextField("아이콘 저장 폴더", _outputFolder);
        _resolutionIndex = EditorGUILayout.Popup("해상도", _resolutionIndex, _resolutionLabels);

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("카메라 설정", EditorStyles.boldLabel);
        _pitch = EditorGUILayout.Slider("Pitch (상하 각도)", _pitch, -80f, 80f);
        _yaw = EditorGUILayout.Slider("Yaw (좌우 각도)", _yaw, -180f, 180f);
        _padding = EditorGUILayout.Slider("Padding (여유 배율)", _padding, 0.5f, 2.0f);
        _orthographic = EditorGUILayout.Toggle("직교(Orthographic) 카메라", _orthographic);
        _cameraOffset = EditorGUILayout.Vector3Field("오프셋 (위치 조정)", _cameraOffset);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("조명 설정", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("주광 (Key Light)", EditorStyles.miniBoldLabel);
        _keyIntensity = EditorGUILayout.Slider("세기", _keyIntensity, 0f, 4f);
        _keyPitch = EditorGUILayout.Slider("상하 각도", _keyPitch, -180f, 180f);
        _keyYaw = EditorGUILayout.Slider("좌우 각도", _keyYaw, -180f, 180f);
        _keyColor = EditorGUILayout.ColorField("색", _keyColor);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("보조광 (Fill Light)", EditorStyles.miniBoldLabel);
        _fillIntensity = EditorGUILayout.Slider("세기", _fillIntensity, 0f, 4f);
        _fillPitch = EditorGUILayout.Slider("상하 각도", _fillPitch, -180f, 180f);
        _fillYaw = EditorGUILayout.Slider("좌우 각도", _fillYaw, -180f, 180f);
        _fillColor = EditorGUILayout.ColorField("색", _fillColor);

        EditorGUILayout.Space(2);
        _ambientColor = EditorGUILayout.ColorField("환경광 (Ambient)", _ambientColor);

        bool settingsChanged = EditorGUI.EndChangeCheck();

        // 설정 변경 시 실시간 미리보기 갱신
        if (settingsChanged && _livePreview)
        {
            RefreshPreview();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("대상 선택", EditorStyles.boldLabel);
        _onlySelected = EditorGUILayout.ToggleLeft(
            "선택된 CCollectibleSO만 생성 (해제 시 프로젝트 전체 재생성)", _onlySelected);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sprite Atlas (선택 사항)", EditorStyles.boldLabel);
        _atlasToRepack = (UnityEngine.U2D.SpriteAtlas)EditorGUILayout.ObjectField(
            "즉시 리패킹할 아틀라스", _atlasToRepack, typeof(UnityEngine.U2D.SpriteAtlas), false);
        EditorGUILayout.HelpBox(
            "저장 폴더가 아틀라스 설정에 포함되어 있으면 자동 갱신됩니다.\n" +
            "즉시 적용이 필요할 때만 아래 리패킹 버튼을 사용하세요.",
            MessageType.Info);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_outputFolder)))
        {
            string buttonLabel = _onlySelected ? "선택 항목 아이콘 생성" : "전체 아이콘 재생성";
            if (GUILayout.Button(buttonLabel, GUILayout.Height(32)))
            {
                GenerateIcons(_onlySelected);
            }
        }

        if (_atlasToRepack != null && GUILayout.Button("아틀라스 즉시 리패킹"))
        {
            UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(
                new[] { _atlasToRepack }, EditorUserBuildSettings.activeBuildTarget);
            Debug.Log($"'{_atlasToRepack.name}' 아틀라스를 리패킹했습니다.");
        }

        DrawPreview();
        DrawFailLog();
    }

    // 미리보기 텍스처 렌더링 및 출력
    private void DrawPreview()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);

        _livePreview = EditorGUILayout.ToggleLeft(
            "실시간 미리보기 (카메라/조명 변경 시 자동 갱신)", _livePreview);

        if (GUILayout.Button("미리보기 갱신 (0번 항목)"))
        {
            RefreshPreview();
        }

        if (_previewTexture != null)
        {
            if (!string.IsNullOrEmpty(_previewLabel))
            {
                EditorGUILayout.LabelField(_previewLabel, EditorStyles.miniLabel);
            }

            float size = Mathf.Min(EditorGUIUtility.currentViewWidth - 30f, 256f);
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));

            EditorGUI.DrawTextureTransparent(rect, _previewTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUILayout.HelpBox("미리보기가 없습니다.", MessageType.None);
        }
    }

    // 대상 0번째 객체를 미리보기 텍스처로 렌더링
    private void RefreshPreview()
    {
        List<CCollectibleSO> targets = CollectTargets(_onlySelected);
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("미리보기", "대상 CCollectibleSO가 없습니다.", "확인");
            return;
        }

        CCollectibleSO so = targets[0];
        int resolution = _resolutions[_resolutionIndex];

        if (_previewTexture != null)
        {
            DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }

        PreviewRenderUtility preview = new PreviewRenderUtility();
        try
        {
            if (RenderToTexture(so, preview, resolution, out Texture2D tex, out string error))
            {
                _previewTexture = tex;
                _previewLabel = $"{so.name}  ({resolution}x{resolution})";
            }
            else
            {
                EditorUtility.DisplayDialog("미리보기 실패", $"{so.name}\n{error}", "확인");
            }
        }
        finally
        {
            preview.Cleanup();
        }

        Repaint();
    }

    // 렌더링 실패 로그 출력
    private void DrawFailLog()
    {
        if (_failLog.Count == 0) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"실패 목록 ({_failLog.Count}건)", EditorStyles.boldLabel);
        _failScroll = EditorGUILayout.BeginScrollView(_failScroll, GUILayout.Height(140));
        foreach (string line in _failLog)
        {
            EditorGUILayout.HelpBox(line, MessageType.Warning);
        }
        EditorGUILayout.EndScrollView();
    }
    #endregion

    #region ─────────────────────────▶ 생성 파이프라인 ◀─────────────────────────
    // 타겟 아이콘 일괄 생성 및 파일 저장
    private void GenerateIcons(bool onlySelected)
    {
        List<CCollectibleSO> targets = CollectTargets(onlySelected);
        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("아이콘 생성", "대상 CCollectibleSO가 없습니다.", "확인");
            return;
        }

        _failLog.Clear();
        EnsureFolderExists(_outputFolder);

        int successCount = 0;
        int resolution = _resolutions[_resolutionIndex];

        PreviewRenderUtility preview = new PreviewRenderUtility();

        try
        {
            for (int i = 0; i < targets.Count; ++i)
            {
                CCollectibleSO so = targets[i];

                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    "아이템 아이콘 생성 중",
                    $"({i + 1}/{targets.Count}) {so.name}",
                    (float)i / targets.Count);
                if (cancel) break;

                if (TryGenerateOne(so, preview, resolution, out string error))
                {
                    successCount++;
                }
                else
                {
                    _failLog.Add($"{so.name} : {error}");
                }
            }
        }
        finally
        {
            // 렌더링 리소스 강제 정리
            EditorUtility.ClearProgressBar();
            if (preview != null)
            {
                preview.Cleanup();
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"아이콘 생성 완료: 성공 {successCount} / 실패 {_failLog.Count} (전체 {targets.Count})");
        EditorUtility.DisplayDialog(
            "아이콘 생성 완료",
            $"성공: {successCount}\n실패: {_failLog.Count}\n(자세한 내역은 창 하단의 실패 목록 참고)",
            "확인");
    }

    // 조건에 따른 CCollectibleSO 목록 수집
    private List<CCollectibleSO> CollectTargets(bool onlySelected)
    {
        if (onlySelected)
        {
            return Selection.objects.OfType<CCollectibleSO>().ToList();
        }

        string[] guids = AssetDatabase.FindAssets("t:CCollectibleSO");
        List<CCollectibleSO> result = new(guids.Length);
        for (int i = 0; i < guids.Length; ++i)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            CCollectibleSO so = AssetDatabase.LoadAssetAtPath<CCollectibleSO>(path);
            if (so != null) result.Add(so);
        }
        return result;
    }

    // 단일 아이콘 생성 및 에셋 임포트 처리
    private bool TryGenerateOne(CCollectibleSO so, PreviewRenderUtility preview, int resolution, out string error)
    {
        if (!RenderToTexture(so, preview, resolution, out Texture2D texture, out error))
        {
            return false;
        }

        try
        {
            string idOrName = !string.IsNullOrWhiteSpace(so.Id) ? so.Id : so.name;
            string fileName = SanitizeFileName(idOrName);
            string assetPath = $"{_outputFolder}/{fileName}.png";

            File.WriteAllBytes(ToAbsolutePath(assetPath), texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            ApplySpriteImportSettings(assetPath, resolution);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                error = "Sprite 로드에 실패했습니다.";
                return false;
            }

            so.SetIcon(sprite);
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
        finally
        {
            if (texture != null) DestroyImmediate(texture);
        }
    }

    // 프리팹 렌더링 및 텍스처 추출
    private bool RenderToTexture(CCollectibleSO so, PreviewRenderUtility preview, int resolution, out Texture2D texture, out string error)
    {
        error = string.Empty;
        texture = null;

        GameObject prefab = so.Prefab;
        if (prefab == null)
        {
            error = "Prefab이 비어있습니다.";
            return false;
        }

        GameObject instance = null;
        try
        {
            instance = Instantiate(prefab);
            preview.AddSingleGO(instance);

            if (!TryCalculateBounds(instance, out Bounds bounds))
            {
                error = "Renderer를 찾을 수 없습니다.";
                return false;
            }

            preview.BeginStaticPreview(new Rect(0, 0, resolution, resolution));
            SetupLights(preview);
            PositionCamera(preview.camera, bounds, _pitch, _yaw, _padding, _orthographic, _cameraOffset);

            // 투명 배경 처리
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = Color.clear;

            preview.Render(true, true);

            {
                // 렌더링 결과(RT) 픽셀 복사
                RenderTexture internalRT = preview.camera.targetTexture;
                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = internalRT;

                texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                texture.Apply();

                RenderTexture.active = prevActive;

                Texture2D discard = preview.EndStaticPreview();
                if (discard != null) DestroyImmediate(discard);
            }

            if (texture == null)
            {
                error = "렌더링에 실패했습니다.";
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            if (texture != null) { DestroyImmediate(texture); texture = null; }
            return false;
        }
        finally
        {
            if (instance != null)
            {
                DestroyImmediate(instance, true);
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ Bounds / 카메라 ◀─────────────────────────
    // 루트 오브젝트의 전체 렌더러 바운드 계산
    private static bool TryCalculateBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; ++i)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return true;
    }

    // 피사체 크기에 맞춰 카메라 위치 및 클리핑 평면 설정
    private static void PositionCamera(
        Camera camera, Bounds bounds, float pitch, float yaw, float padding, bool orthographic, Vector3 offset)
    {
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 forward = rotation * Vector3.forward;

        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
        camera.transform.rotation = rotation;
        camera.orthographic = orthographic;

        float distance;
        if (orthographic)
        {
            camera.orthographicSize = radius * padding;
            distance = radius * 3f;
        }
        else
        {
            camera.fieldOfView = 30f;
            distance = (radius * padding) / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        // 거리 및 위치 오프셋(Local) 적용
        camera.transform.position = bounds.center - forward * distance;
        camera.transform.Translate(offset, Space.Self);

        camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
        camera.farClipPlane = distance + radius * 2f;
    }

    // 주광/보조광/환경광 렌더러 조명 세팅
    private void SetupLights(PreviewRenderUtility preview)
    {
        preview.lights[0].intensity = _keyIntensity;
        preview.lights[0].transform.rotation = Quaternion.Euler(_keyPitch, _keyYaw, 0f);
        preview.lights[0].color = _keyColor;

        preview.lights[1].intensity = _fillIntensity;
        preview.lights[1].transform.rotation = Quaternion.Euler(_fillPitch, _fillYaw, 0f);
        preview.lights[1].color = _fillColor;

        preview.ambientColor = _ambientColor;
    }
    #endregion

    #region ─────────────────────────▶ Sprite Import ◀─────────────────────────
    // 임포트된 텍스처를 Sprite(UI) 포맷으로 설정 변경
    private static void ApplySpriteImportSettings(string assetPath, int maxSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = Mathf.NextPowerOfTwo(maxSize);
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.isReadable = false;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }
    #endregion

    #region ─────────────────────────▶ 경로 유틸 ◀─────────────────────────
    // 지정된 폴더가 없으면 생성
    private static void EnsureFolderExists(string assetsRelativeFolder)
    {
        string absolute = ToAbsolutePath(assetsRelativeFolder);
        if (!Directory.Exists(absolute))
        {
            Directory.CreateDirectory(absolute);
            AssetDatabase.Refresh();
        }
    }

    // Assets 하위 경로를 로컬 절대 경로로 변환
    private static string ToAbsolutePath(string assetsRelativePath)
    {
        string projectRoot = Application.dataPath.Substring(
            0, Application.dataPath.Length - "Assets".Length);
        return Path.Combine(projectRoot, assetsRelativePath);
    }

    // 파일 이름으로 사용할 수 없는 특수 문자 제거
    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
    #endregion
}
#endif
