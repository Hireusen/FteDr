using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/// <summary>
/// 선택된 오브젝트(프리팹 포함)들을 일괄 수정하는 에디터 도구입니다.
/// </summary>
public class CPrefabModifier : EditorWindow
{
    #region ─────────────────────────▶ 열거형 ◀─────────────────────────
    // 탐색 범위
    private enum EScanScope
    {
        RootOnly = 0,   // 루트만
        ChildrenOnly,   // 자식만 (루트 제외)
        Both,           // 루트 + 자식
    }

    // 수행할 작업
    private enum EOperation
    {
        AddComponent = 0,    // 컴포넌트 추가
        RemoveComponent,     // 컴포넌트 제거
        RenameObject,        // 이름 변경
        SetProperty,         // 컴포넌트 속성 하나 설정
    }
    #endregion

    #region ─────────────────────────▶ 설정 값 ◀─────────────────────────
    // 1단계: 대상 선정
    private EScanScope _scope = EScanScope.Both;

    private bool _filterByName = false;
    private string _targetName = "Visual";

    private bool _filterByComponent = false;
    private bool _filterCompIsBuiltIn = true;
    private string _filterCompBuiltInName = "MeshRenderer";
    private MonoScript _filterCompScript;

    // 2단계: 작업 선택
    private EOperation _operation = EOperation.AddComponent;

    // 작업 대상 컴포넌트(추가/제거 공용)
    private bool _opCompIsBuiltIn = true;
    private string _opCompBuiltInName = "BoxCollider";
    private MonoScript _opCompScript;

    // 이름 변경용
    private string _newName = "NewName";

    // 속성 설정용
    private string[] _propNames = new string[0];   // 새로고침으로 채우는 편집 가능 속성 목록
    private int _selectedPropIndex = 0;             // 선택된 속성 인덱스
    private SerializedPropertyType _selectedPropType; // 선택된 속성의 타입
    // 타입별 입력 값 (선택된 속성 타입에 맞는 것만 사용)
    private bool _valBool;
    private int _valInt;
    private float _valFloat;
    private string _valString = "";
    private Vector3 _valVector3;
    private Color _valColor = Color.white;
    private int _valEnumIndex;
    private string[] _valEnumNames = new string[0];
    private UnityEngine.Object _valObject;
    #endregion

