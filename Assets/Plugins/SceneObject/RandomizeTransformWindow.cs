using UnityEngine;
using UnityEditor;

public class RandomizeTransformWindow : EditorWindow
{
    // 회전 관련 변수
    private bool randomizeRotation = true;
    private bool randomizeYOnly = true; // 나무/바위 등 지형에 맞춘 채 방향만 돌릴 때
    private Vector2 rotXRange = new Vector2(-5f, 5f);
    private Vector2 rotYRange = new Vector2(0f, 360f);
    private Vector2 rotZRange = new Vector2(-5f, 5f);

    // 스케일 관련 변수
    private bool randomizeScale = true;
    private bool uniformScale = true;   // 비율을 유지한 채 크기만 조절할지 여부
    private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    private Vector2 scaleXRange = new Vector2(0.8f, 1.2f);
    private Vector2 scaleYRange = new Vector2(0.8f, 1.2f);
    private Vector2 scaleZRange = new Vector2(0.8f, 1.2f);

    [MenuItem("Tools/Randomize Transform")]
    public static void ShowWindow()
    {
        GetWindow<RandomizeTransformWindow>("Transform Randomizer");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🎲 오브젝트 트랜스폼 랜덤 조절기", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. 회전 설정 그룹
        randomizeRotation = EditorGUILayout.BeginToggleGroup("회전 (Rotation) 랜덤", randomizeRotation);
        randomizeYOnly = EditorGUILayout.Toggle("Y축만 회전 (수평 유지)", randomizeYOnly);

        if (randomizeYOnly)
        {
            rotYRange = EditorGUILayout.Vector2Field("Y 회전 범위 (Min / Max)", rotYRange);
        }
        else
        {
            rotXRange = EditorGUILayout.Vector2Field("X 각도 (Min / Max)", rotXRange);
            rotYRange = EditorGUILayout.Vector2Field("Y 각도 (Min / Max)", rotYRange);
            rotZRange = EditorGUILayout.Vector2Field("Z 각도 (Min / Max)", rotZRange);
        }
        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space(10);

        // 2. 스케일 설정 그룹
        randomizeScale = EditorGUILayout.BeginToggleGroup("크기 (Scale) 랜덤", randomizeScale);
        uniformScale = EditorGUILayout.Toggle("비율 고정 (Uniform Scale)", uniformScale);

        if (uniformScale)
        {
            scaleRange = EditorGUILayout.Vector2Field("전체 크기 범위 (Min / Max)", scaleRange);
        }
        else
        {
            scaleXRange = EditorGUILayout.Vector2Field("X축 크기 (Min / Max)", scaleXRange);
            scaleYRange = EditorGUILayout.Vector2Field("Y축 크기 (Min / Max)", scaleYRange);
            scaleZRange = EditorGUILayout.Vector2Field("Z축 크기 (Min / Max)", scaleZRange);
        }
        EditorGUILayout.EndToggleGroup();

        GUILayout.Space(20);

        // 3. 실행 버튼
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("선택한 오브젝트에 랜덤 적용", GUILayout.Height(40)))
        {
            ApplyRandomization();
        }
        GUI.backgroundColor = Color.white;
    }

    private void ApplyRandomization()
    {
        Transform[] selected = Selection.transforms;

        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("알림", "선택된 오브젝트가 없습니다. 씬에서 오브젝트를 먼저 선택해 주세요.", "확인");
            return;
        }

        // 실행 취소(Ctrl+Z) 등록
        Undo.RecordObjects(selected, "Randomize Transforms");

        foreach (Transform t in selected)
        {
            // 회전 적용
            if (randomizeRotation)
            {
                if (randomizeYOnly)
                {
                    float randY = Random.Range(rotYRange.x, rotYRange.y);
                    Vector3 currentEuler = t.localEulerAngles;
                    t.localEulerAngles = new Vector3(currentEuler.x, randY, currentEuler.z);
                }
                else
                {
                    float randX = Random.Range(rotXRange.x, rotXRange.y);
                    float randY = Random.Range(rotYRange.x, rotYRange.y);
                    float randZ = Random.Range(rotZRange.x, rotZRange.y);
                    t.localEulerAngles = new Vector3(randX, randY, randZ);
                }
            }

            // 스케일 적용
            if (randomizeScale)
            {
                if (uniformScale)
                {
                    float randScale = Random.Range(scaleRange.x, scaleRange.y);
                    t.localScale = Vector3.one * randScale;
                }
                else
                {
                    float sx = Random.Range(scaleXRange.x, scaleXRange.y);
                    float sy = Random.Range(scaleYRange.x, scaleYRange.y);
                    float sz = Random.Range(scaleZRange.x, scaleZRange.y);
                    t.localScale = new Vector3(sx, sy, sz);
                }
            }
        }
    }
}
