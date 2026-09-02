using UnityEngine;
using UnityEditor;

public class BrushPlacementTool : EditorWindow
{
    private GameObject prefabToPlace;
    private bool isPainting = false;

    // 배치 옵션
    private bool alignToNormal = true;
    private bool randomYRotation = true;
    private bool randomScale = true;
    private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    // 브러쉬 간격 옵션
    private bool randomSpacing = true;
    private float fixedSpacing = 2.0f;
    private Vector2 spacingRange = new Vector2(1.9f, 2.1f);

    // 내부 상태 추적용 필드 (메모리 할당 방지)
    private Vector3 lastPlacedPosition = Vector3.positiveInfinity;
    private float currentTargetSpacing = 0f;

    [MenuItem("Tools/Brush Placement Tool")]
    public static void ShowWindow()
    {
        GetWindow<BrushPlacementTool>("브러쉬 배치 툴");
    }

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🎨 마우스 드래그 오브젝트 배치 툴", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        prefabToPlace = (GameObject)EditorGUILayout.ObjectField("배치할 프리팹", prefabToPlace, typeof(GameObject), false);

        GUILayout.Space(10);

        EditorGUILayout.LabelField("배치 옵션", EditorStyles.boldLabel);
        alignToNormal = EditorGUILayout.Toggle("지형 굴곡(Normal)에 맞춤", alignToNormal);
        randomYRotation = EditorGUILayout.Toggle("Y축 랜덤 회전", randomYRotation);

        randomScale = EditorGUILayout.BeginToggleGroup("랜덤 크기 (Uniform)", randomScale);
        scaleRange = EditorGUILayout.Vector2Field("크기 범위 (Min/Max)", scaleRange);
        EditorGUILayout.EndToggleGroup();

        GUILayout.Space(10);

        EditorGUILayout.LabelField("브러쉬 옵션", EditorStyles.boldLabel);
        randomSpacing = EditorGUILayout.Toggle("랜덤 배치 간격 사용", randomSpacing);

        if (randomSpacing)
        {
            spacingRange = EditorGUILayout.Vector2Field("간격 범위 (Min/Max)", spacingRange);
        }
        else
        {
            fixedSpacing = EditorGUILayout.FloatField("고정 배치 간격", fixedSpacing);
        }

        GUILayout.Space(20);

        GUI.backgroundColor = isPainting ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.8f, 0.4f);
        string buttonText = isPainting ? "배치 모드 종료 (ESC)" : "배치 모드 시작";

        if (GUILayout.Button(buttonText, GUILayout.Height(40)))
        {
            if (prefabToPlace == null)
            {
                EditorUtility.DisplayDialog("알림", "배치할 프리팹을 먼저 등록해주세요!", "확인");
                return;
            }
            isPainting = !isPainting;

            // 페인팅 시작 시 위치 및 초기 간격 셋업
            lastPlacedPosition = Vector3.positiveInfinity;
            currentTargetSpacing = randomSpacing ? Random.Range(spacingRange.x, spacingRange.y) : fixedSpacing;
        }
        GUI.backgroundColor = Color.white;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting || prefabToPlace == null) return;

        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            isPainting = false;
            Repaint();
            return;
        }

        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 현재 캐싱된 목표 간격(currentTargetSpacing)을 기준으로 거리 체크
                if (e.type == EventType.MouseDown || Vector3.Distance(hit.point, lastPlacedPosition) >= currentTargetSpacing)
                {
                    PlaceObject(hit);
                    lastPlacedPosition = hit.point;

                    // 배치 직후, 다음 배치에 필요한 목표 간격을 즉시 계산하여 캐싱
                    currentTargetSpacing = randomSpacing ? Random.Range(spacingRange.x, spacingRange.y) : fixedSpacing;
                }
                e.Use();
            }
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            lastPlacedPosition = Vector3.positiveInfinity;
        }
    }

    private void PlaceObject(RaycastHit hit)
    {
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);
        Undo.RegisterCreatedObjectUndo(newObj, "Brush Place Object");

        newObj.transform.position = hit.point;

        if (alignToNormal) newObj.transform.up = hit.normal;
        if (randomYRotation) newObj.transform.Rotate(0, Random.Range(0f, 360f), 0, Space.Self);
        if (randomScale) newObj.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
    }
}
