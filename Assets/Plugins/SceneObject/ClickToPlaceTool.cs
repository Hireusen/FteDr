using UnityEngine;
using UnityEditor;

public class ClickToPlaceTool : EditorWindow
{
    private GameObject prefabToPlace;
    private bool isPainting = false;

    // 배치 옵션
    private bool alignToNormal = true;
    private bool randomYRotation = true;
    private bool randomScale = true;
    private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    [MenuItem("Tools/Click To Place Tool")]
    public static void ShowWindow()
    {
        GetWindow<ClickToPlaceTool>("클릭 배치 툴");
    }

    // 씬 뷰에 이벤트 연결
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🎯 마우스 클릭 오브젝트 배치 툴", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 프리팹 할당
        prefabToPlace = (GameObject)EditorGUILayout.ObjectField("배치할 프리팹", prefabToPlace, typeof(GameObject), false);

        GUILayout.Space(10);

        // 옵션 설정
        EditorGUILayout.LabelField("배치 옵션", EditorStyles.boldLabel);
        alignToNormal = EditorGUILayout.Toggle("지형 굴곡(Normal)에 맞춤", alignToNormal);
        randomYRotation = EditorGUILayout.Toggle("Y축 랜덤 회전", randomYRotation);

        randomScale = EditorGUILayout.BeginToggleGroup("랜덤 크기 (Uniform)", randomScale);
        scaleRange = EditorGUILayout.Vector2Field("크기 범위 (Min/Max)", scaleRange);
        EditorGUILayout.EndToggleGroup();

        GUILayout.Space(20);

        // 페인팅 모드 토글 버튼
        GUI.backgroundColor = isPainting ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.8f, 0.4f);
        string buttonText = isPainting ? "배치 모드 종료 (클릭 시 중지)" : "배치 모드 시작 (Scene 뷰 클릭)";

        if (GUILayout.Button(buttonText, GUILayout.Height(40)))
        {
            if (prefabToPlace == null)
            {
                EditorUtility.DisplayDialog("알림", "배치할 프리팹을 먼저 등록해주세요!", "확인");
                return;
            }
            isPainting = !isPainting;
        }
        GUI.backgroundColor = Color.white;

        if (isPainting)
        {
            EditorGUILayout.HelpBox("Scene 뷰에서 바닥을 좌클릭하면 오브젝트가 생성됩니다.\n종료하려면 위 버튼을 누르거나 'ESC' 키를 누르세요.", MessageType.Info);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting || prefabToPlace == null) return;

        Event e = Event.current;

        // ESC 키를 누르면 배치 모드 종료
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            isPainting = false;
            Repaint();
            return;
        }

        // 배치 모드 중에는 다른 오브젝트가 선택되지 않도록 클릭 이벤트 가로채기
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        // 마우스 좌클릭 시
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // 마우스 커서 위치에서 씬을 향해 레이(Ray) 발사
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PlaceObject(hit);
                e.Use(); // 이벤트를 소모하여 다른 동작(선택 등) 방지
            }
        }
    }

    private void PlaceObject(RaycastHit hit)
    {
        // 프리팹 연결을 유지한 채로 인스턴스화
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace);

        // Ctrl+Z 지원
        Undo.RegisterCreatedObjectUndo(newObj, "Click Place Object");

        // 1. 위치 설정
        newObj.transform.position = hit.point;

        // 2. 지형 노멀에 맞춤
        if (alignToNormal)
        {
            newObj.transform.up = hit.normal;
        }

        // 3. Y축 랜덤 회전 (자신의 로컬 축 기준)
        if (randomYRotation)
        {
            newObj.transform.Rotate(0, Random.Range(0f, 360f), 0, Space.Self);
        }

        // 4. 랜덤 크기
        if (randomScale)
        {
            newObj.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
        }
    }
}
