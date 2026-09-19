// CColliderFitterWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ColliderFitter
{
    public class CColliderFitterWindow : EditorWindow
    {
        private const string CHILD_NAME = "_Collider";

        private CFitSettings _settings = new CFitSettings();

        // 반자동 워크플로우 변수
        private bool _isPaintMode = false;
        private float _brushRadius = 0.5f;
        private MeshFilter _targetFilter;
        private Vector3[] _cachedVertices;
        private HashSet<int> _selectedIndices = new HashSet<int>();
        private MeshCollider _tempRaycastCollider;

        private Vector2 _scroll;

        [MenuItem("Tools/Collider Fitter (Semi-Auto)")]
        public static void Open()
        {
            var win = GetWindow<CColliderFitterWindow>("Collider Fitter");
            win.minSize = new Vector2(320, 500);
            win.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            UpdateTarget();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
            CleanupTempCollider();
        }

        private void OnSelectionChanged()
        {
            if (!_isPaintMode) UpdateTarget();
            Repaint();
        }

        private void UpdateTarget()
        {
            _targetFilter = null;
            _cachedVertices = null;
            _selectedIndices.Clear();
            CleanupTempCollider();

            if (Selection.activeGameObject != null)
            {
                _targetFilter = Selection.activeGameObject.GetComponentInChildren<MeshFilter>();
                if (_targetFilter != null && _targetFilter.sharedMesh != null)
                {
                    // 메모리 할당 최소화를 위해 미리 정점 배열 캐싱
                    _cachedVertices = _targetFilter.sharedMesh.vertices;
                }
            }
        }

        private void CleanupTempCollider()
        {
            if (_tempRaycastCollider != null)
            {
                DestroyImmediate(_tempRaycastCollider);
                _tempRaycastCollider = null;
            }
        }

        private void SetupTempCollider()
        {
            if (_targetFilter == null) return;
            _tempRaycastCollider = _targetFilter.gameObject.AddComponent<MeshCollider>();
            _tempRaycastCollider.sharedMesh = _targetFilter.sharedMesh;
            _tempRaycastCollider.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("반자동 콜라이더 조립기 (페인트 모드)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scene 뷰에서 브러시로 메시에 칠한 부위에만 콜라이더를 생성합니다.", MessageType.Info);

            if (_targetFilter == null)
            {
                EditorGUILayout.HelpBox("MeshFilter가 포함된 게임 오브젝트를 선택해주세요.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField($"타겟: {_targetFilter.name} (정점: {_cachedVertices.Length}개)");
            EditorGUILayout.Space();

            GUI.backgroundColor = _isPaintMode ? new Color(0.6f, 0.9f, 0.6f) : Color.white;
            if (GUILayout.Button(_isPaintMode ? "페인트 모드 종료" : "페인트 모드 시작", GUILayout.Height(30)))
            {
                _isPaintMode = !_isPaintMode;
                if (_isPaintMode) SetupTempCollider();
                else CleanupTempCollider();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            using (new EditorGUI.DisabledScope(!_isPaintMode))
            {
                _brushRadius = EditorGUILayout.Slider("브러시 크기", _brushRadius, 0.02f, 5f);
                EditorGUILayout.LabelField("조작: 좌클릭 드래그(칠하기), Shift+드래그(지우기)");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"선택된 정점: {_selectedIndices.Count}개");

                if (GUILayout.Button("선택 영역 초기화"))
                {
                    _selectedIndices.Clear();
                    SceneView.RepaintAll();
                }

                EditorGUILayout.Space();
                _settings.refineOBB = EditorGUILayout.Toggle("최소부피 탐색 (박스 최적화)", _settings.refineOBB);
                _settings.slack = EditorGUILayout.Slider("여유 (팽창/수축)", _settings.slack, -0.1f, 0.1f);

                EditorGUILayout.Space();
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("선택 영역에 콜라이더 씌우기", GUILayout.Height(35)))
                {
                    GenerateColliderFromSelection();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0.95f, 0.7f, 0.7f);
            if (GUILayout.Button("타겟의 모든 생성된 콜라이더 제거", GUILayout.Height(24)))
            {
                ClearColliders(_targetFilter.gameObject);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        private void GenerateColliderFromSelection()
        {
            if (_selectedIndices.Count < 3)
            {
                Debug.LogWarning("선택된 정점이 너무 적습니다.");
                return;
            }

            SFitResult result = CFitter.FitSelection(_cachedVertices, _selectedIndices, _settings);
            AttachCollider(_targetFilter.gameObject, result);

            _selectedIndices.Clear(); // 씌운 후 초기화하여 다음 작업 준비
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView view)
        {
            if (!_isPaintMode || _targetFilter == null || _tempRaycastCollider == null) return;

            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // 마우스 광선 추적
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool isHit = _tempRaycastCollider.Raycast(ray, out RaycastHit hit, 1000f);

            if (isHit)
            {
                // 브러시 그리기
                Handles.color = new Color(0f, 1f, 1f, 0.2f);
                Handles.DrawSolidDisc(hit.point, hit.normal, _brushRadius);
                Handles.color = Color.cyan;
                Handles.DrawWireDisc(hit.point, hit.normal, _brushRadius);

                // 페인트 로직
                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && e.button == 0 && !e.alt)
                {
                    GUIUtility.hotControl = controlID; // 카메라 회전 방지
                    bool isErasing = e.shift;

                    Matrix4x4 localToWorld = _targetFilter.transform.localToWorldMatrix;
                    float sqrRadius = _brushRadius * _brushRadius;

                    // 빠른 거리 계산을 위해 월드 좌표 변환 후 검사
                    for (int i = 0; i < _cachedVertices.Length; i++)
                    {
                        Vector3 worldVert = localToWorld.MultiplyPoint3x4(_cachedVertices[i]);
                        if ((worldVert - hit.point).sqrMagnitude <= sqrRadius)
                        {
                            if (isErasing) _selectedIndices.Remove(i);
                            else _selectedIndices.Add(i);
                        }
                    }
                    e.Use();
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                GUIUtility.hotControl = 0;
            }

            // 선택된 정점 시각화 (성능을 위해 단순 픽셀 도트로 렌더링)
            if (_selectedIndices.Count > 0)
            {
                Handles.color = Color.yellow;
                Matrix4x4 matrix = _targetFilter.transform.localToWorldMatrix;
                foreach (int idx in _selectedIndices)
                {
                    Handles.DrawLine(matrix.MultiplyPoint3x4(_cachedVertices[idx]), matrix.MultiplyPoint3x4(_cachedVertices[idx]) + Vector3.up * 0.02f);
                }
            }

            // Scene 뷰 강제 갱신으로 부드러운 브러시 이동 구현
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                SceneView.RepaintAll();
            }
        }

        private static void AttachCollider(GameObject root, SFitResult r)
        {
            bool needsRotation = r.kind != EPrimitiveKind.Sphere && Quaternion.Angle(r.rotation, Quaternion.identity) > 0.5f;

            Undo.RegisterFullObjectHierarchyUndo(root, "Fit Collider to Selection");

            if (!needsRotation)
            {
                AddColliderComponent(root, r, r.center);
                return;
            }

            var child = new GameObject(CHILD_NAME);
            Undo.RegisterCreatedObjectUndo(child, "Create Collider Child");

            var ct = child.transform;
            ct.SetParent(root.transform, false);
            ct.localPosition = r.center;
            ct.localRotation = r.rotation;
            ct.localScale = Vector3.one;

            AddColliderComponent(child, r, Vector3.zero);
        }

        private static void AddColliderComponent(GameObject go, SFitResult r, Vector3 localCenter)
        {
            if (r.kind == EPrimitiveKind.Box)
            {
                var c = Undo.AddComponent<BoxCollider>(go);
                c.center = localCenter; c.size = r.boxSize;
            }
            else if (r.kind == EPrimitiveKind.Sphere)
            {
                var c = Undo.AddComponent<SphereCollider>(go);
                c.center = localCenter; c.radius = r.radius;
            }
            else if (r.kind == EPrimitiveKind.Capsule)
            {
                var c = Undo.AddComponent<CapsuleCollider>(go);
                c.center = localCenter; c.radius = r.capRadius; c.height = r.capHeight; c.direction = r.capDirection;
            }
        }

        private static void ClearColliders(GameObject root)
        {
            Undo.RegisterFullObjectHierarchyUndo(root, "Clear Colliders");
            foreach (var c in root.GetComponents<BoxCollider>()) Undo.DestroyObjectImmediate(c);
            foreach (var c in root.GetComponents<SphereCollider>()) Undo.DestroyObjectImmediate(c);
            foreach (var c in root.GetComponents<CapsuleCollider>()) Undo.DestroyObjectImmediate(c);

            var t = root.transform;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                if (child.name == CHILD_NAME) Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }
}
#endif
