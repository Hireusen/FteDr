#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// CCollectibleSO.Prefab(3D 프리팹)을 촬영하여 투명 배경 PNG 아이콘을 생성하고,
/// Sprite Import 설정 및 CCollectibleSO.SetIcon() 연결까지 자동으로 수행하는 에디터 도구입니다.
///
/// 생성된 PNG는 지정한 폴더에 저장됩니다. 해당 폴더가 Sprite Atlas의
/// "Objects for Packing"에 이미 등록되어 있다면 별도 코드 없이 자동으로 아틀라스에 포함됩니다.
///
/// 실제 씬을 전혀 건드리지 않기 위해 RenderTexture/Camera를 직접 씬에 만들지 않고,
/// 에디터 에셋 프리뷰(썸네일) 생성에 쓰이는 PreviewRenderUtility를 사용합니다.
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

    private UnityEngine.U2D.SpriteAtlas _atlasToRepack;

    private readonly List<string> _failLog = new();
    private Vector2 _failScroll;
    #endregion

    #region ─────────────────────────▶ 메뉴 진입점 ◀─────────────────────────
    [MenuItem("Tools/아이템 아이콘 생성기")]
    private static void Open()
    {
        var window = GetWindow<CollectibleIconGenerator>("아이템 아이콘 생성기");
        window.minSize = new Vector2(380, 520);
    }
    #endregion

    #region ─────────────────────────▶ GUI ◀─────────────────────────
    private void OnGUI()
    {
        EditorGUILayout.LabelField("출력 설정", EditorStyles.boldLabel);
        _outputFolder = EditorGUILayout.TextField("아이콘 저장 폴더", _outputFolder);
        _resolutionIndex = EditorGUILayout.Popup("해상도", _resolutionIndex, _resolutionLabels);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("카메라 설정", EditorStyles.boldLabel);
        _pitch = EditorGUILayout.Slider("Pitch (상하 각도)", _pitch, -80f, 80f);
        _yaw = EditorGUILayout.Slider("Yaw (좌우 각도)", _yaw, -180f, 180f);
        _padding = EditorGUILayout.Slider("Padding (여유 배율)", _padding, 1.0f, 2.0f);
        _orthographic = EditorGUILayout.Toggle("직교(Orthographic) 카메라", _orthographic);

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

        DrawFailLog();
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
        error = string.Empty;

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

            // 개별 렌더 버퍼 초기화 및 조명 세팅 
            preview.BeginStaticPreview(new Rect(0, 0, resolution, resolution));
            SetupLights(preview);

            PositionCamera(preview.camera, bounds, _pitch, _yaw, _padding, _orthographic);

            preview.Render();
            Texture2D texture = preview.EndStaticPreview();

            if (texture == null)
            {
                error = "미리보기 렌더링에 실패했습니다.";
                return false;
            }

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

    // 2점 조명(Key + Fill) 세팅. 그림자로 인한 아이콘 형태 왜곡을 줄입니다.
    private static void SetupLights(PreviewRenderUtility preview)
    {
        preview.lights[0].intensity = 1.4f;
        preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
        preview.lights[1].intensity = 1.0f;
        preview.lights[1].transform.rotation = Quaternion.Euler(40f, 220f, 0f);
        preview.ambientColor = new Color(0.15f, 0.15f, 0.15f, 0f);
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
