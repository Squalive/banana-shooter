#define UPDATE_LOCAL_EULER
using System.Linq;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;

using CodingDaniel.MapEditor.Interaction;

namespace CodingDaniel.MapEditor.Handle
{
    [DefaultExecutionOrder(3)]
    public class RotationHandle : BaseHandle
    {
        public float GridSize = 15.0f;
        public float XSpeed = 0.5f;
        public float YSpeed = 0.5f;

        private float _deltaX;
        private float _deltaY;
        private Vector2 _prevPointer;
  
        private Quaternion _targetInverse = Quaternion.identity;
        private Matrix4x4 _targetInverseMatrix;
        private Vector3 _startingRotationAxis = Vector3.zero;
        private Quaternion _targetRotation = Quaternion.identity;
        private Quaternion _startingRotation = Quaternion.identity;
        private Quaternion StartingRotation
        {
            get { return PivotRotation == MEPivotRotation.Global ? _startingRotation : Quaternion.identity; }
        }

        private Quaternion _startinRotationInv = Quaternion.identity;
        private Quaternion StartingRotationInv
        {
            get { return PivotRotation == MEPivotRotation.Global ? _startinRotationInv : Quaternion.identity; }
        }

        private Quaternion TargetRotation
        {
            get { return PivotRotation == MEPivotRotation.Global ? Target.rotation : ActiveRealTargets[0].rotation; }
        }

        protected override float CurrentGridUnitSize
        {
            get { return GridSize; }
        }

        public override EditorTool Tool
        {
            get { return EditorTool.Rotate; }
        }

#if UPDATE_LOCAL_EULER
        private Vector3[] _accumulatedEuler;
        private Vector3[] _startingEuler;
        private Quaternion[] _localRotationsBuffer;
        private ExposeToEditor[] _exposedTargets;
        public override Transform[] Targets
        {
            get { return base.Targets; }
            set 
            {
                base.Targets = value; 

                if(ActiveRealTargets != null)
                {
                    _exposedTargets = ActiveRealTargets.Select(t => t.GetComponent<ExposeToEditor>()).ToArray();
                    _startingEuler = new Vector3[_exposedTargets.Length];
                    _accumulatedEuler = new Vector3[_exposedTargets.Length];
                    _localRotationsBuffer = new Quaternion[_exposedTargets.Length];
                }
                else
                {
                    _exposedTargets = null;
                    _startingEuler = null;
                    _accumulatedEuler = null;
                    _localRotationsBuffer = null;
                }
            }
        }
#endif

        protected override void Start()
        {
            base.Start();
            OnPivotRotationChanged();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnPivotRotationChanged();
            Editor.Tools.PivotRotationChanged += OnPivotRotationChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Editor.Tools.PivotRotationChanged -= OnPivotRotationChanged;
        }

        protected override void UpdateOverride()
        {
            base.UpdateOverride();

            if (Editor.Tools.IsViewing)
            {
                SelectedAxis = HandleAxis.None;
                return;
            }
            if (!IsDragging)
            {
                UpdateMatricesAndRotations(false);
            }
        }

        protected override void OnPivotRotationChanged()
        {
            UpdateMatricesAndRotations(true);
            base.OnPivotRotationChanged();
        }

        private void UpdateMatricesAndRotations(bool forceUpdate)
        {
            if (Target == null)
            {
                return;
            }

            _targetInverseMatrix = Matrix4x4.TRS(Target.position, TargetRotation * StartingRotationInv, Vector3.one).inverse;

            if (forceUpdate || _targetRotation != TargetRotation)
            {
                _startingRotation = TargetRotation;
                _startinRotationInv = Quaternion.Inverse(_startingRotation);
                _targetRotation = TargetRotation;
            }
        }

