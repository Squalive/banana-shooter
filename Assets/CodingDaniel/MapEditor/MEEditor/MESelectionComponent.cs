using System;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using BaseHandle = CodingDaniel.MapEditor.Handle.BaseHandle;
using UnityObject = UnityEngine.Object;
using CodingDaniel.MapEditor.Interaction;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.MEEditor
{
    public enum FocusMode
    {
        Selected,
        AllActive,
        Default = Selected
    }

    public interface IScenePivot
    {
        Vector3 Pivot
        {
            get;
            set;
        }

        Vector3 SecondaryPivot
        {
            get;
            set;
        }

        Vector3 CameraPosition
        {
            get;
            set;
        }

        // bool IsOrthographic
        // {
        //     get;
        //     set;
        // }
        //
        // float OrthographicSize
        // {
        //     get;
        //     set;
        // }

        [Obsolete]
        void Focus();
        void Focus(FocusMode mode = FocusMode.Default);
        void Focus(Vector3 objPosition, float objSize);
        void SetCameraPositionAndPivot(Vector3 position, Vector3 pivot);
    }
    public class MESelectionChangingArgs : EventArgs
    {
        public bool Cancel
        {
            get;
            set;
        }

        public IList<UnityObject> Selected
        {
            get;
            private set;
        }

        public MESelectionChangingArgs(IEnumerable<UnityObject> selected)
        {
            Selected = selected.ToList();
        }
    }

    public class MESelectionFilteringArgs : EventArgs
    {
        public IList<RaycastHit> Hits
        {
            get;
            private set;
        }

        public MESelectionFilteringArgs(IEnumerable<RaycastHit> hits)
        {
            Hits = hits.ToList();
        }
    }
    public interface IMESelectionComponent : IScenePivot
    {

        PositionHandle PositionHandle
        {
            get;
        }

        RotationHandle RotationHandle
        {
            get;
        }
        
        ScaleHandle ScaleHandle
        {
            get;
        }
        //
        // RectTool RectTool
        // {
        //     get;
        // }

        BaseHandle CustomHandle
        {
            get;
            set;
        }

        BoxSelection BoxSelection
        {
            get;
        }

        bool IsPositionHandleEnabled
        {
            get;
            set;
        }

        bool IsRotationHandleEnabled
        {
            get;
            set;
        }
        
        bool IsScaleHandleEnabled
        {
            get;
            set;
        }
        //
        // bool IsRectToolEnabled
        // {
        //     get;
        //     set;
        // }

        bool IsBoxSelectionEnabled
        {
            get;
            set;
        }

        bool IsSelectionVisible
        {
            get;
            set;
        }

        bool CanSelect
        {
            get;
            set;
        }

        bool CanSelectAll
        {
            get;
            set;
        }

        float SizeOfGrid
        {
            get;
            set;
        }


        IMESelection Selection
        {
            get;
            set;
        }

        Transform[] GetHandleTargets();
    }
    
    public class MESelectionComponent : MonoBehaviour,IMESelectionComponent
    {
        public static MESelectionComponent Instance { private set; get; }

        private IME _editor;
        
        public event EventHandler<MESelectionFilteringArgs> Filtering;
        public event EventHandler<MESelectionChangingArgs> SelectionChanging;
        public event EventHandler SelectionChanged;

        [SerializeField]
        [FormerlySerializedAs("m_outlineManager")]
        private OutlineManager _outlineManager = null;
        [SerializeField]
        [FormerlySerializedAs("m_positionHandle")]
        private PositionHandle _positionHandle = null;
        [SerializeField]
        [FormerlySerializedAs("m_rotationHandle")]
        private RotationHandle _rotationHandle = null;
        [SerializeField]
        [FormerlySerializedAs("m_scaleHandle")]
        private ScaleHandle _scaleHandle = null;
        [SerializeField]
        [FormerlySerializedAs("m_rectTool")]
        private RectTool _rectTool = null;
        [SerializeField]
        [FormerlySerializedAs("m_customHandle")]
        private BaseHandle _customHandle = null;
        [SerializeField]
        [FormerlySerializedAs("m_boxSelection")]
        private BoxSelection _boxSelection = null;
        [SerializeField]
        [FormerlySerializedAs("m_grid")]
        private SceneGridVisual _grid = null;
        [SerializeField]
        [FormerlySerializedAs("m_pivot")]
        private Transform _pivot = null;
        [SerializeField]
        [FormerlySerializedAs("m_secondaryPivot")]
        private Transform _secondaryPivot = null;
        
        

        protected Transform PivotTransform
        {
            get { return _pivot; }
        }

        protected Transform SecondaryPivotTransform
        {
            get { return _secondaryPivot; }
        }
        
        public virtual bool IsOrthographic
        {
            get { return _editor.Camera.orthographic; }
            set { _editor.Camera.orthographic = value; }
        }

        public virtual float OrthographicSize
        {
            get { return _editor.Camera.orthographicSize; }
            set { _editor.Camera.orthographicSize = value; }
        }

        public virtual Vector3 CameraPosition
        {
            get { return _editor.Camera.transform.position; }
            set
            {
                _editor.Camera.transform.position = value;
                _editor.Camera.transform.LookAt(Pivot);
            }
        }

        public void Focus()
        {
            
        }

        public void Focus(FocusMode mode = FocusMode.Selected)
        {
        }

        public void Focus(Vector3 objPosition, float objSize)
        {
        }

        public void SetCameraPositionAndPivot(Vector3 position, Vector3 pivot)
        {
            _editor.Camera.transform.position = position;
            _editor.Camera.transform.LookAt(pivot);
            _pivot.transform.position = pivot;
        }

        public virtual Vector3 Pivot
        {
            get { return _pivot.transform.position; }
            set
            {
                _pivot.transform.position = value;
                _editor.Camera.transform.LookAt(Pivot);
            }
        }
        
        public virtual Vector3 SecondaryPivot
        {
            get { return _secondaryPivot.transform.position; }
            set { _secondaryPivot.transform.position = value; }
        }
        
        public BoxSelection BoxSelection
        {
            get { return _boxSelection; }
        }

        public PositionHandle PositionHandle
        {
            get { return _positionHandle; }
        }
        private bool _isPositionHandleEnabled = true;
        //[SerializeField]
        [FormerlySerializedAs("m_isRotationHandleEnabled")]
        private bool _isRotationHandleEnabled = true;
        //[SerializeField]
        [FormerlySerializedAs("m_isScaleHandleEnabled")]
        private bool _isScaleHandleEnabled = true;
        //[SerializeField]
        [FormerlySerializedAs("m_isRectToolEnabled")]
        private bool _isRectToolEnabled = true;
        //[SerializeField]
        [FormerlySerializedAs("m_isBoxSelectionEnabled")]
        private bool _isBoxSelectionEnabled = true;
        public bool IsPositionHandleEnabled
        {
            get { return _isPositionHandleEnabled && _positionHandle != null; }
            set
            {
                _isPositionHandleEnabled = value;
                if (_positionHandle != null)
                {
                    if (value && _editor.Tools.Current == EditorTool.Move)
                    {
                        _positionHandle.Targets = GetHandleTargets();
                    }
                    _positionHandle.gameObject.SetActive(value && _editor.Tools.Current == EditorTool.Move && _positionHandle.Target != null);
                }
            }
        }
        public bool IsRotationHandleEnabled
        {
            get { return _isRotationHandleEnabled && _rotationHandle != null; }
            set
            {
                _isRotationHandleEnabled = value;
                if (_rotationHandle != null)
                {
                    if (value && _editor.Tools.Current == EditorTool.Rotate)
                    {
                        _rotationHandle.Targets = GetHandleTargets();
                    }
                    _rotationHandle.gameObject.SetActive(value && _editor.Tools.Current == EditorTool.Rotate && _rotationHandle.Target != null);
                }
            }
        }
        
        public bool IsScaleHandleEnabled
        {
            get { return _isScaleHandleEnabled && _scaleHandle != null; }
            set
            {
                _isScaleHandleEnabled = value;
                if (_scaleHandle != null)
                {
                    if (value && _editor.Tools.Current == EditorTool.Scale)
                    {
                        _scaleHandle.Targets = GetHandleTargets();
                    }
                    _scaleHandle.gameObject.SetActive(value && _editor.Tools.Current == EditorTool.Scale && _scaleHandle.Target != null);
                }
            }
        }
        
        public bool IsRectToolEnabled
        {
            get { return _isRectToolEnabled && _rectTool != null; }
            set
            {
                _isRectToolEnabled = value;
                if (_rectTool != null)
                {
                    if (value && _editor.Tools.Current == EditorTool.Rect)
                    {
                        _rectTool.Targets = GetHandleTargets();
                    }
                    _rectTool.gameObject.SetActive(value && _editor.Tools.Current == EditorTool.Rect && _rectTool.Target != null);
                }
            }
        }

        public bool IsBoxSelectionEnabled
        {
            get { return _isBoxSelectionEnabled && _boxSelection != null; }
            set
            {
                _isBoxSelectionEnabled = value;
                if (_boxSelection != null)
                {
                    _boxSelection.enabled = _isBoxSelectionEnabled;
                }
            }
        }
        public RotationHandle RotationHandle
        {
            get { return _rotationHandle; }
        }
        
        public ScaleHandle ScaleHandle
        {
            get { return _scaleHandle; }
        }
        //
        // public RectTool RectTool
        // {
        //     get { return _rectTool; }
        // }
        public BaseHandle CustomHandle
        {
            get { return _customHandle; }
            set
            {
                if (_customHandle == value)
                {
                    return;
                }

                if (_customHandle != null)
                {
                    _customHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                    _customHandle.Drop.RemoveListener(OnDrop);
                }

                _customHandle = value;

                if (_customHandle != null)
                {
                    _customHandle.gameObject.SetActive(false);

                    if (_customHandle.BeforeDrag == null)
                    {
                        _customHandle.BeforeDrag = new BaseHandleUnityEvent();
                    }
                    _customHandle.BeforeDrag.AddListener(OnBeforeDrag);

                    if (_customHandle.Drop == null)
                    {
                        _customHandle.Drop = new BaseHandleUnityEvent();
                    }
                    _customHandle.Drop.AddListener(OnDrop);

                    if (_editor.Tools.Current == EditorTool.Custom)
                    {
                        Transform[] targets = GetHandleTargets();
                        if (targets != null && targets.Length > 0)
                        {
                            _customHandle.Targets = targets;
                            _customHandle.gameObject.SetActive(true);
                        }
                        else
                        {
                            _customHandle.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
        //[SerializeField]
        [FormerlySerializedAs("m_isSelectionVisible")]
        private bool _isSelectionVisible = true;
        [SerializeField]
        [FormerlySerializedAs("m_canSelect")]
        private bool _canSelect = true;
        [SerializeField]
        [FormerlySerializedAs("m_canSelectAll")]
        private bool _canSelectAll = true;
        [SerializeField]
        [FormerlySerializedAs("m_canSelectExposedOnly")]
        private bool _canSelectExposedOnly = true;
        public bool IsSelectionVisible
        {
            get { return _isSelectionVisible; }
            set { _isSelectionVisible = value; }
        }

        public bool CanSelect
        {
            get { return _canSelect; }
            set { _canSelect = value; }
        }

        public bool CanSelectAll
        {
            get { return _canSelectAll; }
            set { _canSelectAll = value; }
        }
        public float SizeOfGrid
        {
            get
            {
                if (_grid == null)
                {
                    return 0.5f;
                }
                return _grid.SizeOfGrid;
            }
            set
            {
                if (_grid == null)
                {
                    return;
                }

                _grid.SizeOfGrid = value;
                ApplySizeOfGrid();
            }
        }

        private void ApplySizeOfGrid()
        {
            if (_positionHandle != null)
            {
                _positionHandle.SizeOfGrid = SizeOfGrid;
            }

            if (_scaleHandle != null)
            {
                _scaleHandle.SizeOfGrid = SizeOfGrid;
            }
            
            if (_rectTool != null)
            {
                _rectTool.SizeOfGrid = SizeOfGrid;
            }

            if (_customHandle != null)
            {
                _customHandle.SizeOfGrid = SizeOfGrid;
            }
        }
        private IMESelection _selectionOverride;
        public IMESelection Selection
        {
            get
            {
                if (_selectionOverride != null)
                {
                    return _selectionOverride;
                }

                return _editor.Selection;
            }
            set
            {
                if (_selectionOverride != value)
                {
                    if (_selectionOverride != null)
                    {
                        _selectionOverride.SelectionChanged -= OnRuntimeSelectionChanged;
                    }

                    _selectionOverride = value;
                    if (_selectionOverride == _editor.Selection)
                    {
                        _selectionOverride = null;
                    }

                    if (_selectionOverride != null)
                    {
                        OnRuntimeSelectionChanged(_editor.Selection.Objects);
                        _selectionOverride.SelectionChanged += OnRuntimeSelectionChanged;
                    }

                    if (_outlineManager != null)
                    {
                        _outlineManager.Selection = _selectionOverride;
                    }
                }
            }
        }
        private void Awake()
        {
            Instance = this;
            _editor = MEBase.Instance;
            
            if (_boxSelection == null)
            {
                _boxSelection = GetComponentInChildren<BoxSelection>(true);
            }

            if (_positionHandle == null)
            {
                _positionHandle = GetComponentInChildren<PositionHandle>(true);

            }
            
            if (_rotationHandle == null)
            {
                _rotationHandle = GetComponentInChildren<RotationHandle>(true);
            }
            if (_scaleHandle == null)
            {
                _scaleHandle = GetComponentInChildren<ScaleHandle>(true);
            }
            if (_rectTool == null)
            {
                _rectTool = GetComponentInChildren<RectTool>(true);
            }
            if (_grid == null)
            {
                _grid = GetComponentInChildren<SceneGridVisual>(true);
            }
            
            if (_boxSelection != null)
            {
                _boxSelection.Filtering += OnSelectionFiltering;
                _boxSelection.Selection += OnBoxSelection;
            }

            if (_positionHandle != null)
            {
                _positionHandle.gameObject.SetActive(true);
                _positionHandle.gameObject.SetActive(false);

                _positionHandle.BeforeDrag.AddListener(OnBeforeDrag);
                _positionHandle.Drop.AddListener(OnDrop);
            }
            
            
            if (_rotationHandle != null)
            {
            
                _rotationHandle.gameObject.SetActive(true);
                _rotationHandle.gameObject.SetActive(false);
            
                _rotationHandle.BeforeDrag.AddListener(OnBeforeDrag);
                _rotationHandle.Drop.AddListener(OnDrop);
            }
            
            if (_scaleHandle != null)
            {
                _scaleHandle.gameObject.SetActive(true);
                _scaleHandle.gameObject.SetActive(false);
            
                _scaleHandle.BeforeDrag.AddListener(OnBeforeDrag);
                _scaleHandle.Drop.AddListener(OnDrop);
            }
            
            if (_rectTool != null)
            {
                _rectTool.gameObject.SetActive(true);
                _rectTool.gameObject.SetActive(false);
            
                _rectTool.BeforeDrag.AddListener(OnBeforeDrag);
                _rectTool.Drop.AddListener(OnDrop);
            }

            _editor.Selection.SelectionChanged += OnRuntimeEditorSelectionChanged;
            _editor.Tools.ToolChanged += OnRuntimeToolChanged;
            
            if (_pivot == null)
            {
                GameObject pivot = new GameObject("Pivot");
                pivot.transform.SetParent(transform, true);
                pivot.transform.position = Vector3.zero;
                _pivot = pivot.transform;
            }
            if (_secondaryPivot == null)
            {
                GameObject secondaryPivot = new GameObject("SecondaryPivot");
                secondaryPivot.transform.SetParent(transform, true);
                secondaryPivot.transform.position = Vector3.zero;
                _secondaryPivot = secondaryPivot.transform;
            }
            
            OnRuntimeEditorSelectionChanged(null);
        }
        protected virtual bool CanTransformObject(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            ExposeToEditor exposeToEditor = go.GetComponentInParent<ExposeToEditor>();
            if (exposeToEditor == null)
            {
                return true;
            }
            return exposeToEditor.CanTransform;
        }
        public virtual Transform[] GetHandleTargets()
        {
            if (Selection.GameObjects == null)
            {
                return null;
            }

            return Selection.GameObjects.Where(CanTransformObject).Select(g => g.transform).OrderByDescending(g => Selection.ActiveTransform == g).ToArray();
        }
        private void Start()
        {
            if (_positionHandle != null && !_positionHandle.gameObject.activeSelf)
            {
                _positionHandle.gameObject.SetActive(true);
                _positionHandle.gameObject.SetActive(false);
            }

            if (_rotationHandle != null && !_rotationHandle.gameObject.activeSelf)
            {
                _rotationHandle.gameObject.SetActive(true);
                _rotationHandle.gameObject.SetActive(false);
            }
            
            if (_scaleHandle != null && !_scaleHandle.gameObject.activeSelf)
            {
                _scaleHandle.gameObject.SetActive(true);
                _scaleHandle.gameObject.SetActive(false);
            }
            
            if (_rectTool != null && !_rectTool.gameObject.activeSelf)
            {
                _rectTool.gameObject.SetActive(true);
                _rectTool.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_boxSelection != null)
            {
                _boxSelection.Filtering -= OnSelectionFiltering;
                _boxSelection.Selection -= OnBoxSelection;
            }

            if (_editor != null)
            {
                _editor.Tools.ToolChanged -= OnRuntimeToolChanged;
                _editor.Selection.SelectionChanged -= OnRuntimeEditorSelectionChanged;
            }

            if (_positionHandle != null)
            {
                _positionHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                _positionHandle.Drop.RemoveListener(OnDrop);
            }

            if (_rotationHandle != null)
            {
                _rotationHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                _rotationHandle.Drop.RemoveListener(OnDrop);
            }
            
            if (_scaleHandle != null)
            {
                _scaleHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                _scaleHandle.Drop.RemoveListener(OnDrop);
            }
            
            if (_rectTool != null)
            {
                _rectTool.BeforeDrag.RemoveListener(OnBeforeDrag);
                _rectTool.Drop.RemoveListener(OnDrop);
            }

            if (_customHandle != null)
            {
                _customHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                _customHandle.Drop.RemoveListener(OnDrop);
            }

            if (_selectionOverride != null)
            {
                _selectionOverride.SelectionChanged -= OnRuntimeSelectionChanged;
                _selectionOverride = null;
            }
        }
        
        public virtual void SelectGO(bool multiselect, bool allowUnselect)
        {
            if (!CanSelect)
            {
                return;
            }
            if(_boxSelection != null && _boxSelection.IsThresholdPassed)
            {
                return;
            }
            RaycastHit[] hits = Raycast3DObjects();
            ProcessHits(multiselect, allowUnselect, hits);
        }
        private void ProcessHits(bool multiselect, bool allowUnselect, RaycastHit[] hits)
        {
            bool canSelect = hits.Length > 0;
            if (canSelect)
            {
                hits = FilterHits(hits);
            }
            else
            {
                hits = new RaycastHit[0];
            }

            if (hits.Length != 1 || !(hits[0].collider is TerrainCollider) )
            {
                SelectGO(multiselect, allowUnselect, hits, hit => hit.collider.gameObject);
            }
        }
        public virtual void SelectAll()
        {
            if (!CanSelect || !CanSelectAll)
            {
                return;
            }

            UnityObject[] selection = _editor.Object.Get(true).Select(exposed => exposed.gameObject).ToArray();
            // UnityObject[] selection = MapSaver.ObjectsNeedToSave.ToArray();
            UnityObject[] filteredSelection = selection;
            if (RaiseSelectionChanging(selection, out filteredSelection))
            {
                if (filteredSelection.Length == 0)
                {
                    Selection.Objects = null;
                }
                else
                {
                    Selection.Select(selection.FirstOrDefault(),selection);
                }
                RaiseSelectionChanged();
            }
        }
        private void SelectGO<T>(bool multiselect, bool allowUnselect, IList<T> hits, Func<T, GameObject> getGameObject)
        {
            if (hits.Count > 0)
            {
                GameObject hitGO = GetNextGo(hits, getGameObject);
                if (CanSelectObject(hitGO))
                {
                    SelectGO(multiselect, allowUnselect, hitGO);
                }
                else
                {
                    if (!multiselect)
                    {
                        TryToClearSelection();
                    }
                }
            }
            else
            {
                if (!multiselect)
                {
                    TryToClearSelection();
                }
            }
        }
        private GameObject GetNextGo<T>(IList<T> hits, Func<T, GameObject> getGameObject)
        {
            int nextIndex = GetNextIndex(hits, getGameObject);
            GameObject go = getGameObject(hits[nextIndex]);
            ExposeToEditor exposeToEditor = go.GetComponentInParent<ExposeToEditor>();
            GameObject hitGO = exposeToEditor != null ? exposeToEditor.gameObject : go;
            return hitGO;
        }
        private Ray _prevPointer;
        private int GetNextIndex<T>(IList<T> hits, Func<T, GameObject> getGameObject)
        {
            int index = -1;
            if (hits == null || hits.Count == 0)
            {
                return index;
            }

            Ray pointer = _editor.Pointer;
            if(_prevPointer.origin == pointer.origin && _prevPointer.direction == pointer.direction)
            {
                if (Selection.ActiveGameObject != null)
                {
                    for (int i = 0; i < hits.Count; ++i)
                    {
                        if (Selection.IsSelected(getGameObject(hits[i])))
                        {
                            index = i;
                        }
                    }
                }
            }
            _prevPointer = pointer;

            index++;
            index %= hits.Count;
            return index;
        }
        public void TryToClearSelection()
        {
            UnityObject[] filteredSelection;
            if (RaiseSelectionChanging(new UnityObject[0], out filteredSelection))
            {
                if (filteredSelection.Length == 0)
                {
                    Selection.ActiveGameObject = null;
                }
                else
                {
                    Selection.Objects = filteredSelection;
                }

                RaiseSelectionChanged();
            }
        }
        private void RaiseSelectionChanged()
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, EventArgs.Empty);
            }
        }
        private void SelectGO(bool multiselect, bool allowUnselect, GameObject hitGO)
        {   
            if(GetFilteredSelection(multiselect, allowUnselect, hitGO, out UnityObject[] selection))
            {
                UpdateSelection(multiselect, selection);
            } 
        }
        private bool GetFilteredSelection(bool multiselect, bool allowUnselect, GameObject hitGO, out UnityObject[] filteredSelection)
        {
            if (multiselect)
            {
                IList<UnityObject> selectionList;
                if (Selection.Objects != null)
                {
                    selectionList = Selection.Objects.ToList();
                }
                else
                {
                    selectionList = new List<UnityObject>();
                }

                if (selectionList.Contains(hitGO))
                {
                    selectionList.Remove(hitGO);
                    if (!allowUnselect)
                    {
                        selectionList.Insert(0, hitGO);
                    }
                }
                else
                {
                    selectionList.Insert(0, hitGO);
                }

                UnityObject[] selection = selectionList.ToArray();
                if (RaiseSelectionChanging(selection, out filteredSelection) && filteredSelection.Length > 0)
                {
                    filteredSelection = filteredSelection.OrderByDescending(o => o == hitGO).ToArray();
                    return true;
                }
            }
            else
            {
                if (RaiseSelectionChanging(new[] { hitGO }, out filteredSelection) && filteredSelection.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateSelection(bool multiselect, UnityObject[] selection)
        {
            if (selection == null)
            {
                Selection.Objects = null;
            }
            else
            {
                if (multiselect)
                {
                    Selection.Select(selection.FirstOrDefault(), selection);
                }
                else
                {
                    Selection.Objects = selection;
                }
            }
            RaiseSelectionChanged();
        }
        private RaycastHit[] FilterHits(RaycastHit[] hits)
        {
            IEnumerable<RaycastHit> orderedHits = hits.OrderBy(hit => hit.distance);
            if (Filtering != null)
            {
                MESelectionFilteringArgs args = new MESelectionFilteringArgs(orderedHits);
                Filtering(this, args);
                orderedHits = args.Hits;
            }

            RaycastHit closestHit = orderedHits.FirstOrDefault();
            return orderedHits.Where(h => IsReachable(h.transform, closestHit.transform)).ToArray();
        }
        private bool IsReachable(Transform t1, Transform t2)
        {
            Transform p1 = t1;
            while (p1 != null)
            {
                if (p1 == t2)
                {
                    return true;
                }

                p1 = p1.parent;
            }

            Transform p2 = t2;
            while (p2 != null)
            {
                if (p2 == t1)
                {
                    return true;
                }

                p2 = p2.parent;
            }

            return false;
        }
        protected virtual RaycastHit[] Raycast3DObjects()
        {
            RaycastHit[] hits = Physics.RaycastAll(_editor.Pointer, float.MaxValue);
            return hits.Where(hit => CanSelectObject(hit.collider.gameObject)).OrderBy(hit => GetDepth(hit.transform)).ToArray();
        }
        private int GetDepth(Transform tr)
        {
            int depth = 0;

            while (tr.parent != null)
            {
                depth++;
                tr = tr.parent;
            }

            return depth;
        }
        protected HashSet<GameObject> _uiBoxcastResults = new HashSet<GameObject>();
        protected virtual IEnumerable<GameObject> BoxcastUIObjects()
        {
            _uiBoxcastResults.Clear();

            Bounds bounds = _boxSelection.SelectionBounds;
            
            Vector3[] corners = new Vector3[4];
            foreach (BaseRaycaster raycaster in FindRaycasters())
            {
                CanvasRenderer[] renderers = raycaster.GetComponentsInChildren<CanvasRenderer>();

                for(int i = 0; i < renderers.Length; ++i)
                {
                    CanvasRenderer renderer = renderers[i];
                    RectTransform rectTransform = (RectTransform)renderer.transform;
                    rectTransform.GetWorldCorners(corners);
                    for (int j = 0; j < 4; ++j)
                    {
                        Vector3 corner = _editor.Camera.WorldToScreenPoint(corners[j]);
                        corner.z = 0;

                        if (bounds.Contains(corner))
                        {
                            if (CanSelectObject(renderer.gameObject))
                            {
                                ExposeToEditor exposeToEditor = renderer.GetComponentInParent<ExposeToEditor>();
                                if(exposeToEditor != null)
                                {
                                    _uiBoxcastResults.Add(exposeToEditor.gameObject);
                                }
                            }
                            break;
                        }
                    }
                }

            }
            return _uiBoxcastResults;
        }
        protected virtual IEnumerable<BaseRaycaster> FindRaycasters()
        {
            return FindObjectsOfType<BaseRaycaster>().Where(raycaster => raycaster.GetComponentInParent<IME>() == null);
        }
        protected virtual bool CanSelectObject(GameObject go)
        {
            return !_canSelectExposedOnly || go.GetComponentInParent<ExposeToEditor>();
        }
        private bool _wasUnitSnappingEnabled;
        private void OnBeforeDrag(BaseHandle handle)
        {
            _wasUnitSnappingEnabled = _editor.Tools.UnitSnapping;
            _editor.Tools.UnitSnapping = true;

        }

        private void OnDrop(BaseHandle handle)
        {
            _editor.Tools.UnitSnapping = _wasUnitSnappingEnabled;
        }
        private void OnSelectionFiltering(object sender, FilteringArgs e)
        {
            if (e.Object == null)
            {
                e.Cancel = true;
            }

            ExposeToEditor exposeToEditor = e.Object.GetComponent<ExposeToEditor>();
            if (!exposeToEditor && _canSelectExposedOnly)
            {
                e.Cancel = true;
            }
        }

        private void OnBoxSelection(object sender, BoxSelectionArgs e)
        {
            if (!CanSelect)
            {
                return;
            }

            IEnumerable<GameObject> gameObjects = BoxcastUIObjects();
            UnityObject[] filteredSelection;
            if (RaiseSelectionChanging(gameObjects.Union(e.GameObjects).ToArray(), out filteredSelection))
            {
                if (filteredSelection.Length == 0)
                {
                    Selection.Objects = null;
                }
                else
                {
                    Selection.Objects = filteredSelection;
                }
                RaiseSelectionChanged();
            }
        }

        private void OnRuntimeEditorSelectionChanged(UnityObject[] unselected)
        {
            HandleRuntimeSelectionChange(_editor.Selection, unselected);

            if (_editor.Selection == Selection)
            {
                UpdateHandlesState();
            }
        }

        private void OnRuntimeSelectionChanged(UnityObject[] unselected)
        {
            HandleRuntimeSelectionChange(_selectionOverride, unselected);

            UpdateHandlesState();
        }

        private void UpdateHandlesState()
        {
            if (Selection.ActiveGameObject == null || Selection.ActiveGameObject.IsPrefab())
            {
                SetHandlesActive(false);
            }
            else
            {
                SetHandlesActive(false);
                OnRuntimeToolChanged();
            }
        }

        private void HandleRuntimeSelectionChange(IMESelection selection, UnityObject[] unselected)
        {
            if (!IsSelectionVisible)
            {
                return;
            }

            if (unselected != null)
            {
                for (int i = 0; i < unselected.Length; ++i)
                {
                    GameObject unselectedObj = unselected[i] as GameObject;
                    if (unselectedObj != null)
                    {
                        ExposeToEditor exposeToEditor = unselectedObj.GetComponent<ExposeToEditor>();
                        if (exposeToEditor)
                        {
                            if (exposeToEditor.unselected != null)
                            {
                                exposeToEditor.unselected.Invoke(exposeToEditor);
                            }
                        }
                    }
                }
            }

            GameObject[] selected = selection.GameObjects;
            if (selected != null)
            {
                for (int i = 0; i < selected.Length; ++i)
                {
                    GameObject selectedObj = selected[i];
                    ExposeToEditor exposeToEditor = selectedObj.GetComponent<ExposeToEditor>();
                    if (exposeToEditor && !selectedObj.IsPrefab() && !selectedObj.isStatic)
                    {
                        if (exposeToEditor.selected != null)
                        {
                            exposeToEditor.selected.Invoke(exposeToEditor);
                        }
                    }
                }
            }
        }
        private void SetHandlesActive(bool isActive)
        {
            if (_positionHandle != null)
            {
                _positionHandle.gameObject.SetActive(isActive);
            }
            if (_rotationHandle != null)
            {
                _rotationHandle.gameObject.SetActive(isActive);
            }
            if (_scaleHandle != null)
            {
                _scaleHandle.gameObject.SetActive(isActive);
            }
            if (_rectTool != null)
            {
                _rectTool.gameObject.SetActive(isActive);
            }
            if (_customHandle != null)
            {
                _customHandle.gameObject.SetActive(isActive);
            }
        }
        private bool RaiseSelectionChanging(UnityObject[] selected, out UnityObject[] filteredSelection)
        {
            // selected = RuntimeSelectionUtil.GetRoots(selected);
          
            if (SelectionChanging != null)
            {
                MESelectionChangingArgs args = new MESelectionChangingArgs(selected);
                SelectionChanging(this, args);
                filteredSelection = args.Selected.ToArray();
                return !args.Cancel;
            }

            filteredSelection = selected;
            return true;
        }
        
        private void OnRuntimeToolChanged()
        {
            bool hasSelection = Selection.ActiveTransform != null;

            if (_positionHandle != null)
            {
                if (hasSelection && _editor.Tools.Current == EditorTool.Move && IsPositionHandleEnabled)
                {
                    _positionHandle.transform.position = Selection.ActiveTransform.position;
                    _positionHandle.Targets = GetHandleTargets();
                    _positionHandle.gameObject.SetActive(_positionHandle.Targets.Length > 0);
                }
                else
                {
                    _positionHandle.gameObject.SetActive(false);
                }
            }
            if (_rotationHandle != null)
            {
                if (hasSelection && _editor.Tools.Current == EditorTool.Rotate && IsRotationHandleEnabled)
                {
                    _rotationHandle.transform.position = Selection.ActiveTransform.position;
                    _rotationHandle.Targets = GetHandleTargets();
                    _rotationHandle.gameObject.SetActive(_rotationHandle.Targets.Length > 0);
                }
                else
                {
                    _rotationHandle.gameObject.SetActive(false);
                }
            }
            if (_scaleHandle != null)
            {
                if (hasSelection && _editor.Tools.Current == EditorTool.Scale && IsScaleHandleEnabled)
                {
                    _scaleHandle.transform.position = Selection.ActiveTransform.position;
                    _scaleHandle.Targets = GetHandleTargets();
                    _scaleHandle.gameObject.SetActive(_scaleHandle.Targets.Length > 0);
                }
                else
                {
                    _scaleHandle.gameObject.SetActive(false);
                }
            }
            if (_rectTool != null)
            {
                if (hasSelection && _editor.Tools.Current == EditorTool.Rect && IsRectToolEnabled)
                {
                    _rectTool.transform.position = Selection.ActiveTransform.position;
                    _rectTool.Targets = GetHandleTargets();
                    _rectTool.gameObject.SetActive(_rectTool.Targets.Length > 0);
                }
                else
                {
                    _rectTool.gameObject.SetActive(false);
                }
            }
            if (_customHandle != null)
            {
                if (hasSelection && _editor.Tools.Current == EditorTool.Custom)
                {
                    _customHandle.transform.position = Selection.ActiveTransform.position;
                    _customHandle.Targets = GetHandleTargets();
                    _customHandle.gameObject.SetActive(_customHandle.Targets.Length > 0);
                }
                else
                {
                    _customHandle.gameObject.SetActive(false);
                }
            }
        }
    }
}