// CColliderFitter.cs
using System.Collections.Generic;
using UnityEngine;

namespace ColliderFitter
{
    public enum EPrimitiveKind { Box, Capsule, Sphere }

    [System.Serializable]
    public class CFitSettings
    {
        [Range(-0.1f, 0.1f)] public float slack = 0f;
        [Range(1.0f, 4.0f)] public float capsuleAspect = 1.8f;
        [Range(0f, 0.3f)] public float sphereTolerance = 0.15f;

        public bool allowBox = true;
        public bool allowCapsule = true;
        public bool allowSphere = true;
        public bool refineOBB = true;

        public int RefineSteps => refineOBB ? 8 : 0;
        public float RefineRangeDeg => 20f;
    }

    public struct SFitResult
    {
        public EPrimitiveKind kind;
        public Vector3 center;
        public Quaternion rotation;
        public Vector3 boxSize;
        public float radius;
        public float capRadius;
        public float capHeight;
        public int capDirection;
    }

    public static class CFitter
    {
        // 선택된 정점 인덱스들만 사용하여 프리미티브를 피팅합니다.
        public static SFitResult FitSelection(Vector3[] verts, HashSet<int> selectedIndices, CFitSettings s)
        {
            if (selectedIndices.Count == 0) return new SFitResult { kind = EPrimitiveKind.Box, boxSize = Vector3.one };

            // 무게중심 계산 (루프 최적화)
            Vector3 mean = Vector3.zero;
            foreach (int i in selectedIndices) { mean += verts[i]; }
            mean /= selectedIndices.Count;

            // 공분산 행렬 계산
            float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            foreach (int i in selectedIndices)
            {
                Vector3 d = verts[i] - mean;
                xx += d.x * d.x; xy += d.x * d.y; xz += d.x * d.z;
                yy += d.y * d.y; yz += d.y * d.z; zz += d.z * d.z;
            }

            int n = selectedIndices.Count;
            xx /= n; xy /= n; xz /= n; yy /= n; yz /= n; zz /= n;

            var cov = new double[3, 3] { { xx, xy, xz }, { xy, yy, yz }, { xz, yz, zz } };
            JacobiEigen(cov, out double[] eigVals, out double[][] eigVecs);

            int[] order = { 0, 1, 2 };
            System.Array.Sort(order, (a, b) => eigVals[b].CompareTo(eigVals[a]));

            Vector3 axisLong = ToVec(eigVecs[order[0]]).normalized;
            Vector3 axisMid = ToVec(eigVecs[order[1]]).normalized;
            Vector3 axisShort = ToVec(eigVecs[order[2]]).normalized;

            axisShort = Vector3.Cross(axisLong, axisMid).normalized;
            if (axisShort.sqrMagnitude < 1e-6f) { axisShort = Vector3.up; }
            axisMid = Vector3.Cross(axisShort, axisLong).normalized;

            // 정점 투영 및 바운딩 박스 최적화
            MeasureExtents(verts, selectedIndices, mean, axisLong, axisMid, axisShort, out Vector3 bestSize, out Vector3 bestOffset);
            float bestVolume = bestSize.x * bestSize.y * bestSize.z;

            int steps = s.RefineSteps;
            if (steps > 0)
            {
                Vector3[] spinAxes = { axisLong, axisMid, axisShort };
                foreach (var spin in spinAxes)
                {
                    for (int k = -steps; k <= steps; k++)
                    {
                        if (k == 0) continue;
                        float deg = (s.RefineRangeDeg / steps) * k;
                        Quaternion q = Quaternion.AngleAxis(deg, spin);

                        Vector3 tLong = q * axisLong;
                        Vector3 tMid = q * axisMid;
                        Vector3 tShort = q * axisShort;

                        MeasureExtents(verts, selectedIndices, mean, tLong, tMid, tShort, out Vector3 tSize, out Vector3 tOffset);
                        float vol = tSize.x * tSize.y * tSize.z;

                        if (vol < bestVolume)
                        {
                            bestVolume = vol;
                            bestSize = tSize;
                            bestOffset = tOffset;
                            axisLong = tLong;
                            axisMid = tMid;
                            axisShort = tShort;
                        }
                    }
                }
            }

            ReorderByExtent(ref axisLong, ref axisMid, ref axisShort, ref bestSize, ref bestOffset);

            Quaternion rotation = Quaternion.LookRotation(axisShort, axisMid);
            Vector3 center = mean + (axisLong * bestOffset.x) + (axisMid * bestOffset.y) + (axisShort * bestOffset.z);

            float scale = 1f - s.slack * 2f;
            Vector3 size = bestSize * Mathf.Max(0.01f, scale);

            float axisX = Mathf.Max(size.x, 1e-5f);
            float axisY = Mathf.Max(size.y, 1e-5f);
            float axisZ = Mathf.Max(size.z, 1e-5f);

            bool nearlyEqual = Mathf.Abs(axisX - axisY) / axisX < s.sphereTolerance && Mathf.Abs(axisX - axisZ) / axisX < s.sphereTolerance;
            float longAspect = axisX / axisZ;
            float crossSectionRatio = axisY / axisZ;
            bool isRodLike = longAspect >= s.capsuleAspect && crossSectionRatio < s.capsuleAspect;

            var r = new SFitResult { center = center, rotation = rotation };

            if (s.allowSphere && nearlyEqual)
            {
                r.kind = EPrimitiveKind.Sphere;
                r.radius = (axisX + axisY + axisZ) / 6f;
            }
            else if (s.allowCapsule && isRodLike)
            {
                r.kind = EPrimitiveKind.Capsule;
                r.capDirection = 0;
                r.capRadius = (axisY + axisZ) * 0.25f;
                r.capHeight = axisX;
            }
            else
            {
                r.kind = EPrimitiveKind.Box;
                r.boxSize = new Vector3(axisX, axisY, axisZ);
            }

            return r;
        }

