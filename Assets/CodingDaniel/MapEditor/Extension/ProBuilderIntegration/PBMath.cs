using UnityEngine;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public static class PBMath
    {
        /// <summary>
        /// Epsilon to use when comparing vertex positions for equality.
        /// </summary>
        const float FltCompareEpsilon = .0001f;

        public static Vector2 DivideBy(this Vector2 v, Vector2 o)
        {
            return new Vector2(v.x / o.x, v.y / o.y);
        }

        /// <summary>
        /// Returns a new point by rotating the Vector2 around an origin point.
        /// </summary>
        /// <param name="v">Vector2 original point.</param>
        /// <param name="origin">The pivot to rotate around.</param>
        /// <param name="theta">How far to rotate in degrees.</param>
        /// <returns></returns>
        public static Vector2 RotateAroundPoint(this Vector2 v, Vector2 origin, float theta)
        {
            float cx = origin.x, cy = origin.y; // origin
            float px = v.x, py = v.y;           // point

            float s = Mathf.Sin(theta * Mathf.Deg2Rad);
            float c = Mathf.Cos(theta * Mathf.Deg2Rad);

            // translate point back to origin:
            px -= cx;
            py -= cy;

            // rotate point
            float xnew = px * c + py * s;
            float ynew = -px * s + py * c;

            // translate point back:
            px = xnew + cx;
            py = ynew + cy;

            return new Vector2(px, py);
        }

        public static float SqDistanceTo(Vector2 l0, Vector2 l1, Vector2 p)
        {
            float sqLineLength = (l0 - l1).sqrMagnitude;
            if (sqLineLength == 0.0)
            {
                return (p - l0).sqrMagnitude;
            }
            float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(p - l0, l1 - l0) / sqLineLength));
            Vector2 projection = l0 + t * (l1 - l0);
            return (p - projection).sqrMagnitude;
        }

        public static bool Intersects(Vector2 min, Vector2 max, Vector2 uv0, Vector2 uv1)
        {
            if (min.x <= uv0.x && min.y <= uv0.y && uv0.x <= max.x && uv0.y <= max.y ||
               min.x <= uv1.x && min.y <= uv1.y && uv1.x <= max.x && uv1.y <= max.y)
            {
                return true;
            }

            Vector2 p0 = min;
            Vector2 p1 = new Vector2(max.x, min.y);
            Vector2 p2 = max;
            Vector3 p3 = new Vector2(min.x, max.y);

            return SegIntersects(p0, p1, uv0, uv1) ||
                SegIntersects(p1, p2, uv0, uv1) ||
                SegIntersects(p2, p3, uv0, uv1) ||
                SegIntersects(p3, p0, uv0, uv1);
        }

        public static bool SegIntersects(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            Vector2 b = a2 - a1;
            Vector2 d = b2 - b1;
            float bDotDPerp = b.x * d.y - b.y * d.x;

            if (bDotDPerp == 0)
            {
                return false;
            }

            Vector2 c = b1 - a1;
            float t = (c.x * d.y - c.y * d.x) / bDotDPerp;
            if (t < 0 || t > 1)
                return false;

            float u = (c.x * b.y - c.y * b.x) / bDotDPerp;
            if (u < 0 || u > 1)
                return false;

            return true;
        }

        /// <summary>
        /// Compares two Vector2 values component-wise, allowing for a margin of error.
        /// </summary>
        /// <param name="a">First Vector2 value.</param>
        /// <param name="b">Second Vector2 value.</param>
        /// <param name="delta">The maximum difference between components allowed.</param>
        /// <returns>True if a and b components are respectively within delta distance of one another.</returns>
        internal static bool Approx2(this Vector2 a, Vector2 b, float delta = FltCompareEpsilon)
        {
            return
                Mathf.Abs(a.x - b.x) < delta &&
                Mathf.Abs(a.y - b.y) < delta;
        }

        /// <summary>
        /// Compares two Vector3 values component-wise, allowing for a margin of error.
        /// </summary>
        /// <param name="a">First Vector3 value.</param>
        /// <param name="b">Second Vector3 value.</param>
        /// <param name="delta">The maximum difference between components allowed.</param>
        /// <returns>True if a and b components are respectively within delta distance of one another.</returns>
        internal static bool Approx3(this Vector3 a, Vector3 b, float delta = FltCompareEpsilon)
        {
            return
                Mathf.Abs(a.x - b.x) < delta &&
                Mathf.Abs(a.y - b.y) < delta &&
                Mathf.Abs(a.z - b.z) < delta;
        }

        /// <summary>
        /// Compares two Vector4 values component-wise, allowing for a margin of error.
        /// </summary>
        /// <param name="a">First Vector4 value.</param>
        /// <param name="b">Second Vector4 value.</param>
        /// <param name="delta">The maximum difference between components allowed.</param>
        /// <returns>True if a and b components are respectively within delta distance of one another.</returns>

        internal static bool Approx4(this Vector4 a, Vector4 b, float delta = FltCompareEpsilon)
        {
            return
                Mathf.Abs(a.x - b.x) < delta &&
                Mathf.Abs(a.y - b.y) < delta &&
                Mathf.Abs(a.z - b.z) < delta &&
                Mathf.Abs(a.w - b.w) < delta;
        }
    }
}