using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 선택한 프리팹들을 읽어 각각에 대응하는 CCollectibleSO 에셋을 생성하는 에디터 유틸리티입니다.
/// </summary>
public class CCollectibleSOGenerator : EditorWindow
{
    #region ─────────────────────────▶ 설정 값 ◀─────────────────────────
    private const string ID_PREFIX = "Collectible_";

    private string _saveFolder = "Assets/Resources/ScriptableObjects/Collectible/Normal";
    private bool _connectToPrefab = true;
    private bool _skipIfExists = true;
    #endregion

    #region ─────────────────────────▶ 윈도우 ◀─────────────────────────
    [MenuItem("Tools/Create/수집품 SO 생성기")]
    public static void ShowWindow()
    {
        CCollectibleSOGenerator window = GetWindow<CCollectibleSOGenerator>("수집품 SO 생성기");
        window.minSize = new Vector2(360f, 200f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "프로젝트 창에서 프리팹을 선택한 뒤 실행하세요.\n" +
            "각 프리팹마다 CCollectibleSO를 생성하고 자동 연결합니다.",
            MessageType.Info);
        EditorGUILayout.Space();

        _saveFolder = EditorGUILayout.TextField("저장 폴더", _saveFolder);
        _connectToPrefab = EditorGUILayout.Toggle("프리팹에 자동 연결", _connectToPrefab);
        _skipIfExists = EditorGUILayout.Toggle("이미 있으면 건너뛰기", _skipIfExists);
        EditorGUILayout.Space();

        if (GUILayout.Button("선택한 프리팹으로 SO 생성", GUILayout.Height(30)))
        {
            Generate();
        }
    }
    #endregion

    #region ─────────────────────────▶ 실행 ◀─────────────────────────
    private void Generate()
    {
        // 프로젝트 창에서 선택된 프리팹만 추출
        GameObject[] prefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets);

        if (prefabs.Length == 0)
        {
            UDebug.Print("선택한 프리팹이 없습니다.", LogType.Warning);
            return;
        }

        EnsureFolderExists(_saveFolder);

        int created = 0;
        int skipped = 0;
        int connected = 0;
        int length = prefabs.Length;

        for (int i = 0; i < length; ++i)
        {
            GameObject prefab = prefabs[i];

            // 프리팹 에셋인지 확인 (씬 오브젝트 방지)
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                continue;
            }

            string id = ID_PREFIX + prefab.name.Replace(" ", "_");
            string assetPath = $"{_saveFolder}/{id}.asset";

            // 중복 처리
            if (_skipIfExists && File.Exists(assetPath))
            {
                ++skipped;
                continue;
            }

            // SO 생성 및 값 설정
            CCollectibleSO so = CreateOrLoadSO(assetPath);
            SetupSO(so, id, prefab);

            // 프리팹의 CCollectible._data에 연결
            if (_connectToPrefab && ConnectToPrefab(prefabPath, so))
            {
                ++connected;
            }

            ++created;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UDebug.Print($"수집품 SO 생성 완료: 생성 {created}개, 건너뜀 {skipped}개, 프리팹 연결 {connected}개.");
    }
    #endregion

    #region ─────────────────────────▶ 내부 메서드 ◀─────────────────────────
    // 저장 폴더가 없으면 생성한다. (중첩 경로 대응)
    private void EnsureFolderExists(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string[] parts = folder.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; ++i)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    // 경로에 SO가 이미 있으면 로드, 없으면 새로 생성한다.
    private CCollectibleSO CreateOrLoadSO(string assetPath)
    {
        CCollectibleSO existing = AssetDatabase.LoadAssetAtPath<CCollectibleSO>(assetPath);
        if (existing != null)
        {
            return existing;
        }

        CCollectibleSO so = ScriptableObject.CreateInstance<CCollectibleSO>();
        AssetDatabase.CreateAsset(so, assetPath);
        return so;
    }

    // SO의 _id / _type / _prefab을 SerializedObject로 설정한다. (protected 필드 접근)
    private void SetupSO(CCollectibleSO so, string id, GameObject prefab)
    {
        SerializedObject sObj = new SerializedObject(so);
        sObj.FindProperty("_id").stringValue = id;
        sObj.FindProperty("_type").enumValueIndex = GetEnumIndex(EDataType.Collectible);
        sObj.FindProperty("_prefab").objectReferenceValue = prefab;
        sObj.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(so);
    }

    // 프리팹 루트의 CCollectible._data에 SO를 연결한다.
    private bool ConnectToPrefab(string prefabPath, CCollectibleSO so)
    {
        // 프리팹 에셋을 편집용으로 로드
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            return false;
        }

        bool result = false;
        if (root.TryGetComponent(out CCollectible collectible))
        {
            SerializedObject sObj = new SerializedObject(collectible);
            sObj.FindProperty("_data").objectReferenceValue = so;
            sObj.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            result = true;
        }
        else
        {
            UDebug.Print($"{root.name}: 루트에 CCollectible이 없어 연결을 건너뜁니다.", LogType.Warning);
        }

        PrefabUtility.UnloadPrefabContents(root);
        return result;
    }

    // enum 값을 SerializedProperty.enumValueIndex에 넣을 인덱스로 변환한다.
    private int GetEnumIndex(EDataType value)
    {
        // enumValueIndex는 "정의 순서상 인덱스"이지 enum의 정수값이 아니므로 직접 계산.
        string[] names = System.Enum.GetNames(typeof(EDataType));
        string target = value.ToString();
        for (int i = 0; i < names.Length; ++i)
        {
            if (names[i] == target)
            {
                return i;
            }
        }
        return 0;
    }
    #endregion
}
