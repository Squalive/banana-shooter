using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;
using ExposeToEditor = CodingDaniel.MapEditor.MEEditor.ExposeToEditor;

using CodingDaniel.MapEditor.Interaction;

namespace CodingDaniel.MapEditor.Handle
{
    [DefaultExecutionOrder(1)]
    public class PositionHandle : BaseHandle
    {
        
        public float GridSize =1f;
        
        private Vector3 _cursorPosition;
        private Vector3 _currentPosition;

        private Vector3 _prevPoint;
        private Matrix4x4 _matrix;
        private Matrix4x4 _inverse;

        private Vector2 _prevMousePosition;
        private int[] _targetLayers;
        private Transform[] _snapTargets;
        private Bounds[] _snapTargetsBounds;
        private ExposeToEditor[] _allExposedToEditor;
    
        public override float SizeOfGrid
        {
            get { return GridSize; }
            set { GridSize = value; }
        }
        protected override float CurrentGridUnitSize
        {
            get { return SizeOfGrid; }
        }


        private bool _isInVertexSnappingMode = false;
        public bool IsInVertexSnappingMode
        {
            get { return _isInVertexSnappingMode; }
            set
            {
                _isInVertexSnappingMode = value;

                if(_isInVertexSnappingMode)
                {
                    if (SharedLockObject == null || !SharedLockObject.IsPositionLocked)
                    {
                        if (Editor.Pointer.XY(Position, out _prevMousePosition))
                        {
                            BeginSnap();
                        }
                    }
                }
                else
                {
                    SelectedAxis = HandleAxis.None;
                    if (!(IsInVertexSnappingMode || Editor.Tools.IsSnapping))
                    {
                        _handleOffset = Vector3.zero;
                    }
                }


            }
        }

        private Vector3[] _boundingBoxCorners = new Vector3[8];
        private Vector3 _handleOffset;
        public override Vector3 Position
        {
            get { return transform.position + _handleOffset; }
            set
            {
                transform.position = value - _handleOffset;
            }
        }

        public override EditorTool Tool
        {
            get { return EditorTool.Move; }
        }

        public override void SetVertexSnapping(bool snapping)
        {
            IsInVertexSnappingMode = snapping;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        
            _isInVertexSnappingMode = false;
            Editor.Tools.IsSnapping = false;
            _handleOffset = Vector3.zero;
            _targetLayers = null;
            _snapTargets = null;
            _snapTargetsBounds = null;
            _allExposedToEditor = null;

            Editor.Tools.IsSnappingChanged += OnSnappingChanged;
            OnSnappingChanged();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if(Editor != null)
            {
                Editor.Tools.IsSnapping = false;
                Editor.Tools.IsSnappingChanged -= OnSnappingChanged;
            }
            
            _targetLayers = null;
            _snapTargets = null;
            _snapTargetsBounds = null;
            _allExposedToEditor = null;
        }

