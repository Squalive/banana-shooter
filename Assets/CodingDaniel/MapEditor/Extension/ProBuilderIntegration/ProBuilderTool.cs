
using System;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.UI;
using CodingDaniel.MapEditor.UI.AddObject;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.UI;
using ExposeToEditor = CodingDaniel.MapEditor.MEEditor.ExposeToEditor;
using Math = System.Math;

using CodingDaniel.MapEditor.Interaction;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public enum ProBuilderToolMode
    {
        Object = 0,
        Vertex = 1,
        Edge = 2,
        Face = 3,
        Custom = 4,

        [Obsolete("Use IProBuilderTool.CustomTool set to \"PolyShape\" instead")]
        PolyShape = 4,
    }
    public static class CustomToolNames
    {
        public const string PolyShape = "PolyShape";
    }

    public interface IProBuilderTool
    {
        event Action<ProBuilderToolMode> ModeChanged;
        event Action<string> CustomToolChanged;

        event Action SelectionChanging;
        event Action SelectionChanged;
        event Action MeshesChanged;

        ProBuilderToolMode Mode
        {
            get;
            set;
        }

        string CustomTool
        {
            get;
            set;
        }

        bool HasSelection
        {
            get;
        }

        bool HasSelectedFaces
        {
            get;
        }

        bool HasSelectedManualUVs
        {
            get;
        }

        bool HasSelectedAutoUVs
        {
            get;
        }

        PBAutoUnwrapSettings UV
        {
            get;
        }


        event Action<bool> UVEditingModeChanged;
        bool UVEditingMode
        {
            get;
            set;
        }

        string[] GetCustomToolNames();
        // IProBuilderCustomTool GetCustomTool(string name);
        void RegisterCustomTool(string name, GameObject prefab, GameObject uiPrefab);
        void UnregisterCustomTool(string name);

        IMeshEditor GetEditor();
        void TryUpdatePivotTransform();
        void ApplyMaterial(Material material);
        void ApplyMaterial(Material material, int submeshIndex);
        void SetSelection(MeshSelection selection);
        void SelectFaces(Material material);
        void UnselectFaces(Material material);
        void Extrude(float value);
        void Delete();
        void SubdivideFaces();
        void MergeFaces();
        void SubdivideEdges();
        void SelectHoles();
        void FillHoles();
        void Bridge();
        void CenterPivot();
        void Subdivide();
        void GroupFaces();
        void UngroupFaces();
        void SelectFaceGroup();
        void ConvertUVs(bool auto);
        void ResetUVs();
        

        void FlipNormal();
        void InsertEdgeLoop();
        void RecordState(MeshEditorState oldState, MeshEditorState newState, bool raiseMeshChanged = false);
        void CreateNewShapeAndRecord(int shapeType,ObjectItem item);
        ExposeToEditor CreateNewShape(PBShapeType type);
        void GetPositionAndRotation(out Vector3 position, out Quaternion rotation, bool rotateToTerrain = false);
    }
    [DefaultExecutionOrder(-90)]
    public class ProBuilderTool : MonoBehaviour,IProBuilderTool
    {
        public static ProBuilderTool Instance { private set; get; }
        private IME _me;
        public event Action<ProBuilderToolMode> ModeChanged;
        public event Action<string> CustomToolChanged;
        public event Action<bool> UVEditingModeChanged;
        public event Action SelectionChanging;
        public event Action SelectionChanged;
        public event Action MeshesChanged;
        
        private bool _modeChaning;
        private ProBuilderToolMode _mode = ProBuilderToolMode.Object;
        public ProBuilderToolMode Mode
        {
            get { return _mode; }
            set
            {
                try
                {
                    if(_modeChaning)
                    {
                        return;
                    }

                    _modeChaning = true;
                    ProBuilderToolMode oldMode = _mode;
                    _mode = value;

                    OnCurrentModeChanged(oldMode);
                    if (ModeChanged != null)
                    {
                        ModeChanged(oldMode);
                    }
                }
                finally
                {
                    _modeChaning = false;
                }
            }
        }

        private string _customTool;
        public string CustomTool
        {
            get { return _customTool; }
            set
            {
                if(_customTool != value)
                {
                    string oldToolName = _customTool;
                    _customTool = value;
                    if(CustomToolChanged != null)
                    {
                        CustomToolChanged(oldToolName);
                    }
                }
            }
        }
         public bool HasSelection
        {
            get
            {
                IMeshEditor editor = GetEditor();
                return editor != null && editor.HasSelection;
            }
        }

        public bool HasSelectedFaces
        {
            get
            {
                IMeshEditor editor = GetEditor();
                if (editor == null || !editor.HasSelection)
                {
                    return false;
                }

                MeshSelection selection = editor.GetSelection();
                selection = selection.ToFaces(false, false);

                return selection.HasFaces;
            }
        }

        private PBAutoUnwrapSettings _uv;
        public PBAutoUnwrapSettings UV
        {
            get { return _uv; }
        }

        public bool HasSelectedManualUVs
        {
            get
            {
                IMeshEditor editor = GetEditor();
                if (editor == null || !editor.HasSelection)
                {
                    return false;
                }
                MeshSelection selection = editor.GetSelection();
                return _autoUVEditor.HasAutoUV(selection, false);
            }
        }

        public bool HasSelectedAutoUVs
        {
            get
            {
                IMeshEditor editor = GetEditor();
                if (editor == null || !editor.HasSelection)
                {
                    return false;
                }
                MeshSelection selection = editor.GetSelection();
                return _autoUVEditor.HasAutoUV(selection, true);
            }
        }


        private bool _uvEditingMode = false;
        public bool UVEditingMode
        {
            get { return _uvEditingMode; }
            set
            {
                if (_uvEditingMode != value)
                {
                    bool oldMode = _uvEditingMode;
                    _uvEditingMode = value;
                    UpdatePivotLockAxesState();

                    if (_me.Selection.IsSelected(_pivot.gameObject))
                    {
                        _me.Selection.Select(null, null);
                        _me.Selection.Select(_pivot.gameObject, new[] { _pivot.gameObject });
                    }

                    foreach (IMeshEditor editor in _meshEditors)
                    {
                        if (editor == null)
                        {
                            continue;
                        }
                        editor.UVEditingMode = _uvEditingMode;
                    }

                    IMeshEditor currentEditor = GetEditor();
                    if (currentEditor != null)
                    {
                        PivotPosition = currentEditor.Position;
                        PivotRotation = GetPivotRotation(currentEditor);
                    }

                    if (_uvEditingMode)
                    {
                        CurrentSelection = CurrentSelection;
                    }

                    if (UVEditingModeChanged != null)
                    {
                        UVEditingModeChanged(oldMode);
                    }
                }
            }
        }
        private Vector3 _pivotPosition;
        private Vector3 PivotPosition
        {
            get { return _pivot.position; }
            set
            {
                _pivot.position = value;
                _pivotPosition = value;
            }
        }

        private Quaternion PivotRotation
        {
            get { return _pivot.rotation; }
            set { _pivot.rotation = value; }
        }

        private Vector3 PivotLocalScale
        {
            get { return _pivot.localScale; }
            set { _pivot.localScale = value; }
        }

        public IMeshEditor GetEditor()
        {
            return _meshEditors[(int)_mode];
        }
        private MeshSelection _faceGroupSelection;
        private MeshSelection _currentSelection;
        private MeshSelection CurrentSelection
        {
            get { return _currentSelection; }
            set
            {
                _currentSelection = value;
                if(_currentSelection == null)
                {
                    _faceGroupSelection = null;
                }
                else
                {
                    if(UVEditingMode)
                    {
                        _faceGroupSelection = _autoUVEditor.SelectFaceGroup(_currentSelection);
                    }
                    else
                    {
                        _faceGroupSelection = null;
                    }
                }
            }
        }

        private IMESelectionComponent _selectionComponent;
        private IMeshEditor[] _meshEditors;
        private IMaterialEditor _materialEditor;
        private IAutoUVEditor _autoUVEditor;
        private IBoxSelection _boxSelection;
        [HideInInspector]
        [FormerlySerializedAs("m_pivot")]
        public Transform _pivot;
        private Dictionary<string, GameObject> _customTools;
        
        private Vector2 _initialUVOffset;
        private PBTextureMoveTool _textureMoveTool = new PBTextureMoveTool();
        private Vector3 _initialRight;
        private Quaternion _initialRotation;
        private float _initialUVRotation;
        private PBTextureRotateTool _textureRotateTool = new PBTextureRotateTool();
        private Vector2 _initialUVScale;
        private PBTextureScaleTool _textureScaleTool = new PBTextureScaleTool();
        private bool _hasManualUVs;
        private bool _hasAutoUVs;
        private void UpdatePivotLockAxesState()
        {
            LockAxes lockAxes = _pivot.gameObject.GetComponent<LockAxes>();
            lockAxes.PivotRotationValue = MEPivotRotation.Local;
            lockAxes.RotationFree = true;

            lockAxes.RotationX = _uvEditingMode;
            lockAxes.RotationY = _uvEditingMode;
            lockAxes.RotationScreen = _uvEditingMode;
            lockAxes.ScaleZ = _uvEditingMode;
            lockAxes.PositionZ = _uvEditingMode;
            lockAxes.PivotRotation = _uvEditingMode;
            
            bool noSelectedFacesInUVEditingMode = !HasSelectedFaces && _uvEditingMode;
            lockAxes.PositionX = noSelectedFacesInUVEditingMode;
            lockAxes.PositionY = noSelectedFacesInUVEditingMode;
            lockAxes.RotationZ = noSelectedFacesInUVEditingMode;
            lockAxes.ScaleX = noSelectedFacesInUVEditingMode;
            lockAxes.ScaleY = noSelectedFacesInUVEditingMode;
        }

        private void Awake()
        {
            Instance = this;
            _me = MEBase.Instance;

            gameObject.AddComponent<MaterialPaletteManager>();
            _materialEditor = gameObject.AddComponent<PBMaterialEditor>();
            _autoUVEditor = gameObject.AddComponent<PBAutoUVEditor>();
            _customTools = new Dictionary<string, GameObject>();
            
            _uv = new PBAutoUnwrapSettings();
            _uv.Changed += OnUVChanged;
            
            bool wasActive = gameObject.activeSelf;
            gameObject.SetActive(false);
            PBVertexSelection vertexSelection = gameObject.AddComponent<PBVertexSelection>();
            PBVertexEditor vertexEditor = gameObject.AddComponent<PBVertexEditor>();
            vertexEditor._vertexSelection = vertexSelection;
            
            PBEdgeSelection edgeSelection = gameObject.AddComponent<PBEdgeSelection>();
            PBEdgeEditor edgeEditor = gameObject.AddComponent<PBEdgeEditor>();
            edgeEditor._edgeSelection = edgeSelection;

            PBFaceSelection faceSelection = gameObject.AddComponent<PBFaceSelection>();
            PBFaceEditor faceEditor = gameObject.AddComponent<PBFaceEditor>();
            faceEditor._faceSelection = faceSelection;

            _meshEditors = new IMeshEditor[5];
            _meshEditors[(int)ProBuilderToolMode.Vertex] = vertexEditor;
            _meshEditors[(int)ProBuilderToolMode.Edge] = edgeEditor;
            _meshEditors[(int)ProBuilderToolMode.Face] = faceEditor;

            foreach (IMeshEditor editor in _meshEditors)
            {
                if (editor == null)
                {
                    continue;
                }
                editor.CenterMode = _me.Tools.PivotMode == MEPivotMode.Center;
            }

            _pivot = new GameObject("Pivot").transform;
            _pivot.gameObject.hideFlags = HideFlags.HideInHierarchy;
            UpdateGlobalMode();

            LockAxes lockAxes = _pivot.gameObject.AddComponent<LockAxes>();
            UpdatePivotLockAxesState();

            _pivot.SetParent(transform, false);

            ExposeToEditor exposed = _pivot.gameObject.AddComponent<ExposeToEditor>();
            exposed.CanDelete = false;
            exposed.CanDuplicate = false;
            exposed.CanInspect = false;

            gameObject.SetActive(wasActive);
        }

        private void Start()
        {
            SetCanSelect(Mode == ProBuilderToolMode.Object);

            if (_me != null)
            {
                _selectionComponent = MESelectionComponent.Instance;
                _boxSelection = BoxSelection.Instance;

                SubscribeToEvents();

                _me.Selection.SelectionChanged += OnEditorSelectionChanged;
                _me.Tools.ToolChanged += OnEditorToolChanged;
                _me.Tools.PivotModeChanging += OnPivotModeChanging;
                _me.Tools.PivotModeChanged += OnPivotModeChanged;
                _me.Tools.PivotRotationChanging += OnPivotRotationChanging;
                _me.Tools.PivotRotationChanged += OnPivotRotationChanged;
            }
        }

        private void OnDestroy()
        {
            Mode = ProBuilderToolMode.Object;

            if (_me != null)
            {
                _me.Selection.SelectionChanged -= OnEditorSelectionChanged;
                _me.Tools.ToolChanged -= OnEditorToolChanged;
                _me.Tools.PivotModeChanging -= OnPivotModeChanging;
                _me.Tools.PivotModeChanged -= OnPivotModeChanged;
                _me.Tools.PivotRotationChanging -= OnPivotRotationChanging;
                _me.Tools.PivotRotationChanged -= OnPivotRotationChanged;

               
            }

            UnsubscribeFromEvents();

            for (int i = 0; i < _meshEditors.Length; ++i)
            {
                MonoBehaviour meshEditor = _meshEditors[i] as MonoBehaviour;
                if (meshEditor != null)
                {
                    Destroy(meshEditor);
                }
            }
            if (_materialEditor != null)
            {
                Destroy(_materialEditor as MonoBehaviour);
            }

            if (_autoUVEditor != null)
            {
                Destroy(_autoUVEditor as MonoBehaviour);
            }

            if (_pivot != null)
            {
                Destroy(_pivot.gameObject);
            }

            foreach(GameObject customTool in _customTools.Values)
            {
                Destroy(customTool);
            }

            _customTools = null;

            _uv.Changed -= OnUVChanged;
        }
        private void UnsubscribeFromEvents()
        {
            if (_boxSelection != null)
            {
                _boxSelection.Begin -= OnBeginBoxSelection;
                _boxSelection.Selection -= OnBoxSelection;
            }

            if (_selectionComponent != null)
            {
                if (_selectionComponent.PositionHandle != null)
                {
                    _selectionComponent.PositionHandle.BeforeDrag.RemoveListener(OnBeginMove);
                    _selectionComponent.PositionHandle.Drop.RemoveListener(OnEndMove);
                    if(_selectionComponent.PositionHandle.IsDragging)
                    {
                        OnEndMove(_selectionComponent.PositionHandle);
                    }
                }

                if (_selectionComponent.RotationHandle != null)
                {
                    _selectionComponent.RotationHandle.BeforeDrag.RemoveListener(OnBeginRotate);
                    _selectionComponent.RotationHandle.Drop.RemoveListener(OnEndRotate);
                    if (_selectionComponent.RotationHandle.IsDragging)
                    {
                        OnEndRotate(_selectionComponent.RotationHandle);
                    }
                }

                if (_selectionComponent.ScaleHandle != null)
                {
                    _selectionComponent.ScaleHandle.BeforeDrag.RemoveListener(OnBeginScale);
                    _selectionComponent.ScaleHandle.Drop.RemoveListener(OnEndScale);
                    if (_selectionComponent.ScaleHandle.IsDragging)
                    {
                        OnEndScale(_selectionComponent.ScaleHandle);
                    }
                }
            }
        }
        private void OnBeginBoxSelection(object sender, BeginBoxSelectionArgs e)
        {
            if(Mode != ProBuilderToolMode.Object)
            {
                _boxSelection.MethodOverride = BoxSelectionMethod.PixelPerfectDepthTest;
            }
        }

        [SerializeField]
        private Canvas canvas;
        private void OnBoxSelection(object sender, BoxSelectionArgs e)
        {
            _boxSelection.MethodOverride = BoxSelectionMethod.Default;
            IMeshEditor meshEditor = _meshEditors[(int)_mode];
            if (meshEditor == null)
            {
                return;
            }

            bool depthTest = true;

            Vector2 min = _boxSelection.SelectionBounds.min;
            Vector2 max = _boxSelection.SelectionBounds.max;


            Rect rect;
            RectTransform sceneOutput = canvas.GetComponent<RectTransform>();
            if(sceneOutput.childCount > 0 )
            {
                sceneOutput = (RectTransform)sceneOutput.GetChild(0);
            }
                

            Camera canvasCamera = canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneOutput, min, canvasCamera, out min);
            min.y = sceneOutput.rect.height - min.y;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneOutput, max, canvasCamera, out max);
            max.y = sceneOutput.rect.height - max.y;

            /*quick fix for ui scale issue. TODO: replace with better solution*/
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                min *= scaler.scaleFactor;
                max *= scaler.scaleFactor;
            }

            rect = new Rect(new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y)), new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y)));
            var pixelRect = _me.Camera.pixelRect;
            rect.x += pixelRect.x;
            rect.y += canvas.pixelRect.height - (pixelRect.y + pixelRect.height);

            _me.Undo.BeginRecord();

            MeshSelection oldSelection = CurrentSelection;
            if (meshEditor.Select(_me.Camera, rect, canvas.pixelRect, e.GameObjects.Where(g => g.GetComponent<ExposeToEditor>() != null).ToArray(), depthTest, MeshEditorSelectionMode.Add) != null) 
            {
                CurrentSelection = meshEditor.GetSelection();
                RecordSelection(oldSelection, CurrentSelection);
            }

            TryUpdatePivotTransform();
            TrySelectPivot(meshEditor);
            TryUpdatePivotVisibility();

            _me.Undo.EndRecord();
        }
        private void RecordSelection(MeshSelection oldSelection, MeshSelection newSelection, bool oldStateChanged = true, bool newStateChanged = true)
        {
            UndoRedoCallback redo = record =>
            {
                CurrentSelection = newSelection;

                IMeshEditor meshEditor = GetEditor();
                if(meshEditor != null)
                {
                    meshEditor.SetSelection(CurrentSelection);
                }

                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor, false);
                TryUpdatePivotVisibility();
                OnSelectionChanged();
                return newStateChanged;
            };

            UndoRedoCallback undo = record =>
            {
                CurrentSelection = oldSelection;

                IMeshEditor meshEditor = GetEditor();
                if (meshEditor != null)
                {
                    meshEditor.SetSelection(CurrentSelection);
                }
                
                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor, false);
                TryUpdatePivotVisibility();
                OnSelectionChanged();
                return oldStateChanged;
            };

            _me.Undo.CreateRecord(redo, undo);
            OnSelectionChanged();
        }
        private void TrySelectPivot(IMeshEditor meshEditor, bool record = true)
        {
            bool wasEnabled = _me.Undo.Enabled;
            _me.Undo.Enabled = record;

            if (meshEditor != null && meshEditor.HasSelection)
            {
                _me.Selection.ActiveObject = _pivot.gameObject;
            }
     
            _me.Undo.Enabled = wasEnabled;
        }
        private void TryUpdatePivotVisibility()
        {
            ExposeToEditor exposeToEditor = _pivot.GetComponent<ExposeToEditor>();
            if (Mode == ProBuilderToolMode.Object)
            {
                exposeToEditor.CanTransform = false;
            }
            else
            {
                IMeshEditor meshEditor = GetEditor();
                exposeToEditor.CanTransform = meshEditor == null || meshEditor.HasSelection;
                if(HasSelectedManualUVs && HasSelectedAutoUVs)
                {
                    exposeToEditor.CanTransform = false;
                }
            }

            if (_me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                _me.Undo.Enabled = false;
                _me.Selection.SelectionChanged -= OnEditorSelectionChanged;
                _me.Selection.Select(null, null);
                _me.Selection.Select(_pivot.gameObject, new[] { _pivot.gameObject });
                _me.Selection.SelectionChanged += OnEditorSelectionChanged;
                _me.Undo.Enabled = true;
            }
        }
        private void OnBeginMove(BaseHandle positionHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if(meshEditor != null && CurrentSelection != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                positionHandle.EnableUndo = false;

                if (UVEditingMode)
                {
                    _hasAutoUVs = HasSelectedAutoUVs;
                    _hasManualUVs = HasSelectedManualUVs;
                    if(_hasAutoUVs && _hasManualUVs)
                    {
                        _hasAutoUVs = false;
                        _hasManualUVs = false;
                    }

                    if(_hasAutoUVs)
                    {
                        _initialUVOffset = UV.offset;
                        _me.Undo.BeginRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.offset));
                    }
                    else if(_hasManualUVs)
                    {
                        MeshEditorState oldState = meshEditor.GetState(true);
                        RecordState(oldState, null);

                        MeshSelection selection = CurrentSelection;
                        _textureMoveTool.BeginDrag(selection, PivotPosition, PivotRotation);
                    }
                }
                else
                {
                    MeshEditorState oldState = meshEditor.GetState(true);
                    MeshSelection oldSelection = CurrentSelection;
                    bool control = Input.GetKey(KeyCode.LeftShift);
                    if (control)
                    {
                        meshEditor.Extrude(0.01f);
                    }

                    RecordStateAndSelection(oldState, null, oldSelection, null);
                }
            
                meshEditor.BeginMove();
            }
        }

        private void OnEndMove(BaseHandle positionHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && CurrentSelection != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                positionHandle.EnableUndo = true;

                if (UVEditingMode)
                {
                    if(_hasAutoUVs)
                    {
                        _me.Undo.EndRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.offset));
                        TryUpdatePivotTransform();
                    }
                    else if(_hasManualUVs)
                    {
                        MeshEditorState newState = meshEditor.GetState(true);
                        RecordState(null, newState);

                        _textureMoveTool.EndDrag();
                    }
                    
                    PivotPosition = meshEditor.Position;
                }
                else
                {
                    MeshEditorState newState = meshEditor.GetState(true);
                    MeshSelection newSelection = meshEditor.GetSelection();
                    CurrentSelection = newSelection;
                    RecordStateAndSelection(null, newState, null, newSelection);
                }
                meshEditor.EndMove();
            }
        }
        private void OnBeginRotate(BaseHandle rotationHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if(meshEditor != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                rotationHandle.EnableUndo = false;

                _initialRotation = GetPivotRotation(meshEditor);
                PivotRotation = _initialRotation;
                
                if(UVEditingMode)
                {
                    _hasAutoUVs = HasSelectedAutoUVs;
                    _hasManualUVs = HasSelectedManualUVs;
                    if (_hasAutoUVs && _hasManualUVs)
                    {
                        _hasAutoUVs = false;
                        _hasManualUVs = false;
                    }

                    if(_hasAutoUVs)
                    {
                        _initialRight = _pivot.TransformDirection(Vector3.right);
                        _initialUVRotation = UV.rotation;
                        _me.Undo.BeginRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.rotation));
                    }
                    else if(_hasManualUVs)
                    {
                        MeshEditorState oldState = meshEditor.GetState(true);
                        RecordState(oldState, null);
                        _textureRotateTool.BeginDrag(meshEditor.GetSelection(), PivotPosition, PivotRotation);
                    }
                }
                else
                {
                    MeshEditorState oldState = meshEditor.GetState(true);
                    RecordState(oldState, null);
                }
                meshEditor.BeginRotate(_initialRotation);
                
            }
        }

        private void OnEndRotate(BaseHandle rotationHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if(meshEditor != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                rotationHandle.EnableUndo = true;

                Quaternion initialRotation = _initialRotation;
                Quaternion endRotation = PivotRotation;
                meshEditor.EndRotate();

                Quaternion newStartRotation = GetPivotRotation(meshEditor);
                PivotRotation = newStartRotation;

                if (UVEditingMode)
                {
                    if(_hasAutoUVs)
                    {
                        _me.Undo.EndRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.rotation));
                        TryUpdatePivotTransform();
                    }
                    else if(_hasManualUVs)
                    {
                        MeshEditorState newState = meshEditor.GetState(true);
                        RecordState(null, newState);
                        _textureRotateTool.EndDrag(true);
                    }

                    TryUpdatePivotTransform();
                }
                else
                {
                    MeshEditorState newState = meshEditor.GetState(true);
                    RecordState(null, newState);
                }  
            }
        }

        private void OnBeginScale(BaseHandle scaleHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                scaleHandle.EnableUndo = false;
                PivotLocalScale = Vector3.one;
                if (UVEditingMode)
                {
                    _hasAutoUVs = HasSelectedAutoUVs;
                    _hasManualUVs = HasSelectedManualUVs;
                    if (_hasAutoUVs && _hasManualUVs)
                    {
                        _hasAutoUVs = false;
                        _hasManualUVs = false;
                    }

                    if(_hasAutoUVs)
                    {
                        _initialUVScale = UV.scale;
                        _me.Undo.BeginRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.scale));
                    }
                    else if(_hasManualUVs)
                    {
                        MeshEditorState oldState = meshEditor.GetState(true);
                        RecordState(oldState, null);
                        _textureScaleTool.BeginDrag(meshEditor.GetSelection(), PivotPosition, PivotRotation);
                    }
                }
                else
                {
                    MeshEditorState oldState = meshEditor.GetState(true);
                    RecordState(oldState, null);
                    
                    bool control = Input.GetKey(KeyCode.LeftShift);
                    if (control)
                    {
                        // meshEditor.InsertFace();
                    }
                }
                meshEditor.BeginScale();
            }
        }

        private void OnEndScale(BaseHandle scaleHandle)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                scaleHandle.EnableUndo = true;
                meshEditor.EndScale();

                Vector3 newScale = PivotLocalScale;
                Quaternion rotation = PivotRotation;
                PivotLocalScale = Vector3.one;

                if (UVEditingMode)
                {
                    if (_hasAutoUVs)
                    {
                        _me.Undo.EndRecordValue(UV, Strong.PropertyInfo((PBAutoUnwrapSettings x) => x.scale));
                        TryUpdatePivotTransform();
                    }
                    else if(_hasManualUVs)
                    {
                        MeshEditorState newState = meshEditor.GetState(true);
                        RecordState(null, newState);
                        _textureScaleTool.EndDrag();
                    }                    
                }
                else
                {
                    MeshEditorState newState = meshEditor.GetState(true);
                    RecordState(null, newState);
                } 
            }
        }
        private void OnUVChanged()
        {            
            if(_faceGroupSelection != null)
            {
                _autoUVEditor.ApplySettings(_uv, _faceGroupSelection);
            }

            if(_selectionComponent == null ||
               (_selectionComponent.PositionHandle == null || !_selectionComponent.PositionHandle.IsDragging) &&
               (_selectionComponent.RotationHandle == null || !_selectionComponent.RotationHandle.IsDragging) &&
               (_selectionComponent.ScaleHandle == null || !_selectionComponent.ScaleHandle.IsDragging))
            {
                TryUpdatePivotTransform();
            }

            if (UVEditorUI.Instance)
            {
                UVEditorUI.Instance.RefreshAll();
            }
        }
        private void OnEditorSelectionChanged(UnityEngine.Object[] unselectedObjects)
        {
            IMeshEditor editor = GetEditor();
            if (editor != null)
            {
                if (_me.Selection.IsSelected(_pivot.gameObject))
                {
                    editor.SetSelection(CurrentSelection);
                }
                else
                {
                    editor.ClearSelection();
                }

                OnSelectionChanged();
            }
        }
        private void OnSelectionChanged()
        {
            if(SelectionChanging != null)
            {
                SelectionChanging();
            }

            IMeshEditor editor = GetEditor();
            if (editor != null)
            {
                MeshSelection selection = editor.GetSelection();
                PBAutoUnwrapSettings settings = _autoUVEditor.GetSettings(selection);
                _uv.CopyFrom(settings);

                UpdatePivotLockAxesState();
            }

            if (SelectionChanged != null)
            {
                SelectionChanged();
            }
        }
        private void OnPivotModeChanging()
        {
            _me.Undo.BeginRecordValue(_me.Tools, Strong.PropertyInfo((EditorToolState x) => x.PivotMode));
        }

        private void OnPivotModeChanged()
        {
            _me.Undo.EndRecordValue(_me.Tools, Strong.PropertyInfo((EditorToolState x) => x.PivotMode));
            UpdateCenterMode();
        }

        private void OnPivotRotationChanging()
        {
            _me.Undo.BeginRecordValue(_me.Tools, Strong.PropertyInfo((EditorToolState x) => x.PivotRotation));
        }

        private void OnPivotRotationChanged()
        {
            _me.Undo.EndRecordValue(_me.Tools, Strong.PropertyInfo((EditorToolState x) => x.PivotRotation));
            TryUpdatePivotTransform();
        }
        public void TryUpdatePivotTransform()
        {
            UpdateCenterMode();
            UpdateGlobalMode();
        }

        private void UpdateCenterMode()
        {
            if(!string.IsNullOrEmpty(CustomTool))
            {
                return;
            }

            foreach(IMeshEditor editor in _meshEditors)
            {
                if(editor == null)
                {
                    continue;
                }
                editor.CenterMode = _me.Tools.PivotMode == MEPivotMode.Center;
            }

            IMeshEditor meshEditor = _meshEditors[(int)_mode];
            if (meshEditor != null)
            {
                PivotPosition = meshEditor.Position;
                PivotRotation = GetPivotRotation(meshEditor);
            }
        }

        private void OnEditorToolChanged()
        {
            EditorTool current = _me.Tools.Current;
            if (current != EditorTool.Move && current != EditorTool.Rotate && current != EditorTool.Scale)
            {
                if(Mode != ProBuilderToolMode.Object)
                {
                    Mode = ProBuilderToolMode.Object;
                }
            }
        }
        private void SetCanSelect(bool value)
        {
            IMESelectionComponent selectionComponent = MESelectionComponent.Instance;
            if (selectionComponent != null)
            {
                selectionComponent.CanSelect = value;
                selectionComponent.CanSelectAll = value;
            }
        }
        private void SubscribeToEvents()
        {
            if (_boxSelection != null)
            {
                _boxSelection.Begin += OnBeginBoxSelection;
                _boxSelection.Selection += OnBoxSelection;
            }

            if (_selectionComponent != null)
            {
                if (_selectionComponent.PositionHandle != null)
                {
                    _selectionComponent.PositionHandle.BeforeDrag.AddListener(OnBeginMove);
                    _selectionComponent.PositionHandle.Drop.AddListener(OnEndMove);
                }

                if (_selectionComponent.RotationHandle != null)
                {
                    _selectionComponent.RotationHandle.BeforeDrag.AddListener(OnBeginRotate);
                    _selectionComponent.RotationHandle.Drop.AddListener(OnEndRotate);
                }

                if (_selectionComponent.ScaleHandle != null)
                {
                    _selectionComponent.ScaleHandle.BeforeDrag.AddListener(OnBeginScale);
                    _selectionComponent.ScaleHandle.Drop.AddListener(OnEndScale);
                }
            }
        }
        private void UpdateGlobalMode()
        {
            if (!string.IsNullOrEmpty(CustomTool))
            {
                return;
            }

            foreach (IMeshEditor editor in _meshEditors)
            {
                if (editor == null)
                {
                    continue;
                }

                if(UVEditingMode)
                {
                    editor.GlobalMode = false;
                }
                else
                {
                    editor.GlobalMode = _me.Tools.PivotRotation == MEPivotRotation.Global;
                }
            }

            IMeshEditor currentEditor = GetEditor();
            if(currentEditor != null)
            {
                PivotRotation = GetPivotRotation(currentEditor);
            }
        }
        private void OnCurrentModeChanged(ProBuilderToolMode oldMode)
        {
            if (_mode != ProBuilderToolMode.Object)
            {
                EditorTool current = _me.Tools.Current;
                _me.Tools.Current = EditorTool.None; //This is required to notifiy other tools
                if (current != EditorTool.Move && current != EditorTool.Rotate && current != EditorTool.Scale)
                {
                    _me.Tools.Current = EditorTool.Move;
                }
                else
                {
                    _me.Tools.Current = current;
                }
            }

            IMeshEditor disabledEditor = _meshEditors[(int)oldMode];
            IMeshEditor enabledEditor = _meshEditors[(int)_mode];

            if (disabledEditor != null)
            {
                disabledEditor.ClearSelection();
            }

            if (enabledEditor != null)
            {
                if (CurrentSelection != null)
                {
                    enabledEditor.SetSelection(CurrentSelection);

                    TryUpdatePivotTransform();
                    TrySelectPivot(enabledEditor, false);
                    OnSelectionChanged();
                }
            }

            if (Mode == ProBuilderToolMode.Object)
            {
                SetCanSelect(true);
            }
            else
            {
                SetCanSelect(false);
            }

            TryUpdatePivotVisibility();
        }
        private Quaternion GetPivotRotation(IMeshEditor meshEditor)
        {
            if(UVEditingMode)
            {
                return meshEditor.Rotation;
            }

            return _me.Tools.PivotRotation == MEPivotRotation.Global ? Quaternion.identity : meshEditor.Rotation;
        }

        private void LateUpdate()
        {
            if (TabHolder.Instance.UsingUI()) return;
            // if (Input.GetKeyDown(KeyCode.L))
            // {
            //     FlipNormal();
            // }
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor == null)
            {
                return;
            }

            //Testign
            
            if (_me.Tools.ActiveTool != null)
            {
                if (UVEditingMode)
                {
                    if (_selectionComponent.PositionHandle != null && _selectionComponent.PositionHandle.IsDragging)
                    {
                        if (_hasAutoUVs)
                        {
                            Vector2 uv = (Quaternion.Inverse(PivotRotation) * (PivotPosition - meshEditor.Position));
                            if (!UV.flipU)
                            {
                                uv.x = -uv.x;
                            }

                            if (UV.flipV)
                            {
                                uv.x = -uv.x;
                            }

                            if (UV.swapUV)
                            {
                                uv.x = -uv.x;
                            }

                            UV.offset = _initialUVOffset + (Vector2.one * Vector2.Scale(uv, UV.scale));
                        }
                        else if (_hasManualUVs)
                        {
                            _textureMoveTool.Drag(PivotPosition, PivotRotation, PivotLocalScale);
                        }
                    }
                    else if (_selectionComponent.RotationHandle != null && _selectionComponent.RotationHandle.IsDragging)
                    {
                        if (_hasAutoUVs)
                        {
                            Vector3 fwd = _pivot.forward;
                            if(Math.Abs(Mathf.Sign(UV.scale.x) - Mathf.Sign(UV.scale.y)) > 0.1f)
                            {
                                fwd = -fwd;
                            }

                            UV.rotation = _initialUVRotation - Vector3.SignedAngle(_initialRight, _pivot.right, fwd);
                        }
                        else if (_hasManualUVs)
                        {
                            _textureRotateTool.Drag(_pivot);
                        }
                    }
                    else if (_selectionComponent.ScaleHandle != null && _selectionComponent.ScaleHandle.IsDragging)
                    {
                        if (_hasAutoUVs)
                        {
                            Vector2 scale = PivotLocalScale;
                    
                            if (UV.swapUV)
                            {
                                (scale.x, scale.y) = (scale.y, scale.x);
                            }

                            if (Mathf.Approximately(scale.x, 0))
                            {
                                scale.x = Mathf.Epsilon;
                            }
                            if (Mathf.Approximately(scale.y, 0))
                            {
                                scale.y = Mathf.Epsilon;
                            }

                            Vector2 s = new Vector2(1 / scale.x, 1 / scale.y);
                            UV.scale = Vector2.Scale(_initialUVScale, s);
                        }
                        else
                        {
                            _textureScaleTool.Drag(PivotPosition, PivotRotation, PivotLocalScale);
                        }
                    }
                }
                else
                {
                    if (_selectionComponent.PositionHandle != null && _selectionComponent.PositionHandle.IsDragging)
                    {
                        meshEditor.Position = PivotPosition;
                    }
                    else if (_selectionComponent.RotationHandle != null && _selectionComponent.RotationHandle.IsDragging)
                    {
                        meshEditor.Rotate(PivotRotation);
                    }
                    else if (_selectionComponent.ScaleHandle != null && _selectionComponent.ScaleHandle.IsDragging)
                    {
                        Vector3 localScale = PivotLocalScale;
                        if (Mathf.Approximately(localScale.x, 0))
                        {
                            localScale.x = 0.00001f;
                        }
                        if (Mathf.Approximately(localScale.y, 0))
                        {
                            localScale.y = 0.000001f;
                        }
                        if (Mathf.Approximately(localScale.z, 0))
                        {
                            localScale.z = 0.000001f;
                        }
                        PivotLocalScale = localScale;
                        meshEditor.Scale(PivotLocalScale, PivotRotation);
                    }
                }

                if(MeshesChanged != null)
                {
                    MeshesChanged();
                }
            }
            else
            {
                if (_pivotPosition != _pivot.position)
                {
                    PivotPosition = _pivot.position;
                    meshEditor.Position = PivotPosition;
                }

                meshEditor.Hover(_me.Camera, Input.mousePosition);

                if (Input.GetMouseButtonDown(0))
                {
                    bool ctrl = Input.GetKey(KeyCode.LeftShift);
                    bool shift = Input.GetKey(KeyCode.LeftControl);

                    _me.Undo.BeginRecord();

                    if(_me.Selection.ActiveGameObject != _pivot.gameObject)
                    {
                        CurrentSelection = null;
                    }

                    MeshSelection oldSelection = CurrentSelection;
                    if (meshEditor.Select(_me.Camera, Input.mousePosition, shift, ctrl, true) != null)
                    {
                        CurrentSelection = meshEditor.GetSelection();
                        
                        if (UVEditingMode && Input.GetKey(KeyCode.S) && oldSelection != null && CurrentSelection != null)
                        {
                            PBMesh prevMesh = oldSelection.GetSelectedMeshes().FirstOrDefault();
                            PBMesh mesh = CurrentSelection.GetSelectedMeshes().FirstOrDefault();
                            IEnumerable<int> oldFaces = oldSelection.GetFaces(mesh);
                            IEnumerable<int> newFaces = CurrentSelection.GetFaces(mesh);
                            if (prevMesh == mesh && oldFaces.Count() == 1 && newFaces.Count() == 1 && oldFaces.First() != newFaces.First())
                            {
                                MeshEditorState oldState = meshEditor.GetState(true);
                                PBUVEditing.AutoStitch(mesh, oldFaces.First(), newFaces.First(), 0);
                                MeshEditorState newState = meshEditor.GetState(true);
                                RecordStateAndSelection(oldState, newState, oldSelection, CurrentSelection);
                            }
                            else
                            {
                                RecordSelection(oldSelection, CurrentSelection);
                            }
                        }
                        else
                        {
                            RecordSelection(oldSelection, CurrentSelection);
                        }
                    }

                    if (meshEditor.HasSelection)
                    {
                        TryUpdatePivotTransform();
                        TrySelectPivot(meshEditor);
                        TryUpdatePivotVisibility();
                    }
                    else
                    {
                        if (_me.Selection.ActiveGameObject == _pivot.gameObject)
                        {
                            _me.Selection.ActiveGameObject = null;
                        }
                    }

                    _me.Undo.EndRecord();

                }
                else if (Input.GetKeyDown(KeyCode.Delete))
                {
                    Delete();
                }
                else if (Input.GetKeyDown(KeyCode.H))
                {
                    FillHoles();
                }
                // else if (Input.GetKeyDown(KeyCode.N))
                // { 
                //     meshEditor.InsertFace();
                // }

            }
        }

        public void SetSelection(IMeshEditor meshEditor)
        {
            CurrentSelection = meshEditor.GetSelection();
            
            if (meshEditor.HasSelection)
            {
                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();
            }
        }
        private void RunStateChangeAction(Action<IMeshEditor> action, bool clearSelection)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                _me.Undo.BeginRecord();
                _me.Undo.BeginRecordTransform(_pivot);
                _me.Undo.RecordValue(meshEditor, Strong.PropertyInfo((IMeshEditor x) => x.Position));

                MeshEditorState oldState = meshEditor.GetState(true);
                action(meshEditor);
                MeshEditorState newState = meshEditor.GetState(true);
                MeshSelection oldSelection = null;
                if (clearSelection)
                {
                    oldSelection = CurrentSelection;
                    meshEditor.ClearSelection();
                    CurrentSelection = meshEditor.GetSelection();
                    RecordStateAndSelection(oldState, newState, oldSelection, CurrentSelection);
                }
                else
                {
                    RecordState(oldState, newState, true);
                }

                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();
                _me.Undo.EndRecord();
            }
        }
        public void Delete()
        {
            RunStateChangeAction(meshEditor => meshEditor.Delete(), true);
        }
        public void FillHoles()
        {
            RunStateChangeAction(meshEditor => meshEditor.FillHoles(), false);
        }

        public void Bridge()
        {
            RunStateChangeAction(meshEditor => meshEditor.Bridge(), false);
        }

        public string[] GetCustomToolNames()
        {
            return _customTools.Keys.ToArray();
        }
        
        public void RegisterCustomTool(string name, GameObject prefab, GameObject uiPrefab)
        {
            GameObject customTool = Instantiate(prefab, transform, false);
            customTool.name = name;

            _customTools.Add(name, customTool);
        }
        public void UnregisterCustomTool(string name)
        {
            if(_customTools.TryGetValue(name, out GameObject customTool))
            {
                Destroy(customTool);
                _customTools.Remove(name);
            }
        }
        public void ApplyMaterial(Material material)
        {
            ApplyMaterial(material, -1);
        }
        
        public void ApplyMaterial(Material material, int submeshIndex)
        {
            IMeshEditor editor = GetEditor();
            if(editor != null)
            {
                MeshSelection selection = editor.GetSelection();
              
                ApplyMaterialResult result = _materialEditor.ApplyMaterial(material, selection);
                RecordApplyMaterialResult(result);
            }
            else
            {
                ApplyMaterialToSelectedGameObjects(material, submeshIndex);
            }
        }
        private void RecordApplyMaterialResult(ApplyMaterialResult result)
        {
            _me.Undo.CreateRecord(record =>
                {
                    _materialEditor.ApplyMaterials(result.NewState);
                    return true;
                },
                record =>
                {
                    _materialEditor.ApplyMaterials(result.OldState);
                    return true;
                },
                record => { },
                (record, oldReference, newReference) =>
                {
                    result.OldState.Erase(oldReference, newReference);
                    result.NewState.Erase(oldReference, newReference);
                    return false;
                });
        }
        private void ApplyMaterialToSelectedGameObjects(Material material, int submeshIndex)
        {
            _me.Undo.BeginRecord();

            GameObject[] gameObjects = _me.Selection.GameObjects;
            if (gameObjects != null)
            {
                
                for (int i = 0; i < gameObjects.Length; ++i)
                {
                    if (gameObjects[i] == null)
                    {
                        continue;
                    }
                    
                    ApplyMaterialResult result = _materialEditor.ApplyMaterial(material, gameObjects[i], submeshIndex);
                    RecordApplyMaterialResult(result);
                }
            }

            _me.Undo.EndRecord();
        }
        
        public void SetSelection(MeshSelection selection)
        {
            IMeshEditor editor = GetEditor();
            if (editor != null)
            {
                CurrentSelection = selection;

                editor.SetSelection(selection);

                TryUpdatePivotTransform();
                TrySelectPivot(editor, false);
                TryUpdatePivotVisibility();
                OnSelectionChanged();
            }
        }
        
        public void SelectFaces(Material material)
        {
            IMeshEditor meshEditor = GetEditor();
            if(meshEditor != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                _me.Undo.BeginRecord();

                MeshSelection oldSelection = CurrentSelection;
                if (meshEditor.Select(material) != null)
                {
                    CurrentSelection = meshEditor.GetSelection();
                    RecordSelection(oldSelection, CurrentSelection);
                }

                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();

                _me.Undo.EndRecord();
            }
        }
        
        public void UnselectFaces(Material material)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                _me.Undo.BeginRecord();

                MeshSelection oldSelection = CurrentSelection;
                if (meshEditor.Unselect(material) != null)
                {
                    CurrentSelection = meshEditor.GetSelection();
                    RecordSelection(oldSelection, CurrentSelection);
                }

                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();

                _me.Undo.EndRecord();
            }
        }
        
        public void Extrude(float distance)
        {
            IMeshEditor meshEditor = GetEditor();
            MeshSelection oldSelection = CurrentSelection;
            MeshEditorState oldState = meshEditor.GetState(false);
            meshEditor.Extrude(distance);
            MeshSelection newSelection = meshEditor.GetSelection();
            MeshEditorState newState = meshEditor.GetState(false);
            TryUpdatePivotTransform();
            CurrentSelection = newSelection;
            RecordStateAndSelection(oldState, newState, oldSelection, newSelection);
        }
        public void RecordStateAndSelection(
           MeshEditorState oldState, MeshEditorState newState,
           MeshSelection oldSelection, MeshSelection newSelection,
           bool oldStateChanged = true,
           bool newStateChanged = true)
        {
            UndoRedoCallback redo = record =>
            {
                if (newState != null)
                {
                    CurrentSelection = newSelection;
            
                    IMeshEditor meshEditor = GetEditor();
                    if (meshEditor != null)
                    {
                        meshEditor.ClearSelection();
                        meshEditor.SetState(newState);
                        foreach (PBMesh mesh in newState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
            
                        meshEditor.SetSelection(CurrentSelection);
                    }
                    else
                    {
                        newState.Apply();
                        foreach (PBMesh mesh in newState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
                    }
            
                    TryUpdatePivotTransform();
                    TrySelectPivot(meshEditor, false);
                    TryUpdatePivotVisibility();
                    OnSelectionChanged();
                    return newStateChanged;
                }
                return false;
            };

            UndoRedoCallback undo = record =>
            {
                if (oldState != null)
                {
                    CurrentSelection = oldSelection;
            
                    IMeshEditor meshEditor = GetEditor();
                    if (meshEditor != null)
                    {
                        meshEditor.ClearSelection();
                        meshEditor.SetState(oldState);
                        foreach (PBMesh mesh in oldState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
            
                        meshEditor.SetSelection(CurrentSelection);
                    }
                    else
                    {
                        oldState.Apply();
                        foreach (PBMesh mesh in oldState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
                    }
            
                    TryUpdatePivotTransform();
                    TrySelectPivot(meshEditor, false);
                    TryUpdatePivotVisibility();
                    OnSelectionChanged();
                    return oldStateChanged;
                }
                return false;
            };

            _me.Undo.CreateRecord(redo, undo);
            OnSelectionChanged();
        }
        public void SubdivideFaces()
        {
            RunStateChangeAction(meshEditor => meshEditor.Subdivide(), true);
        }
        
        public void MergeFaces()
        {
            RunStateChangeAction(meshEditor => meshEditor.Merge(), true);
        }

        public void SubdivideEdges()
        {
            RunStateChangeAction(meshEditor => meshEditor.Subdivide(), true);
        }
        public void InsertEdgeLoop()
        {
            RunStateChangeAction(meshEditor => meshEditor.InsertEdgeLoop(), false);
        }
        public void Combine()
        {
            GameObject[] gos = MEBase.Instance.Selection.GameObjects;

            GameObject main = MEBase.Instance.Selection.ActiveGameObject;
            if (gos.Length > 0)
            {
                List<ProBuilderMesh> meshes = new List<ProBuilderMesh>();
                ProBuilderMesh mainMesh = main.GetComponent<ProBuilderMesh>();

                for (int i = 0; i < gos.Length; i++)
                {
                    if (gos[i].TryGetComponent<ProBuilderMesh>(out var mesh))
                    {
                        meshes.Add(mesh);
                    }
                    
                }

                List<ProBuilderMesh> pbMeshes = CombineMeshes.Combine(meshes, mainMesh);

                if (pbMeshes.Count < 2)
                {
                    Debug.Log("Combine succeed");
                    foreach (var g in gos)
                    {
                        if(g!=main)
                            Destroy(g);
                    }
                }
                else
                {
                    Debug.LogError("Failed to combine because vertices are too much");
                }



            }
        }

        public void FlipNormal()
        {
            foreach (var go in MESelectionComponent.Instance.Selection.GameObjects)
            {
                ProBuilderMesh mesh = go.GetComponent<ProBuilderMesh>();

                if (mesh == null) continue;
                IList<Face> faces = mesh.faces;
                foreach (var face in faces)
                {
                    face.Reverse();
                }

                mesh.ToMesh();
                mesh.Refresh();
            }
        }
        public void SelectHoles()
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor != null && _me.Selection.ActiveGameObject == _pivot.gameObject)
            {
                MeshSelection oldSelection = CurrentSelection;
                if(meshEditor.SelectHoles() != null)
                {
                    CurrentSelection = meshEditor.GetSelection();
                    RecordSelection(oldSelection, CurrentSelection);
                }
            }
        }
        private void RunUVEditingAction(Action<MeshSelection> action)
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor == null || _me.Selection.ActiveGameObject != _pivot.gameObject)
            {
                return;
            }

            _me.Undo.BeginRecord();
            _me.Undo.BeginRecordTransform(_pivot);
            _me.Undo.RecordValue(meshEditor, Strong.PropertyInfo((IMeshEditor x) => x.Position));
            MeshEditorState oldState = meshEditor.GetState(true);
            MeshSelection selection = meshEditor.GetSelection();
            action(selection);
            MeshEditorState newState = meshEditor.GetState(true);
            RecordState(oldState, newState);
            TryUpdatePivotTransform();
            TrySelectPivot(meshEditor);
            TryUpdatePivotVisibility();
            _me.Undo.EndRecord();
        }
        public void GroupFaces()
        {
            RunUVEditingAction(selection => _autoUVEditor.GroupFaces(selection));
        }

        public void UngroupFaces()
        {
            RunUVEditingAction(selection => { _autoUVEditor.UngroupFaces(selection); });
        }
        public void Subdivide()
        {
            RunPBMeshAction(pbMesh => pbMesh.Subdivide(), false);
        }

        private void RunPBMeshAction(Action<PBMesh> action, bool recordPosition)
        {
            if (_me.Selection.ActiveGameObject != null)
            {
                _me.Undo.BeginRecord();
                foreach (GameObject go in _me.Selection.GameObjects)
                {
                    PBMesh pbMesh = go.GetComponent<PBMesh>();
                    if (pbMesh == null)
                    {
                        continue;
                    }

                    Vector3 oldPosition = pbMesh.transform.position;
                    MeshState oldState = pbMesh.GetState(true);
                    if (pbMesh != null)
                    {
                        action(pbMesh);
                    }

                    Vector3 newPosition = pbMesh.transform.position;
                    MeshState newState = pbMesh.GetState(true);
                    _me.Undo.CreateRecord(record =>
                        {
                            pbMesh.transform.position = newPosition;
                            pbMesh.SetState(newState);
                            return true;
                        },
                        record =>
                        {
                            pbMesh.transform.position = oldPosition;
                            pbMesh.SetState(oldState);
                            return true;
                        });
                }
                _me.Undo.EndRecord();
            }
        }

        public void CenterPivot()
        {
            RunPBMeshAction(pbMesh => pbMesh.CenterPivot(), true);
        }
        
        public void SelectFaceGroup()
        {
            IMeshEditor meshEditor = GetEditor();
            if (meshEditor == null)
            {
                return;
            }

            MeshSelection oldSelection = CurrentSelection;
            MeshSelection faceGroupSelection = _autoUVEditor.SelectFaceGroup(oldSelection);
            if(faceGroupSelection != null)
            {
                meshEditor.SetSelection(faceGroupSelection);

                _me.Undo.BeginRecord();
                CurrentSelection = meshEditor.GetSelection();
                RecordSelection(oldSelection, CurrentSelection);

                TryUpdatePivotTransform();
                TrySelectPivot(meshEditor);
                TryUpdatePivotVisibility();

                _me.Undo.EndRecord();
            }
        }

        public void ConvertUVs(bool auto)
        {
            RunUVEditingAction(selection => { _autoUVEditor.SetAutoUV(selection, auto); });
        }

        public void ResetUVs()
        {
            RunUVEditingAction(selection => { _autoUVEditor.ResetUV(selection); });
        }
        public void RecordState(MeshEditorState oldState, MeshEditorState newState, bool raiseMeshChanged = false)
        {
            UndoRedoCallback redo = record =>
            {
                if (newState != null)
                {
                    IMeshEditor meshEditor = GetEditor();
                    if(meshEditor != null)
                    {
                        meshEditor.SetState(newState);
                    }
                    else
                    {
                        newState.Apply();
                    }
            
                    if(raiseMeshChanged)
                    {
                        foreach (PBMesh mesh in newState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
                    }
                    TryUpdatePivotTransform();
                    TrySelectPivot(meshEditor, false);
                    TryUpdatePivotVisibility();
                    OnSelectionChanged();
                    return true;
                }
                return false;
            };

            UndoRedoCallback undo = record =>
            {
                if (oldState != null)
                {
                    IMeshEditor meshEditor = GetEditor();
                    if (meshEditor != null)
                    {
                        meshEditor.SetState(oldState);
                    }
                    else
                    {
                        oldState.Apply();
                    }
            
                    if(raiseMeshChanged)
                    {
                        foreach (PBMesh mesh in newState.GetMeshes())
                        {
                            mesh.RaiseChanged(false, true);
                        }
                    }
                    
                    TryUpdatePivotTransform();
                    TrySelectPivot(meshEditor, false);
                    TryUpdatePivotVisibility();
                    OnSelectionChanged();
                    return true;
                }
                return false;
            };

            _me.Undo.CreateRecord(redo, undo);
            OnSelectionChanged();
        }
        
        public void CreateNewShapeAndRecord(int shapeType,ObjectItem item)
        {
            ExposeToEditor exposeToEditor = CreateNewShape((PBShapeType)shapeType);

            exposeToEditor.gameObject.AddComponent<MapSaveObject>().Init(item);
            
            IMESelectionComponent selectionComponent = _selectionComponent;

            _me.Undo.BeginRecord();
            if (selectionComponent == null || selectionComponent.CanSelect)
            {
                _me.Selection.ActiveGameObject = exposeToEditor.gameObject;
            }

            _me.Undo.RegisterCreatedObjects(new[] { exposeToEditor });
            _me.Undo.EndRecord();

            // return exposeToEditor;
        }

        public ExposeToEditor CreateNewShape(PBShapeType type)
        {
            GameObject go = PBShapeGenerator.CreateShape(type);
            go.transform.SetParent(_me.EditedObject.transform);
            go.AddComponent<PBMesh>();

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                // A renderer with a null material reports an empty sharedMaterials array, not a
                // one-element array holding null. Testing only for Length == 1 therefore skipped the
                // palette whenever the shape was created without a prototype material - which is
                // exactly when the palette material matters most, leaving the object with no material.
                Material currentMaterial = renderer.sharedMaterial;

                if (currentMaterial == null || currentMaterial == PBBuiltinMaterials.DefaultMaterial)
                {
                    IMaterialPaletteManager paletteManager = MaterialPaletteManager.Instance;
                    if (paletteManager != null && paletteManager.Palette != null && paletteManager.Palette.Materials.Count > 0)
                    {
                        renderer.sharedMaterial = paletteManager.Palette.Materials[0];
                    }
                }
            }

            Vector3 position;
            Quaternion rotation;
            GetPositionAndRotation(out position, out rotation);

            ExposeToEditor exposeToEditor = go.AddComponent<ExposeToEditor>();
            go.transform.position = position + rotation * Vector3.up * exposeToEditor.Bounds.extents.y;
            go.transform.rotation = rotation;

            return exposeToEditor;
        }

        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation, bool rotateToTerrain = false)
        {
            var transform1 = _me.Camera.transform;
            Ray ray = new Ray(transform1.position, transform1.forward);

            RaycastHit[] hits = Physics.RaycastAll(ray);
            for (int i = 0; i < hits.Length; ++i)
            {
                RaycastHit hit = hits[i];
                if (hit.collider is TerrainCollider)
                {
                    position = hit.point;
                    if (rotateToTerrain)
                    {
                        rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    }
                    else
                    {
                        rotation = Quaternion.identity;
                    }
                    return;
                }
            }

            Vector3 up = Vector3.up;
            Vector3 pivot = Vector3.zero;
            IScenePivot scenePivot = _selectionComponent;
            if (Mathf.Abs(Vector3.Dot(transform1.up, Vector3.up)) > Mathf.Cos(Mathf.Deg2Rad))
            {
                up = Vector3.Cross(transform1.right, Vector3.up);
            }

            pivot = scenePivot.SecondaryPivot;

            Plane dragPlane = new Plane(up, pivot);
            rotation = Quaternion.identity;
            if (!GetPointOnDragPlane(ray, dragPlane, out position))
            {
                position = transform1.position + transform1.forward * 10.0f;
            }
        }
        private bool GetPointOnDragPlane(Ray ray, Plane dragPlane, out Vector3 point)
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
    }
}