    #region ─────────────────────────▶ 윈도우 ◀─────────────────────────
    [MenuItem("Tools/Worker/프리팹 일괄 수정 도구")]
    public static void ShowWindow()
    {
        CPrefabModifier window = GetWindow<CPrefabModifier>("프리팹 일괄 수정");
        window.minSize = new Vector2(400f, 520f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField($"선택된 루트 오브젝트 수: {Selection.gameObjects.Length}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawSelectionSection();
        EditorGUILayout.Space(12);
        DrawOperationSection();
        EditorGUILayout.Space(12);
        DrawExecuteSection();
    }

    // ── 1단계 UI ──────────────────────────────────────────────
    private void DrawSelectionSection()
    {
        EditorGUILayout.LabelField("▶ 1단계. 대상 선정", EditorStyles.boldLabel);
        _scope = (EScanScope)EditorGUILayout.EnumPopup("탐색 범위", _scope);

        EditorGUILayout.LabelField("선택 조건 (OR · 아무것도 안 켜면 전체)");

        _filterByName = EditorGUILayout.Toggle("이름으로 필터", _filterByName);
        if (_filterByName)
        {
            _targetName = EditorGUILayout.TextField("일치할 이름", _targetName);
        }

        _filterByComponent = EditorGUILayout.Toggle("컴포넌트 보유로 필터", _filterByComponent);
        if (_filterByComponent)
        {
            _filterCompIsBuiltIn = EditorGUILayout.Toggle("  기본 컴포넌트 사용", _filterCompIsBuiltIn);
            if (_filterCompIsBuiltIn)
            {
                _filterCompBuiltInName = EditorGUILayout.TextField("  컴포넌트 이름", _filterCompBuiltInName);
            }
            else
            {
                _filterCompScript = (MonoScript)EditorGUILayout.ObjectField("  커스텀 스크립트", _filterCompScript, typeof(MonoScript), false);
            }
        }
    }

    // ── 2단계 UI ──────────────────────────────────────────────
    private void DrawOperationSection()
    {
        EditorGUILayout.LabelField("▶ 2단계. 작업 선택", EditorStyles.boldLabel);
        _operation = (EOperation)EditorGUILayout.EnumPopup("작업", _operation);

        switch (_operation)
        {
            case EOperation.AddComponent:
            case EOperation.RemoveComponent:
                _opCompIsBuiltIn = EditorGUILayout.Toggle("기본 컴포넌트 사용", _opCompIsBuiltIn);
                if (_opCompIsBuiltIn)
                {
                    _opCompBuiltInName = EditorGUILayout.TextField("컴포넌트 이름", _opCompBuiltInName);
                }
                else
                {
                    _opCompScript = (MonoScript)EditorGUILayout.ObjectField("커스텀 스크립트", _opCompScript, typeof(MonoScript), false);
                }
                break;

            case EOperation.RenameObject:
                _newName = EditorGUILayout.TextField("변경할 이름", _newName);
                break;

            case EOperation.SetProperty:
                DrawSetPropertyUI();
                break;
        }
    }

    // 속성 설정 작업의 UI: 컴포넌트 지정 → 속성 목록 새로고침 → 속성 선택 → 값 입력
    private void DrawSetPropertyUI()
    {
        _opCompIsBuiltIn = EditorGUILayout.Toggle("기본 컴포넌트 사용", _opCompIsBuiltIn);
        if (_opCompIsBuiltIn)
        {
            _opCompBuiltInName = EditorGUILayout.TextField("컴포넌트 이름", _opCompBuiltInName);
        }
        else
        {
            _opCompScript = (MonoScript)EditorGUILayout.ObjectField("커스텀 스크립트", _opCompScript, typeof(MonoScript), false);
        }

        if (GUILayout.Button("이 컴포넌트의 속성 목록 불러오기"))
        {
            RefreshPropertyList();
        }

        if (_propNames.Length == 0)
        {
            EditorGUILayout.HelpBox("선택된 프리팹 중 하나에서 위 컴포넌트를 찾아 속성 목록을 불러옵니다.", MessageType.Info);
            return;
        }

        int newIndex = EditorGUILayout.Popup("설정할 속성", _selectedPropIndex, _propNames);
        if (newIndex != _selectedPropIndex)
        {
            _selectedPropIndex = newIndex;
            RefreshSelectedPropertyMeta();
        }

        DrawValueFieldByType();
    }

    // 선택된 속성 타입에 맞는 값 입력 UI를 그린다.
    private void DrawValueFieldByType()
    {
        switch (_selectedPropType)
        {
            case SerializedPropertyType.Boolean:
                _valBool = EditorGUILayout.Toggle("값", _valBool);
                break;
            case SerializedPropertyType.Integer:
                _valInt = EditorGUILayout.IntField("값", _valInt);
                break;
            case SerializedPropertyType.Float:
                _valFloat = EditorGUILayout.FloatField("값", _valFloat);
                break;
            case SerializedPropertyType.String:
                _valString = EditorGUILayout.TextField("값", _valString);
                break;
            case SerializedPropertyType.Vector3:
                _valVector3 = EditorGUILayout.Vector3Field("값", _valVector3);
                break;
            case SerializedPropertyType.Color:
                _valColor = EditorGUILayout.ColorField("값", _valColor);
                break;
            case SerializedPropertyType.Enum:
                _valEnumIndex = EditorGUILayout.Popup("값", _valEnumIndex, _valEnumNames);
                break;
            case SerializedPropertyType.ObjectReference:
                _valObject = EditorGUILayout.ObjectField("값", _valObject, typeof(UnityEngine.Object), false);
                break;
            default:
                EditorGUILayout.HelpBox($"이 속성 타입({_selectedPropType})은 아직 지원하지 않습니다.", MessageType.Warning);
                break;
        }
    }

    // ── 3단계 UI ──────────────────────────────────────────────
    private void DrawExecuteSection()
    {
        EditorGUILayout.LabelField("▶ 3단계. 실행", EditorStyles.boldLabel);
        if (GUILayout.Button("선택한 오브젝트에 작업 실행", GUILayout.Height(30)))
        {
            Execute();
        }
    }
    #endregion

    #region ─────────────────────────▶ 실행 코어 ◀─────────────────────────
    private void Execute()
    {
        GameObject[] roots = Selection.gameObjects;
        if (roots.Length == 0)
        {
            Debug.LogWarning("[CPrefabModifier.cs] 선택된 오브젝트가 없습니다.");
            return;
        }

        // 작업에 필요한 타입을 미리 해석 (실패 시 조기 중단)
        if (!TryResolveOperationType(out Type opType, out Type filterType))
        {
            return;
        }

        int rootCount = 0;
        int affectedCount = 0;
        int length = roots.Length;

        for (int i = 0; i < length; ++i)
        {
            GameObject obj = roots[i];
            string assetPath = AssetDatabase.GetAssetPath(obj);
            bool isPrefabAsset = !string.IsNullOrEmpty(assetPath) && PrefabUtility.IsPartOfPrefabAsset(obj);

            if (isPrefabAsset)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
                affectedCount += ApplyToRoot(contents, opType, filterType);
                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
                PrefabUtility.UnloadPrefabContents(contents);
            }
            else
            {
                Undo.RegisterFullObjectHierarchyUndo(obj, "Prefab Modify");
                affectedCount += ApplyToRoot(obj, opType, filterType);
                EditorUtility.SetDirty(obj);
            }
            ++rootCount;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CPrefabModifier.cs] 작업 완료: 루트 {rootCount}개, 대상 {affectedCount}개에 적용했습니다.");
    }

    // 한 루트에 대해 대상을 추리고 작업을 적용한다. 적용된 대상 수를 반환.
    private int ApplyToRoot(GameObject root, Type opType, Type filterType)
    {
        List<Transform> candidates = CollectByScope(root);
        int applied = 0;

        for (int i = 0; i < candidates.Count; ++i)
        {
            Transform t = candidates[i];
            if (t == null) continue;
            if (!IsSelected(t, filterType)) continue;

            if (ApplyOperation(t.gameObject, opType))
            {
                ++applied;
            }
        }
        return applied;
    }
    #endregion

    #region ─────────────────────────▶ 1단계: 대상 선정 ◀─────────────────────────
    // 탐색 범위에 따라 후보 Transform을 모은다.
    private List<Transform> CollectByScope(GameObject root)
    {
        List<Transform> list = new List<Transform>();

        switch (_scope)
        {
            case EScanScope.RootOnly:
                list.Add(root.transform);
                break;

            case EScanScope.ChildrenOnly:
                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; ++i)
                {
                    if (all[i] != root.transform) list.Add(all[i]);
                }
                break;

            case EScanScope.Both:
            default:
                list.AddRange(root.GetComponentsInChildren<Transform>(true));
                break;
        }
        return list;
    }