        protected override void UpdateOverride()
        {
            base.UpdateOverride();

            UpdateCurrentMode();

            if (Editor.Tools.IsViewing)
            {
                SelectedAxis = HandleAxis.None;
                return;
            }

            IME editor = Editor;

            if (IsInVertexSnappingMode || Editor.Tools.IsSnapping)
            {
                Vector2 mousePosition;
                if(editor.Pointer.XY(Position, out mousePosition))
                {
                    if (editor.Tools.SnappingMode == SnappingMode.BoundingBox)
                    {
                        if (IsDragging)
                        {
                            SelectedAxis = HandleAxis.Snap;
                            if (_prevMousePosition != mousePosition)
                            {
                                _prevMousePosition = mousePosition;
                                float minDistance = float.MaxValue;
                                Vector3 minPoint = Vector3.zero;
                                bool minPointFound = false;
                                for (int i = 0; i < _allExposedToEditor.Length; ++i)
                                {
                                    ExposeToEditor exposeToEditor = _allExposedToEditor[i];
                                    Bounds bounds = exposeToEditor.Bounds;
                                    Vector3 center = new Vector3(Mathf.CeilToInt(bounds.center.x),
                                        Mathf.CeilToInt(bounds.center.y), Mathf.CeilToInt(bounds.center.z));
                                    _boundingBoxCorners[0] = center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                                    _boundingBoxCorners[1] = center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z);
                                    _boundingBoxCorners[2] = center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
                                    _boundingBoxCorners[3] = center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
                                    _boundingBoxCorners[4] = center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
                                    _boundingBoxCorners[5] = center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z);
                                    _boundingBoxCorners[6] = center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z);
                                    _boundingBoxCorners[7] = center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
                                    GetMinPoint(ref minDistance, ref minPoint, ref minPointFound, exposeToEditor.boundsObject.transform);
                                }

                                if (minPointFound)
                                {
                                    Position = minPoint;
                                }
                            }
                        }
                        else
                        {
                            SelectedAxis = HandleAxis.None;
                            if (_prevMousePosition != mousePosition)
                            {
                                _prevMousePosition = mousePosition;

                                float minDistance = float.MaxValue;
                                Vector3 minPoint = Vector3.zero;
                                bool minPointFound = false;
                                for (int i = 0; i < _snapTargets.Length; ++i)
                                {
                                    Transform snapTarget = _snapTargets[i];
                                    Bounds bounds = _snapTargetsBounds[i];
                                    Vector3 center = new Vector3(Mathf.CeilToInt(bounds.center.x),
                                        Mathf.CeilToInt(bounds.center.y), Mathf.CeilToInt(bounds.center.z));
                                    _boundingBoxCorners[0] = center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                                    _boundingBoxCorners[1] = center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z);
                                    _boundingBoxCorners[2] = center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
                                    _boundingBoxCorners[3] = center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
                                    _boundingBoxCorners[4] = center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
                                    _boundingBoxCorners[5] = center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z);
                                    _boundingBoxCorners[6] = center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z);
                                    _boundingBoxCorners[7] = center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
                                    if (Targets[i] != null)
                                    {
                                        GetMinPoint(ref minDistance, ref minPoint, ref minPointFound, snapTarget);
                                    }
                                }

                                if (minPointFound)
                                {
                                    _handleOffset = minPoint - transform.position;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (IsDragging)
                        {
                            SelectedAxis = HandleAxis.Snap;
                            if (_prevMousePosition != mousePosition)
                            {
                                _prevMousePosition = mousePosition;

                                Ray ray = editor.Pointer;
                                RaycastHit hitInfo;

                                LayerMask layerMask = (1 << Physics.IgnoreRaycastLayer);
                                layerMask = ~layerMask;
                                layerMask &= Editor.CameraLayerSettings.RaycastMask;

                                for (int i = 0; i < _snapTargets.Length; ++i)
                                {
                                    _targetLayers[i] = _snapTargets[i].gameObject.layer;
                                    _snapTargets[i].gameObject.layer = Physics.IgnoreRaycastLayer;
                                }

                                GameObject closestObject = null;
                                if (Physics.Raycast(ray, out hitInfo, float.PositiveInfinity, layerMask))
                                {
                                    closestObject = hitInfo.collider.gameObject;
                                }
                                else
                                {
                                    float minDistance = float.MaxValue;
                                    for (int i = 0; i < _allExposedToEditor.Length; ++i)
                                    {
                                        ExposeToEditor exposedToEditor = _allExposedToEditor[i];
                                        Bounds bounds = exposedToEditor.Bounds;

                                        _boundingBoxCorners[0] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z);
                                        _boundingBoxCorners[1] = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z);
                                        _boundingBoxCorners[2] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
                                        _boundingBoxCorners[3] = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
                                        _boundingBoxCorners[4] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
                                        _boundingBoxCorners[5] = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z);
                                        _boundingBoxCorners[6] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z);
                                        _boundingBoxCorners[7] = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);