        private static Vector3 ToVec(double[] v) => new Vector3((float)v[0], (float)v[1], (float)v[2]);

        private static void MeasureExtents(Vector3[] verts, HashSet<int> idx, Vector3 mean, Vector3 axisA, Vector3 axisB, Vector3 axisC, out Vector3 size, out Vector3 offset)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (int i in idx)
            {
                Vector3 d = verts[i] - mean;
                float pA = Vector3.Dot(d, axisA);
                float pB = Vector3.Dot(d, axisB);
                float pC = Vector3.Dot(d, axisC);

                if (pA < min.x) min.x = pA; if (pA > max.x) max.x = pA;
                if (pB < min.y) min.y = pB; if (pB > max.y) max.y = pB;
                if (pC < min.z) min.z = pC; if (pC > max.z) max.z = pC;
            }
            size = max - min;
            offset = (max + min) * 0.5f;
        }

        private static void ReorderByExtent(ref Vector3 axisLong, ref Vector3 axisMid, ref Vector3 axisShort, ref Vector3 size, ref Vector3 offset)
        {
            var items = new List<KeyValuePair<float, KeyValuePair<Vector3, float>>>(3)
            {
                new KeyValuePair<float, KeyValuePair<Vector3, float>>(size.x, new KeyValuePair<Vector3, float>(axisLong, offset.x)),
                new KeyValuePair<float, KeyValuePair<Vector3, float>>(size.y, new KeyValuePair<Vector3, float>(axisMid, offset.y)),
                new KeyValuePair<float, KeyValuePair<Vector3, float>>(size.z, new KeyValuePair<Vector3, float>(axisShort, offset.z)),
            };
            items.Sort((a, b) => b.Key.CompareTo(a.Key));

            axisLong = items[0].Value.Key; axisMid = items[1].Value.Key; axisShort = items[2].Value.Key;
            size = new Vector3(items[0].Key, items[1].Key, items[2].Key);
            offset = new Vector3(items[0].Value.Value, items[1].Value.Value, items[2].Value.Value);
        }

        private static void JacobiEigen(double[,] a, out double[] eigVals, out double[][] eigVecs)
        {
            var v = new double[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            for (int iter = 0; iter < 50; iter++)
            {
                int p = 0, q = 1;
                double maxOff = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = i + 1; j < 3; j++)
                    {
                        if (System.Math.Abs(a[i, j]) > maxOff) { maxOff = System.Math.Abs(a[i, j]); p = i; q = j; }
                    }
                }

                if (maxOff < 1e-12) break;

                double app = a[p, p], aqq = a[q, q], apq = a[p, q];
                double phi = 0.5 * System.Math.Atan2(2 * apq, aqq - app);
                double c = System.Math.Cos(phi), sn = System.Math.Sin(phi);

                for (int i = 0; i < 3; i++)
                {
                    double aip = a[i, p], aiq = a[i, q];
                    a[i, p] = c * aip - sn * aiq; a[i, q] = sn * aip + c * aiq;

                    double api = a[p, i], aqi = a[q, i];
                    a[p, i] = c * api - sn * aqi; a[q, i] = sn * api + c * aqi;

                    double vip = v[i, p], viq = v[i, q];
                    v[i, p] = c * vip - sn * viq; v[i, q] = sn * vip + c * viq;
                }
            }

            eigVals = new double[3] { a[0, 0], a[1, 1], a[2, 2] };
            eigVecs = new double[3][] {
                new double[3] { v[0, 0], v[1, 0], v[2, 0] },
                new double[3] { v[0, 1], v[1, 1], v[2, 1] },
                new double[3] { v[0, 2], v[1, 2], v[2, 2] }
            };
        }
    }
}
