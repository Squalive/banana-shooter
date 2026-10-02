using System;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Handle
{
    public enum BoxSelectionMethod
    {
        Vertex,
        PixelPerfectDepthTest,
        LooseFitting,
        BoundsCenter,
        TansformCenter,
        Default = Vertex
    }

    public class BeginBoxSelectionArgs : EventArgs
    {
        public bool Cancel
        {
            get;
            set;
        }
    }

    public class FilteringArgs : EventArgs
    {
        private bool _cancel;

        public bool Cancel
        {
            get { return _cancel; }
            set
            {
                if (value) //can't reset cancel flag
                {
                    _cancel = true;
                }
            }
        }

        public GameObject Object
        {
            get;
            set;
        }

        public void Reset()
        {
            _cancel = false;
        }
    }

    public class BoxSelectionArgs
    {
        public GameObject[] GameObjects;
    }
    

    public interface IBoxSelection
    {
        event EventHandler<BeginBoxSelectionArgs> Begin;
        event EventHandler<FilteringArgs> Filtering;
        event EventHandler<BoxSelectionArgs> Selection;
        bool IsDragging
        {
            get;
        }

        bool IsThresholdPassed
        {
            get;
        }

        Bounds SelectionBounds
        {
            get;
        }

        Canvas Canvas
        {
            get;
        }

        BoxSelectionMethod MethodOverride
        {
            get;
            set;
        }
    }
    public class BoxSelection : MonoBehaviour,IBoxSelection
    {
        public static BoxSelection Instance { private set; get; }
        private IME _editor;
        public IME Editor => _editor;
        
        public Sprite Graphics;
        protected Image _image;
        protected RectTransform _rectTransform;
        protected Canvas _canvas;
        protected bool _isDragging;
        protected Vector3 _startMousePosition;
        protected Vector2 _startPt;
        protected Vector2 _endPt;

        private Camera _windowCanvasCamera;
        [SerializeField]
        [FormerlySerializedAs("m_windowRectTransform")]
        private RectTransform _windowRectTransform;

        public Vector2 ScreenSpaceMargin = new Vector2(2, 2);
        public bool UseCameraSpace = false;

        [SerializeField]
        [FormerlySerializedAs("m_method")]
        private BoxSelectionMethod _method = BoxSelectionMethod.Default;
        private BoxSelectionMethod _methodOverride = BoxSelectionMethod.Default;
        public BoxSelectionMethod MethodOverride
        {
            get
            {
                if(_methodOverride != BoxSelectionMethod.Default)
                {
                    return _methodOverride;
                }

                return _method;
            }
            set
            {
                _methodOverride = value;
            }
        }

        public event EventHandler<BeginBoxSelectionArgs> Begin;
        public event EventHandler<FilteringArgs> Filtering;
        public event EventHandler<BoxSelectionArgs> Selection;

        public Bounds SelectionBounds
        {
            get;
            private set;
        }

        public bool IsThresholdPassed
        {
            get { return _rectTransform != null && _rectTransform.sizeDelta.magnitude > 25f; }
        }

        public bool IsDragging
        {
            get { return _isDragging; }
        }       

        public Canvas Canvas
        {
            get { return _canvas; }
        }

        private void Awake()
        {
            Instance = this;
            _editor = MEBase.Instance;
            if (_canvas == null)
            {
                GameObject go = new GameObject("BoxSelection");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform.parent, false);
                _canvas = go.AddComponent<Canvas>();
                _canvas.sortingOrder = 2;
            }
            if (UseCameraSpace)
            {
                _canvas.worldCamera = MEBase.Instance.Camera;
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.planeDistance = MEBase.Instance.Camera.nearClipPlane + 0.05f;
                var transform1 = _canvas.transform;
                transform1.rotation = Quaternion.identity;
                transform1.position = Vector3.zero;
            }
            else
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            Transform transform2;
            (transform2 = transform).SetParent(_canvas.gameObject.transform, false);
            transform2.rotation = Quaternion.identity;
            transform2.position = Vector3.zero;

            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.referencePixelsPerUnit = 1;
            

            if(!GetComponent<BoxSelectionInput>())
            {
                gameObject.AddComponent<BoxSelectionInput>();
            }

            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }
            _rectTransform.sizeDelta = new Vector2(0, 0);
            _rectTransform.pivot = new Vector2(0, 0);
            _rectTransform.anchoredPosition = new Vector3(0, 0);

            _image = gameObject.AddComponent<Image>();
            _image.type = Image.Type.Sliced;
            if (Graphics == null)
            {
                Graphics = Resources.Load<Sprite>("Image/BoxSelection");
            }
            _image.sprite = Graphics;
            _image.raycastTarget = false;
            _selectionRenderer = new SelectionPicker(args => Filtering?.Invoke(this, args));
        }

        void Start()
        {
            Canvas windowCanvas = _windowRectTransform.GetComponentInParent<Canvas>();
            _windowCanvasCamera = windowCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? windowCanvas.worldCamera : null;
        }
        void OnDestroy()
        {
            if ( MEBase.Instance.Tools != null && MEBase.Instance.Tools.ActiveTool == this)
            {
                if (MEBase.Instance.Tools.ActiveTool == this)
                {
                    MEBase.Instance.Tools.ActiveTool = null;
                }
            }
        }
        
        public void BeginSelect()
        {
            // Debug.Log(MEBase.Instance.Tools.ActiveTool);
            if(MEBase.Instance.Tools.ActiveTool != null || _windowRectTransform == null)
            {
                return;
            }

            if(Begin != null)
            {
                BeginBoxSelectionArgs args = new BeginBoxSelectionArgs();
                Begin(this, args);
                if(args.Cancel)
                {
                    return;
                }
            }

            _startMousePosition = _editor.Pointer.ScreenPoint;
            _isDragging = GetPoint(out _startPt);
            if (_isDragging)
            {
                _rectTransform.anchoredPosition = _startPt;
                _rectTransform.sizeDelta = new Vector2(0, 0);
            }
        }

        public void EndSelect()
        {
            if (_isDragging)
            {
                
                _isDragging = false;

                HitTest();
                _rectTransform.sizeDelta = new Vector2(0, 0);
                if (MEBase.Instance.Tools.ActiveTool == this)
                {
                    MEBase.Instance.Tools.ActiveTool = null;
                }
            }
        }


        private void Update()
        {
            if (!_editor.Selection.Enabled)
            {
                return;
            }

            if (_editor.Tools.ActiveTool != this && _editor.Tools.ActiveTool != null)
            {
                return;
            }

            if (_editor.Tools.IsViewing || /*_editor.Tools.Current == RuntimeTool.None ||*/ !_editor.Tools.IsBoxSelectionEnabled)
            {
                if(_editor.Tools.ActiveTool == this)
                {
                    _editor.Tools.ActiveTool = null;
                }
                
                _isDragging = false;
                _rectTransform.sizeDelta = new Vector2(0, 0);
                return;
            }

            if (_isDragging)
            {
                GetPoint(out _endPt);

                Vector2 size = _endPt - _startPt;
                _editor.Tools.ActiveTool = this;
                // if (size != Vector2.zero)
                // {
                //     _editor.Tools.ActiveTool = this;
                // }
                _rectTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
                _rectTransform.localScale = new Vector3(Mathf.Sign(size.x), Mathf.Sign(size.y), 1);
            }
        }

        private SelectionPicker _selectionRenderer;
        private void HitTest()
        {
            if (!IsThresholdPassed)
            {
                return;
            }

            Vector3 center = (_startMousePosition + (Vector3)_editor.Pointer.ScreenPoint) / 2;
            center.z = 0.0f;

            Bounds selectionBounds = new Bounds(center, _rectTransform.sizeDelta);
            SelectionBounds = selectionBounds;

            FilteringArgs filteringArgs = new FilteringArgs();
            HashSet<GameObject> selection;
            Renderer[] renderers = FindObjectsOfType<Renderer>();

            selection = new HashSet<GameObject>();

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_editor.Camera);
            for (int i = 0; i < renderers.Length; ++i)
            {
                Renderer r = renderers[i];
                Bounds bounds = r.bounds;
                GameObject go = r.gameObject;
                TrySelect(ref selectionBounds, selection, filteringArgs, ref bounds, go, frustumPlanes);
            }

            if(Selection != null)
            {
                Selection(this, new BoxSelectionArgs { GameObjects = selection.ToArray() });
            }
            else
            {
                _editor.Selection.Objects = selection.ToArray();
            }
        }

        private HashSet<Renderer> FiterRenderers(IEnumerable<Renderer> renderers, FilteringArgs filteringArgs)
        {
            HashSet<Renderer> selection = new HashSet<Renderer>();
            foreach (Renderer rend in renderers)
            {
                if (!selection.Contains(rend))
                {
                    if (Filtering != null)
                    {
                        filteringArgs.Object = rend.gameObject;
                        Filtering(this, filteringArgs);
                        if (!filteringArgs.Cancel)
                        {
                            selection.Add(rend);
                        }
                        filteringArgs.Reset();
                    }
                    else
                    {
                        selection.Add(rend);
                    }
                }
            }

            return selection;
        }

        private void TrySelect(ref Bounds selectionBounds, HashSet<GameObject> selection, FilteringArgs args, ref Bounds bounds, GameObject go, Plane[] frustumPlanes)
        {
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
            {
                return;
            }
            bool select;
            if (MethodOverride == BoxSelectionMethod.LooseFitting)
            {
                select = LooseFitting(ref selectionBounds, ref bounds);              
            }
            else if(MethodOverride == BoxSelectionMethod.Vertex)
            {
                select = LooseFitting(ref selectionBounds, ref bounds);
                if (select && !selection.Contains(go))
                {
                    select = false;
                    MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        Vector3[] vertices = meshFilter.sharedMesh.vertices;

                        for (int i = 0; i < vertices.Length; ++i)
                        {
                            Vector3 vertex = go.transform.TransformPoint(vertices[i]);
                            vertex = _editor.Camera.WorldToScreenPoint(vertex);
                            vertex.z = 0;
                            if (selectionBounds.Contains(vertex))
                            {
                                select = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                        
                        if (smr != null && smr.sharedMesh != null)
                        {
                            Mesh bakedMesh = new Mesh();
                            smr.BakeMesh(bakedMesh);

                            Matrix4x4 m = Matrix4x4.TRS(go.transform.localPosition, go.transform.localRotation, Vector3.one);
                            if(smr.transform.parent != null)
                            {
                                m = m * smr.transform.parent.localToWorldMatrix;
                            }

                            Vector3[] vertices = bakedMesh.vertices;

                            for (int i = 0; i < vertices.Length; ++i)
                            {
                                Vector3 vertex = m.MultiplyPoint(vertices[i]);
                                vertex = _editor.Camera.WorldToScreenPoint(vertex);
                                vertex.z = 0;
                                if (selectionBounds.Contains(vertex))
                                {
                                    select = true;
                                    break;
                                }
                            }

                            Destroy(bakedMesh);
                        }
                    }
                    
                }
            }
            else if (MethodOverride == BoxSelectionMethod.BoundsCenter)
            {
                select = BoundsCenter(ref selectionBounds, ref bounds);
            }
            else
            {
                select = TransformCenter(ref selectionBounds, go.transform);
            }

            if (select)
            {
                FilterGameObjects(selection, args, go);
            }
        }

        private void FilterGameObjects(HashSet<GameObject> selection, FilteringArgs args, GameObject go)
        {
            if (!selection.Contains(go))
            {
                if (Filtering != null)
                {
                    args.Object = go;
                    Filtering(this, args);
                    if (!args.Cancel)
                    {
                        selection.Add(go);
                    }
                    args.Reset();
                }
                else
                {
                    selection.Add(go);
                }
            }
        }

        private bool TransformCenter(ref Bounds selectionBounds, Transform tr)
        {
            Vector3 screenPoint = _editor.Camera.WorldToScreenPoint(tr.position);
            screenPoint.z = 0;
            return selectionBounds.Contains(screenPoint);
        }

        private bool BoundsCenter(ref Bounds selectionBounds, ref Bounds bounds)
        {
            Vector3 screenPoint = _editor.Camera.WorldToScreenPoint(bounds.center);
            screenPoint.z = 0;
            return selectionBounds.Contains(screenPoint);
        }

        private bool LooseFitting(ref Bounds selectionBounds, ref Bounds bounds)
        {
            Vector3 p0 = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            Vector3 p1 = bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            Vector3 p2 = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            Vector3 p3 = bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z);
            Vector3 p4 = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z);
            Vector3 p5 = bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z);
            Vector3 p6 = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z);
            Vector3 p7 = bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z);

            p0 = _editor.Camera.WorldToScreenPoint(p0);
            p1 = _editor.Camera.WorldToScreenPoint(p1);
            p2 = _editor.Camera.WorldToScreenPoint(p2);
            p3 = _editor.Camera.WorldToScreenPoint(p3);
            p4 = _editor.Camera.WorldToScreenPoint(p4);
            p5 = _editor.Camera.WorldToScreenPoint(p5);
            p6 = _editor.Camera.WorldToScreenPoint(p6);
            p7 = _editor.Camera.WorldToScreenPoint(p7);

            float minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x, p4.x, p5.x, p6.x, p7.x);
            float maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x, p4.x, p5.x, p6.x, p7.x);
            float minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y, p4.y, p5.y, p6.y, p7.y);
            float maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y, p4.y, p5.y, p6.y, p7.y);
            Vector3 min = new Vector2(minX, minY);
            Vector3 max = new Vector2(maxX, maxY);

            Bounds b = new Bounds((min + max) / 2, (max - min));
            return selectionBounds.Intersects(b);
        }

        private bool GetPoint(out Vector2 localPoint)
        {
            Camera cam = null;
            if(_canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = _canvas.worldCamera;
            }

            Vector3 screenPoint;
            if (UseCameraSpace)
            {
                screenPoint = _editor.Pointer.ScreenPoint;
            }
            else
            {
                screenPoint = Input.mousePosition;
                Rect rect = _windowRectTransform.rect;

                Vector2 min = RectTransformUtility.WorldToScreenPoint(_windowCanvasCamera, _windowRectTransform.TransformPoint(rect.min)) + ScreenSpaceMargin;
                Vector2 max = RectTransformUtility.WorldToScreenPoint(_windowCanvasCamera, _windowRectTransform.TransformPoint(rect.max)) - ScreenSpaceMargin;

                screenPoint.x = Mathf.Clamp(screenPoint.x, min.x, max.x);
                screenPoint.y = Mathf.Clamp(screenPoint.y, min.y, max.y);
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.GetComponent<RectTransform>(), screenPoint, cam, out localPoint);
        }
    }
}