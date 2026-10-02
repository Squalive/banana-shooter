using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.Interaction.Rendering;
using CodingDaniel.MapEditor.MECommon;
using UnityEngine;

namespace CodingDaniel.MapEditor.Interaction.Picking
{
    /// <summary>
    /// Axis picking for the move, rotate and scale handles.
    ///
    /// These were methods on MEHandleComponent; they read the same appearance but need no geometry or
    /// materials. HitCentre lost its dead <c>virtual</c> - nothing ever overrode it.
    ///
    /// Picking maths is unchanged from the original.
    /// </summary>
    public sealed class HandleHitTester
    {
        private readonly IHandleAppearance _appearance;

        public HandleHitTester(IHandleAppearance appearance)
        {
            _appearance = appearance;
        }

        private const float innerRadius = 1.0f;
        private const float outerRadius = 1.2f;

        private Transform HostTransform => _appearance.HostTransform;
        private float HandleScale => _appearance.HandleScale;
        private float SelectionMargin => _appearance.SelectionMargin;
        private float SelectionMarginPixels => _appearance.SelectionMarginPixels;
        private bool PositionHandleArrowOnly => _appearance.PositionHandleArrowOnly;
        private bool InvertZAxis => _appearance.InvertZAxis;
        private Vector3 Forward => _appearance.Forward;

        private static float GetScreenScale(Vector3 position, Camera camera)
        {
            return GraphicsUtility.GetScreenScale(position, camera);
        }

        public HandleAxis HitTestPositionHandle(Camera camera, Ray ray, HandleDrawSettings settings, out float distance)
        {
            LockObject lockObject = settings.LockObject;
            Vector3 position = settings.Position;
            Quaternion rotation = settings.Rotation;

            Matrix4x4 _matrix = Matrix4x4.TRS(position, rotation, InvertZAxis ? new Vector3(1, 1, -1) : Vector3.one);
            
            float scale = GetScreenScale(position, camera);
            if (!PositionHandleArrowOnly)
            {
                float s = 0.23f * scale;

                if (lockObject == null || !lockObject.PositionX && !lockObject.PositionZ)
                {
                    if (HitQuad(camera, ray, position, Vector3.up, _matrix, s * HandleScale, out distance))
                    {
                        return HandleAxis.XZ;
                    }
                }

                if (lockObject == null || !lockObject.PositionY && !lockObject.PositionZ)
                {
                    if (HitQuad(camera, ray, position, Vector3.right, _matrix, s * HandleScale, out distance))
                    {
                        return HandleAxis.YZ;
                    }
                }

                if (lockObject == null || !lockObject.PositionX && !lockObject.PositionY)
                {
                    if (HitQuad(camera, ray, position, Vector3.forward, _matrix, s * HandleScale, out distance))
                    {
                        return HandleAxis.XY;
                    }
                }
            }

            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, new Vector3(scale, scale, scale));
            float distToYAxis = float.MaxValue;
            float distToZAxis = float.MaxValue;
            float distToXAxis = float.MaxValue;
            bool hit = (lockObject == null || !lockObject.PositionY) && HitAxis(camera, ray, Vector3.up * HandleScale, matrix, out distToYAxis);
            hit |= (lockObject == null || !lockObject.PositionZ) && HitAxis(camera, ray, Forward * HandleScale, matrix, out distToZAxis);
            hit |= (lockObject == null || !lockObject.PositionX) && HitAxis(camera, ray, Vector3.right * HandleScale, matrix, out distToXAxis);

            if (hit)
            {
                if (distToYAxis <= distToZAxis && distToYAxis <= distToXAxis)
                {
                    distance = distToYAxis;
                    return HandleAxis.Y;
                }
                else if (distToXAxis <= distToYAxis && distToXAxis <= distToZAxis)
                {
                    distance = distToXAxis;
                    return HandleAxis.X;
                }
                else
                {
                    distance = distToZAxis;
                    return HandleAxis.Z;
                }
            }

            distance = float.PositiveInfinity;
            return HandleAxis.None;
        }
        
