using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// 프로젝트 창의 각 폴더가 그려질 때 개입하여 색상/아이콘을 덧그립니다.
/// 색상 상속 규칙: 색이 지정되지 않은 폴더는 트리를 거슬러 올라가
/// "처음 만나는, 색이 지정된 조상"의 색을 따릅니다. (CSS 상속과 동일)
/// 설정은 CFolderCustomizerSO 애셋에서 읽어오며, 이 애셋은 Git으로 공유됩니다.
/// </summary>
[InitializeOnLoad]
public static class CFolderCustomizerDrawer
{
    #region ─────────────────────────▶ 필드 ◀─────────────────────────
    // 여러 설정 SO를 Priority 내림차순으로 보관. [0]이 가장 높은 우선순위(전역 설정 대표).
    private static readonly List<CFolderCustomizerSO> _settings = new();
    private static bool _settingsLoaded = false;
    private static double _nextSettingScan = 0d;

    private static readonly Dictionary<string, Color> _resolvedColorCache = new();

    private const float ICON_TINT_ALPHA = 1f;
    #endregion

    #region ─────────────────────────▶ 초기화 ◀─────────────────────────
    static CFolderCustomizerDrawer()
    {
        EditorApplication.projectWindowItemOnGUI -= OnProjectItemGUI;
        EditorApplication.projectWindowItemOnGUI += OnProjectItemGUI;

        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.projectChanged += OnProjectChanged;
    }

    private static void OnProjectChanged() => Refresh();

    /// <summary>캐시를 비우고 프로젝트 창을 즉시 다시 그립니다. (설정 편집 후 호출)</summary>
    public static void Refresh()
    {
        _resolvedColorCache.Clear();
        InvalidateAllLookups();
        _settings.Clear();
        _settingsLoaded = false;
        _nextSettingScan = 0d;
        EditorApplication.RepaintProjectWindow();
    }

    /// <summary>슬라이더 드래그 등 매 프레임 갱신용 가벼운 리프레시. (애셋 재로드 안 함)</summary>
    public static void RefreshLive()
    {
        _resolvedColorCache.Clear();
        InvalidateAllLookups();
        EditorApplication.RepaintProjectWindow();
    }

    private static void InvalidateAllLookups()
    {
        for (int i = 0; i < _settings.Count; ++i)
        {
            if (_settings[i] != null) _settings[i].InvalidateLookup();
        }
    }
    #endregion

