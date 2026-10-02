using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;

using CodingDaniel.MapEditor.Interaction;

namespace CodingDaniel.MapEditor.Handle
{
    [DefaultExecutionOrder(2)]
    public class ScaleHandle : BaseHandle
    {
        public bool AbsoluteGrid = false;
        public float GridSize = 0.1f;
        public Vector3 MinScale = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        private Vector3 _prevPoint;
        private Matrix4x4 _matrix;
        private Matrix4x4 _inverse;

        private Vector3 _roundedScale;
        private Vector3 _scale;
        private Vector3[] _refScales;
        private float _screenScale;
 
        public override bool SnapToGrid
        {
            get { return AbsoluteGrid; }
            set { AbsoluteGrid = value; }
        }

        public override float SizeOfGrid
        {
            get { return GridSize; }
            set { GridSize = value; }
        }

        protected override float CurrentGridUnitSize
        {
            get { return SizeOfGrid; }
        }

        public override EditorTool Tool
        {
            get { return EditorTool.Scale; }
        }

        protected override void Awake()
        {
            base.Awake();
        
            _scale = Vector3.one;
            _roundedScale = _scale;
        }

        protected override void UpdateOverride()
        {
            base.UpdateOverride();
            UpdateCurrentMode();
        }
        protected override bool OnBeginDrag()
        {
            if(!base.OnBeginDrag())
            {
                return false;
            }

            if(SelectedAxis == HandleAxis.Free)
            {
                DragPlane = GetDragPlane(Vector3.zero);
            }
            else if(SelectedAxis == HandleAxis.None)
            {
                return false;
            }

            _refScales = new Vector3[ActiveTargets.Length];
            for (int i = 0; i < _refScales.Length; ++i)
            {
                Quaternion rotation = PivotRotation == MEPivotRotation.Global ? ActiveTargets[i].rotation : Quaternion.identity;
                _refScales[i] = rotation * ActiveTargets[i].localScale;
            }

            Vector3 axis = Vector3.zero;
            switch (SelectedAxis)
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

            DragPlane = GetDragPlane(axis);
            bool result = GetPointOnDragPlane(Editor.Pointer, out _prevPoint);
            if(!result)
            {
                SelectedAxis = HandleAxis.None;
            }
            return result;
        }

        protected override void OnDrag()
        {
            base.OnDrag();

            Vector3 point;
            if (GetPointOnDragPlane(Editor.Pointer, out point))
            {
                Vector3 offset = _inverse.MultiplyVector((point - _prevPoint) / _screenScale);
                float mag = offset.magnitude;
                if (SelectedAxis == HandleAxis.X)
                {
                    offset.y = offset.z = 0.0f;

                    if (SharedLockObject == null || !SharedLockObject.ScaleX)
                    {
                        _scale.x += Mathf.Sign(offset.x) * mag;
                    }
                }
                else if (SelectedAxis == HandleAxis.Y)
                {
                    offset.x = offset.z = 0.0f;
                    if(SharedLockObject == null || !SharedLockObject.ScaleY)
                    {
                        _scale.y += Mathf.Sign(offset.y) * mag;
                    }
                }
                else if(SelectedAxis == HandleAxis.Z)
                {
                    offset.x = offset.y = 0.0f;
                    if(SharedLockObject == null || !SharedLockObject.ScaleZ)
                    {
                        _scale.z += Mathf.Sign(offset.z) * mag;
                    }
                }
                if(SelectedAxis == HandleAxis.Free)
                {
                    float sign = Mathf.Sign(offset.x + offset.y);

                    if(SharedLockObject != null)
                    {
                        if (!SharedLockObject.ScaleX)
                        {
                            _scale.x += sign * mag;
                        }

                        if (!SharedLockObject.ScaleY)
                        {
                            _scale.y += sign * mag;
                        }

                        if (!SharedLockObject.ScaleZ)
                        {
                            _scale.z += sign * mag;
                        }
                    }
                    else
                    {
                        _scale.x += sign * mag;
                        _scale.y += sign * mag;
                        _scale.z += sign * mag;
                    }
                }

                if(SnapToGrid)
                {
                    for (int i = 0; i < _refScales.Length; ++i)
                    {
                        Quaternion rotation = PivotRotation == MEPivotRotation.Global ? Targets[i].rotation : Quaternion.identity;

                        float gridSize = EffectiveGridUnitSize * 2;

                        _roundedScale = Vector3.Scale(_refScales[i], _scale);
                        if (EffectiveGridUnitSize > 0.01)
                        {
                            _roundedScale.x = Mathf.RoundToInt(_roundedScale.x / gridSize) * gridSize;
                            _roundedScale.y = Mathf.RoundToInt(_roundedScale.y / gridSize) * gridSize;
                            _roundedScale.z = Mathf.RoundToInt(_roundedScale.z / gridSize) * gridSize;
                        }

                        Vector3 scale = Quaternion.Inverse(rotation) * _roundedScale;
                        scale.x = Mathf.Max(MinScale.x, scale.x);
                        scale.y = Mathf.Max(MinScale.y, scale.y);
                        scale.z = Mathf.Max(MinScale.z, scale.z);
                        ActiveTargets[i].localScale = scale;
                    }


                }
                else
                {
                    _roundedScale = _scale;

                    if (EffectiveGridUnitSize > 0.01)
                    {
                        _roundedScale.x = Mathf.RoundToInt(_roundedScale.x / EffectiveGridUnitSize) * EffectiveGridUnitSize;
                        _roundedScale.y = Mathf.RoundToInt(_roundedScale.y / EffectiveGridUnitSize) * EffectiveGridUnitSize;
                        _roundedScale.z = Mathf.RoundToInt(_roundedScale.z / EffectiveGridUnitSize) * EffectiveGridUnitSize;
                    }


                    for (int i = 0; i < _refScales.Length; ++i)
                    {
                        Quaternion rotation = PivotRotation == MEPivotRotation.Global ? Targets[i].rotation : Quaternion.identity;

                        Vector3 scale = Quaternion.Inverse(rotation) * Vector3.Scale(_refScales[i], _roundedScale);
                        
                        scale.x = Mathf.Max(MinScale.x, scale.x);
                        scale.y = Mathf.Max(MinScale.y, scale.y);
                        scale.z = Mathf.Max(MinScale.z, scale.z);
                        ActiveTargets[i].localScale = scale;
                    }
                }
               
                _prevPoint = point;
            }
        }

        protected override void OnDrop()
        {
            base.OnDrop();

            _scale = Vector3.one;
            _roundedScale = _scale;

        }

        private HandleDrawSettings _settings = new HandleDrawSettings();
        protected override void RefreshCommandBuffer(IMECamera camera)
        {
            _settings.Position = Target.position;
            _settings.Rotation = Rotation;
            _settings.Scale = _roundedScale;
            _settings.SelectedAxis = SelectedAxis;
            _settings.LockObject = SharedLockObject;

            appearance.DoScaleHandle(camera.CommandBuffer, camera.Camera, _settings);
        }
        public override HandleAxis HitTest(out float distance)
        {
            _screenScale = MEHandleComponent.GetScreenScale(transform.position, Editor.Camera) * appearance.HandleScale;
            _matrix = Matrix4x4.TRS(transform.position, Rotation, appearance.InvertZAxis ? new Vector3(1, 1, -1) : Vector3.one);
            _inverse = _matrix.inverse;

            return appearance.HitTestScaleHandle(Editor.Camera, Editor.Pointer, _settings, out distance);
        }
    }
}