        private bool ForceScreenRotationMode()
        {
            if (SelectedAxis == HandleAxis.Free)
            {
                return false;
            }

            if (SelectedAxis == HandleAxis.X)
            {
                return Mathf.Abs(Vector3.Dot(Editor.Camera.transform.forward, (Target.rotation * StartingRotationInv) * Vector3.right)) > 0.8;
            }
            else if (SelectedAxis == HandleAxis.Y)
            {
                return Mathf.Abs(Vector3.Dot(Editor.Camera.transform.forward, (Target.rotation * StartingRotationInv) * Vector3.up)) > 0.8;
            }
            else if (SelectedAxis == HandleAxis.Z)
            {
                return Mathf.Abs(Vector3.Dot(Editor.Camera.transform.forward, (Target.rotation * StartingRotationInv) * Vector3.forward)) > 0.8;
            }
            return false;
        }

        private Quaternion ScreenRotation(Vector3 delta)
        {
            Vector3 cameraAxis = _targetInverseMatrix.MultiplyVector(Editor.Camera.cameraToWorldMatrix.MultiplyVector(-Vector3.forward));
            if (SelectedAxis == HandleAxis.Screen)
            {
                Quaternion rotation = Quaternion.AngleAxis(delta.x, cameraAxis);
                if (SharedLockObject == null || !SharedLockObject.RotationScreen)
                {
                    return rotation;
                }
            }
            else
            {
                if (SelectedAxis == HandleAxis.X)
                {
                    Vector3 axis = Quaternion.Inverse(Target.rotation) * ((Target.rotation * StartingRotationInv) * Vector3.right);
                    Quaternion rotation = Quaternion.AngleAxis(delta.x * Mathf.Sign(Vector3.Dot(axis, cameraAxis)), axis);
                    if (SharedLockObject == null || !SharedLockObject.RotationX)
                    {
                        return rotation;
                    }
                }
                else if (SelectedAxis == HandleAxis.Y)
                {
                    Vector3 axis = Quaternion.Inverse(Target.rotation) * ((Target.rotation * StartingRotationInv) * Vector3.up);
                    Quaternion rotation = Quaternion.AngleAxis(delta.x * Mathf.Sign(Vector3.Dot(axis, cameraAxis)), axis);
                    if (SharedLockObject == null || !SharedLockObject.RotationY)
                    {
                        return rotation;
                    }
                }
                else if (SelectedAxis == HandleAxis.Z)
                {
                    Vector3 axis = Quaternion.Inverse(Target.rotation) * ((Target.rotation * StartingRotationInv) * Vector3.forward);
                    Quaternion rotation = Quaternion.AngleAxis(delta.x * Mathf.Sign(Vector3.Dot(axis, cameraAxis)), axis);
                    if (SharedLockObject == null || !SharedLockObject.RotationZ)
                    {
                        return rotation;
                    }
                }
            }

            return Quaternion.identity;
        }

        private bool _forceScreenRotationMode;
        
        protected override bool OnBeginDrag()
        {
            if(Target == null)
            {
                return false;
            }

     
            _targetRotation = Target.rotation;
            _targetInverseMatrix = Matrix4x4.TRS(Target.position, Target.rotation * StartingRotationInv, Vector3.one).inverse;

            if (!base.OnBeginDrag())
            {
                return false;
            }

            _deltaX = 0.0f;
            _deltaY = 0.0f;

            Vector2 point;
            if (Editor.Pointer.XY(Target.position, out point))
            {
                _prevPointer = point;
            }
            else
            {
                SelectedAxis = HandleAxis.None;
            }

            _forceScreenRotationMode = ForceScreenRotationMode();
            if (SelectedAxis == HandleAxis.Screen || _forceScreenRotationMode)
            {
                Vector2 center;

                if (Editor.Pointer.WorldToScreenPoint(Target.position, Target.position, out center))
                {
                    if (Editor.Pointer.XY(Target.position, out point))
                    {
                        float angle = Mathf.Atan2(point.y - center.y, point.x - center.x);
                        _targetInverse = Quaternion.Inverse(Quaternion.AngleAxis(Mathf.Rad2Deg * angle, Vector3.forward));
                        _targetInverseMatrix = Matrix4x4.TRS(Target.position, Target.rotation, Vector3.one).inverse;
                        _prevPointer = point;
                    }
                    else
                    {
                        SelectedAxis = HandleAxis.None;
                    }
                }
                else
                {
                    SelectedAxis = HandleAxis.None;
                }
            }
            else
            {
                if (SelectedAxis == HandleAxis.X)
                {
                    _startingRotationAxis = (Target.rotation * Quaternion.Inverse(StartingRotation)) * Vector3.right;
                }
                else if (SelectedAxis == HandleAxis.Y)
                {
                    _startingRotationAxis = (Target.rotation * Quaternion.Inverse(StartingRotation)) * Vector3.up;
                }
                else if (SelectedAxis == HandleAxis.Z)
                {
                    _startingRotationAxis = (Target.rotation * Quaternion.Inverse(StartingRotation)) * Vector3.forward;
                }

                _targetInverse = Quaternion.Inverse(Target.rotation);
            }

#if UPDATE_LOCAL_EULER
            for (int i = 0; i < _exposedTargets.Length; ++i)
            {
                if (_exposedTargets[i] != null)
                {
                    _startingEuler[i] = _exposedTargets[i].LocalEuler;
                }
            }
#endif

            return SelectedAxis != HandleAxis.None;
        }

