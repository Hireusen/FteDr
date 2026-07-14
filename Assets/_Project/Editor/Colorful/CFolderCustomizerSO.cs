using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프로젝트 창의 폴더에 색상/아이콘을 입히기 위한 설정 데이터입니다.
/// 이 애셋은 Assets 하위에 저장되어 Git으로 팀에 공유됩니다. (개인 설정 아님)
/// 색상은 알파(a)가 0이면 "미지정"으로 간주되어 부모 폴더의 색을 상속합니다.
/// </summary>
[CreateAssetMenu(fileName = "FolderCustomizer", menuName = "Editor/Folder Customizer Setting")]
public class CFolderCustomizerSO : ScriptableObject
{
    #region ─────────────────────────▶ 타입 정의 ◀─────────────────────────
    /// <summary>색상을 어디에 적용할지 결정합니다. (팀 공유 설정)</summary>
    public enum EColorTarget
    {
        Icon,       // 폴더 아이콘만 틴트
        Label,      // 폴더 이름(글자)만 틴트
        Both,       // 아이콘 + 글자 모두
    }

    /// <summary>폴더 하나에 대한 커스터마이즈 항목입니다. GUID로 폴더를 식별합니다.</summary>
    [System.Serializable]
    public class FolderEntry
    {
        [Tooltip("폴더의 GUID. 폴더를 옮기거나 이름을 바꿔도 유지됩니다.")]
        public string guid = null;

        [Tooltip("에디터에서 알아보기 쉽도록 저장 시점의 경로를 함께 기록합니다. (식별에는 사용 안 함)")]
        public string cachedPath = null;

        [Tooltip("알파(a)가 0이면 미지정으로 간주되어 부모 색을 상속합니다.")]
        public Color color = new(0f, 0f, 0f, 0f);

        [Tooltip("커스텀 아이콘. 비어 있으면 내장 아이콘 이름을 사용합니다.")]
        public Texture2D customIcon = null;

        [Tooltip("customIcon이 비었을 때 사용할 Unity 내장 아이콘 이름. (선택)")]
        public string builtinIconName = null;

        /// <summary>색이 실제로 지정되었는지 여부. (알파 0 = 미지정)</summary>
        public bool HasColor => color.a > 0f;

        /// <summary>아이콘이 실제로 지정되었는지 여부.</summary>
        public bool HasIcon => customIcon != null || !string.IsNullOrEmpty(builtinIconName);
    }
    #endregion

    #region ─────────────────────────▶ 인스펙터 ◀─────────────────────────
    [Header("전역 표시 설정 (팀 공유)")]
    [SerializeField] private bool _enabled = true;

    [Tooltip("여러 설정 SO가 있을 때, 같은 폴더에 규칙이 겹치면 Priority가 높은 SO가 이깁니다.\n" +
             "전역 표시 설정(색 대상·상속·아이콘 오프셋)도 Priority가 가장 높은 SO의 것을 대표로 사용합니다.\n" +
             "Priority가 같으면 스캔 순서(비결정적)를 따릅니다. — 중복 지정을 피하세요.")]
    [SerializeField] private int _priority = 0;

    [SerializeField] private EColorTarget _colorTarget = EColorTarget.Both;

    [Tooltip("색을 지정하지 않은 폴더가 부모 폴더의 색을 상속할지 여부입니다.")]
    [SerializeField] private bool _inheritColor = true;

    [Tooltip("아이콘 상속 여부. 보통 꺼두는 것을 권장합니다. (부모 아이콘이 자식에 번지는 것 방지)")]
    [SerializeField] private bool _inheritIcon = false;

    [Header("아이콘 위치/크기 (기본 폴더 아이콘 위에 어떻게 얹을지)")]
    [Tooltip("리스트/트리(작은 아이콘) 모드에서 아이콘 크기(px). 기본 16.")]
    [Range(4f, 24f)]
    [SerializeField] private float _iconSizeSmall = 16f;

    [Tooltip("아이콘 가로 오프셋(px). 양수는 오른쪽으로.")]
    [SerializeField] private float _iconOffsetX = 0f;

    [Tooltip("아이콘 세로 오프셋(px). 양수는 아래로.")]
    [SerializeField] private float _iconOffsetY = 0f;

    [Header("폴더별 설정")]
    [SerializeField] private List<FolderEntry> _entries = new();
    #endregion

    #region ─────────────────────────▶ 공개 멤버 ◀─────────────────────────
    public bool Enabled => _enabled;
    public int Priority => _priority;
    public EColorTarget ColorTarget => _colorTarget;
    public bool InheritColor => _inheritColor;
    public bool InheritIcon => _inheritIcon;
    public float IconSizeSmall => _iconSizeSmall;
    public float IconOffsetX => _iconOffsetX;
    public float IconOffsetY => _iconOffsetY;
    public IReadOnlyList<FolderEntry> Entries => _entries;
    #endregion

    #region ─────────────────────────▶ 조회 (GUID → 항목 캐시) ◀─────────────────────────
    private Dictionary<string, FolderEntry> _lookup = null;

    /// <summary>GUID로 직접 지정된 항목을 찾습니다. 없으면 null.</summary>
    public FolderEntry FindDirect(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;
        RebuildLookupIfNeeded();
        return _lookup.TryGetValue(guid, out FolderEntry entry) ? entry : null;
    }

    /// <summary>캐시를 강제로 무효화합니다. (인스펙터 편집 후 호출)</summary>
    public void InvalidateLookup()
    {
        _lookup = null;
    }

    private void RebuildLookupIfNeeded()
    {
        if (_lookup != null) return;
        _lookup = new Dictionary<string, FolderEntry>(_entries.Count);
        int length = _entries.Count;
        for (int i = 0; i < length; ++i)
        {
            FolderEntry entry = _entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.guid)) continue;
            _lookup[entry.guid] = entry;
        }
    }
    #endregion

    #region ─────────────────────────▶ 메시지 함수 ◀─────────────────────────
    private void OnValidate()
    {
        InvalidateLookup();
    }
    #endregion
}