                                        for (int j = 0; j < _boundingBoxCorners.Length; ++j)
                                        {
                                            Vector2 screenPoint;
                                            if(Editor.Pointer.WorldToScreenPoint(Position, exposedToEditor.boundsObject.transform.TransformPoint(_boundingBoxCorners[j]), out screenPoint))
                                            {
                                                float distance = (screenPoint - mousePosition).magnitude;
                                                if (distance < minDistance)
                                                {
                                                    closestObject = exposedToEditor.gameObject;
                                                    minDistance = distance;
                                                }
                                            }   
                                        }
                                    }
                                }

                                if (closestObject != null)
                                {
                                    float minDistance = float.MaxValue;
                                    Vector3 minPoint = Vector3.zero;
                                    bool minPointFound = false;
                                    Transform meshTransform;
                                    Mesh mesh = GetMesh(closestObject, out meshTransform);
                                    GetMinPoint(meshTransform, ref minDistance, ref minPoint, ref minPointFound, mesh);

                                    if (minPointFound)
                                    {
                                        Position = minPoint;
                                    }

                                }

                                for (int i = 0; i < _snapTargets.Length; ++i)
                                {
                                    _snapTargets[i].gameObject.layer = _targetLayers[i];
                                }
                            }
                        }
                        else
                        {
                            SelectedAxis = HandleAxis.None;
                            if (_prevMousePosition != mousePosition)
                            {
                                _prevMousePosition = mousePosition;

                                float minDistance = float.MaxValue;
                                Vector3 minPoint = Vector3.zero;
                                bool minPointFound = false;
                                for (int i = 0; i < RealTargets.Length; ++i)
                                {
                                    Transform snapTarget = RealTargets[i];
                                    Transform meshTranform;
                                    Mesh mesh = GetMesh(snapTarget.gameObject, out meshTranform);
                                    GetMinPoint(meshTranform, ref minDistance, ref minPoint, ref minPointFound, mesh);
                                }
                                if (minPointFound)
                                {
                                    _handleOffset = minPoint - transform.position;
                                }
                            }
                        }
                    }
                }
            }     
        }

        private void GetMinPoint(Transform meshTransform, ref float minDistance, ref Vector3 minPoint, ref bool minPointFound, Mesh mesh)
        {
            if (mesh != null && mesh.isReadable)
            {
                IME editor = Editor;
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; ++i)
                {
                    Vector3 vert = vertices[i];
                    vert = meshTransform.TransformPoint(vert);

                    Vector2 screenPoint;
                    if(editor.Pointer.WorldToScreenPoint(Position, vert, out screenPoint))
                    {
                        Vector2 mousePoint;
                        if (editor.Pointer.XY(Position, out mousePoint))
                        {
                            float distance = (screenPoint - mousePoint).magnitude;
                            if (distance < minDistance)
                            {
                                minPointFound = true;
                                minDistance = distance;
                                minPoint = vert;
                            }
                        }
                    }
                }
            }
        }

        private static Mesh GetMesh(GameObject go, out Transform meshTransform)
        {
            Mesh mesh = null;
            meshTransform = null;
            MeshFilter filter = go.GetComponentInChildren<MeshFilter>();
            if (filter != null)
            {
                mesh = filter.sharedMesh;
                meshTransform = filter.transform;
            }
            else
            {
                SkinnedMeshRenderer skinnedMeshRender = go.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRender != null)
                {
                    mesh = skinnedMeshRender.sharedMesh;
                    meshTransform = skinnedMeshRender.transform;
                }
                else
                {
                    MeshCollider collider = go.GetComponentInChildren<MeshCollider>();
                    if (collider != null)
                    {
                        mesh = collider.sharedMesh;
                        meshTransform = collider.transform;
                    }
                }
            }

            return mesh;
        }

        protected override void OnDrop()
        {
            base.OnDrop();

            if (SnapToGrid)
            {
                SnapActiveTargetsToGrid();
            }

        }

        private void OnSnappingChanged()
        {
            if (Editor.Tools.IsSnapping)
            {
                BeginSnap();
            }
            else
            {
                _handleOffset = Vector3.zero;
            }
        }

        private void BeginSnap()
        {
            if(Editor.Camera == null)
            {
                return;
            }


            HashSet<Transform> snapTargetsHS = new HashSet<Transform>();
            List<Transform> snapTargets = new List<Transform>();
            List<Bounds> snapTargetBounds = new List<Bounds>();
            
            if (Target != null)
            {
                for (int i = 0; i < RealTargets.Length; ++i)
                {
                    Transform target = RealTargets[i];
                    if (target != null)
                    {
                        ExposeToEditor exposeToEditor = target.GetComponent<ExposeToEditor>();
                        if (exposeToEditor != null)
                        {
                            snapTargetBounds.Add(exposeToEditor.Bounds);
                            snapTargets.Add(exposeToEditor.boundsObject.transform);
                            snapTargetsHS.Add(exposeToEditor.boundsObject.transform);
                        }
                        else
                        {
                            snapTargets.Add(target);
                            snapTargetsHS.Add(target);

                            MeshFilter filter = target.GetComponent<MeshFilter>();
                            if(filter != null && filter.sharedMesh != null)
                            {
                                snapTargetBounds.Add(filter.sharedMesh.bounds);
                            }
                            else
                            {
                                SkinnedMeshRenderer smr = target.GetComponent<SkinnedMeshRenderer>();
                                if(smr != null && smr.sharedMesh != null)
                                {
                                    snapTargetBounds.Add(smr.sharedMesh.bounds);
                                }
                                else
                                {
                                    Bounds b = new Bounds(Vector3.zero, Vector3.zero);
                                    snapTargetBounds.Add(b);
                                }
                            }
                        }
                    }
                }
            }

            _snapTargets = snapTargets.ToArray();
            _targetLayers = new int[_snapTargets.Length];
            _snapTargetsBounds = snapTargetBounds.ToArray();

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Editor.Camera);
            ExposeToEditor[] exposeToEditorObjects = FindObjectsOfType<ExposeToEditor>();
            List<ExposeToEditor> insideOfFrustum = new List<ExposeToEditor>();
            for (int i = 0; i < exposeToEditorObjects.Length; ++i)
            {
                ExposeToEditor exposeToEditor = exposeToEditorObjects[i];
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, new Bounds(exposeToEditor.transform.TransformPoint(exposeToEditor.Bounds.center), Vector3.zero)))
                {
                    if (!snapTargetsHS.Contains(exposeToEditor.transform))
                    {
                        insideOfFrustum.Add(exposeToEditor);
                    }
                }
            }
            _allExposedToEditor = insideOfFrustum.ToArray();
        }

        private void GetMinPoint(ref float minDistance, ref Vector3 minPoint, ref bool minPointFound, Transform tr)
        {
            for (int j = 0; j < _boundingBoxCorners.Length; ++j)
            {
                //local bounding point -> 
                //world bounding point -> _boundingBoxCorners[j]
                Vector3 worldPoint = tr.TransformPoint(_boundingBoxCorners[j]);
                Vector2 screenPoint;

                if(Editor.Pointer.WorldToScreenPoint(Position, worldPoint, out screenPoint))
                {
                    Vector2 mousePoint;
                    if (Editor.Pointer.XY(Position, out mousePoint))
                    {
                        float distance = (screenPoint - mousePoint).magnitude;
                        if (distance < minDistance)
                        {
                            minPointFound = true;
                            minDistance = distance;
                            minPoint = worldPoint;
                        }
                    }
                }
            }
        }

        private bool HitSnapHandle()
        {
            Vector2 sp;

            if(Editor.Pointer.WorldToScreenPoint(Position, Position, out sp))
            {
                Vector2 mp;
                if (Editor.Pointer.XY(Position, out mp))
                {
                    const float pixelSize = 10;

                    return sp.x - pixelSize <= mp.x && mp.x <= sp.x + pixelSize &&
                           sp.y - pixelSize <= mp.y && mp.y <= sp.y + pixelSize;
                }
            }
            
            return false;
        }

        protected override bool OnBeginDrag()
        {
            if(!base.OnBeginDrag())
            {
                return false;
            }
           
            _currentPosition = Position;
            _cursorPosition = Position;

            if ((IsInVertexSnappingMode || Editor.Tools.IsSnapping) && SelectedAxis != HandleAxis.Snap)
            {
                return HitSnapHandle();
            }

            if (SelectedAxis == HandleAxis.XZ)
            {
                DragPlane = GetDragPlane(_matrix, Vector3.up);
                return GetPointOnDragPlane(Editor.Pointer, out _prevPoint);
            }

            if (SelectedAxis == HandleAxis.YZ)
            {
                DragPlane = GetDragPlane(_matrix, Vector3.right);
                return GetPointOnDragPlane(Editor.Pointer, out _prevPoint);
            }

            if (SelectedAxis == HandleAxis.XY)
            {
                DragPlane = GetDragPlane(_matrix, Vector3.forward);
                return GetPointOnDragPlane(Editor.Pointer, out _prevPoint);
            }

            if (SelectedAxis != HandleAxis.None)
            {
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

            return false;
        }

        protected override void OnDrag()
        {
            if (IsInVertexSnappingMode || Editor.Tools.IsSnapping)
            {
                return;
            }

            Vector3 point;
            bool success = GetPointOnDragPlane(Editor.Pointer, out point);
            if (success)
            {
                Vector3 offset = _inverse.MultiplyVector(point - _prevPoint);
                float mag = offset.magnitude;
                HandleAxis axis = SelectedAxis;

                float x = _currentPosition.x, y = _currentPosition.y, z = _currentPosition.z;
                
                x = GetClosedValue(x);
                y = GetClosedValue(y);
                z = GetClosedValue(z);
                // Debug.Log(transform.forward);
                var trans = transform;
                switch(axis)
                {
                    case HandleAxis.X:
                        switch (trans.right)
                        {
                            case var value when value == Vector3.forward:
                                z = GetClosedValue(z);
                                break;
                            case var value when value == Vector3.up:
                                y = GetClosedValue(y);
                                break;
                            case var value when value == Vector3.right:
                                x = GetClosedValue(x);
                                break;
                        }
                        
                        offset.y = offset.z = 0.0f;
                        break;
                    case HandleAxis.Y:
                        switch (trans.up)
                        {
                            case var value when value == Vector3.forward:
                                z = GetClosedValue(z);
                                break;
                            case var value when value == Vector3.up:
                                y = GetClosedValue(y);
                                break;
                            case var value when value == Vector3.right:
                                x = GetClosedValue(x);
                                break;
                        }
                        
                        offset.x = offset.z = 0.0f;
                        break;
                    case HandleAxis.Z:
                        switch (trans.forward)
                        {
                            case var value when value == Vector3.forward:
                                z = GetClosedValue(z);
                                break;
                            case var value when value == Vector3.up:
                                y = GetClosedValue(y);
                                break;
                            case var value when value == Vector3.right:
                                x = GetClosedValue(x);
                                break;
                        }

                        offset.x = offset.y = 0.0f;
                        break;
                    case HandleAxis.XY:
                        switch (trans.right + trans.up)
                        {
                            case var value when value == new Vector3(1,1, 0):
                                x = GetClosedValue(x);
                                y = GetClosedValue(y);
                                break;
                            case var value when value == new Vector3(1,-1,0):
                                x = GetClosedValue(x);
                                y = GetClosedValue(y);
                                break;
                            case var value when value == new Vector3(-1,-1,0):
                                x = GetClosedValue(x);
                                y = GetClosedValue(y);
                                break;
                            case var value when value == new Vector3(-1,1,0):
                                x = GetClosedValue(x);
                                y = GetClosedValue(y);
                                break;
                        }
                        offset.z = 0;
                        break;
                    case HandleAxis.XZ:
                        switch (trans.right + trans.forward)
                        {
                            case var value when value == new Vector3(1,0, 1):
                                x = GetClosedValue(x);
                                z = GetClosedValue(z);
                                break;
                            case var value when value == new Vector3(1,0,-1):
                                x = GetClosedValue(x);
                                z = GetClosedValue(z);
                                break;
                            case var value when value == new Vector3(-1,0,-1):
                                x = GetClosedValue(x);
                                z = GetClosedValue(z);
                                break;
                            case var value when value == new Vector3(-1,0,1):
                                x = GetClosedValue(x);
                                z = GetClosedValue(z);
                                break;
                        }
                        offset.y = 0;
                        break;
                    case HandleAxis.YZ:
                        switch (trans.forward + trans.up)
                        {
                            case var value when value == new Vector3(0,1, 1):
                                y = GetClosedValue(y);
                                z = GetClosedValue(z);
                                break;
                            case var value when value == new Vector3(0,-1,1):
                                y = GetClosedValue(y);
                                z = GetClosedValue(z);
                                break;
                            case var value when value == new Vector3(0,-1,-1):
                                y = GetClosedValue(y);
                                z = GetClosedValue(z);
                                break;
                            case var value when value == new Vector3(0,1,-1):
                                y = GetClosedValue(y);
                                z = GetClosedValue(z);
                                break;
                        }
                        offset.x = 0;
                        break;
                }
        
                if (SharedLockObject != null)
                {
                    if (SharedLockObject.PositionX)
                    {
                        offset.x = 0.0f;
                    }
                    if (SharedLockObject.PositionY)
                    {
                        offset.y = 0.0f;
                    }
                    if (SharedLockObject.PositionZ)
                    {
                        offset.z = 0.0f;
                    }
                }

                Vector3 prevPosition = Position;
                Vector3 prevCurrentPosition = _currentPosition;
                if (EffectiveGridUnitSize == 0.0)
                {
                    offset = _matrix.MultiplyVector(offset).normalized * mag;
                    
                    transform.position += offset;
                    _currentPosition = Position;
                    _cursorPosition = Position;
                }
                else
                {

                    _currentPosition = new Vector3(x, y, z);
                    // _currentPosition = x * trans.right + y * trans.up + z * trans.forward;
                    offset = _matrix.MultiplyVector(offset).normalized * mag;
                    _cursorPosition += offset;
                    Vector3 toCurrentPosition = _cursorPosition - _currentPosition;
                    Vector3 gridOffset = Vector3.zero;
                    if (Mathf.Abs(toCurrentPosition.x * 1.5f) >= EffectiveGridUnitSize)
                    {
                        gridOffset.x = EffectiveGridUnitSize * Mathf.Sign(toCurrentPosition.x); 
                    }

                    if (Mathf.Abs(toCurrentPosition.y * 1.5f) >= EffectiveGridUnitSize)
                    {
                        gridOffset.y = EffectiveGridUnitSize * Mathf.Sign(toCurrentPosition.y);
                    }

                    if (Mathf.Abs(toCurrentPosition.z * 1.5f) >= EffectiveGridUnitSize)
                    {
                        gridOffset.z = EffectiveGridUnitSize * Mathf.Sign(toCurrentPosition.z);
                    }
                  
                    _currentPosition += gridOffset;
                    Position = _currentPosition;

                    if (SnapToGrid)
                    {
                        float gridSize = SizeOfGrid;
                        if (!Mathf.Approximately(gridSize, 0))
                        {
                            gridOffset = GetGridOffset(gridSize, Position);
                            _currentPosition += gridOffset;
                            Position = _currentPosition;
                        }
                    }
                }

                float allowedRadius = Editor.Camera.farClipPlane * 0.95f;
                Vector3 toHandle = Position - Editor.Camera.transform.position;
                if(toHandle.magnitude > allowedRadius)
                {
                    Position = prevPosition;
                    _currentPosition = prevCurrentPosition;
                }
                else
                {
                    _prevPoint = point;
                }
            }
        }
        float GetClosedValue(float a)
        {
            float d = Mathf.Abs(a);
            int b = (int) d;
            float c = d - b;
            if (Mathf.Abs(c-0.25f) < 0.1f)
            {
                c = 0.25f;
            }
            else if (Mathf.Abs(c-0.5f) < 0.1f)
            {
                c = 0.5f;
            }
            else if (Mathf.Abs(c-0.75f) < 0.1f)
            {
                c = 0.75f;
            }
            else if(c < 0.5f)
            {
                c = 0;
            }
            else
            {
                c = 1f;
            }

            c *= a > 0 ? 1f : -1f;
            b *= a > 0 ? 1 : -1;

            return b + c;
        }

        private void SnapActiveTargetsToGrid()
        {
            float gridSize = SizeOfGrid;
            if (Mathf.Approximately(gridSize, 0))
            {
                return;
            }

            for (int i = 0; i < ActiveTargets.Length; ++i)
            {
                Transform activeTransform = ActiveTargets[i];
                Vector3 position = activeTransform.position;

                Vector3 offset = GetGridOffset(gridSize, position);

                activeTransform.position += offset;
            }
        }

        private HandleDrawSettings _settings = new HandleDrawSettings();
        protected override void RefreshCommandBuffer(IMECamera camera)
        {
            _settings.Position = Position;
            _settings.Rotation = Rotation;
            _settings.SelectedAxis = SelectedAxis;
            _settings.LockObject = SharedLockObject;

            appearance.DoPositionHandle(camera.CommandBuffer, camera.Camera, _settings, IsInVertexSnappingMode || Editor.Tools.IsSnapping);
        }

        public override HandleAxis HitTest(out float distance)
        {
            _matrix = Matrix4x4.TRS(Position, Rotation, appearance.InvertZAxis ? new Vector3(1, 1, -1) : Vector3.one);
            _inverse = _matrix.inverse;

            return appearance.HitTestPositionHandle(Editor.Camera, Editor.Pointer, _settings, out distance);
        }
    }
}