        private bool HitQuad(Camera camera, Ray ray, Vector3 position, Vector3 axis, Matrix4x4 matrix, float size, out float distance)
        {
            Plane plane = new Plane(matrix.MultiplyVector(axis).normalized, matrix.MultiplyPoint(Vector3.zero));

            if (!plane.Raycast(ray, out distance))
            {
                return false;
            }

            Vector3 point = ray.GetPoint(distance);
            point = matrix.inverse.MultiplyPoint(point);

            Vector3 toCam = matrix.inverse.MultiplyVector(camera.transform.position - position);

            float fx = Mathf.Sign(Vector3.Dot(toCam, Vector3.right));
            float fy = Mathf.Sign(Vector3.Dot(toCam, Vector3.up));
            float fz = Mathf.Sign(Vector3.Dot(toCam, Vector3.forward));

            point.x *= fx;
            point.y *= fy;
            point.z *= fz;

            float lowBound = -0.01f;

            bool result = point.x >= lowBound && point.x <= size && point.y >= lowBound && point.y <= size && point.z >= lowBound && point.z <= size;
            return result;
        }
        
        private bool HitAxis(Camera camera, Ray ray, Vector3 axis, Matrix4x4 matrix, out float distanceToAxis)
        {
            Vector3 position = matrix.GetColumn(3);

            axis = matrix.MultiplyVector(axis);
            Vector2 screenVectorBegin = camera.WorldToScreenPoint(position);
            Vector2 screenVectorEnd = camera.WorldToScreenPoint(axis + position);
            Vector3 screenVector = screenVectorEnd - screenVectorBegin;
            float screenVectorMag = screenVector.magnitude;
            screenVector.Normalize();

            Vector2 screenPosition;
            if(!GetScreenPosition(camera, ray, position, out screenPosition))
            {
                distanceToAxis = float.PositiveInfinity;
                return false;
            }

            if (screenVector != Vector3.zero)
            {
                return HitScreenAxis(screenPosition, screenVectorBegin, screenVector, screenVectorMag, out distanceToAxis);
            }
            else
            {
                distanceToAxis = (screenVectorBegin - screenPosition).magnitude;
                bool result = distanceToAxis <= SelectionMargin * SelectionMarginPixels;
                if (!result)
                {
                    distanceToAxis = float.PositiveInfinity;
                }
                else
                {
                    distanceToAxis = 0.0f;
                }
                return result;
            }

        }
        
        private bool HitScreenAxis(Vector2 screenPosition, Vector2 screenVectorBegin, Vector3 screenVector, float screenVectorMag, out float distanceToAxis)
        {
            Vector2 perp = PerpendicularClockwise(screenVector).normalized;
            Vector2 relMousePositon = screenPosition - screenVectorBegin;

            distanceToAxis = Mathf.Abs(Vector2.Dot(perp, relMousePositon));
            Vector2 hitPoint = (relMousePositon - perp * distanceToAxis);
            float vectorSpaceCoord = Vector2.Dot(screenVector, hitPoint);

            float selectionMargin = SelectionMargin * SelectionMarginPixels;
            bool result = vectorSpaceCoord <= screenVectorMag + selectionMargin && vectorSpaceCoord >= -selectionMargin && distanceToAxis <= selectionMargin;
            if (!result)
            {
                distanceToAxis = float.PositiveInfinity;
            }
            else
            {
                if (screenVectorMag < selectionMargin)
                {
                    distanceToAxis = 0.0f;
                }
            }
            return result;
        }

        private static Vector2 PerpendicularClockwise(Vector2 vector2)
        {
            return new Vector2(-vector2.y, vector2.x);
        }
        private bool GetScreenPosition(Camera camera, Ray ray, Vector3 position, out Vector2 screenPosition)
        {
            Plane plane = new Plane(-camera.transform.forward, position);
            float distance;
            if (!plane.Raycast(ray, out distance))
            {
                screenPosition = Vector2.zero;
                return false;
            }

            screenPosition = camera.WorldToScreenPoint(ray.GetPoint(distance));
            return true;
        }
        

