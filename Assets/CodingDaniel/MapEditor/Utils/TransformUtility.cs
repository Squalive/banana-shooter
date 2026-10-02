using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CodingDaniel.MapEditor.Utils
{
    public static class TransformUtility
    {
        public static bool ScreenRectToLocalRectInRectangle(RectTransform rt, Rect screenRect, Camera cam, out Rect localRect)
        {
            Vector2 min, max;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenRect.min, cam, out min) &&
               RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenRect.max, cam, out max))
            {
                localRect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
                return true;
            }

            localRect = Rect.zero;
            return false;
        }

        public static Rect BoundsToScreenRect(Camera cam, Bounds[] bounds, bool isInFrustumCheck)
        {
            if(!isInFrustumCheck)
            {
                return BoundsToScreenRect(cam, bounds);
            }

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;
            
            int i;
            for(i = 0; i < bounds.Length; ++i)
            {
                if (GeometryUtility.TestPlanesAABB(planes, bounds[i]))
                {
                    Rect rect = BoundsToScreenRect(cam, bounds[i]);
                    min = rect.min;
                    max = rect.max;
                    break;
                }
            }

            for (i = 0; i < bounds.Length; ++i)
            {
                if (!GeometryUtility.TestPlanesAABB(planes, bounds[i]))
                {
                    continue;
                }

                Rect rect = BoundsToScreenRect(cam, bounds[i]);
                min.x = Mathf.Min(rect.min.x, min.x);
                min.y = Mathf.Min(rect.min.y, min.y);
                max.x = Mathf.Max(rect.max.x, max.x);
                max.y = Mathf.Max(rect.max.y, max.y);
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        public static Rect BoundsToScreenRect(Camera cam, Bounds[] bounds)
        {
            if(bounds.Length == 0)
            {
                return Rect.zero;
            }

            Rect rect = BoundsToScreenRect(cam, bounds[0]);
            Vector2 min = rect.min;
            Vector2 max = rect.max;

            for (int i = 1; i < bounds.Length; ++i)
            {
                rect = BoundsToScreenRect(cam, bounds[i]);
                min.x = Mathf.Min(rect.min.x, min.x);
                min.y = Mathf.Min(rect.min.y, min.y);
                max.x = Mathf.Max(rect.max.x, max.x);
                max.y = Mathf.Max(rect.max.y, max.y);
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        public static Rect BoundsToScreenRect(Camera cam, Bounds bounds)
        {
            Vector3 cen = bounds.center;
            Vector3 ext = bounds.extents;

            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z - ext.z));
            Vector2 max = min;

            Vector2 point = min;
            GetMinMax(point, ref min, ref max);

            point = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z - ext.z));
            GetMinMax(point, ref min, ref max);

            point = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(cen.x - ext.x, cen.y - ext.y, cen.z + ext.z));
            GetMinMax(point, ref min, ref max);

            point = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(cen.x + ext.x, cen.y - ext.y, cen.z + ext.z));
            GetMinMax(point, ref min, ref max);

            point = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z - ext.z));
            GetMinMax(point, ref min, ref max);

            point = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z - ext.z));
            GetMinMax(point, ref min, ref max);

            point = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(cen.x - ext.x, cen.y + ext.y, cen.z + ext.z));
            GetMinMax(point, ref min, ref max);

            point = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(cen.x + ext.x, cen.y + ext.y, cen.z + ext.z));
            GetMinMax(point, ref min, ref max);

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        private static void GetMinMax(Vector2 point, ref Vector2 min, ref Vector2 max)
        {
            min = new Vector2(min.x >= point.x ? point.x : min.x, min.y >= point.y ? point.y : min.y);
            max = new Vector2(max.x <= point.x ? point.x : max.x, max.y <= point.y ? point.y : max.y);
        }

        public static Vector3 GetCenter(this Transform target)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                return target.TransformPoint(filter.sharedMesh.bounds.center);
            }

            SkinnedMeshRenderer smr = target.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                return target.TransformPoint(smr.sharedMesh.bounds.center);
            }

            return target.position;
        }

        public static Vector3 GetCommonCenter(IList<Transform> transforms)
        {
            Vector3 centerPosition = GetCenter(transforms[0]);
            for (int i = 1; i < transforms.Count; ++i)
            {
                Transform target = transforms[i];
                centerPosition += GetCenter(target);
            }

            centerPosition = centerPosition / transforms.Count;
            return centerPosition;
        }

        public static Vector3 CenterPoint(Vector3[] vectors)
        {
            Vector3 sum = Vector3.zero;
            if (vectors == null || vectors.Length == 0)
            {
                return sum;
            }

            foreach (Vector3 vec in vectors)
            {
                sum += vec;
            }
            return sum / vectors.Length;
        }

        public static Bounds CalculateBounds(Transform[] transforms)
        {
            CalculateBoundsResult result = new CalculateBoundsResult();
            for (int i = 0; i < transforms.Length; ++i)
            {
                Transform t = transforms[i];
                CalculateBounds(t, result);
            }

            if (result.Initialized)
            {
                return result.Bounds;
            }

            Vector3 center = CenterPoint(transforms.Select(t => t.position).ToArray());
            return new Bounds(center, Vector3.zero);
        }

        private static CalculateBoundsResult _result = new CalculateBoundsResult();
        public static Bounds CalculateBounds(Transform transform)
        {
            _result.Initialized = false;

            CalculateBounds(transform, _result);

            if (_result.Initialized)
            {
                return _result.Bounds;
            }

            return new Bounds(transform.position, Vector3.zero);
        }

        private class CalculateBoundsResult
        {
            public Bounds Bounds;
            public bool Initialized;
        }

        private static void CalculateBounds(Transform t, CalculateBoundsResult result)
        {
            if (t is RectTransform)
            {
                CalculateBounds((RectTransform)t, result);
            }
            else
            {
                Renderer renderer = t.GetComponent<Renderer>();
                if (renderer != null)
                {
                    CalculateBounds(renderer, result);
                }
            }

            foreach (Transform child in t)
            {
                CalculateBounds(child, result);
            }
        }
        private static void CalculateBounds(RectTransform rt, CalculateBoundsResult result)
        {
            Bounds relativeBounds = TransformExtensions.CalculateRelativeRectTransformBounds(rt);
            Matrix4x4 localToWorldMatrix = rt.localToWorldMatrix;
            Bounds bounds = TransformBounds(ref localToWorldMatrix, ref relativeBounds);
            if (!result.Initialized)
            {
                result.Bounds = bounds;
                result.Initialized = true;
            }
            else
            {
                result.Bounds.Encapsulate(bounds.min);
                result.Bounds.Encapsulate(bounds.max);
            }
        }

        private static void CalculateBounds(Renderer renderer, CalculateBoundsResult result)
        {
            if (renderer is ParticleSystemRenderer)
            {
                return;
            }

            Bounds bounds = renderer.bounds;
            if (bounds.size == Vector3.zero && bounds.center != renderer.transform.position)
            {
                Matrix4x4 localToWorldMatrix = renderer.transform.localToWorldMatrix;
                bounds = TransformBounds(ref localToWorldMatrix, ref bounds);
            }

            if (!result.Initialized)
            {
                result.Bounds = bounds;
                result.Initialized = true;
            }
            else
            {
                result.Bounds.Encapsulate(bounds.min);
                result.Bounds.Encapsulate(bounds.max);
            }
        }

        public static Bounds TransformBounds(ref Matrix4x4 matrix, ref Bounds bounds)
        {
            var center = matrix.MultiplyPoint(bounds.center);

            // transform the local extents' axes
            var extents = bounds.extents;
            var axisX = matrix.MultiplyVector(new Vector3(extents.x, 0, 0));
            var axisY = matrix.MultiplyVector(new Vector3(0, extents.y, 0));
            var axisZ = matrix.MultiplyVector(new Vector3(0, 0, extents.z));

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds { center = center, extents = extents };
        }

    }
}


