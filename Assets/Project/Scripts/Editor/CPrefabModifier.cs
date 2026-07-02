using UnityEngine;
using UnityEditor;
using System;

/// <summary>
/// 선택된 오브젝트(프리팹 포함)의 자식을 탐색하여 이름 변경 및 컴포넌트 일괄 추가를 수행합니다.
/// 유니티 내장 컴포넌트와 커스텀 스크립트를 모두 지원합니다.
/// </summary>
public class CPrefabModifier : EditorWindow
{
    #region ─────────────────────────▶ 설정 값 ◀─────────────────────────
    private string _targetName = "default";
    private string _newName = "NewName";

    // 추가할 컴포넌트 설정
    private bool _addIsBuiltIn = true;
    private string _addBuiltInName = "BoxCollider";
    private MonoScript _addCustomScript;

    // 조건(찾을) 컴포넌트 설정
    private bool _targetIsBuiltIn = true;
    private string _targetBuiltInName = "MeshRenderer";
    private MonoScript _targetCustomScript;
    #endregion

    #region ─────────────────────────▶ 윈도우 ◀─────────────────────────
    [MenuItem("Tools/프리팹 일괄 수정 도구")]
    public static void ShowWindow()
    {
        CPrefabModifier window = GetWindow<CPrefabModifier>("프리팹 일괄 수정");
        window.minSize = new Vector2(380f, 450f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("선택된 루트 오브젝트 수: " + Selection.gameObjects.Length, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. 이름 변경 기능
        EditorGUILayout.LabelField("▶ 1. 자식 이름 일괄 변경", EditorStyles.boldLabel);
        _targetName = EditorGUILayout.TextField("찾을 이름", _targetName);
        _newName = EditorGUILayout.TextField("변경할 이름", _newName);
        if (GUILayout.Button("선택한 대상의 자식 이름 변경", GUILayout.Height(25)))
        {
            ExecuteAction(ModifyName);
        }
        EditorGUILayout.Space(15);

        // 2. 추가할 컴포넌트 설정
        EditorGUILayout.LabelField("▶ [설정] 추가할 컴포넌트", EditorStyles.boldLabel);
        _addIsBuiltIn = EditorGUILayout.Toggle("유니티 기본 컴포넌트 사용", _addIsBuiltIn);
        if (_addIsBuiltIn)
            _addBuiltInName = EditorGUILayout.TextField("컴포넌트 이름 (예: Rigidbody)", _addBuiltInName);
        else
            _addCustomScript = (MonoScript)EditorGUILayout.ObjectField("커스텀 스크립트", _addCustomScript, typeof(MonoScript), false);
        EditorGUILayout.Space();

        // 3. 찾을 조건 컴포넌트 설정
        EditorGUILayout.LabelField("▶ [설정] 탐색 조건 컴포넌트", EditorStyles.boldLabel);
        _targetIsBuiltIn = EditorGUILayout.Toggle("유니티 기본 컴포넌트 사용", _targetIsBuiltIn);
        if (_targetIsBuiltIn)
            _targetBuiltInName = EditorGUILayout.TextField("컴포넌트 이름 (예: MeshRenderer)", _targetBuiltInName);
        else
            _targetCustomScript = (MonoScript)EditorGUILayout.ObjectField("커스텀 스크립트", _targetCustomScript, typeof(MonoScript), false);
        EditorGUILayout.Space(15);

        // 실행 버튼
        EditorGUILayout.LabelField("▶ 실행", EditorStyles.boldLabel);
        if (GUILayout.Button($"'{_targetName}' 이름을 가진 자식에 컴포넌트 추가", GUILayout.Height(25)))
        {
            ExecuteAction(AddComponentByName);
        }
        if (GUILayout.Button("특정 컴포넌트를 가진 자식에 새 컴포넌트 추가", GUILayout.Height(25)))
        {
            ExecuteAction(AddComponentByComponent);
        }
    }
    #endregion

    #region ─────────────────────────▶ 타입 리플렉션 ◀─────────────────────────
    // 문자열 또는 MonoScript로부터 실제 Type을 추출합니다.
    private Type GetResolvedType(bool isBuiltIn, string className, MonoScript script)
    {
        if (!isBuiltIn)
        {
            return script != null ? script.GetClass() : null;
        }

        // 유니티 기본 어셈블리 내에서 타입 탐색
        string[] namespaces = { "UnityEngine.", "UnityEngine.UI.", "UnityEngine.AI.", "" };
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var ns in namespaces)
            {
                Type type = assembly.GetType(ns + className);
                if (type != null) return type;
            }
        }
        return null;
    }
    #endregion

    #region ─────────────────────────▶ 실행 코어 ◀─────────────────────────
    private void ExecuteAction(Action<GameObject> modifyLogic)
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("선택된 오브젝트가 없습니다.");
            return;
        }

        int successCount = 0;
        int length = selectedObjects.Length;

        for (int i = 0; i < length; ++i)
        {
            GameObject obj = selectedObjects[i];
            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (!string.IsNullOrEmpty(assetPath) && PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
                modifyLogic(prefabContents);
                PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);
                successCount++;
            }
            else
            {
                Undo.RecordObject(obj, "Modify GameObject");
                modifyLogic(obj);
                EditorUtility.SetDirty(obj);
                successCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"작업 완료: 총 {successCount}개의 루트 오브젝트를 갱신했습니다.");
    }
    #endregion

    #region ─────────────────────────▶ 조작 로직 ◀─────────────────────────
    private void ModifyName(GameObject root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        int length = children.Length;
        for (int i = 0; i < length; ++i)
        {
            if (children[i].name == _targetName)
                children[i].name = _newName;
        }
    }

    private void AddComponentByName(GameObject root)
    {
        Type addType = GetResolvedType(_addIsBuiltIn, _addBuiltInName, _addCustomScript);
        if (addType == null)
        {
            Debug.LogError("추가할 컴포넌트의 타입을 찾을 수 없습니다. 이름을 확인해주세요.");
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        int length = children.Length;
        for (int i = 0; i < length; ++i)
        {
            if (children[i].name == _targetName)
            {
                if (children[i].GetComponent(addType) == null)
                    children[i].gameObject.AddComponent(addType);
            }
        }
    }

    private void AddComponentByComponent(GameObject root)
    {
        Type targetType = GetResolvedType(_targetIsBuiltIn, _targetBuiltInName, _targetCustomScript);
        Type addType = GetResolvedType(_addIsBuiltIn, _addBuiltInName, _addCustomScript);

        if (targetType == null || addType == null)
        {
            Debug.LogError("컴포넌트의 타입을 찾을 수 없습니다. 설정 값을 확인해주세요.");
            return;
        }

        Component[] components = root.GetComponentsInChildren(targetType, true);
        int length = components.Length;
        for (int i = 0; i < length; ++i)
        {
            if (components[i].GetComponent(addType) == null)
                components[i].gameObject.AddComponent(addType);
        }
    }
    #endregion
}
