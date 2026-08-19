// CColliderFitter.cs
// 메시 콜라이더 없이 Box/Capsule/Sphere를 자동 조립하는 코어 로직.
// 핵심 개선: PCA 회전을 자식 GameObject의 Transform으로 전달해 실제로 적용한다.
//
// 이전 버전의 결함: BoxCollider는 자체 회전 불가라서 PCA 회전을 버리고
//                    축정렬로 넣어 기울어진 물체에 헐렁하게 박혔음.
// 이번 해결: FitResult가 회전을 함께 반환 → 에디터가 회전된 자식에 콜라이더 부착.

using System.Collections.Generic;
using UnityEngine;

namespace ColliderFitter
{
    // 프리미티브 종류
    public enum EPrimitiveKind
    {
        Box,
        Capsule,
        Sphere,
    }

    // 피팅 전략
    public enum EFitMode
    {
        SingleOBB,   // 분할 없이 메시 전체에 회전 박스 하나 (책/골드바/상자류)
        AutoSplit,   // voxel 클러스터링으로 여러 덩어리에 각각 프리미티브
    }

    // 사용자 슬라이더 + 세부 옵션
    [System.Serializable]
    public class CFitSettings
    {
        public EFitMode mode = EFitMode.SingleOBB;

        // 핵심 슬라이더 (0~1)
        [Range(0f, 1f)] public float accuracy = 0.5f;   // 높을수록 메시를 촘촘히 따라감
        [Range(0f, 1f)] public float economy = 0.5f;    // 높을수록 프리미티브를 공격적으로 병합
        [Range(-0.1f, 0.1f)] public float slack = 0f;    // +면 튀어나옴 허용(수축), -면 다 감쌈(팽창)

        // 형상 판정 문턱값
        [Range(1.0f, 4.0f)] public float capsuleAspect = 1.8f;  // 긴축/짧은축 비율이 이보다 크면 캡슐
        [Range(0f, 0.3f)] public float sphereTolerance = 0.15f;  // 세 축이 이 정도로 비슷하면 구

        // 허용 형상
        public bool allowBox = true;
        public bool allowCapsule = true;
        public bool allowSphere = true;

        // 하드 리밋 (AutoSplit 전용)
        [Range(1, 64)] public int maxColliders = 8;

        // 수동 분할 개수 (AutoSplit 전용). 0이면 자동(voxel), N이면 가장 큰 덩어리를 K-means로 N개까지 강제 분할.
        // 안경 두 알처럼 얇은 다리로 이어져 voxel로는 안 갈리는 경우 사용.
        [Range(0, 16)] public int forceSplitCount = 0;

        // 최소부피 OBB 각도 탐색: PCA 축 주변을 회전시켜 가장 작은 박스를 찾는다.
        // 정확도가 높을수록 촘촘히 탐색(부풀음 감소, 느려짐).
        public bool refineOBB = true;

        // 정확도 → voxel 셀 크기 비율 (바운딩 최대변 대비)
        public float VoxelRatio { get { return Mathf.Lerp(0.35f, 0.06f, accuracy); } }

        // 정확도 → 각도 탐색 스텝 수 (한 축당). 0이면 탐색 안 함.
        public int RefineSteps { get { return refineOBB ? Mathf.RoundToInt(Mathf.Lerp(2f, 12f, accuracy)) : 0; } }

        // 각도 탐색 범위 (도). PCA 축 기준 ±이 값.
        public float RefineRangeDeg { get { return 20f; } }

        // 개수 절약 → 실효 budget
        public int EffectiveBudget
        {
            get { return Mathf.Max(1, Mathf.RoundToInt(maxColliders * Mathf.Lerp(1f, 0.4f, economy))); }
        }
    }

    // 피팅 결과 하나 (콜라이더 하나에 대응, 회전 포함)
    public struct SFitResult
    {
        public EPrimitiveKind kind;

