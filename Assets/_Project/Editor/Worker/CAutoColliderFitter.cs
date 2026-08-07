// 선택한 오브젝트/프리팹의 메시를 PCA로 분석해 Box/Sphere/Capsule 콜라이더를 자동 부착.
// MeshCollider 미사용(convex/음수 스케일 경고 없음), 볼록 근사(외곽 기준 속 채움).
// 프리팹 에셋 선택 시 LoadPrefabContents 로 원본에 저장. Editor 폴더에 배치할 것.

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CAutoColliderFitter : EditorWindow
{
    private enum EShapeMode
    {
        AutoPCA,
        ForceBox,
        ForceCapsule,
        ForceSphere,
    }

    private EShapeMode _mode = EShapeMode.AutoPCA;
    private bool _skipIfHasCollider = true;
    private bool _includeChildren = true;
    private bool _useOBB = true;
    private float _sphereTolerance = 0.30f;
    private float _capsuleRatio = 1.60f;
    private string _childName = "AutoCollider";

    [MenuItem("Tools/Worker/Auto Collider Fitter")]
    private static void Open()
    {
        GetWindow<CAutoColliderFitter>("Auto Collider Fitter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Auto Collider Fitter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "선택한 오브젝트/프리팹의 메시를 분석해 Box/Sphere/Capsule 을 자동 부착합니다.\n" +
            "볼록 근사(외곽 기준 속 채움)이며 MeshCollider 는 쓰지 않습니다.\n" +
            "프로젝트 창의 프리팹 에셋을 선택하면 원본 프리팹에 저장됩니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        _mode = (EShapeMode)EditorGUILayout.EnumPopup("Shape Mode", _mode);

        using (new EditorGUI.DisabledScope(_mode != EShapeMode.AutoPCA))
        {
            EditorGUI.indentLevel++;
            _capsuleRatio = EditorGUILayout.Slider(
                new GUIContent("Capsule Ratio", "최장축/중간축 비율이 이 값 이상이면 Capsule"),
                _capsuleRatio, 1.1f, 4f);
            _sphereTolerance = EditorGUILayout.Slider(
                new GUIContent("Sphere Tolerance", "세 축이 이 비율 안으로 비슷하면 Sphere"),
                _sphereTolerance, 0.05f, 0.6f);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        _useOBB = EditorGUILayout.Toggle(
            new GUIContent("Oriented (OBB)", "켜면 PCA 회전을 담은 자식에 부착(타이트). 끄면 로컬축 정렬."),
            _useOBB);

        using (new EditorGUI.DisabledScope(!_useOBB))
        {
            EditorGUI.indentLevel++;
            _childName = EditorGUILayout.TextField(
                new GUIContent("Child Name", "콜라이더를 담을 자식 오브젝트 이름. 같은 이름이 이미 있으면 재사용."),
                _childName);
            EditorGUI.indentLevel--;
        }
        _includeChildren = EditorGUILayout.Toggle(
            new GUIContent("Include Children", "선택 오브젝트의 자식 렌더러까지 각각 처리"),
            _includeChildren);
        _skipIfHasCollider = EditorGUILayout.Toggle(
            new GUIContent("Skip If Has Collider", "이미 콜라이더가 있으면 건너뜀(OBB면 해당 이름 자식 기준)"),
            _skipIfHasCollider);

        EditorGUILayout.Space();
        int count = Selection.objects != null ? Selection.objects.Length : 0;
        EditorGUILayout.LabelField($"Selected objects: {count}");

        using (new EditorGUI.DisabledScope(count == 0))
        {
            if (GUILayout.Button("Fit Colliders on Selection", GUILayout.Height(32)))
            {
                Run();
            }
        }
    }

    private void Run()
    {
        int made = 0;
        int skipped = 0;
        int failed = 0;
        int prefabs = 0;

        var prefabPaths = new List<string>();
        var sceneRoots = new List<GameObject>();

        // 프리팹 에셋 / 씬 오브젝트 분류
        foreach (var obj in Selection.objects)
        {
            var go = obj as GameObject;
            if (go == null)
            {
                continue;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(go) && AssetDatabase.Contains(go))
            {
                string path = AssetDatabase.GetAssetPath(go);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
                {
                    prefabPaths.Add(path);
                }
            }
            else
            {
                sceneRoots.Add(go);
            }
        }

        // 프리팹 에셋: 원본을 열어 편집 후 저장
        foreach (var path in prefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            try
            {
                var renderers = CollectRenderers(root);
                foreach (var r in renderers)
                {
                    if (_skipIfHasCollider && HasCollider(r))
                    {
                        skipped++;
                        continue;
                    }

                    if (FitOne(r, useUndo: false))
                    {
                        made++;
                        changed = true;
                    }
                    else
                    {
                        failed++;
                    }
                }
            }
            finally
            {
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            prefabs++;
        }

        // 씬 인스턴스: Undo 지원
        if (sceneRoots.Count > 0)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Auto Fit Colliders");

            foreach (var go in sceneRoots)
            {
                var renderers = CollectRenderers(go);
                foreach (var r in renderers)
                {
                    if (_skipIfHasCollider && HasCollider(r))
                    {
                        skipped++;
                        continue;
                    }

                    if (FitOne(r, useUndo: true))
                    {
                        made++;
                    }
                    else
                    {
                        failed++;
                    }
                }
            }

            Undo.CollapseUndoOperations(group);
        }

        Debug.Log($"[AutoColliderFitter] 완료 — 생성 {made}, 건너뜀 {skipped}, 실패 {failed}" +
                  (prefabs > 0 ? $" (프리팹 에셋 {prefabs}개 저장됨)" : ""));
    }

    private List<Renderer> CollectRenderers(GameObject go)
    {
        var list = new List<Renderer>();
        if (_includeChildren)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    list.Add(r);
                }
            }
        }
        else
        {
            var r = go.GetComponent<Renderer>();
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
            {
                list.Add(r);
            }
        }

        return list;
    }

    // 콜라이더 존재 검사. OBB면 지정 이름 자식 위를, 아니면 렌더러 자신을 본다.
    private bool HasCollider(Renderer r)
    {
        if (_useOBB)
        {
            string childName = string.IsNullOrEmpty(_childName) ? "AutoCollider" : _childName;
            Transform existing = r.transform.Find(childName);
            return existing != null && existing.GetComponent<Collider>() != null;
        }

        return r.GetComponent<Collider>() != null;
    }

    // 렌더러 하나에 콜라이더 하나 생성
    private bool FitOne(Renderer r, bool useUndo)
    {
        Mesh mesh = GetMesh(r);
        if (mesh == null || mesh.vertexCount == 0)
        {
            return false;
        }

        Vector3[] verts = mesh.vertices;
        if (verts.Length == 0)
        {
            return false;
        }

        // 무게중심
        Vector3 mean = Vector3.zero;
        for (int i = 0; i < verts.Length; i++)
        {
            mean += verts[i];
        }

        mean /= verts.Length;

        // 공분산 행렬
        double cxx = 0;
        double cyy = 0;
        double czz = 0;
        double cxy = 0;
        double cxz = 0;
        double cyz = 0;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 d = verts[i] - mean;
            cxx += d.x * d.x;
            cyy += d.y * d.y;
            czz += d.z * d.z;
            cxy += d.x * d.y;
            cxz += d.x * d.z;
            cyz += d.y * d.z;
        }

        int n = verts.Length;
        cxx /= n;
        cyy /= n;
        czz /= n;
        cxy /= n;
        cxz /= n;
        cyz /= n;

        var cov = new double[3, 3]
        {
            { cxx, cxy, cxz },
            { cxy, cyy, cyz },
            { cxz, cyz, czz },
        };
        JacobiEigen(cov, out double[] eval, out double[,] evec);

        // 고유값 내림차순 정렬
        int[] order = { 0, 1, 2 };
        System.Array.Sort(order, (a, b) => eval[b].CompareTo(eval[a]));

        Vector3 axis0 = ColVec(evec, order[0]); // 최장축
        Vector3 axis1 = ColVec(evec, order[1]); // 중간축
        Vector3 axis2 = ColVec(evec, order[2]); // 최단축
        if (Vector3.Dot(Vector3.Cross(axis0, axis1), axis2) < 0)
        {
            axis2 = -axis2;
        }

        // PCA 축상의 로컬 크기/중심
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 d = verts[i] - mean;
            float p0 = Vector3.Dot(d, axis0);
            float p1 = Vector3.Dot(d, axis1);
            float p2 = Vector3.Dot(d, axis2);
            if (p0 < min.x)
            {
                min.x = p0;
            }

            if (p0 > max.x)
            {
                max.x = p0;
            }

            if (p1 < min.y)
            {
                min.y = p1;
            }

            if (p1 > max.y)
            {
                max.y = p1;
            }

            if (p2 < min.z)
            {
                min.z = p2;
            }

            if (p2 > max.z)
            {
                max.z = p2;
            }
        }

        Vector3 sizeLocal = max - min;                  // PCA축 방향 로컬 길이(정렬됨: x>=y>=z)
        Vector3 centerOffsetLocal = (max + min) * 0.5f; // axis 좌표계 중심 오프셋

        // 메시 로컬공간 기준 콜라이더 중심
        Vector3 centerMeshLocal = mean
            + (axis0 * centerOffsetLocal.x)
            + (axis1 * centerOffsetLocal.y)
            + (axis2 * centerOffsetLocal.z);

        // 모양 결정(비율 비교이므로 스케일 미반영 길이로 충분)
        float l0 = sizeLocal.x;
        float l1 = sizeLocal.y;
        float l2 = sizeLocal.z;
        EShapeMode decided = _mode;
        if (_mode == EShapeMode.AutoPCA)
        {
            bool nearlyEqual = (l0 - l2) <= (_sphereTolerance * l0);
            if (nearlyEqual)
            {
                decided = EShapeMode.ForceSphere;
            }
            else if (l1 > 1e-6f && (l0 / l1) >= _capsuleRatio)
            {
                decided = EShapeMode.ForceCapsule;
            }
            else
            {
                decided = EShapeMode.ForceBox;
            }
        }

        // 부착 대상 결정(OBB면 회전만 담은 자식, 아니면 렌더러 자신)
        GameObject host;
        if (_useOBB)
        {
            string childName = string.IsNullOrEmpty(_childName) ? "AutoCollider" : _childName;
            Transform existing = r.transform.Find(childName);

            if (existing != null)
            {
                // 같은 이름 자식 재사용: 기존 콜라이더는 제거 후 재부착(중복/누적 방지)
                host = existing.gameObject;
                foreach (var old in host.GetComponents<Collider>())
                {
                    if (useUndo)
                    {
                        Undo.DestroyObjectImmediate(old);
                    }
                    else
                    {
                        DestroyImmediate(old);
                    }
                }
            }
            else
            {
                host = new GameObject(childName);
                if (useUndo)
                {
                    Undo.RegisterCreatedObjectUndo(host, "Create Collider Host");
                }

                host.transform.SetParent(r.transform, worldPositionStays: false);
            }

            if (useUndo)
            {
                Undo.RecordObject(host.transform, "Fit Collider Transform");
            }

            // 정규직교 축으로 회전행렬 직접 구성(LookRotation 불안정 회피)
            // host 로컬축 매핑: Y←axis0(최장, up), Z←axis2(최단, forward), X는 유도
            host.transform.localRotation = BasisToRotation(axis0, axis2);
            host.transform.localPosition = centerMeshLocal;
            host.transform.localScale = Vector3.one;
        }
        else
        {
            host = r.gameObject;
        }

        // 콜라이더 부착 (OBB: host up=axis0=Y, right=axis1=X, forward=axis2=Z → size=(l1,l0,l2))
        switch (decided)
        {
            case EShapeMode.ForceSphere:
                {
                    SphereCollider col = useUndo
                        ? Undo.AddComponent<SphereCollider>(host)
                        : host.AddComponent<SphereCollider>();
                    col.center = _useOBB ? Vector3.zero : centerMeshLocal;
                    col.radius = 0.5f * Mathf.Max(l0, Mathf.Max(l1, l2));
                    break;
                }

            case EShapeMode.ForceCapsule:
                {
                    CapsuleCollider col = useUndo
                        ? Undo.AddComponent<CapsuleCollider>(host)
                        : host.AddComponent<CapsuleCollider>();
                    if (_useOBB)
                    {
                        // host 로컬 크기 = (x:l1, y:l0, z:l2). 최장은 Y(l0).
                        // 캡슐은 최장축을 따라 늘리고, 나머지 두 축의 절반 중 큰 값을 반지름으로.
                        col.direction = 1; // Y = 최장축(axis0)
                        col.center = Vector3.zero;
                        float radius = 0.5f * Mathf.Max(l1, l2);
                        col.radius = radius;
                        col.height = Mathf.Max(l0, radius * 2f);
                    }
                    else
                    {
                        int dir = LongestLocalAxis(sizeLocal);
                        col.direction = dir;
                        col.center = centerMeshLocal;
                        float radius = 0.5f * Mathf.Max(
                            sizeLocal[(dir + 1) % 3], sizeLocal[(dir + 2) % 3]);
                        col.radius = radius;
                        col.height = Mathf.Max(sizeLocal[dir], radius * 2f);
                    }

                    break;
                }

            default: // Box
                {
                    BoxCollider col = useUndo
                        ? Undo.AddComponent<BoxCollider>(host)
                        : host.AddComponent<BoxCollider>();
                    if (_useOBB)
                    {
                        col.center = Vector3.zero;
                        col.size = new Vector3(l1, l0, l2);
                    }
                    else
                    {
                        col.center = centerMeshLocal;
                        col.size = sizeLocal;
                    }

                    break;
                }
        }

        return true;
    }

    // 정규직교 축(up/forward)으로 회전 구성. LookRotation 보다 얇은 형태에서 안정적.
    // right 는 up×forward 로 유도하므로 인자로 받지 않는다.
    private static Quaternion BasisToRotation(Vector3 up, Vector3 forward)
    {
        // 수치오차로 직교성이 깨질 수 있으니 그람-슈미트로 재정규화
        Vector3 f = forward.normalized;
        Vector3 u = (up - (Vector3.Dot(up, f) * f)).normalized;
        Vector3 rgt = Vector3.Cross(u, f);

        var m = new Matrix4x4();
        m.SetColumn(0, rgt);
        m.SetColumn(1, u);
        m.SetColumn(2, f);
        m.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
        return m.rotation;
    }

    private static int LongestLocalAxis(Vector3 v)
    {
        if (v.x >= v.y && v.x >= v.z)
        {
            return 0;
        }

        if (v.y >= v.x && v.y >= v.z)
        {
            return 1;
        }

        return 2;
    }

    private static Vector3 ColVec(double[,] m, int col)
    {
        return new Vector3((float)m[0, col], (float)m[1, col], (float)m[2, col]).normalized;
    }

    private static Mesh GetMesh(Renderer r)
    {
        if (r is MeshRenderer)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            return mf != null ? mf.sharedMesh : null;
        }

        if (r is SkinnedMeshRenderer smr)
        {
            return smr.sharedMesh;
        }

        return null;
    }

    // 대칭 3x3 고유값/고유벡터(Jacobi 회전)
    private static void JacobiEigen(double[,] a, out double[] eigenvalues, out double[,] eigenvectors)
    {
        const int N = 3;
        var v = new double[3, 3];
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                v[i, j] = (i == j) ? 1.0 : 0.0;
            }
        }

        var m = (double[,])a.Clone();

        for (int iter = 0; iter < 50; iter++)
        {
            double off = 0;
            for (int p = 0; p < N; p++)
            {
                for (int q = p + 1; q < N; q++)
                {
                    off += m[p, q] * m[p, q];
                }
            }

            if (off < 1e-20)
            {
                break;
            }

            for (int p = 0; p < N; p++)
            {
                for (int q = p + 1; q < N; q++)
                {
                    if (System.Math.Abs(m[p, q]) < 1e-20)
                    {
                        continue;
                    }

                    double theta = (m[q, q] - m[p, p]) / (2 * m[p, q]);
                    double t = System.Math.Sign(theta) /
                               (System.Math.Abs(theta) + System.Math.Sqrt((theta * theta) + 1));
                    if (theta == 0)
                    {
                        t = 1;
                    }

                    double c = 1 / System.Math.Sqrt((t * t) + 1);
                    double sn = t * c;

                    double mpp = m[p, p];
                    double mqq = m[q, q];
                    double mpq = m[p, q];
                    m[p, p] = (c * c * mpp) - (2 * sn * c * mpq) + (sn * sn * mqq);
                    m[q, q] = (sn * sn * mpp) + (2 * sn * c * mpq) + (c * c * mqq);
                    m[p, q] = 0;
                    m[q, p] = 0;

                    for (int i = 0; i < N; i++)
                    {
                        if (i != p && i != q)
                        {
                            double mip = m[i, p];
                            double miq = m[i, q];
                            m[i, p] = (c * mip) - (sn * miq);
                            m[p, i] = m[i, p];
                            m[i, q] = (sn * mip) + (c * miq);
                            m[q, i] = m[i, q];
                        }

                        double vip = v[i, p];
                        double viq = v[i, q];
                        v[i, p] = (c * vip) - (sn * viq);
                        v[i, q] = (sn * vip) + (c * viq);
                    }
                }
            }
        }

        eigenvalues = new double[3] { m[0, 0], m[1, 1], m[2, 2] };
        eigenvectors = v;
    }
}
#endif
