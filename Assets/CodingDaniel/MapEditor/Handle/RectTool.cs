using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

using CodingDaniel.MapEditor.Interaction;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Handle
{
    public class RectTool : BaseHandle
    {
        public float GridSize = 1.0f;

        [SerializeField]
        [FormerlySerializedAs("m_txtSize1")]
        private TextMeshPro _txtSize1 = null;
        [SerializeField]
        [FormerlySerializedAs("m_txtSize2")]
        private TextMeshPro _txtSize2 = null;
        [SerializeField]
        [FormerlySerializedAs("m_metric")]
        private bool _metric = true;
        public bool Metric
        {
            get { return _metric; }
            set
            {
                if (_metric != value)
                {
                    _metric = value;
                    UpdateText();
                }
            }
        }

        private Quaternion _rotation;
        private Vector3 _position;
        private Vector3 _localScale;
        private float _currentDot;
        private HandleAxis _currentAxis;
        private int _selectedPointIndex = -1;
        private int _selectedEdgeIndex = -1;
        private Vector3 _beginDragPoint;
        private Vector3 _beginDragOffset;
        private bool _isInRectTransformMode;

        private Vector3[] _referencePoints;
        private Bounds _referenceBounds;
        private Vector3[] _referenceScale;
        private Vector3[] _referencePositions;
        private Vector2[] _referenceRectSizes;

        private MeshFilter _lines;
        private MeshRenderer _linesRenderer;
        private MeshFilter _points;
        private MeshRenderer _pointsRenderer;

        public override EditorTool Tool
        {
            get { return EditorTool.Rect; }
        }

        protected override float CurrentGridUnitSize
        {
            get { return SizeOfGrid; }
        }

        public override float SizeOfGrid
        {
            get { return GridSize; }
            set { GridSize = value; }
        }

        private static readonly List<RectTool> _connectedTools = new List<RectTool>();
        private Bounds _bounds;
        private void SetBounds(Bounds value)
        {
            _bounds = value;
            UpdatePointsMesh(_points.sharedMesh, _currentAxis, _bounds);
            UpdateLinesMesh(_lines.sharedMesh, _currentAxis, _bounds);
            UpdateText();
        }

        protected override Transform[] Targets_Internal
        {
            get
            {
                return base.Targets_Internal;
            }
            set
            {
                base.Targets_Internal = value;
                RecalculateBoundsAndRebuild();

                Transform[] targets = ActiveRealTargets;
                _isInRectTransformMode = targets != null && targets.Length > 0 && targets.All(t => t is RectTransform);
            }
        }

        protected override MEPivotMode PivotMode
        {
            get { return MEPivotMode.Center; }
        }

        protected override Vector3 GetCommonCenterPosition()
        {
            return _bounds.center;
        }

        public void RecalculateBoundsAndRebuild()
        {
            _rotation = Quaternion.identity;
            _position = Vector3.zero;
            _localScale = Vector3.one;
            _bounds = new Bounds();

            if (_txtSize1 != null)
            {
                _txtSize1.text = string.Empty;
            }

            if (_txtSize2 != null)
            {
                _txtSize2.text = string.Empty;
            }


            if (RealTargets == null || RealTargets.Length == 0)
            {
                return;
            }

            if (ActiveRealTargets.Length == 1 && ActiveRealTargets[0].GetComponent<ExposeToEditor>())
            {
                ExposeToEditor exposeToEditor = ActiveRealTargets[0].GetComponent<ExposeToEditor>();

                _bounds = exposeToEditor.Bounds;
                _position = exposeToEditor.transform.position;
                _rotation = exposeToEditor.transform.rotation;
                _localScale = exposeToEditor.transform.lossyScale;

                if (_bounds.extents == Vector3.zero)
                {
                    if (_lines != null)
                    {
                        _lines.sharedMesh.Clear();
                    }

                    if (_points != null)
                    {
                        _points.sharedMesh.Clear();
                    }
                    return;
                }
            }
            else
            {
                Bounds[] allBounds = ActiveRealTargets.Where(t => t != null)
                    .SelectMany(t => t.GetComponentsInChildren<Renderer>())
                    .Select(r => r.bounds)
                    .Union(ActiveRealTargets
                        .OfType<RectTransform>()
                        .Select(rt => TransformExtensions.TransformBounds(rt.localToWorldMatrix, rt.CalculateRelativeRectTransformBounds())))
                    .ToArray();

                if (allBounds.Length == 0)
                {
                    if (_lines != null)
                    {
                        _lines.sharedMesh.Clear();
                    }

                    if (_points != null)
                    {
                        _points.sharedMesh.Clear();
                    }

                    return;
                }
                else
                {
                    _bounds = allBounds[0];
                    for (int i = 1; i < allBounds.Length; ++i)
                    {
                        Bounds bounds = allBounds[i];
                        _bounds.Encapsulate(bounds);
                    }
                }
            }

            if (_lines == null && _points == null)
            {
                return;
            }

            _lines.transform.position = _position;
            _lines.transform.rotation = _rotation;
            _lines.transform.localScale = _localScale;
            _points.transform.position = _position;
            _points.transform.rotation = _rotation;
            _points.transform.localScale = _localScale;

            _currentAxis = GetAxis(out _currentDot);
            BuildPointsMesh(_points.sharedMesh, _currentAxis, _bounds);
            BuildLineMesh(_lines.sharedMesh, _currentAxis, _bounds);


            UpdateText();
            UpdateFontSize();
        }

        protected override void Awake()
        {
            base.Awake();

            GameObject lines = new GameObject("Lines");
            lines.transform.SetParent(transform);

            _lines = lines.AddComponent<MeshFilter>();
            _lines.sharedMesh = new Mesh();

            _linesRenderer = lines.AddComponent<MeshRenderer>();

            Material lineMaterial = new Material(Shader.Find("CodingDaniel/MEBuilder/LineBillboard"));
            lineMaterial.SetFloat("_Scale", 1.0f);
            lineMaterial.SetColor("_Color", Color.white);
            lineMaterial.SetInt("_HandleZTest", (int)CompareFunction.Always);
            _linesRenderer.sharedMaterial = lineMaterial;

            GameObject points = new GameObject("Points");
            points.transform.SetParent(transform);

            _points = points.AddComponent<MeshFilter>();
            _points.sharedMesh = new Mesh();

            _pointsRenderer = points.AddComponent<MeshRenderer>();

            Material pointMaterial = new Material(Shader.Find("CodingDaniel/MEHandles/PointBillboard"));

            pointMaterial.SetFloat("_Scale", 4.5f);
            pointMaterial.SetColor("_Color", Color.white);
            pointMaterial.SetInt("_HandleZTest", (int)CompareFunction.Always);
            _pointsRenderer.sharedMaterial = pointMaterial;

#if UNITY_2020_1_OR_NEWER
            if (_txtSize1 != null && _txtSize2 != null)
            {
                CanvasRenderer canvasRenderer1 = _txtSize1.GetComponent<CanvasRenderer>();
                if (canvasRenderer1 != null)
                {
                    DestroyImmediate(canvasRenderer1);
                }

                CanvasRenderer canvasRenderer2 = _txtSize2.GetComponent<CanvasRenderer>();
                if (canvasRenderer2 != null)
                {
                    DestroyImmediate(canvasRenderer2);
                }
            }
#endif
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            RecalculateBoundsAndRebuild();
            _connectedTools.Add(this);

            if (MeCamera != null)
            {
                Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
                MeCamera.RenderersCache.Add(renderers, false, true);
                MeCamera.RenderersCache.Refresh();
            }

            float scale = appearance.HandleScale;
            if (_txtSize1 != null && _txtSize2 != null)
            {
                _txtSize1.transform.localScale = _txtSize2.transform.localScale = new Vector3(scale, scale, scale);
            }

            _linesRenderer.sharedMaterial.SetFloat("_Scale", scale);
            _pointsRenderer.sharedMaterial.SetFloat("_Scale", 4.5f * scale);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            _connectedTools.Remove(this);

            if (MeCamera != null)
            {
                Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
                if (MeCamera.RenderersCache != null)
                {
                    MeCamera.RenderersCache.Remove(renderers);
                    MeCamera.RenderersCache.Refresh();
                }
            }
        }


        protected override void UpdateOverride()
        {
            if (!IsDragging && !Editor.Tools.IsViewing)
            {
                
                SelectPointOrEdge();
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            UpdateFontSize();
        }

        private void UpdateConnectedTools()
        {
            for (int i = 0; i < _connectedTools.Count; ++i)
            {
                RectTool tool = _connectedTools[i];
                if (tool != this)
                {
                    tool.SetBounds(_bounds);
                    tool._lines.transform.position = _lines.transform.position;
                    tool._points.transform.position = _points.transform.position;
                }
            }
        }

        protected override bool OnBeginDrag()
        {
            if (!base.OnBeginDrag())
            {
                return false;
            }

            if (_bounds.extents == Vector3.zero)
            {
                return false;
            }

            if (IsAxisLocked(_currentAxis))
            {
                return false;
            }

            SelectPointOrEdge();

            if (_currentAxis == HandleAxis.XY)
            {
                DragPlane = new Plane(_lines.transform.forward, _lines.transform.TransformPoint(_bounds.center));
            }
            else if (_currentAxis == HandleAxis.XZ)
            {
                DragPlane = new Plane(_lines.transform.up, _lines.transform.TransformPoint(_bounds.center));
            }
            else
            {
                DragPlane = new Plane(_lines.transform.right, _lines.transform.TransformPoint(_bounds.center));
            }

            _referenceBounds = _bounds;
            _referenceBounds.extents = NonZero(_referenceBounds.extents);
            if (!GetPointOnDragPlane(Editor.Pointer, out _beginDragPoint))
            {
                return false;
            }

            if (_selectedEdgeIndex >= 0)
            {
                _referenceRectSizes = ActiveTargets.Select(t => (t as RectTransform) ? ((RectTransform)t).rect.size : Vector2.zero).ToArray();
                _referencePositions = ActiveTargets.Select(t => t.position).ToArray();
                _referenceScale = ActiveTargets.Select(t => t.localScale).ToArray();

                _referencePoints = _lines.sharedMesh.vertices;
                _beginDragPoint = _lines.transform.InverseTransformPoint(_beginDragPoint);

                Vector3 p0;
                Vector3 p1;
                _beginDragOffset = NonZero(GetOffset(_selectedEdgeIndex, _beginDragPoint, out p0, out p1));
                SetSelectionColorColors();
                return true;
            }
            else if (_selectedPointIndex >= 0)
            {
                _referenceRectSizes = ActiveTargets.Select(t => (t as RectTransform) ? ((RectTransform)t).rect.size : Vector2.zero).ToArray();
                _referencePositions = ActiveTargets.Select(t => t.position).ToArray();
                _referenceScale = ActiveTargets.Select(t => t.localScale).ToArray();
                _referencePoints = GetVertices().ToArray();

                if (_selectedPointIndex < _referencePoints.Length - 1)
                {
                    Vector3 refPoint;
                    _beginDragPoint = _points.transform.InverseTransformPoint(_beginDragPoint);
                    _beginDragOffset = NonZero(GetOffset(_selectedPointIndex, _beginDragPoint, out refPoint));
                }
                else
                {
                    RecalculateBoundsAndRebuild();
                    UpdateConnectedTools();
                }
                SetSelectionColorColors();
                return true;
            }

            return false;
        }

        protected override void OnDrag()
        {
            base.OnDrag();

            Vector3 pointOnPlane;
            if (GetPointOnDragPlane(Editor.Pointer, out pointOnPlane))
            {
                if (_selectedPointIndex == _referencePoints.Length - 1)
                {
                    Vector3 offset = pointOnPlane - _beginDragPoint;

                    _points.transform.position = _position + offset;
                    _lines.transform.position = _position + offset;

                    if (EffectiveGridUnitSize > 0.001)
                    {
                        Vector3 gridOffset = GetGridOffset(EffectiveGridUnitSize, _points.transform.position);
                        _points.transform.position += gridOffset;
                        _lines.transform.position += gridOffset;
                        offset += gridOffset;
                    }

                    for (int i = 0; i < ActiveTargets.Length; ++i)
                    {
                        ActiveTargets[i].position = _referencePositions[i] + offset;
                    }
                    UpdateText();
                }
                else
                {
                    Vector3 sign = Vector3.one;
                    if (_selectedPointIndex >= 0)
                    {
                        Vector3 refPoint;
                        pointOnPlane = _points.transform.InverseTransformPoint(pointOnPlane);
                        Vector3 offset = GetOffset(_selectedPointIndex, pointOnPlane, out refPoint);
                        if (EffectiveGridUnitSize > 0.001)
                        {
                            float gridSize = EffectiveGridUnitSize;
                            gridSize /= 2;

                            if (!Mathf.Approximately(_localScale.x, 0))
                            {
                                float gridSizeX = gridSize / _localScale.x;
                                offset.x = Mathf.RoundToInt(offset.x / gridSizeX) * gridSizeX;
                            }

                            if (!Mathf.Approximately(_localScale.y, 0))
                            {
                                float gridSizeY = gridSize / _localScale.y;
                                offset.y = Mathf.RoundToInt(offset.y / gridSizeY) * gridSizeY;
                            }

                            if (!Mathf.Approximately(_localScale.z, 0))
                            {
                                float gridSizeZ = gridSize / _localScale.z;

                                offset.z = Mathf.RoundToInt(offset.z / gridSizeZ) * gridSizeZ;
                            }
                        }

                        _bounds.center = refPoint + offset;
                        Vector3 extents = _bounds.extents;
                        if (_currentAxis == HandleAxis.XY)
                        {
                            offset.z = extents.z;
                            sign.x = Mathf.Sign(offset.x / _beginDragOffset.x);
                            sign.y = Mathf.Sign(offset.y / _beginDragOffset.y);

                        }
                        else if (_currentAxis == HandleAxis.XZ)
                        {
                            offset.y = extents.y;
                            sign.x = Mathf.Sign(offset.x / _beginDragOffset.x);
                            sign.z = Mathf.Sign(offset.z / _beginDragOffset.z);
                        }
                        else
                        {
                            offset.x = extents.x;
                            sign.y = Mathf.Sign(offset.y / _beginDragOffset.y);
                            sign.z = Mathf.Sign(offset.z / _beginDragOffset.z);
                        }
                        _bounds.extents = new Vector3(Mathf.Abs(offset.x), Mathf.Abs(offset.y), Mathf.Abs(offset.z));

                        UpdatePointsMesh(_points.sharedMesh, _currentAxis, _bounds);
                        UpdateLinesMesh(_lines.sharedMesh, _currentAxis, _bounds);
                        UpdateText();
                    }
                    else if (_selectedEdgeIndex >= 0)
                    {
                        pointOnPlane = _lines.transform.InverseTransformPoint(pointOnPlane);

                        Vector3 p0;
                        Vector3 p1;
                        Vector3 offset = GetOffset(_selectedEdgeIndex, pointOnPlane, out p0, out p1);
                        if (EffectiveGridUnitSize > 0.001)
                        {
                            float gridSize = EffectiveGridUnitSize;

                            if (!Mathf.Approximately(_localScale.x, 0))
                            {
                                float gridSizeX = gridSize / _localScale.x;
                                offset.x = Mathf.RoundToInt(offset.x / gridSizeX) * gridSizeX;
                            }

                            if (!Mathf.Approximately(_localScale.y, 0))
                            {
                                float gridSizeY = gridSize / _localScale.y;
                                offset.y = Mathf.RoundToInt(offset.y / gridSizeY) * gridSizeY;
                            }

                            if (!Mathf.Approximately(_localScale.z, 0))
                            {
                                float gridSizeZ = gridSize / _localScale.z;

                                offset.z = Mathf.RoundToInt(offset.z / gridSizeZ) * gridSizeZ;
                            }
                        }

                        Vector3 p2 = p1 + offset;
                        Vector3 ext = (p2 - p0) / 2;

                        _bounds.center = ((p0 + p1) + offset) / 2;

                        Vector3 extents = _bounds.extents;
                        if (_currentAxis == HandleAxis.XY)
                        {
                            ext.z = extents.z;
                            if (Mathf.Abs(offset.y) > Mathf.Abs(offset.x))
                            {
                                sign.y = Mathf.Sign(offset.y / _beginDragOffset.y);
                            }
                            else
                            {
                                sign.x = Mathf.Sign(offset.x / _beginDragOffset.x);
                            }
                        }
                        else if (_currentAxis == HandleAxis.XZ)
                        {
                            ext.y = extents.y;

                            if (Mathf.Abs(offset.z) > Mathf.Abs(offset.x))
                            {
                                sign.z = Mathf.Sign(offset.z / _beginDragOffset.z);
                            }
                            else
                            {
                                sign.x = Mathf.Sign(offset.x / _beginDragOffset.x);
                            }
                        }
                        else
                        {
                            ext.x = extents.x;
                            if (Mathf.Abs(offset.z) > Mathf.Abs(offset.y))
                            {
                                sign.z = Mathf.Sign(offset.z / _beginDragOffset.z);
                            }
                            else
                            {
                                sign.y = Mathf.Sign(offset.y / _beginDragOffset.y);
                            }
                        }
                        _bounds.extents = new Vector3(Mathf.Abs(ext.x), Mathf.Abs(ext.y), Mathf.Abs(ext.z));

                        UpdatePointsMesh(_points.sharedMesh, _currentAxis, _bounds);
                        UpdateLinesMesh(_lines.sharedMesh, _currentAxis, _bounds);
                        UpdateText();
                    }

                    for (int i = 0; i < ActiveTargets.Length; ++i)
                    {
                        Transform target = ActiveTargets[i];
                        Vector3 referenceScale = _referenceScale[i];

                        if (target is RectTransform)
                        {
                            Vector3 scale = Vector3.Scale(referenceScale,
                                   new Vector3(_bounds.extents.x / _referenceBounds.extents.x * sign.x,
                                           _bounds.extents.y / _referenceBounds.extents.y * sign.y,
                                           _bounds.extents.z / _referenceBounds.extents.z * sign.z));

                            RectTransform rt = (RectTransform)target;
                            Vector2 size = _referenceRectSizes[i];

                            if (!Mathf.Approximately(referenceScale.x, 0))
                            {
                                size.x *= scale.x / referenceScale.x;
                            }
                            if (!Mathf.Approximately(referenceScale.y, 0))
                            {
                                size.y *= scale.y / referenceScale.y;
                            }

                            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                        }
                        else
                        {
                            target.localScale = Vector3.Scale(referenceScale,
                                   new Vector3(_bounds.extents.x / _referenceBounds.extents.x * sign.x,
                                           _bounds.extents.y / _referenceBounds.extents.y * sign.y,
                                           _bounds.extents.z / _referenceBounds.extents.z * sign.z));
                        }

                        Vector3 pivotOffset = Vector3.zero;
                        ExposeToEditor exposeToEditor = target.GetComponent<ExposeToEditor>();
                        if (exposeToEditor != null)
                        {
                            pivotOffset = target.TransformVector(-exposeToEditor.Bounds.center);
                        }

                        target.position = _referencePositions[i] + (_points.transform.TransformPoint(_bounds.center) - _referencePositions[i]) + pivotOffset;
                    }
                }

                UpdateConnectedTools();
            }
        }

        protected override void OnDrop()
        {
            base.OnDrop();

            Targets = RealTargets;

            for (int i = 0; i < _connectedTools.Count; ++i)
            {
                RectTool tool = _connectedTools[i];
                if (tool != this)
                {
                    tool.RecalculateBoundsAndRebuild();
                }
            }


            _referencePoints = null;
            _referencePositions = null;
            _referenceScale = null;
            _referenceRectSizes = null;
        }

        private Vector3 GetOffset(int selectedEdgeIndex, Vector3 pointOnPlane, out Vector3 p0, out Vector3 p1)
        {
            p0 = _referencePoints[((selectedEdgeIndex + 2) % 4) * 2];
            p1 = _referencePoints[((selectedEdgeIndex + 2) % 4) * 2 + 1];

            Vector3 nearest = NearestPointOnLine(p0, p1 - p0, pointOnPlane);
            Vector3 beginNearest = NearestPointOnLine(_referencePoints[selectedEdgeIndex * 2],
                _referencePoints[selectedEdgeIndex * 2 + 1] - _referencePoints[selectedEdgeIndex * 2], _beginDragPoint);

            Vector3 delta = beginNearest - _beginDragPoint;
            Vector3 offset = pointOnPlane + delta - nearest;

            return offset;
        }

        private Vector3 GetOffset(int selectedPointIndex, Vector3 pointOnPlane, out Vector3 refPoint)
        {
            refPoint = _referencePoints[(selectedPointIndex + 2) % 4];

            Vector3 delta = _referencePoints[_selectedPointIndex] - _beginDragPoint;

            Vector3 offset = (pointOnPlane + delta - refPoint) / 2;
            return offset;
        }

        private Vector3 NonZero(Vector3 v)
        {
            if (Mathf.Approximately(v.x, .0f))
            {
                v.x = 0.000000001f;
            }
            if (Mathf.Approximately(v.y, .0f))
            {
                v.y = 0.000000001f;
            }
            if (Mathf.Approximately(v.z, .0f))
            {
                v.z = 0.000000001f;
            }
            return v;
        }

        private bool IsAxisLocked(HandleAxis axis)
        {
            if (SharedLockObject == null)
            {
                return false;
            }

            switch (axis)
            {
                case HandleAxis.XY:
                    return SharedLockObject.RectXY;
                case HandleAxis.YZ:
                    return SharedLockObject.RectYZ;
                case HandleAxis.XZ:
                    return SharedLockObject.RectXZ;
                default:
                    return false;
            }
        }

        private HandleAxis GetAxis(out float dot)
        {
            if (_isInRectTransformMode)
            {
                dot = 1;
                return HandleAxis.XY;
            }

            Camera camera = Editor.Camera;
            Vector3 camForward = camera.orthographic ?
                camera.transform.forward :
                (_lines.transform.position - camera.transform.position).normalized;

            float dotZ = Vector3.Dot(_lines.transform.forward, camForward);
            float dotY = Vector3.Dot(_lines.transform.up, camForward);
            float dotX = Vector3.Dot(_lines.transform.right, camForward);
            float zDotAbs = Mathf.Abs(dotZ);
            float yDotAbs = Mathf.Abs(dotY);
            float xDotAbs = Mathf.Abs(dotX);

            if (IsAxisLocked(HandleAxis.XY))
            {
                zDotAbs = 0.1f;
            }

            if (IsAxisLocked(HandleAxis.XZ))
            {
                yDotAbs = 0.1f;
            }

            if (IsAxisLocked(HandleAxis.YZ))
            {
                xDotAbs = 0.1f;
            }

            if (zDotAbs >= yDotAbs && zDotAbs >= xDotAbs)
            {
                dot = dotZ;
                return HandleAxis.XY;
            }

            if (yDotAbs >= xDotAbs && yDotAbs >= zDotAbs)
            {
                dot = dotY;
                return HandleAxis.XZ;
            }

            dot = dotX;
            return HandleAxis.YZ;
        }

        private void BuildPointsMesh(Mesh target, HandleAxis axis, Bounds bounds)
        {
            if (IsAxisLocked(axis))
            {
                target.Clear();
                return;
            }

            Color color;
            Vector3[] vertices;

            GetVerticesAndColors(axis, bounds, out color, out vertices);

            int[] indices = new[]
            {
                0, 1, 2, 3, 4
            };

            Color[] colors = new[]
            {
                color, color, color, color, color
            };

            target.Clear();
            target.subMeshCount = 1;
            target.name = "RectToolVertices";

            if (SystemInfo.supportsGeometryShaders)
            {
                target.vertices = vertices;
                target.SetIndices(indices, MeshTopology.Points, 0);
                target.colors = colors;
            }
            else
            {
                GraphicsUtility.CreatePointBillboardMesh(vertices, indices, colors, target);
            }


            target.RecalculateBounds();
        }

        private void UpdatePointsMesh(Mesh target, HandleAxis axis, Bounds bounds)
        {
            if (IsAxisLocked(axis))
            {
                target.Clear();
                return;
            }

            Vector3[] vertices = GetVertices(axis, bounds);
            if (SystemInfo.supportsGeometryShaders)
            {
                target.vertices = vertices;
            }
            else
            {
                GraphicsUtility.UpdatePointBillboardMeshVertices(vertices, target);
            }
            target.RecalculateBounds();
        }

        private void BuildLineMesh(Mesh target, HandleAxis axis, Bounds bounds)
        {
            if (IsAxisLocked(axis))
            {
                target.Clear();
                return;
            }

            Color color;
            Vector3[] v;

            GetVerticesAndColors(axis, bounds, out color, out v);

            Vector3[] vertices = new[]
            {
                v[0], v[1], v[1], v[2], v[2], v[3], v[3], v[0]
            };

            int[] indices = new[]
            {
                0, 1, 2, 3, 4, 5, 6, 7
            };

            Color[] colors = new[]
            {
                color, color, color, color, color, color, color, color
            };

            target.Clear();
            target.subMeshCount = 1;
            target.name = "RectToolLines";
            target.vertices = vertices;
            target.SetIndices(indices, MeshTopology.Lines, 0);
            target.colors = colors;
            target.RecalculateBounds();
        }

        private void UpdateLinesMesh(Mesh target, HandleAxis axis, Bounds bounds)
        {
            if (IsAxisLocked(axis))
            {
                target.Clear();
                return;
            }

            Vector3[] v = GetVertices(axis, bounds);
            target.vertices = new[]
            {
                v[0], v[1], v[1], v[2], v[2], v[3], v[3], v[0]
            };
            target.RecalculateBounds();
        }

        private void UpdateText()
        {
            if (_txtSize1 == null && _txtSize2 == null)
            {
                return;
            }

            if (_points == null)
            {
                return;
            }


            Vector3[] v = GetVertices();
            if (_txtSize1 != null)
            {
                if (IsAxisLocked(_currentAxis))
                {
                    _txtSize1.gameObject.SetActive(false);
                }
                else
                {
                    _txtSize1.gameObject.SetActive(true);

                    float size;
                    Quaternion textRotation;
                    if (_currentAxis == HandleAxis.XY)
                    {
                        size = _bounds.size.x * _localScale.x;
                        textRotation = Mathf.Sign(_currentDot) > 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
                    }
                    else if (_currentAxis == HandleAxis.XZ)
                    {
                        size = _bounds.size.x * _localScale.x;
                        textRotation = Mathf.Sign(_currentDot) > 0 ? Quaternion.Euler(270, 0, 180) : Quaternion.Euler(90, 0, 0);
                    }
                    else
                    {
                        textRotation = Mathf.Sign(_currentDot) > 0 ? Quaternion.Euler(180, -90, -90) : Quaternion.Euler(0, -90, -90);
                        size = _bounds.size.y * _localScale.y;
                    }

                    _txtSize1.transform.localRotation = _rotation * textRotation;
                    _txtSize1.transform.position = _points.transform.TransformPoint(v[0] + (v[1] - v[0]) / 2);
                    _txtSize1.text = _metric ? size.ToString("F2") : UnitsConverter.MetersToFeetInches(size);
                }
            }

            if (_txtSize2 != null)
            {
                if (IsAxisLocked(_currentAxis))
                {
                    _txtSize2.gameObject.SetActive(false);
                }
                else
                {
                    _txtSize2.gameObject.SetActive(true);

                    float size;
                    Quaternion textRotation;
                    Vector3 position = _points.transform.TransformPoint(v[1] + (v[2] - v[1]) / 2);
                    if (_currentAxis == HandleAxis.XY)
                    {
                        size = _bounds.size.y * _localScale.y;
                        textRotation = Mathf.Sign(_currentDot) > 0 ? Quaternion.Euler(0, 0, 90) : Quaternion.Euler(180, 0, 90);
                    }
                    else if (_currentAxis == HandleAxis.XZ)
                    {
                        size = _bounds.size.z * _localScale.z;
                        textRotation = Mathf.Sign(_currentDot) > 0 ? Quaternion.Euler(270, 0, 90) : Quaternion.Euler(90, 0, 90);
                    }
                    else
                    {
                        size = _bounds.size.z * _localScale.z;
                        textRotation = Mathf.Sign(_currentDot) > 0 ? Quaternion.Euler(0, -270, 0) : Quaternion.Euler(0, -90, 0);
                        position = _points.transform.TransformPoint(v[0] + (v[3] - v[0]) / 2);
                    }

                    _txtSize2.transform.localRotation = _rotation * textRotation;
                    _txtSize2.transform.position = position;
                    _txtSize2.text = _metric ? size.ToString("F2") : UnitsConverter.MetersToFeetInches(size);
                }
            }
        }

        private void UpdateFontSize()
        {
            if (_txtSize1 != null)
            {
                _txtSize1.fontSize = GraphicsUtility.GetScreenScale(_txtSize1.transform.position, Editor.Camera) * 1.7f;
            }

            if (_txtSize2 != null)
            {
                _txtSize2.fontSize = GraphicsUtility.GetScreenScale(_txtSize2.transform.position, Editor.Camera) * 1.7f;
            }
        }

        private Color GetColor(HandleAxis axis)
        {
            if (axis == HandleAxis.XY)
            {
                return appearance.Colors.ZColor;
            }
            else if (axis == HandleAxis.XZ)
            {
                return appearance.Colors.YColor;
            }

            return appearance.Colors.XColor;
        }

        private void GetVerticesAndColors(HandleAxis axis, Bounds bounds, out Color color, out Vector3[] vertices)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            if (axis == HandleAxis.XY)
            {
                color = appearance.Colors.ZColor;
                vertices = new[]
                {
                    c + new Vector3(e.x, e.y, 0), c + new Vector3(-e.x, e.y, 0), c + new Vector3(-e.x, -e.y, 0), c + new Vector3(e.x, -e.y, 0), c
                };
            }
            else if (axis == HandleAxis.XZ)
            {
                color = appearance.Colors.YColor;
                vertices = new[]
                {
                    c + new Vector3(e.x, 0, e.z), c + new Vector3(-e.x, 0, e.z), c + new Vector3(-e.x, 0, -e.z), c + new Vector3(e.x, 0, -e.z), c
                };
            }
            else
            {
                color = appearance.Colors.XColor;
                vertices = new[]
                {
                    c + new Vector3(0, e.y, e.z), c + new Vector3(0, -e.y, e.z), c + new Vector3(0, -e.y, -e.z), c + new Vector3(0, e.y, -e.z), c
                };
            }
        }

        private Vector3[] _getVertRes = new Vector3[5];
        private Vector3[] GetVertices(HandleAxis axis, Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            if (axis == HandleAxis.XY)
            {
                _getVertRes[0] = c + new Vector3(e.x, e.y, 0);
                _getVertRes[1] = c + new Vector3(-e.x, e.y, 0);
                _getVertRes[2] = c + new Vector3(-e.x, -e.y, 0);
                _getVertRes[3] = c + new Vector3(e.x, -e.y, 0);
                _getVertRes[4] = c;
            }
            else if (axis == HandleAxis.XZ)
            {
                _getVertRes[0] = c + new Vector3(e.x, 0, e.z);
                _getVertRes[1] = c + new Vector3(-e.x, 0, e.z);
                _getVertRes[2] = c + new Vector3(-e.x, 0, -e.z);
                _getVertRes[3] = c + new Vector3(e.x, 0, -e.z);
                _getVertRes[4] = c;
            }
            else
            {
                _getVertRes[0] = c + new Vector3(0, e.y, e.z);
                _getVertRes[1] = c + new Vector3(0, -e.y, e.z);
                _getVertRes[2] = c + new Vector3(0, -e.y, -e.z);
                _getVertRes[3] = c + new Vector3(0, e.y, -e.z);
                _getVertRes[4] = c;
            }

            return _getVertRes;
        }

        private Vector3[] GetVertices()
        {
            return GetVertices(_currentAxis, _bounds);
        }

        private struct PickResult
        {
            public int Index;
            public float Distance;

            public PickResult(int index, float distance)
            {
                Index = index;
                Distance = distance;
            }
        }

        private PickResult PickPoint(Vector3[] points, float maxDistance = 20.0f)
        {
            maxDistance *= appearance.SelectionMargin;

            int minIndex = -1;
            float minDistance = maxDistance * maxDistance;
            Vector3 screenPoint = Editor.Pointer.ScreenPoint;
            for (int i = 0; i < points.Length; ++i)
            {
                Vector3 point = points[i];
                point = _points.transform.transform.TransformPoint(point);
                point = Editor.Camera.WorldToScreenPoint(point);
                point.z = screenPoint.z;

                float dist = (point - screenPoint).sqrMagnitude;

                if (dist < minDistance)
                {
                    minIndex = i;
                    minDistance = dist;
                }
            }

            return new PickResult(minIndex, minDistance);
        }

        private PickResult PickEdge(Vector3[] points, float maxDistance = 20.0f)
        {
            maxDistance *= appearance.SelectionMargin;

            int minIndex = -1;
            float minDistance = maxDistance * maxDistance;
            Vector3 screenPoint = Editor.Pointer.ScreenPoint;

            for (int i = 0; i < points.Length - 2; ++i)
            {
                Vector3 p0 = points[i];
                Vector3 p1 = points[(i + 1) % points.Length];
                TryPickEdge(p0, p1, screenPoint, i, ref minDistance, ref minIndex);
            }
            TryPickEdge(points[3], points[0], screenPoint, 3, ref minDistance, ref minIndex);

            return new PickResult(minIndex, minDistance);
        }

        private void TryPickEdge(Vector3 p0, Vector3 p1, Vector3 screenPoint, int i, ref float minDistance, ref int minIndex)
        {
            p0 = _points.transform.transform.TransformPoint(p0);
            p1 = _points.transform.transform.TransformPoint(p1);
            p0 = Editor.Camera.WorldToScreenPoint(p0);
            p1 = Editor.Camera.WorldToScreenPoint(p1);
            p0.z = p1.z = screenPoint.z;

            Vector3 nearest = NearestPointOnSegment(p0, p1, screenPoint);

            float dist = (nearest - screenPoint).sqrMagnitude;

            if (dist < minDistance)
            {
                minIndex = i;
                minDistance = dist;
            }
        }

        private Vector2 NearestPointOnSegment(Vector2 origin, Vector2 end, Vector2 point)
        {
            Vector2 heading = (end - origin);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            Vector2 lhs = point - origin;
            float dotP = Vector2.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return origin + heading * dotP;
        }

        public Vector3 NearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
        {
            lineDir.Normalize();//this needs to be a unit vector
            var v = pnt - linePnt;
            var d = Vector3.Dot(v, lineDir);
            return linePnt + lineDir * d;
        }

        private void SetSelectionColorColors()
        {
            Color[] colors = _points.sharedMesh.colors;
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = appearance.Colors.SelectionColor;
            }
            _points.sharedMesh.colors = colors;
            colors = _lines.sharedMesh.colors;
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = appearance.Colors.SelectionColor;
            }
            _lines.sharedMesh.colors = colors;
        }

        private void SelectPointOrEdge()
        {
            HandleAxis axis = GetAxis(out _currentDot);
            if (_currentAxis != axis)
            {
                _currentAxis = axis;
                RecalculateBoundsAndRebuild();
                _selectedPointIndex = -1;
                _selectedEdgeIndex = -1;
            }

            if (_points.sharedMesh.vertexCount == 0)
            {
                return;
            }

            Vector3[] vertices = GetVertices();
            PickResult pointPickResult = PickPoint(vertices);
            pointPickResult.Distance *= 0.1f;
            PickResult edgePickResult = PickEdge(vertices);

            if (pointPickResult.Distance < edgePickResult.Distance)
            {
                if (_selectedEdgeIndex != -1)
                {
                    Color[] colors = _lines.sharedMesh.colors;
                    Color color = GetColor(_currentAxis);
                    colors[_selectedEdgeIndex * 2] = color;
                    colors[_selectedEdgeIndex * 2 + 1] = color;
                    _lines.sharedMesh.colors = colors;

                    _selectedEdgeIndex = -1;
                }

                if (pointPickResult.Index != _selectedPointIndex)
                {
                    Color[] colors = _points.sharedMesh.colors;
                    if (_selectedPointIndex >= 0)
                    {
                        if (SystemInfo.supportsGeometryShaders)
                        {
                            colors[_selectedPointIndex] = GetColor(_currentAxis);
                        }
                        else
                        {
                            Color color = GetColor(_currentAxis);
                            for (int i = 0; i < 4; ++i)
                            {
                                colors[_selectedPointIndex * 4 + i] = color;
                            }
                        }
                    }

                    _selectedPointIndex = pointPickResult.Index;

                    if (_selectedPointIndex >= 0)
                    {
                        if (SystemInfo.supportsGeometryShaders)
                        {
                            colors[_selectedPointIndex] = appearance.Colors.SelectionColor;
                        }
                        else
                        {
                            Color color = appearance.Colors.SelectionColor;
                            for (int i = 0; i < 4; ++i)
                            {
                                colors[_selectedPointIndex * 4 + i] = color;
                            }
                        }
                    }

                    _points.sharedMesh.colors = colors;
                }
            }
            else if (edgePickResult.Distance < pointPickResult.Distance)
            {
                if (_selectedPointIndex != -1)
                {
                    Color[] colors = _points.sharedMesh.colors;

                    if (SystemInfo.supportsGeometryShaders)
                    {
                        colors[_selectedPointIndex] = GetColor(_currentAxis);
                    }
                    else
                    {
                        Color color = GetColor(_currentAxis);
                        for (int i = 0; i < 4; ++i)
                        {
                            colors[_selectedPointIndex * 4 + i] = color;
                        }
                    }

                    _points.sharedMesh.colors = colors;
                    _selectedPointIndex = -1;
                }

                if (edgePickResult.Index != _selectedEdgeIndex)
                {
                    Color[] colors = _lines.sharedMesh.colors;
                    if (_selectedEdgeIndex >= 0)
                    {
                        Color color = GetColor(_currentAxis);
                        colors[_selectedEdgeIndex * 2] = color;
                        colors[_selectedEdgeIndex * 2 + 1] = color;
                    }

                    _selectedEdgeIndex = edgePickResult.Index;

                    if (_selectedEdgeIndex >= 0)
                    {
                        Color color = appearance.Colors.SelectionColor;
                        colors[_selectedEdgeIndex * 2] = color;
                        colors[_selectedEdgeIndex * 2 + 1] = color;
                    }
                }
            }
        }

    }
}