        public Vector3 center;       // 로컬 중심 (root 메시 공간)
        public Quaternion rotation;  // PCA 회전 (자식 Transform에 적용)

        public Vector3 boxSize;      // Box
        public float radius;         // Sphere
        public float capRadius;      // Capsule
        public float capHeight;      // Capsule 전체 높이
        public int capDirection;     // Capsule 축 (0=X,1=Y,2=Z)
    }

    public static class CFitter
    {
        // 메인 진입점: 메시 로컬 정점 → 프리미티브 목록
        public static List<SFitResult> Fit(Mesh mesh, CFitSettings s)
        {
            var results = new List<SFitResult>();
            if (mesh == null || mesh.vertexCount == 0) { return results; }

            var verts = mesh.vertices;

            if (s.mode == EFitMode.SingleOBB)
            {
                // 전체 정점을 하나의 클러스터로 취급
                var all = new List<int>(verts.Length);
                for (int i = 0; i < verts.Length; i++) { all.Add(i); }
                results.Add(FitCluster(verts, all, s));
                return results;
            }

            // AutoSplit: voxel 격자로 분할
            var bounds = mesh.bounds;
            float maxExtent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float cell = Mathf.Max(1e-4f, maxExtent * s.VoxelRatio);

            var occupied = BuildVoxelMap(verts, bounds, cell);
            var clusters = FloodClusters(occupied);
            clusters = MergeSmallClusters(clusters, verts.Length, s);

            // 수동 분할: voxel로 안 갈리는 덩어리를 K-means로 강제 분할
            if (s.forceSplitCount > 0)
            {
                clusters = ForceSplit(verts, clusters, s.forceSplitCount);
            }

            clusters = EnforceBudget(clusters, s.EffectiveBudget);

            foreach (var cluster in clusters)
            {
                results.Add(FitCluster(verts, cluster, s));
            }
            return results;
        }

        // 가장 큰 클러스터를 K-means로 targetCount개까지 강제 분할.
        // 안경 두 알처럼 얇은 다리로 이어진 덩어리를 나눌 때 사용.
        private static List<List<int>> ForceSplit(Vector3[] verts, List<List<int>> clusters, int targetCount)
        {
            // 이미 충분히 나뉘어 있으면 그대로
            if (clusters.Count >= targetCount) { return clusters; }

            // 가장 큰 클러스터를 골라 (targetCount - 나머지수 + 1)개로 쪼갬
            clusters.Sort((a, b) => b.Count.CompareTo(a.Count));
            var biggest = clusters[0];
            int splitInto = targetCount - (clusters.Count - 1);
            if (splitInto < 2) { return clusters; }

            var parts = KMeans(verts, biggest, splitInto);

            var result = new List<List<int>>();
            result.AddRange(parts);
            for (int i = 1; i < clusters.Count; i++) { result.Add(clusters[i]); }
            return result;
        }

        // 단순 K-means (Lloyd). k개 중심으로 정점 인덱스를 분할.
        private static List<List<int>> KMeans(Vector3[] verts, List<int> idx, int k)
        {
            var centers = new Vector3[k];

            // 초기 중심: 인덱스를 k등분한 지점의 정점
            for (int c = 0; c < k; c++)
            {
                int pick = idx[(int)((long)c * idx.Count / k)];
                centers[c] = verts[pick];
            }

            var assign = new int[idx.Count];

            for (int iter = 0; iter < 12; iter++)
            {
                // 할당
                for (int i = 0; i < idx.Count; i++)
                {
                    Vector3 p = verts[idx[i]];
                    int best = 0;
                    float bestDist = float.MaxValue;
                    for (int c = 0; c < k; c++)
                    {
                        float dist = (p - centers[c]).sqrMagnitude;
                        if (dist < bestDist) { bestDist = dist; best = c; }
                    }
                    assign[i] = best;
                }

                // 중심 갱신
                var sum = new Vector3[k];
                var cnt = new int[k];
                for (int i = 0; i < idx.Count; i++)
                {
                    sum[assign[i]] += verts[idx[i]];
                    cnt[assign[i]]++;
                }
                for (int c = 0; c < k; c++)
                {
                    if (cnt[c] > 0) { centers[c] = sum[c] / cnt[c]; }
                }
            }

            // 결과 묶기 (빈 클러스터 제외)
            var buckets = new List<List<int>>();
            for (int c = 0; c < k; c++) { buckets.Add(new List<int>()); }
            for (int i = 0; i < idx.Count; i++) { buckets[assign[i]].Add(idx[i]); }

            var nonEmpty = new List<List<int>>();
            foreach (var b in buckets)
            {
                if (b.Count > 0) { nonEmpty.Add(b); }
            }
            return nonEmpty;
        }

