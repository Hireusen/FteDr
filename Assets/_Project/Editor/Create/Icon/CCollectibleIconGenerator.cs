#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 3D 프리팹을 촬영하여 투명 배경 PNG 아이콘을 생성하고 패킹된 아틀라스에 자동 등록합니다.
/// </summary>
public class CollectibleIconGenerator : EditorWindow
{
    #region ─────────────────────────▶ 내부 변수 ◀─────────────────────────
    private string _outputFolder = K.ITEM_ICON_EXPORT_PATH;

    private readonly int[] _resolutions = { 256, 512, 1024 };
    private readonly string[] _resolutionLabels = { "256 x 256", "512 x 512", "1024 x 1024" };
    private int _resolutionIndex = 1;

    private float _pitch = 25f;   // 카메라 상하 각도
    private float _yaw = 35f;     // 카메라 좌우 각도
    private float _padding = 1.35f; // 여유 배율 (1.0 = 딱 맞음, 클수록 여유 공간 넓음)
    private bool _orthographic = true;
    private bool _onlySelected = false;

    // 조명 설정
    private float _keyIntensity = 1.6f;                       // 주광 세기
    private float _keyPitch = 30f;                            // 주광 상하 각도
    private float _keyYaw = 40f;                              // 주광 좌우 각도
    private Color _keyColor = Color.white;                    // 주광 색

    private float _fillIntensity = 1.2f;                      // 보조광 세기
    private float _fillPitch = -25f;                          // 보조광 상하 각도
    private float _fillYaw = 220f;                            // 보조광 좌우 각도
    private Color _fillColor = new Color(0.9f, 0.9f, 1f);     // 보조광 색

    private Color _ambientColor = new Color(0.6f, 0.6f, 0.6f, 1f); // 환경광

    private UnityEngine.U2D.SpriteAtlas _atlasToRepack;

    private readonly List<string> _failLog = new();
    private Vector2 _failScroll;

    // 미리보기용
    private Texture2D _previewTexture;
    private string _previewLabel;
    private bool _livePreview = false;   // 실시간 미리보기 (값 변경 시에만 갱신)

    // 프리셋
    private CIconGeneratorPreset _preset;
    #endregion

    #region ─────────────────────────▶ 메뉴 진입점 ◀─────────────────────────
    [MenuItem("Tools/Create/아이템 아이콘 생성기")]
    private static void Open()
    {
        var window = GetWindow<CollectibleIconGenerator>("아이템 아이콘 생성기");
        window.minSize = new Vector2(380, 720);
    }