    // 선택 조건(OR)을 통과하는지 검사한다. 조건을 하나도 안 켰으면 무조건 통과.
    private bool IsSelected(Transform t, Type filterType)
    {
        bool anyFilter = _filterByName || _filterByComponent;
        if (!anyFilter) return true;

        if (_filterByName && t.name == _targetName)
        {
            return true;
        }
        if (_filterByComponent && filterType != null && t.GetComponent(filterType) != null)
        {
            return true;
        }
        return false;
    }
    #endregion

    #region ─────────────────────────▶ 2단계: 작업 적용 ◀─────────────────────────
    // 작업/필터에 필요한 Type을 해석한다. 실패 시 false.
    private bool TryResolveOperationType(out Type opType, out Type filterType)
    {
        opType = null;
        filterType = null;

        // 필터 컴포넌트 타입 (컴포넌트 필터를 켠 경우에만 필요)
        if (_filterByComponent)
        {
            filterType = GetResolvedType(_filterCompIsBuiltIn, _filterCompBuiltInName, _filterCompScript);
            if (filterType == null)
            {
                Debug.LogError("[CPrefabModifier.cs] 탐색 조건 컴포넌트의 타입을 찾을 수 없습니다.");
                return false;
            }
        }

        // 작업 컴포넌트 타입 (추가/제거/속성설정인 경우 필요)
        if (_operation == EOperation.AddComponent
            || _operation == EOperation.RemoveComponent
            || _operation == EOperation.SetProperty)
        {
            opType = GetResolvedType(_opCompIsBuiltIn, _opCompBuiltInName, _opCompScript);
            if (opType == null)
            {
                Debug.LogError("[CPrefabModifier.cs] 작업 컴포넌트의 타입을 찾을 수 없습니다.");
                return false;
            }
        }

        return true;
    }

    // 실제 작업 하나를 대상 오브젝트에 적용한다. 변경이 일어나면 true.
    private bool ApplyOperation(GameObject go, Type opType)
    {
        switch (_operation)
        {
            case EOperation.AddComponent:
                if (go.GetComponent(opType) != null) return false; // 중복 방지
                go.AddComponent(opType);
                return true;

            case EOperation.RemoveComponent:
                Component[] comps = go.GetComponents(opType);
                if (comps.Length == 0) return false;
                for (int i = 0; i < comps.Length; ++i)
                {
                    if (comps[i] == null) continue;
                    DestroyImmediate(comps[i], true); // 에셋 편집 중이므로 즉시 파괴 허용
                }
                return true;

            case EOperation.RenameObject:
                if (go.name == _newName) return false;
                go.name = _newName;
                return true;

            case EOperation.SetProperty:
                return ApplySetProperty(go, opType);
        }
        return false;
    }