        protected override void OnDrag()
        {
            base.OnDrag();

            Vector2 point;
            if (!Editor.Pointer.XY(Target.position, out point))
            {
                return;
            }

            float deltaX = point.x - _prevPointer.x;
            float deltaY = point.y - _prevPointer.y;
            _prevPointer = point;

            deltaX = deltaX * XSpeed;
            deltaY = deltaY * YSpeed;

            _deltaX += deltaX;
            _deltaY += deltaY;

            Matrix4x4 toWorldMatrix;
            if (!Editor.Pointer.ToWorldMatrix(Target.position, out toWorldMatrix))
            {
                return;
            }

            Vector3 delta = StartingRotation * Quaternion.Inverse(Target.rotation) * toWorldMatrix.MultiplyVector(new Vector3(_deltaY, -_deltaX, 0));
            Quaternion rotation;

            if (SelectedAxis == HandleAxis.Screen || _forceScreenRotationMode)
            {
                delta = _targetInverse * new Vector3(_deltaY, -_deltaX, 0);
                if (EffectiveGridUnitSize != 0.0f)
                {
                    if (Mathf.Abs(delta.x) >= EffectiveGridUnitSize)
                    {
                        delta.x = Mathf.Sign(delta.x) * EffectiveGridUnitSize;
                        _deltaX = 0.0f;
                        _deltaY = 0.0f;
                    }
                    else
                    {
                        delta.x = 0.0f;
                    }
                }

                rotation = ScreenRotation(delta);
            }

            else if (SelectedAxis == HandleAxis.X)
            {
                Vector3 rotationAxis = Quaternion.Inverse(Target.rotation) * _startingRotationAxis;

                if (EffectiveGridUnitSize != 0.0f)
                {
                    if (Mathf.Abs(delta.x) >= EffectiveGridUnitSize)
                    {
                        delta.x = Mathf.Sign(delta.x) * EffectiveGridUnitSize;
                        _deltaX = 0.0f;
                        _deltaY = 0.0f;
                    }
                    else
                    {
                        delta.x = 0.0f;
                    }
                }

                if (SharedLockObject != null && SharedLockObject.RotationX)
                {
                    delta.x = 0.0f;
                }

                rotation = Quaternion.AngleAxis(delta.x, rotationAxis);
            }
            else if (SelectedAxis == HandleAxis.Y)
            {
                Vector3 rotationAxis = Quaternion.Inverse(Target.rotation) * _startingRotationAxis;

                if (EffectiveGridUnitSize != 0.0f)
                {
                    if (Mathf.Abs(delta.y) >= EffectiveGridUnitSize)
                    {
                        delta.y = Mathf.Sign(delta.y) * EffectiveGridUnitSize;
                        _deltaX = 0.0f;
                        _deltaY = 0.0f;
                    }
                    else
                    {
                        delta.y = 0.0f;
                    }
                }

                if (SharedLockObject != null && SharedLockObject.RotationY)
                {
                    delta.y = 0.0f;
                }

                rotation = Quaternion.AngleAxis(delta.y, rotationAxis);

            }
            else if (SelectedAxis == HandleAxis.Z)
            {
                Vector3 rotationAxis = Quaternion.Inverse(Target.rotation) * _startingRotationAxis;

                if (EffectiveGridUnitSize != 0.0f)
                {
                    if (Mathf.Abs(delta.z) >= EffectiveGridUnitSize)
                    {
                        delta.z = Mathf.Sign(delta.z) * EffectiveGridUnitSize;
                        _deltaX = 0.0f;
                        _deltaY = 0.0f;
                    }
                    else
                    {
                        delta.z = 0.0f;
                    }
                }

                if (SharedLockObject != null && SharedLockObject.RotationZ)
                {
                    delta.z = 0.0f;
                }

                rotation = Quaternion.AngleAxis(delta.z, rotationAxis);

            }
            else
            {
                delta = StartingRotationInv * delta;

                if (SharedLockObject != null && SharedLockObject.RotationFree)
                {
                    delta.x = 0.0f;
                    delta.y = 0.0f;
                    delta.z = 0.0f;
                }

                rotation = Quaternion.Euler(delta.x, delta.y, delta.z);
                _deltaX = 0.0f;
                _deltaY = 0.0f;
            }
           

            if (EffectiveGridUnitSize == 0.0f)
            {
                _deltaX = 0.0f;
                _deltaY = 0.0f;
            }

#if UPDATE_LOCAL_EULER
            for (int i = 0; i < _exposedTargets.Length; i++)
            {
                ExposeToEditor exposed = _exposedTargets[i];
                if(exposed != null)
                {
                    _localRotationsBuffer[i] = exposed.transform.localRotation;
                }
            }
#endif

            for (int i = 0; i < ActiveTargets.Length; ++i)
            {
                ActiveTargets[i].rotation *= rotation;
            }

#if UPDATE_LOCAL_EULER
            for(int i = 0; i < _exposedTargets.Length; i++)
            {
                ExposeToEditor exposed = _exposedTargets[i];
                if(exposed != null)
                {
                    Quaternion localRotation = exposed.transform.localRotation;

                    Vector3 euler = (Quaternion.Inverse(_localRotationsBuffer[i]) * localRotation).eulerAngles;
                    euler.x = euler.x < 180 ? euler.x : -360 + euler.x;
                    euler.y = euler.y < 180 ? euler.y : -360 + euler.y;
                    euler.z = euler.z < 180 ? euler.z : -360 + euler.z;

                    _accumulatedEuler[i] += euler;

                    exposed.SetLocalEulerAngles(_startingEuler[i] + _accumulatedEuler[i]);
                }
            }
#endif
        }


        protected override void OnDrop()
        {
            base.OnDrop();
            _targetRotation = Target.rotation;

#if UPDATE_LOCAL_EULER
            for (int i = 0; i < _exposedTargets.Length; ++i)
            {
                if (_exposedTargets[i] != null)
                {
                    _exposedTargets[i].LocalEuler = _startingEuler[i] + _accumulatedEuler[i];
                }

                _accumulatedEuler[i] = Vector3.zero;
            }
#endif

            OnPivotRotationChanged();
        }


        private HandleDrawSettings _settings = new HandleDrawSettings();
        protected override void RefreshCommandBuffer(IMECamera camera)
        {
            _settings.Position = Target.position;
            _settings.Rotation = Target.rotation * StartingRotationInv;
            _settings.SelectedAxis = SelectedAxis;
            _settings.LockObject = SharedLockObject;

            appearance.DoRotationHandle(camera.CommandBuffer, camera.Camera, _settings);
        }

        public override HandleAxis HitTest(out float distance)
        {
            return appearance.HitTestRotationHandle(Editor.Camera, Editor.Pointer, _settings, out distance);
        }
    }
}