        // 정점을 voxel 셀로 양자화
        private static Dictionary<Vector3Int, List<int>> BuildVoxelMap(Vector3[] verts, Bounds bounds, float cell)
        {
            var occupied = new Dictionary<Vector3Int, List<int>>();
            for (int i = 0; i < verts.Length; i++)
            {
                var v = verts[i];
                var key = new Vector3Int(
                    Mathf.FloorToInt((v.x - bounds.min.x) / cell),
                    Mathf.FloorToInt((v.y - bounds.min.y) / cell),
                    Mathf.FloorToInt((v.z - bounds.min.z) / cell));

                List<int> list;
                if (!occupied.TryGetValue(key, out list))
                {
                    list = new List<int>();
                    occupied[key] = list;
                }
                list.Add(i);
            }
            return occupied;
        }

        // 26-이웃 flood fill로 연결된 셀을 하나의 클러스터로 묶기
        private static List<List<int>> FloodClusters(Dictionary<Vector3Int, List<int>> occupied)
        {
            var visited = new HashSet<Vector3Int>();
            var clusters = new List<List<int>>();

            foreach (var kv in occupied)
            {
                if (visited.Contains(kv.Key)) { continue; }

                var vertIndices = new List<int>();
                var stack = new Stack<Vector3Int>();
                stack.Push(kv.Key);
                visited.Add(kv.Key);

                while (stack.Count > 0)
                {
                    var c = stack.Pop();
                    vertIndices.AddRange(occupied[c]);

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dy == 0 && dz == 0) { continue; }
                                var n = new Vector3Int(c.x + dx, c.y + dy, c.z + dz);
                                if (occupied.ContainsKey(n) && !visited.Contains(n))
                                {
                                    visited.Add(n);
                                    stack.Push(n);
                                }
                            }
                        }
                    }
                }
                clusters.Add(vertIndices);
            }
            return clusters;
        }

        // 작은 파편을 가장 큰 클러스터에 흡수 (개수 절약이 높을수록 공격적)
        private static List<List<int>> MergeSmallClusters(List<List<int>> clusters, int totalVerts, CFitSettings s)
        {
            if (clusters.Count <= 1) { return clusters; }

            float smallFrac = Mathf.Lerp(0.02f, 0.15f, s.economy);
            int threshold = Mathf.Max(1, Mathf.RoundToInt(totalVerts * smallFrac));

            clusters.Sort((a, b) => b.Count.CompareTo(a.Count));

            var big = new List<List<int>>();
            var small = new List<List<int>>();
            foreach (var c in clusters)
            {
                if (c.Count >= threshold) { big.Add(c); }
                else { small.Add(c); }
            }

            if (big.Count == 0) { return clusters; }

            foreach (var sm in small)
            {
                big[0].AddRange(sm);
            }
            return big;
        }

        // budget 초과 시 큰 순 유지, 나머지는 마지막 유지분에 병합
        private static List<List<int>> EnforceBudget(List<List<int>> clusters, int budget)
        {
            if (clusters.Count <= budget) { return clusters; }

            clusters.Sort((a, b) => b.Count.CompareTo(a.Count));
            var kept = clusters.GetRange(0, budget);

            for (int i = budget; i < clusters.Count; i++)
            {
                kept[kept.Count - 1].AddRange(clusters[i]);
            }
            return kept;
        }

        // 한 클러스터에 프리미티브 씌우기: PCA로 방향/형상 판정 + 회전 반환
        private static SFitResult FitCluster(Vector3[] verts, List<int> idx, CFitSettings s)
        {
            // 무게중심
            Vector3 mean = Vector3.zero;
            foreach (int i in idx) { mean += verts[i]; }
            mean /= idx.Count;

            // 공분산 행렬
            float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            foreach (int i in idx)
            {
                var d = verts[i] - mean;
                xx += d.x * d.x; xy += d.x * d.y; xz += d.x * d.z;
                yy += d.y * d.y; yz += d.y * d.z; zz += d.z * d.z;
            }
            int n = idx.Count;
            xx /= n; xy /= n; xz /= n; yy /= n; yz /= n; zz /= n;

            // 대칭 3x3 고유분해
            var cov = new double[3, 3] { { xx, xy, xz }, { xy, yy, yz }, { xz, yz, zz } };
            double[] eigVals;
            double[][] eigVecs;
            JacobiEigen(cov, out eigVals, out eigVecs);

            // 고유값 내림차순 → 축 길이 순위
            int[] order = { 0, 1, 2 };
            System.Array.Sort(order, (a, b) => eigVals[b].CompareTo(eigVals[a]));

            Vector3 axisLong = ToVec(eigVecs[order[0]]).normalized;
            Vector3 axisMid = ToVec(eigVecs[order[1]]).normalized;
            Vector3 axisShort = ToVec(eigVecs[order[2]]).normalized;

            // 직교성 보정 (수치 오차 방지): short = long x mid, mid = short x long
            axisShort = Vector3.Cross(axisLong, axisMid).normalized;
            if (axisShort.sqrMagnitude < 1e-6f) { axisShort = Vector3.up; }
            axisMid = Vector3.Cross(axisShort, axisLong).normalized;

            // 최소부피 각도 탐색: PCA 축(axisLong)을 회전축 삼아 나머지 두 축을 여러 각도로 돌려보고
            // 부피가 가장 작은 프레임을 채택한다. PCA가 정점 밀도에 휘둘려 부푸는 문제를 완화.
            Vector3 bestAxisLong = axisLong;
            Vector3 bestAxisMid = axisMid;
            Vector3 bestAxisShort = axisShort;
            Vector3 bestSize;
            Vector3 bestOffset;
            MeasureExtents(verts, idx, mean, axisLong, axisMid, axisShort, out bestSize, out bestOffset);
            float bestVolume = bestSize.x * bestSize.y * bestSize.z;

            int steps = s.RefineSteps;
            if (steps > 0)
            {
                // 세 축을 각각 회전축으로 삼아 탐색 (long/mid/short 주변 모두)
                Vector3[] spinAxes = { axisLong, axisMid, axisShort };
                foreach (var spin in spinAxes)
                {
                    for (int k = -steps; k <= steps; k++)
                    {
                        if (k == 0) { continue; }
                        float deg = (s.RefineRangeDeg / steps) * k;
                        Quaternion q = Quaternion.AngleAxis(deg, spin);

                        Vector3 tLong = q * axisLong;
                        Vector3 tMid = q * axisMid;
                        Vector3 tShort = q * axisShort;

                        Vector3 tSize, tOffset;
                        MeasureExtents(verts, idx, mean, tLong, tMid, tShort, out tSize, out tOffset);
                        float vol = tSize.x * tSize.y * tSize.z;

                        if (vol < bestVolume)
                        {
                            bestVolume = vol;
                            bestSize = tSize;
                            bestOffset = tOffset;
                            bestAxisLong = tLong;
                            bestAxisMid = tMid;
                            bestAxisShort = tShort;
                        }
                    }
                }
            }

            // 탐색 결과 채택. 축 길이 순서가 바뀌었을 수 있으므로 size 기준으로 재정렬.
            axisLong = bestAxisLong;
            axisMid = bestAxisMid;
            axisShort = bestAxisShort;
            ReorderByExtent(ref axisLong, ref axisMid, ref axisShort, ref bestSize, ref bestOffset);

            Vector3 size = bestSize;              // (long, mid, short) 폭
            Vector3 localOffset = bestOffset;

            // 회전: long→X, mid→Y, short→Z로 매핑하는 회전 행렬 구성
            // Quaternion.LookRotation(forward=Z축, up=Y축) 사용
            Quaternion rotation = Quaternion.LookRotation(axisShort, axisMid);

            // 로컬 중심 = 무게중심 + 축가중 오프셋
            Vector3 center = mean
                + axisLong * localOffset.x
                + axisMid * localOffset.y
                + axisShort * localOffset.z;

            // slack 적용: +면 수축(튀어나옴 허용), -면 팽창(감싸기)
            float scale = 1f - s.slack * 2f;  // slack +0.1 → 0.8배, -0.1 → 1.2배
            size *= Mathf.Max(0.01f, scale);

            // 회전 프레임 기준으로 size를 (X=long, Y=mid, Z=short)에 대응
            float axisX = Mathf.Max(size.x, 1e-5f);  // long
            float axisY = Mathf.Max(size.y, 1e-5f);  // mid
            float axisZ = Mathf.Max(size.z, 1e-5f);  // short

            // 구 판정: 세 축이 모두 비슷
            bool nearlyEqual =
                Mathf.Abs(axisX - axisY) / axisX < s.sphereTolerance &&
                Mathf.Abs(axisX - axisZ) / axisX < s.sphereTolerance;

            // 캡슐 판정: 긴축이 충분히 길고(aspect), 나머지 두 축(중간·짧은)이 서로 비슷해야 함.
            // 이 두 번째 조건이 없으면 책 같은 판때기(긴·중 크고 짧은만 작음)가 캡슐로 샌다.
            float longAspect = axisX / axisZ;             // 긴축 / 짧은축
            float crossSectionRatio = axisY / axisZ;      // 중간축 / 짧은축 (1에 가까우면 단면이 원형=막대기)
            bool isRodLike = longAspect >= s.capsuleAspect
                && crossSectionRatio < s.capsuleAspect;   // 중간축이 짧은축의 capsuleAspect배 미만이면 막대기

            var r = new SFitResult { center = center, rotation = rotation };

            if (s.allowSphere && nearlyEqual)
            {
                r.kind = EPrimitiveKind.Sphere;
                r.radius = (axisX + axisY + axisZ) / 6f;
            }
            else if (s.allowCapsule && isRodLike)
            {
                r.kind = EPrimitiveKind.Capsule;
                r.capDirection = 0;  // 긴 축(long)을 X에 매핑했으므로
                r.capRadius = (axisY + axisZ) * 0.25f;
                r.capHeight = axisX;
            }
            else if (s.allowBox)
            {
                r.kind = EPrimitiveKind.Box;
                r.boxSize = new Vector3(axisX, axisY, axisZ);
            }
            else
            {
                r.kind = EPrimitiveKind.Box;
                r.boxSize = new Vector3(axisX, axisY, axisZ);
            }

            return r;
        }

        private static Vector3 ToVec(double[] v)
        {
            return new Vector3((float)v[0], (float)v[1], (float)v[2]);
        }

        // 주어진 축 프레임에 정점을 투영해 각 축 폭(size)과 중심 오프셋을 구한다.
        private static void MeasureExtents(Vector3[] verts, List<int> idx, Vector3 mean,
            Vector3 axisA, Vector3 axisB, Vector3 axisC, out Vector3 size, out Vector3 offset)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (int i in idx)
            {
                var d = verts[i] - mean;
                float pA = Vector3.Dot(d, axisA);
                float pB = Vector3.Dot(d, axisB);
                float pC = Vector3.Dot(d, axisC);
                min = Vector3.Min(min, new Vector3(pA, pB, pC));
                max = Vector3.Max(max, new Vector3(pA, pB, pC));
            }
            size = max - min;
            offset = (max + min) * 0.5f;
        }

        // 축 길이 순서(long ≥ mid ≥ short)가 깨졌으면 축/size/offset을 함께 재정렬.
        private static void ReorderByExtent(ref Vector3 axisLong, ref Vector3 axisMid, ref Vector3 axisShort,
            ref Vector3 size, ref Vector3 offset)
        {
            // (축, 폭, 오프셋성분)을 묶어 폭 내림차순 정렬
            var items = new List<KeyValuePair<float, KeyValuePair<Vector3, float>>>(3)
            {
                new KeyValuePair<float, KeyValuePair<Vector3, float>>(size.x, new KeyValuePair<Vector3, float>(axisLong, offset.x)),
                new KeyValuePair<float, KeyValuePair<Vector3, float>>(size.y, new KeyValuePair<Vector3, float>(axisMid, offset.y)),
                new KeyValuePair<float, KeyValuePair<Vector3, float>>(size.z, new KeyValuePair<Vector3, float>(axisShort, offset.z)),
            };
            items.Sort((a, b) => b.Key.CompareTo(a.Key));

            axisLong = items[0].Value.Key;
            axisMid = items[1].Value.Key;
            axisShort = items[2].Value.Key;
            size = new Vector3(items[0].Key, items[1].Key, items[2].Key);
            offset = new Vector3(items[0].Value.Value, items[1].Value.Value, items[2].Value.Value);
        }

        // 대칭 3x3 행렬 Jacobi 고유분해
        private static void JacobiEigen(double[,] a, out double[] eigVals, out double[][] eigVecs)
        {
            const int DIM = 3;
            var v = new double[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

            for (int iter = 0; iter < 50; iter++)
            {
                // 가장 큰 비대각 성분 위치
                int p = 0, q = 1;
                double maxOff = 0;
                for (int i = 0; i < DIM; i++)
                {
                    for (int j = i + 1; j < DIM; j++)
                    {
                        if (System.Math.Abs(a[i, j]) > maxOff)
                        {
                            maxOff = System.Math.Abs(a[i, j]);
                            p = i; q = j;
                        }
                    }
                }

                if (maxOff < 1e-12) { break; }

                double app = a[p, p], aqq = a[q, q], apq = a[p, q];
                double phi = 0.5 * System.Math.Atan2(2 * apq, aqq - app);
                double c = System.Math.Cos(phi), sn = System.Math.Sin(phi);

                for (int i = 0; i < DIM; i++)
                {
                    double aip = a[i, p], aiq = a[i, q];
                    a[i, p] = c * aip - sn * aiq;
                    a[i, q] = sn * aip + c * aiq;
                }
                for (int i = 0; i < DIM; i++)
                {
                    double api = a[p, i], aqi = a[q, i];
                    a[p, i] = c * api - sn * aqi;
                    a[q, i] = sn * api + c * aqi;
                }
                for (int i = 0; i < DIM; i++)
                {
                    double vip = v[i, p], viq = v[i, q];
                    v[i, p] = c * vip - sn * viq;
                    v[i, q] = sn * vip + c * viq;
                }
            }

            eigVals = new double[3] { a[0, 0], a[1, 1], a[2, 2] };
            eigVecs = new double[3][];
            for (int col = 0; col < 3; col++)
            {
                eigVecs[col] = new double[3] { v[0, col], v[1, col], v[2, col] };
            }
        }
    }
}
