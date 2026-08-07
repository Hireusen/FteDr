// CColliderFitterWindow.cs
// Tools > Collider Fitter 에디터 창.
// 회전이 필요한 콜라이더(Box/Capsule)는 회전된 자식 GameObject "_Collider"에 부착한다.
// 회전 불필요한 Sphere는 root에 바로 붙인다.
//
// 반드시 Editor 폴더에 둘 것. (CColliderFitter.cs는 아무 데나 OK)

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ColliderFitter
{
    public class CColliderFitterWindow : EditorWindow
    {
        private const string CHILD_NAME = "_Collider";  // 생성하는 콜라이더 자식 이름

        private CFitSettings _settings = new CFitSettings();

        // target -> preview results (로컬 공간, 회전 포함)
        private readonly Dictionary<MeshFilter, List<SFitResult>> _preview
            = new Dictionary<MeshFilter, List<SFitResult>>();

        private Vector2 _scroll;
        private bool _autoRecompute = true;

        [MenuItem("Tools/Collider Fitter")]
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
            Recompute();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (_autoRecompute) { Recompute(); }
            Repaint();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("자동 콜라이더 조립기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "회전 박스/캡슐은 자식 오브젝트 \"" + CHILD_NAME + "\"에 부착됩니다.\n" +
                "슬라이더로 조절 후 [적용]을 누르세요.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("피팅 모드", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _settings.mode = (EFitMode)EditorGUILayout.EnumPopup(
                new GUIContent("모드", "SingleOBB: 회전 박스 하나 (책/상자류) / AutoSplit: 여러 덩어리로 분할"),
                _settings.mode);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("핵심 슬라이더", EditorStyles.boldLabel);

            _settings.accuracy = EditorGUILayout.Slider(
                new GUIContent("정확도", "높을수록 각도 탐색 촘촘 + voxel 잘게 (부풀음 감소, 느려짐)"),
                _settings.accuracy, 0f, 1f);

            _settings.refineOBB = EditorGUILayout.ToggleLeft(
                new GUIContent("최소부피 탐색", "박스가 부풀지 않게 PCA 축 주변 각도를 탐색. 끄면 빠르지만 부풀 수 있음."),
                _settings.refineOBB);

            _settings.economy = EditorGUILayout.Slider(
                new GUIContent("개수 절약", "높을수록 프리미티브를 공격적으로 병합 (AutoSplit 전용)"),
                _settings.economy, 0f, 1f);

            _settings.slack = EditorGUILayout.Slider(
                new GUIContent("여유", "+ 튀어나옴 허용(수축) / - 다 감싸기(팽창)"),
                _settings.slack, -0.1f, 0.1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("형상 판정", EditorStyles.boldLabel);

            _settings.capsuleAspect = EditorGUILayout.Slider(
                new GUIContent("캡슐 종횡비", "긴축/짧은축 비율이 이보다 크면 캡슐"),
                _settings.capsuleAspect, 1.0f, 4.0f);

            _settings.sphereTolerance = EditorGUILayout.Slider(
                new GUIContent("구 허용오차", "세 축이 이 정도로 비슷하면 구"),
                _settings.sphereTolerance, 0f, 0.3f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("허용 형상", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _settings.allowBox = GUILayout.Toggle(_settings.allowBox, "Box", "Button");
            _settings.allowCapsule = GUILayout.Toggle(_settings.allowCapsule, "Capsule", "Button");
            _settings.allowSphere = GUILayout.Toggle(_settings.allowSphere, "Sphere", "Button");
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(_settings.mode == EFitMode.SingleOBB))
            {
                _settings.maxColliders = EditorGUILayout.IntSlider(
                    new GUIContent("최대 콜라이더 수", "AutoSplit 전용"),
                    _settings.maxColliders, 1, 64);

                _settings.forceSplitCount = EditorGUILayout.IntSlider(
                    new GUIContent("수동 분할 개수", "0=자동(voxel). N=가장 큰 덩어리를 N개로 강제 분할 (안경 두 알 등)"),
                    _settings.forceSplitCount, 0, 16);
            }

            if (EditorGUI.EndChangeCheck() && _autoRecompute)
            {
                Recompute();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            _autoRecompute = EditorGUILayout.ToggleLeft("변경 시 자동 재계산", _autoRecompute);

            EditorGUILayout.Space();

            int targetCount = _preview.Count;
            int primCount = 0;
            foreach (var kv in _preview) { primCount += kv.Value.Count; }
            EditorGUILayout.LabelField("대상 " + targetCount + "개 · 생성될 콜라이더 " + primCount + "개");

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("미리보기 갱신", GUILayout.Height(28)))
                {
                    Recompute();
                }

                GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
                if (GUILayout.Button("적용", GUILayout.Height(28)))
                {
                    Apply();
                }
                GUI.backgroundColor = Color.white;
            }

            GUI.backgroundColor = new Color(0.95f, 0.7f, 0.7f);
            if (GUILayout.Button("선택 대상의 콜라이더 전부 제거", GUILayout.Height(24)))
            {
                ClearColliders();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        // 선택 오브젝트들의 프리미티브 미리 계산
        private void Recompute()
        {
            _preview.Clear();

            // 부모/자식 중복 선택 시 같은 MeshFilter가 두 번 처리되지 않게 방어
            var seen = new HashSet<MeshFilter>();

            foreach (var go in Selection.gameObjects)
            {
                // 선택 오브젝트 자신 + 모든 자식의 MeshFilter를 훑는다.
                // 상자 프리팹처럼 root는 비어있고 자식에 메시가 있는 구조를 지원.
                var filters = go.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in filters)
                {
                    if (mf == null || mf.sharedMesh == null) { continue; }
                    if (!seen.Add(mf)) { continue; }
                    _preview[mf] = CFitter.Fit(mf.sharedMesh, _settings);
                }
            }
        }

        // 실제 콜라이더 부착
        private void Apply()
        {
            foreach (var kv in _preview)
            {
                var mf = kv.Key;
                var root = mf.gameObject;

                Undo.RegisterFullObjectHierarchyUndo(root, "Fit Colliders");

                // 기존 자동생성 자식 + root의 프리미티브 콜라이더 정리
                RemoveGeneratedColliders(root);

                foreach (var r in kv.Value)
                {
                    AttachCollider(root, r);
                }
            }
            SceneView.RepaintAll();
        }

        // 결과 하나를 부착: 회전 필요 여부에 따라 root 또는 자식에
        private static void AttachCollider(GameObject root, SFitResult r)
        {
            bool needsRotation = r.kind != EPrimitiveKind.Sphere
                && Quaternion.Angle(r.rotation, Quaternion.identity) > 0.5f;

            if (!needsRotation)
            {
                // 회전 불필요 → root에 바로 부착
                AddColliderComponent(root, r, r.center);
                return;
            }

            // 회전 필요 → 회전된 자식 생성 후 로컬 축정렬 콜라이더 부착
            var child = new GameObject(CHILD_NAME);
            Undo.RegisterCreatedObjectUndo(child, "Create Collider Child");

            var ct = child.transform;
            ct.SetParent(root.transform, false);
            ct.localPosition = r.center;   // 로컬 중심으로 이동
            ct.localRotation = r.rotation; // PCA 회전 적용
            ct.localScale = Vector3.one;

            // 자식 로컬 기준으로는 중심이 원점, 축정렬
            AddColliderComponent(child, r, Vector3.zero);
        }

        // 콜라이더 컴포넌트 실제 추가
        private static void AddColliderComponent(GameObject go, SFitResult r, Vector3 localCenter)
        {
            switch (r.kind)
            {
                case EPrimitiveKind.Box:
                    {
                        var c = Undo.AddComponent<BoxCollider>(go);
                        c.center = localCenter;
                        c.size = r.boxSize;
                        break;
                    }
                case EPrimitiveKind.Sphere:
                    {
                        var c = Undo.AddComponent<SphereCollider>(go);
                        c.center = localCenter;
                        c.radius = r.radius;
                        break;
                    }
                case EPrimitiveKind.Capsule:
                    {
                        var c = Undo.AddComponent<CapsuleCollider>(go);
                        c.center = localCenter;
                        c.radius = r.capRadius;
                        c.height = r.capHeight;
                        c.direction = r.capDirection;
                        break;
                    }
            }
        }

        // 선택 대상 콜라이더 제거
        private void ClearColliders()
        {
            foreach (var go in Selection.gameObjects)
            {
                Undo.RegisterFullObjectHierarchyUndo(go, "Clear Colliders");
                RemoveGeneratedColliders(go);
            }
        }

        // root의 프리미티브 콜라이더 + 자동생성 자식 제거 (MeshCollider는 건드리지 않음)
        private static void RemoveGeneratedColliders(GameObject root)
        {
            foreach (var c in root.GetComponents<BoxCollider>()) { Undo.DestroyObjectImmediate(c); }
            foreach (var c in root.GetComponents<SphereCollider>()) { Undo.DestroyObjectImmediate(c); }
            foreach (var c in root.GetComponents<CapsuleCollider>()) { Undo.DestroyObjectImmediate(c); }

            // 자동생성 자식들 제거 (뒤에서부터 순회)
            var t = root.transform;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                if (child.name == CHILD_NAME)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        // Scene 뷰 미리보기 (회전 반영)
        private void OnSceneGUI(SceneView view)
        {
            foreach (var kv in _preview)
            {
                var mf = kv.Key;
                if (mf == null) { continue; }
                var t = mf.transform;

                Handles.color = new Color(0.3f, 1f, 0.5f, 1f);
                foreach (var r in kv.Value)
                {
                    DrawPreview(t, r);
                }
            }
        }

        // 프리미티브 하나를 회전 반영해 그리기
        private static void DrawPreview(Transform t, SFitResult r)
        {
            Matrix4x4 old = Handles.matrix;

            // root 로컬→월드 × 프리미티브 회전/위치
            Matrix4x4 local = Matrix4x4.TRS(r.center, r.rotation, Vector3.one);
            Handles.matrix = t.localToWorldMatrix * local;

            switch (r.kind)
            {
                case EPrimitiveKind.Box:
                    Handles.DrawWireCube(Vector3.zero, r.boxSize);
                    break;
                case EPrimitiveKind.Sphere:
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, r.radius);
                    Handles.DrawWireDisc(Vector3.zero, Vector3.right, r.radius);
                    Handles.DrawWireDisc(Vector3.zero, Vector3.forward, r.radius);
                    break;
                case EPrimitiveKind.Capsule:
                    DrawWireCapsule(r.capRadius, r.capHeight, r.capDirection);
                    break;
            }

            Handles.matrix = old;
        }

        // 로컬 원점 기준 캡슐 와이어프레임 (회전은 Handles.matrix가 처리)
        private static void DrawWireCapsule(float radius, float height, int dir)
        {
            Vector3 axis = dir == 0 ? Vector3.right : (dir == 1 ? Vector3.up : Vector3.forward);
            float half = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 top = axis * half;
            Vector3 bottom = -axis * half;

            Handles.DrawWireDisc(top, axis, radius);
            Handles.DrawWireDisc(bottom, axis, radius);

            Vector3 perp1 = dir == 0 ? Vector3.up : Vector3.right;
            Vector3 perp2 = dir == 2 ? Vector3.up : Vector3.forward;

            Handles.DrawLine(top + perp1 * radius, bottom + perp1 * radius);
            Handles.DrawLine(top - perp1 * radius, bottom - perp1 * radius);
            Handles.DrawLine(top + perp2 * radius, bottom + perp2 * radius);
            Handles.DrawLine(top - perp2 * radius, bottom - perp2 * radius);

            Handles.DrawWireDisc(top, perp1, radius);
            Handles.DrawWireDisc(bottom, perp1, radius);
        }
    }
}
#endif