    #region ─────────────────────────▶ 설정 로드 ◀─────────────────────────
    // 프로젝트의 모든 CFolderCustomizerSO를 로드해 Priority 내림차순으로 정렬한다.
    private static bool EnsureSettingsLoaded()
    {
        if (_settingsLoaded) return _settings.Count > 0;
        if (EditorApplication.timeSinceStartup < _nextSettingScan) return _settings.Count > 0;
        _nextSettingScan = EditorApplication.timeSinceStartup + 2d;

        _settings.Clear();
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(CFolderCustomizerSO)}");
        for (int i = 0; i < guids.Length; ++i)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            CFolderCustomizerSO so = AssetDatabase.LoadAssetAtPath<CFolderCustomizerSO>(path);
            if (so != null) _settings.Add(so);
        }

        // Priority 내림차순 정렬. 같으면 순서 유지(비결정적이지만 중복 미지정 전제).
        _settings.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        _settingsLoaded = true;
        return _settings.Count > 0;
    }

    // 전역 표시 설정을 대표하는 SO. (Priority 최상위, 활성화된 것)
    private static CFolderCustomizerSO GetPrimarySetting()
    {
        for (int i = 0; i < _settings.Count; ++i)
        {
            if (_settings[i] != null && _settings[i].Enabled) return _settings[i];
        }
        return null;
    }
    #endregion

    #region ─────────────────────────▶ 그리기 ◀─────────────────────────
    private static void OnProjectItemGUI(string guid, Rect rect)
    {
        if (!EnsureSettingsLoaded()) return;
        if (string.IsNullOrEmpty(guid)) return;

        // 전역 표시 설정(색 대상·상속·아이콘 오프셋)은 Priority 최상위 SO의 것을 대표로 사용.
        CFolderCustomizerSO primary = GetPrimarySetting();
        if (primary == null) return;

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) return;

        // 아이콘 결정 (여러 SO를 Priority 순으로 조회, 상속 여부는 primary 설정을 따름)
        CFolderCustomizerSO.FolderEntry iconEntry =
            primary.InheritIcon ? ResolveIconEntryMulti(guid) : FindDirectMulti(guid);
        Texture2D icon = ResolveIconTexture(iconEntry);

        // 색 결정 (여러 SO를 Priority 순으로 조회 + 상속 규칙)
        Color color = ResolveColor(guid, primary);
        bool hasColor = color.a > 0f;

        if (!hasColor && icon == null) return;

        bool isListRow = rect.height <= 20f; // 리스트/트리 = 작은 아이콘, 그리드 = 큰 아이콘
        Rect iconRect = GetIconRect(rect, isListRow, primary);

        // 1) 커스텀 아이콘 그리기 (색이 있으면 틴트)
        if (icon != null)
        {
            Color prev = GUI.color;
            GUI.color = hasColor ? new Color(color.r, color.g, color.b, ICON_TINT_ALPHA) : Color.white;
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            GUI.color = prev;
        }
        // 2) 커스텀 아이콘이 없고 색만 있으면 → 기본 폴더 아이콘에 색 틴트
        else if (hasColor && ShouldTintIcon(primary))
        {
            Texture folderIcon = GetDefaultFolderIcon();
            if (folderIcon != null)
            {
                Color prev = GUI.color;
                GUI.color = new Color(color.r, color.g, color.b, ICON_TINT_ALPHA);
                GUI.DrawTexture(iconRect, folderIcon, ScaleMode.ScaleToFit);
                GUI.color = prev;
            }
        }

        // 3) 글자 색 틴트 (리스트/트리 행에서만; 선택된 행은 파란 배경과 충돌하므로 제외)
        if (hasColor && ShouldTintLabel(primary) && isListRow && !IsRowSelected(guid))
        {
            DrawLabelOverlay(rect, iconRect, path, color);
        }
    }
    #endregion

    #region ─────────────────────────▶ 다중 SO 조회 ◀─────────────────────────
    // 모든 SO를 Priority 순(이미 정렬됨)으로 훑어 직접 지정된 항목을 찾는다.
    private static CFolderCustomizerSO.FolderEntry FindDirectMulti(string guid)
    {
        for (int i = 0; i < _settings.Count; ++i)
        {
            CFolderCustomizerSO so = _settings[i];
            if (so == null || !so.Enabled) continue;
            CFolderCustomizerSO.FolderEntry entry = so.FindDirect(guid);
            if (entry != null) return entry;
        }
        return null;
    }

    // 색이 지정된 항목을 Priority 순으로 찾는다. (색 상속 계산에 사용)
    private static CFolderCustomizerSO.FolderEntry FindColorEntryMulti(string guid)
    {
        for (int i = 0; i < _settings.Count; ++i)
        {
            CFolderCustomizerSO so = _settings[i];
            if (so == null || !so.Enabled) continue;
            CFolderCustomizerSO.FolderEntry entry = so.FindDirect(guid);
            if (entry != null && entry.HasColor) return entry;
        }
        return null;
    }

    // 아이콘이 지정된 항목을 Priority 순으로, 부모까지 거슬러 찾는다.
    private static CFolderCustomizerSO.FolderEntry ResolveIconEntryMulti(string guid)
    {
        CFolderCustomizerSO.FolderEntry direct = FindDirectMulti(guid);
        if (direct != null && direct.HasIcon) return direct;

        string path = AssetDatabase.GUIDToAssetPath(guid);
        string parentPath = GetParentFolder(path);
        while (!string.IsNullOrEmpty(parentPath))
        {
            string parentGuid = AssetDatabase.AssetPathToGUID(parentPath);
            for (int i = 0; i < _settings.Count; ++i)
            {
                CFolderCustomizerSO so = _settings[i];
                if (so == null || !so.Enabled) continue;
                CFolderCustomizerSO.FolderEntry e = so.FindDirect(parentGuid);
                if (e != null && e.HasIcon) return e;
            }
            parentPath = GetParentFolder(parentPath);
        }
        return null;
    }
    #endregion

    #region ─────────────────────────▶ 색 상속 해석 ◀─────────────────────────
    // 색을 여러 SO에서 Priority 순으로 조회하고, 없으면 상속 규칙(부모 탐색)을 적용한다.
    private static Color ResolveColor(string guid, CFolderCustomizerSO primary)
    {
        if (_resolvedColorCache.TryGetValue(guid, out Color cached)) return cached;

        Color result = new(0f, 0f, 0f, 0f);
        CFolderCustomizerSO.FolderEntry direct = FindColorEntryMulti(guid);
        if (direct != null)
        {
            result = direct.color;
        }
        else if (primary.InheritColor)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string parentPath = GetParentFolder(path);
            while (!string.IsNullOrEmpty(parentPath))
            {
                string parentGuid = AssetDatabase.AssetPathToGUID(parentPath);
                CFolderCustomizerSO.FolderEntry parentEntry = FindColorEntryMulti(parentGuid);
                if (parentEntry != null)
                {
                    result = parentEntry.color;
                    break;
                }
                parentPath = GetParentFolder(parentPath);
            }
        }

        _resolvedColorCache[guid] = result;
        return result;
    }

    private static Texture2D ResolveIconTexture(CFolderCustomizerSO.FolderEntry entry)
    {
        if (entry == null || !entry.HasIcon) return null;
        if (entry.customIcon != null) return entry.customIcon;
        if (!string.IsNullOrEmpty(entry.builtinIconName))
        {
            GUIContent content = EditorGUIUtility.IconContent(entry.builtinIconName);
            if (content != null) return content.image as Texture2D;
        }
        return null;
    }
    #endregion

    #region ─────────────────────────▶ 헬퍼 ◀─────────────────────────
    private static bool ShouldTintIcon(CFolderCustomizerSO setting)
    {
        return setting.ColorTarget == CFolderCustomizerSO.EColorTarget.Icon
            || setting.ColorTarget == CFolderCustomizerSO.EColorTarget.Both;
    }

    private static bool ShouldTintLabel(CFolderCustomizerSO setting)
    {
        return setting.ColorTarget == CFolderCustomizerSO.EColorTarget.Label
            || setting.ColorTarget == CFolderCustomizerSO.EColorTarget.Both;
    }

    private static string GetParentFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "Assets") return null;
        int slash = path.LastIndexOf('/');
        return slash <= 0 ? null : path.Substring(0, slash);
    }

    private static bool IsRowSelected(string guid)
    {
        string[] selected = Selection.assetGUIDs;
        for (int i = 0; i < selected.Length; ++i)
        {
            if (selected[i] == guid) return true;
        }
        return false;
    }

    // 아이콘 사각형. 리스트/트리는 설정된 크기(기본 16px) + 오프셋으로 배치.
    // 오프셋을 조절하면 기본 폴더 아이콘 위 원하는 위치(예: 오른쪽 아래 배지)로 옮길 수 있다.
    private static Rect GetIconRect(Rect rect, bool isListRow, CFolderCustomizerSO setting)
    {
        if (isListRow)
        {
            float size = setting.IconSizeSmall;
            float y = rect.y + (rect.height - size) * 0.5f;
            return new Rect(
                rect.x + setting.IconOffsetX,
                y + setting.IconOffsetY,
                size, size);
        }
        // 그리드(큰 아이콘): 오프셋은 비율로 반영.
        float iconSize = rect.width;
        return new Rect(
            rect.x + setting.IconOffsetX,
            rect.y + setting.IconOffsetY,
            iconSize, iconSize);
    }

    private static Texture _defaultFolderIcon = null;
    private static Texture GetDefaultFolderIcon()
    {
        if (_defaultFolderIcon != null) return _defaultFolderIcon;
        GUIContent content = EditorGUIUtility.IconContent("Folder Icon");
        _defaultFolderIcon = content != null ? content.image : null;
        return _defaultFolderIcon;
    }

    // 폴더 이름 위에 색이 입혀진 라벨을 덧그립니다. (기본 라벨을 배경색으로 지우고 다시 그림)
    private static void DrawLabelOverlay(Rect rect, Rect iconRect, string path, Color color)
    {
        string folderName = System.IO.Path.GetFileName(path);

        Rect labelRect = new(
            iconRect.xMax + 2f,
            rect.y,
            rect.width - iconRect.width - 2f,
            rect.height);

        Color bg = EditorGUIUtility.isProSkin
            ? new Color(0.219f, 0.219f, 0.219f)
            : new Color(0.76f, 0.76f, 0.76f);
        EditorGUI.DrawRect(labelRect, bg);

        GUIStyle style = new(EditorStyles.label) { normal = { textColor = color } };
        EditorGUI.LabelField(labelRect, folderName, style);
    }
    #endregion
}