    // 대상 오브젝트의 지정 컴포넌트에서 선택된 속성 하나만 설정한다.
    private bool ApplySetProperty(GameObject go, Type opType)
    {
        Component comp = go.GetComponent(opType);
        if (comp == null) return false;
        if (_propNames.Length == 0) return false;

        SerializedObject sObj = new SerializedObject(comp);
        SerializedProperty prop = sObj.FindProperty(_propNames[_selectedPropIndex]);
        if (prop == null) return false;

        if (!WriteValueToProperty(prop)) return false;

        sObj.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // 선택된 타입의 입력 값을 SerializedProperty에 기록한다. 성공 시 true.
    private bool WriteValueToProperty(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Boolean:
                prop.boolValue = _valBool;
                return true;
            case SerializedPropertyType.Integer:
                prop.intValue = _valInt;
                return true;
            case SerializedPropertyType.Float:
                prop.floatValue = _valFloat;
                return true;
            case SerializedPropertyType.String:
                prop.stringValue = _valString;
                return true;
            case SerializedPropertyType.Vector3:
                prop.vector3Value = _valVector3;
                return true;
            case SerializedPropertyType.Color:
                prop.colorValue = _valColor;
                return true;
            case SerializedPropertyType.Enum:
                prop.enumValueIndex = _valEnumIndex;
                return true;
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = _valObject;
                return true;
            default:
                return false;
        }
    }

    // 선택된 프리팹 중 하나에서 지정 컴포넌트를 찾아 편집 가능한 속성 이름 목록을 읽어온다.
    private void RefreshPropertyList()
    {
        _propNames = new string[0];
        _selectedPropIndex = 0;

        Type opType = GetResolvedType(_opCompIsBuiltIn, _opCompBuiltInName, _opCompScript);
        if (opType == null)
        {
            Debug.LogError("[CPrefabModifier.cs] 컴포넌트 타입을 찾을 수 없습니다.");
            return;
        }

        Component sample = FindSampleComponent(opType);
        if (sample == null)
        {
            Debug.LogWarning("[CPrefabModifier.cs] 선택된 프리팹에서 해당 컴포넌트를 찾지 못했습니다.");
            return;
        }

        // SerializedObject로 편집 가능한 속성 순회 (m_Script 등 내부 속성 제외)
        List<string> names = new List<string>();
        SerializedObject sObj = new SerializedObject(sample);
        SerializedProperty it = sObj.GetIterator();
        bool enterChildren = true;
        while (it.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (it.name == "m_Script") continue;
            names.Add(it.name);
        }

        _propNames = names.ToArray();
        if (_propNames.Length > 0)
        {
            RefreshSelectedPropertyMeta();
        }
    }

    // 선택된 속성의 타입 메타를 갱신한다. (enum이면 선택지 이름도 채움)
    private void RefreshSelectedPropertyMeta()
    {
        Type opType = GetResolvedType(_opCompIsBuiltIn, _opCompBuiltInName, _opCompScript);
        if (opType == null) return;

        Component sample = FindSampleComponent(opType);
        if (sample == null) return;

        SerializedObject sObj = new SerializedObject(sample);
        SerializedProperty prop = sObj.FindProperty(_propNames[_selectedPropIndex]);
        if (prop == null) return;

        _selectedPropType = prop.propertyType;
        if (prop.propertyType == SerializedPropertyType.Enum)
        {
            _valEnumNames = prop.enumDisplayNames;
            _valEnumIndex = Mathf.Clamp(_valEnumIndex, 0, _valEnumNames.Length - 1);
        }
    }

    // 선택된 프리팹들을 순회하며 지정 컴포넌트를 가진 첫 인스턴스를 찾는다. (속성 목록 샘플용)
    private Component FindSampleComponent(Type opType)
    {
        GameObject[] roots = Selection.gameObjects;
        for (int i = 0; i < roots.Length; ++i)
        {
            Component c = roots[i].GetComponentInChildren(opType, true);
            if (c != null) return c;
        }
        return null;
    }
    #endregion

    #region ─────────────────────────▶ 타입 리플렉션 ◀─────────────────────────
    // 문자열 또는 MonoScript로부터 실제 Type을 추출한다.
    private Type GetResolvedType(bool isBuiltIn, string className, MonoScript script)
    {
        if (!isBuiltIn)
        {
            return script != null ? script.GetClass() : null;
        }

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
}