    private void OnDisable()
    {
        // 창이 닫힐 때 미리보기 텍스처 해제
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

        // 프리셋 에셋 선택 필드. 여기에 프리셋을 넣으면 해당 설정을 불러올 수 있습니다.
        EditorGUI.BeginChangeCheck();
        _preset = (CIconGeneratorPreset)EditorGUILayout.ObjectField(
            "현재 프리셋", _preset, typeof(CIconGeneratorPreset), false);
        // 새 프리셋을 필드에 끼우면 자동으로 불러옴
        if (EditorGUI.EndChangeCheck() && _preset != null)
        {
            LoadFromPreset(_preset);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            // 현재 필드의 프리셋에 덮어쓰기 저장
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

    // 현재 창의 값을 프리셋 에셋에 기록합니다.
    private void SaveToPreset(CIconGeneratorPreset preset)
    {
        if (preset == null) return;

        preset.resolutionIndex = _resolutionIndex;

        preset.pitch = _pitch;
        preset.yaw = _yaw;
        preset.padding = _padding;
        preset.orthographic = _orthographic;

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

    // 프리셋 에셋의 값을 현재 창으로 불러옵니다.
    private void LoadFromPreset(CIconGeneratorPreset preset)
    {
        if (preset == null) return;

        _resolutionIndex = Mathf.Clamp(preset.resolutionIndex, 0, _resolutions.Length - 1);

        _pitch = preset.pitch;
        _yaw = preset.yaw;
        _padding = preset.padding;
        _orthographic = preset.orthographic;

        _keyIntensity = preset.keyIntensity;
        _keyPitch = preset.keyPitch;
        _keyYaw = preset.keyYaw;
        _keyColor = preset.keyColor;

        _fillIntensity = preset.fillIntensity;
        _fillPitch = preset.fillPitch;
        _fillYaw = preset.fillYaw;
        _fillColor = preset.fillColor;

        _ambientColor = preset.ambientColor;

        // 실시간 모드면 불러온 값으로 즉시 미리보기 갱신
        if (_livePreview)
        {
            RefreshPreview();
        }
        Repaint();
    }

    // 현재 값을 새 프리셋 에셋 파일로 저장합니다. (저장 위치를 파일 대화상자로 지정)
    private void SaveAsNewPreset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "새 프리셋 저장",
            "IconPreset",
            "asset",
            "프리셋을 저장할 위치와 이름을 지정하세요.");

        if (string.IsNullOrEmpty(path)) return;  // 취소

        CIconGeneratorPreset newPreset = ScriptableObject.CreateInstance<CIconGeneratorPreset>();
        AssetDatabase.CreateAsset(newPreset, path);

        SaveToPreset(newPreset);            // 현재 값 기록
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _preset = newPreset;                // 방금 만든 프리셋을 현재 프리셋으로
        EditorGUIUtility.PingObject(newPreset);  // 프로젝트 창에서 위치 강조
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
        EditorGUI.BeginChangeCheck();  // 여기서부터 값 변경 감지 시작

        EditorGUILayout.LabelField("카메라 설정", EditorStyles.boldLabel);
        _pitch = EditorGUILayout.Slider("Pitch (상하 각도)", _pitch, -80f, 80f);
        _yaw = EditorGUILayout.Slider("Yaw (좌우 각도)", _yaw, -180f, 180f);
        _padding = EditorGUILayout.Slider("Padding (여유 배율)", _padding, 0.5f, 2.0f);
        _orthographic = EditorGUILayout.Toggle("직교(Orthographic) 카메라", _orthographic);

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

        bool settingsChanged = EditorGUI.EndChangeCheck();  // 카메라+조명 변경 감지 종료
        // 실시간 모드에서 값이 바뀌었으면 자동 갱신
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
            "아틀라스의 'Objects for Packing'에 위 저장 폴더가 등록되어 있다면\n" +
            "PNG 저장만으로 자동 포함됩니다. 즉시 미리보고 싶을 때만 아래 버튼을 사용하세요.",
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

    // 카메라 설정으로 대상 목록의 0번째를 한 장 렌더해 하단에 표시합니다. (파일 저장 없음)
    private void DrawPreview()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);

        _livePreview = EditorGUILayout.ToggleLeft(
            "실시간 미리보기 (카메라/조명 값 변경 시 자동 갱신)", _livePreview);

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

            // 창 너비에 맞춰 정사각형 영역 확보 (과도하게 커지지 않도록 상한 256)
            float size = Mathf.Min(EditorGUIUtility.currentViewWidth - 30f, 256f);
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));

            // 투명 영역이 잘 보이도록 체커보드 배경을 먼저 그린 뒤 텍스처를 겹쳐 그림
            EditorGUI.DrawTextureTransparent(rect, _previewTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUILayout.HelpBox("아직 미리보기가 없습니다. 위 버튼을 눌러 생성하세요.", MessageType.None);
        }
    }

    // 미리보기 전용: 대상 0번을 렌더해 _previewTexture에 저장합니다.
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

        // 이전 미리보기 텍스처 정리
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

        // 1. 객체 할당
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
            // 2. 가비지 릭을 완벽하게 방지하기 위한 강제 리소스 정리구조
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

    // 대상이 되는 CCollectibleSO 목록을 수집합니다.
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

    // 개별 SO 하나에 대한 아이콘 생성. 성공하면 true, 실패 사유는 error로 반환합니다.
    private bool TryGenerateOne(CCollectibleSO so, PreviewRenderUtility preview, int resolution, out string error)
    {
        if (!RenderToTexture(so, preview, resolution, out Texture2D texture, out error))
        {
            return false;
        }

        try
        {
            // 의존성 확장을 타지 않는 기본 string 유틸로 교체하여 컴파일 유연성 유지
            string idOrName = !string.IsNullOrWhiteSpace(so.Id) ? so.Id : so.name;
            string fileName = SanitizeFileName(idOrName);
            string assetPath = $"{_outputFolder}/{fileName}.png";

            File.WriteAllBytes(ToAbsolutePath(assetPath), texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            ApplySpriteImportSettings(assetPath, resolution);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                error = "Sprite 로드에 실패했습니다. (Import 설정을 확인하세요)";
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
            // 파일로 저장한 텍스처는 더 이상 필요 없으므로 해제
            if (texture != null) DestroyImmediate(texture);
        }
    }

    // 순수 렌더링: SO 프리팹을 촬영해 알파 포함 Texture2D를 반환합니다. (파일 저장 없음, 미리보기와 공유)
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
                error = "Renderer를 찾을 수 없습니다. (Mesh가 없는 프리팹)";
                return false;
            }

            // BeginStaticPreview로 매니저 초기화 보장
            preview.BeginStaticPreview(new Rect(0, 0, resolution, resolution));
            SetupLights(preview);
            PositionCamera(preview.camera, bounds, _pitch, _yaw, _padding, _orthographic);

            // 투명 배경 세팅 (Render 전에)
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = Color.clear;

            // preview.Render()가 조명 + 매니저를 정상 적용해 카메라의 targetTexture(내부 RT)에 그림
            preview.Render(true, true);

            {
                // BeginStaticPreview가 카메라에 붙여둔 내부 RT에서 직접 알파 포함 픽셀 추출
                RenderTexture internalRT = preview.camera.targetTexture;
                RenderTexture prevActive = RenderTexture.active;
                RenderTexture.active = internalRT;

                texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                texture.Apply();

                RenderTexture.active = prevActive;

                // 렌더링 컨텍스트 정리
                Texture2D discard = preview.EndStaticPreview();
                if (discard != null) DestroyImmediate(discard);
            }

            if (texture == null)
            {
                error = "미리보기 렌더링에 실패했습니다.";
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
                // 인스턴스를 소멸할 때 에셋 디펜던시까지 안전하게 분리 파괴 처리
                DestroyImmediate(instance, true);
            }
        }
    }
    #endregion

    #region ─────────────────────────▶ Bounds / 카메라 ◀─────────────────────────
    // 인스턴스에 포함된 모든 Renderer의 Bounds를 합산합니다.
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

    // Bounds 크기에 맞춰 카메라 위치, 거리, 클리핑 평면을 자동 계산합니다.
    private static void PositionCamera(
        Camera camera, Bounds bounds, float pitch, float yaw, float padding, bool orthographic)
    {
        // 외곽선 검은색 현상 차단
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

        camera.transform.position = bounds.center - forward * distance;
        camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
        camera.farClipPlane = distance + radius * 2f;
    }

    // 2점 조명(Key + Fill) 세팅. GUI에서 설정한 값을 사용합니다.
    private void SetupLights(PreviewRenderUtility preview)
    {
        // 주광
        preview.lights[0].intensity = _keyIntensity;
        preview.lights[0].transform.rotation = Quaternion.Euler(_keyPitch, _keyYaw, 0f);
        preview.lights[0].color = _keyColor;

        // 보조광
        preview.lights[1].intensity = _fillIntensity;
        preview.lights[1].transform.rotation = Quaternion.Euler(_fillPitch, _fillYaw, 0f);
        preview.lights[1].color = _fillColor;

        preview.ambientColor = _ambientColor;
    }
    #endregion

    #region ─────────────────────────▶ Sprite Import ◀─────────────────────────
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
    private static void EnsureFolderExists(string assetsRelativeFolder)
    {
        string absolute = ToAbsolutePath(assetsRelativeFolder);
        if (!Directory.Exists(absolute))
        {
            Directory.CreateDirectory(absolute);
            AssetDatabase.Refresh();
        }
    }

    // "Assets/..." 형태의 경로를 프로젝트 절대 경로로 변환합니다.
    private static string ToAbsolutePath(string assetsRelativePath)
    {
        string projectRoot = Application.dataPath.Substring(
            0, Application.dataPath.Length - "Assets".Length);
        return Path.Combine(projectRoot, assetsRelativePath);
    }

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