        public HandleAxis HitTestRotationHandle(Camera camera, Ray ray, HandleDrawSettings settings, out float distance)
        {
            Vector3 position = settings.Position;
            Quaternion startingRotationInv = Quaternion.identity;
            float hit1Distance;
            float hit2Distance;
            float scale = GetScreenScale(position, camera) * HandleScale;
            if (Intersect(ray, position, outerRadius * scale, out hit1Distance, out hit2Distance))
            {
                HandleAxis selectedAxis = HitAxis(camera, ray, settings, startingRotationInv,  out distance);
                Vector3 axis = Vector3.zero;
                switch (selectedAxis)
                {
                    case HandleAxis.X:
                        axis = Vector3.right;
                        break;
                    case HandleAxis.Y:
                        axis = Vector3.up;
                        break;
                    case HandleAxis.Z:
                        axis = Vector3.forward;
                        break;
                }

                Vector3 dpHitPoint;
                GetPointOnDragPlane(GetDragPlane(camera, axis, settings), ray, out dpHitPoint);

                if (selectedAxis != HandleAxis.None)
                {
                    return selectedAxis;
                }

                bool isInside = (dpHitPoint - position).magnitude <= innerRadius * scale;

                if (isInside)
                {
                    return HandleAxis.Free;
                }
                else
                {
                    return HandleAxis.Screen;
                }
            }

            distance = float.MaxValue;
            return HandleAxis.None;
        }
        Plane GetDragPlane(Camera camera, Vector3 axis, HandleDrawSettings settings)
        {
            Vector3 toCam;
            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(camera.transform.forward, settings.Rotation * axis)), 1))
            {
                toCam = camera.transform.position - HostTransform.position;
            }
            else
            {
                toCam = camera.cameraToWorldMatrix.MultiplyVector(Vector3.forward);
            }

            Plane dragPlane = new Plane(toCam.normalized, HostTransform.position);
            return dragPlane;
        }
        private bool GetPointOnDragPlane(Plane dragPlane, Ray ray, out Vector3 point)
        {
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }
        private HandleAxis HitAxis(Camera camera, Ray ray, HandleDrawSettings settings, Quaternion startingRotationInv, out float distance)
        {
            Vector3 position = settings.Position;
            Quaternion rotation = settings.Rotation;

            float screenScale = GetScreenScale(position, camera) * HandleScale;
            Vector3 scale = new Vector3(screenScale, screenScale, screenScale);
            Matrix4x4 xTranform = Matrix4x4.TRS(Vector3.zero, rotation * startingRotationInv * Quaternion.AngleAxis(-90, Vector3.up), Vector3.one);
            Matrix4x4 yTranform = Matrix4x4.TRS(Vector3.zero, rotation * startingRotationInv * Quaternion.AngleAxis(-90, Vector3.right), Vector3.one);
            Matrix4x4 zTranform = Matrix4x4.TRS(Vector3.zero, rotation * startingRotationInv, Vector3.one);
            Matrix4x4 objToWorld = Matrix4x4.TRS(position, Quaternion.identity, scale);

            float xDistance;
            float yDistance;
            float zDistance;
            bool hitX = HitAxis(camera, ray, xTranform, objToWorld, out xDistance);
            bool hitY = HitAxis(camera, ray, yTranform, objToWorld, out yDistance);
            bool hitZ = HitAxis(camera, ray, zTranform, objToWorld, out zDistance);

            if (hitX && xDistance < yDistance && xDistance < zDistance)
            {
                distance = xDistance;
                return HandleAxis.X;
            }
            else if (hitY && yDistance < xDistance && yDistance < zDistance)
            {
                distance = yDistance;
                return HandleAxis.Y;
            }
            else if (hitZ && zDistance < xDistance && zDistance < yDistance)
            {
                distance = zDistance;
                return HandleAxis.Z;
            }

            distance = float.MaxValue;
            return HandleAxis.None;
        }
        private bool HitAxis(Camera camera, Ray ray, Matrix4x4 transform, Matrix4x4 objToWorld, out float minDistance)
        {
            bool hit = false;
            minDistance = float.PositiveInfinity;

            const float radius = 1.0f;
            const int pointsPerCircle = 32;
            float angle = 0.0f;
            float z = 0.0f;

            Vector3 zeroCamPoint = transform.MultiplyPoint(Vector3.zero);
            zeroCamPoint = objToWorld.MultiplyPoint(zeroCamPoint);
            zeroCamPoint = camera.worldToCameraMatrix.MultiplyPoint(zeroCamPoint);

            Vector3 prevPoint = transform.MultiplyPoint(new Vector3(radius, 0, z));
            prevPoint = objToWorld.MultiplyPoint(prevPoint);
            for (int i = 0; i < pointsPerCircle; i++)
            {
                angle += 2 * Mathf.PI / pointsPerCircle;
                float x = radius * Mathf.Cos(angle);
                float y = radius * Mathf.Sin(angle);
                Vector3 point = transform.MultiplyPoint(new Vector3(x, y, z));
                point = objToWorld.MultiplyPoint(point);

                Vector3 camPoint = camera.worldToCameraMatrix.MultiplyPoint(point);

                if (camPoint.z >= zeroCamPoint.z)
                {
                    Vector3 screenVector = camera.WorldToScreenPoint(point) - camera.WorldToScreenPoint(prevPoint);
                    float screenVectorMag = screenVector.magnitude;
                    screenVector.Normalize();
                    if (screenVector != Vector3.zero)
                    {
                        Vector3 position = objToWorld.GetColumn(3);
                        Vector2 screenPosition;
                        if(!GetScreenPosition(camera, ray, position, out screenPosition))
                        {
                            return false;
                        }
                        float distance;
                        if (HitScreenAxis(screenPosition, camera.WorldToScreenPoint(prevPoint), screenVector, screenVectorMag, out distance))
                        {
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                hit = true;
                                break;
                            }
                        }
                    }
                }

                prevPoint = point;
            }
            return hit;
        }
        private bool Intersect(Ray r, Vector3 sphereCenter, float sphereRadius, out float hit1Distance, out float hit2Distance)
        {
            hit1Distance = 0.0f;
            hit2Distance = 0.0f;

            Vector3 L = sphereCenter - r.origin;
            float tc = Vector3.Dot(L, r.direction);
            if (tc < 0.0)
            {
                return false;
            }

            float d2 = Vector3.Dot(L, L) - (tc * tc);
            float radius2 = sphereRadius * sphereRadius;
            if (d2 > radius2)
            {
                return false;
            }

            float t1c = Mathf.Sqrt(radius2 - d2);
            hit1Distance = tc - t1c;
            hit2Distance = tc + t1c;

            return true;
        }
        

        public HandleAxis HitTestScaleHandle(Camera camera, Ray ray, HandleDrawSettings settings, out float distance)
        {
            Vector3 position = settings.Position;
            float screenScale = GetScreenScale(position, camera) * HandleScale;
          
            Matrix4x4 matrix = Matrix4x4.TRS(position, settings.Rotation, new Vector3(screenScale, screenScale, screenScale));
            if (HitCenter(camera, ray, position, out distance))
            {
                return HandleAxis.Free;
            }

            float distToYAxis;
            float distToZAxis;
            float distToXAxis;
            bool hit = HitAxis(camera, ray,Vector3.up, matrix, out distToYAxis);
            hit |= HitAxis(camera, ray, Forward, matrix, out distToZAxis);
            hit |= HitAxis(camera, ray, Vector3.right, matrix, out distToXAxis);

            if (hit)
            {
                if (distToYAxis <= distToZAxis && distToYAxis <= distToXAxis)
                {
                    distance = distToYAxis;
                    return HandleAxis.Y;
                }
                else if (distToXAxis <= distToYAxis && distToXAxis <= distToZAxis)
                {
                    distance = distToXAxis;
                    return HandleAxis.X;
                }
                else
                {
                    distance = distToZAxis;
                    return HandleAxis.Z;
                }
            }

            distance = float.PositiveInfinity;
            return HandleAxis.None;
        }

        private bool HitCenter(Camera camera, Ray ray, Vector3 position, out float distance)
        {
            Vector2 screenCenter = camera.WorldToScreenPoint(position);
            Vector2 screnPosition;
            if(!GetScreenPosition(camera, ray, position, out screnPosition))
            {
                distance = float.PositiveInfinity;
                return false;
            }
            
            distance = (screnPosition - screenCenter).magnitude;
            return distance <= SelectionMargin * SelectionMarginPixels;
        }
    }
}