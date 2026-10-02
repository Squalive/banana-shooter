
using System;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

using CodingDaniel.MapEditor.Interaction;

using CodingDaniel.MapEditor.Interaction.TransformTools;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Handle
{
    [Serializable]
    public class BaseHandleUnityEvent : UnityEvent<BaseHandle> { }

    /// <summary>
    /// Base class for all handles (Position, Rotation and Scale)
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BaseHandle : MonoBehaviour
    {
        private IME _editor;
        protected IME Editor => _editor;
        
        public BaseHandleUnityEvent BeforeDrag = new BaseHandleUnityEvent();
        public BaseHandleUnityEvent Drag = new BaseHandleUnityEvent();
        public BaseHandleUnityEvent Drop = new BaseHandleUnityEvent();

        private IMECamera _meCamera;

        public IMECamera MeCamera => _meCamera;
        
        private Vector3 _prevScale;
        private Vector3 _prevCamPosition;
        private Quaternion _prevCamRotation;
        private bool _prevCamOrthographic;
        private float _prevCamOrthographicsSize;
        private Rect _prevCamRect;

        public MEHandleComponent appearance;
        
        

        protected virtual Plane DragPlane { get; set; }
        
        public virtual bool IsDragging { set; get; }
        public MEHitTester HitTester;
        public bool EnableUndo = true;
        public virtual EditorTool Tool
        {
            get { return EditorTool.Custom; }
        }
        
        /// <summary>
        /// Quaternion Rotation based on selected coordinate system (local or global)
        /// </summary>
        protected virtual Quaternion Rotation
        {
            get
            {
                if (ActiveRealTargets == null || ActiveRealTargets.Length <= 0 || ActiveRealTargets[0] == null)
                {
                    return Quaternion.identity;
                }

                return PivotRotation == MEPivotRotation.Local ? ActiveRealTargets[0].rotation : Quaternion.identity;
            }
        }
        /// <summary>
        /// Selected axis
        /// </summary>
        private HandleAxis _selectedAxis;
        public virtual HandleAxis SelectedAxis
        {
            get { return _selectedAxis; }
            set
            {
                if (_selectedAxis != value)
                {
                    _selectedAxis = value;
                    if (_meCamera != null)
                    {
                        _meCamera.RefreshCommandBuffer();
                    }
                }
            }
        }

        public virtual bool UnitSnapping { get; set; }
        
        public virtual bool SnapToGrid
        {
            get;
            set;
        }

        public virtual float SizeOfGrid
        {
            get;
            set;
        }
        
        /// <summary>
        /// current size of grid 
        /// </summary>
        protected float EffectiveGridUnitSize
        {
            get;
            private set;
        }

        protected virtual float CurrentGridUnitSize
        {
            get { return 0.0f; }
        }
        
        private LockObject _sharedLockObject;
        private LockObject _rawLockObject;

        protected virtual LockObject SharedLockObject
        {
            get { return _sharedLockObject; }
            set 
            {
                _rawLockObject = value;
                _sharedLockObject = ApplyViewModeLocks(value);

                if (_sharedLockObject != null && Editor.Tools.LockAxes != null)
                {
                    _sharedLockObject.SetGlobalLock(Editor.Tools.LockAxes);
                }
            }
        }

        /// <summary>
        /// A camera-aligned view must not act on the axis pointing at the camera. Which locks that
        /// implies comes from the tool's definition; this was copy-pasted into all three tools before.
        /// </summary>
        private LockObject ApplyViewModeLocks(LockObject locks)
        {
            TransformToolDefinition definition = Definition;
            if (definition == null || _currentMode == ViewMode.XYZ3D)
            {
                return locks;
            }

            LockObject forced = locks != null ? new LockObject(locks) : new LockObject();
            definition.LocksFor(_currentMode).ApplyTo(forced);
            return forced;
        }
        
        private ViewMode _currentMode;

        /// <summary>
        /// Which camera-aligned view mode the handle is in. Changing it re-applies the view-mode locks
        /// from the raw lock object, so a lock forced for the previous plane does not linger.
        /// </summary>
        protected virtual ViewMode CurrentMode
        {
            get { return _currentMode; }
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    SharedLockObject = _rawLockObject;
                }
            }
        }

        private TransformToolDefinition _definition;

        /// <summary>
        /// The tool's data, resolved from the registry by <see cref="Tool"/>. Null for handles that are
        /// not one of the registered transform tools, in which case no view-mode locks are forced.
        /// </summary>
        protected TransformToolDefinition Definition
        {
            get
            {
                if (_definition == null)
                {
                    _definition = TransformToolRegistry.For(Tool);
                }

                return _definition;
            }
        }

        /// <summary>
        /// Whether the handle input should offer vertex snapping. Read from the tool's definition, so the
        /// input component needs no knowledge of which tool it is driving.
        /// </summary>
        public bool SupportsVertexSnapping
        {
            get
            {
                TransformToolDefinition definition = Definition;
                return definition != null && definition.SupportsVertexSnapping;
            }
        }

        /// <summary>Called by the handle input when the vertex-snapping key changes state.</summary>
        public virtual void SetVertexSnapping(bool snapping)
        {
        }
        
        protected void UpdateCurrentMode()
        {
            if(IsDragging)
            {
                return;
            }

            const float threshold = 0.98f;

            Vector3 camPos = _cam.transform.position;
            Vector3 toCam = _cam.orthographic ? _cam.transform.forward : (Position - camPos).normalized;
            Quaternion rotation = Rotation;
            
            if(Mathf.Abs(Vector3.Dot(toCam,  rotation * Vector3.forward)) >= threshold)
            {
                CurrentMode = ViewMode.XY2D;
            }
            else if(Mathf.Abs(Vector3.Dot(toCam, rotation * Vector3.up)) >= threshold)
            {
                CurrentMode = ViewMode.XZ2D;
            }
            else if(Mathf.Abs(Vector3.Dot(toCam, rotation * Vector3.right)) >= threshold)
            {
                CurrentMode = ViewMode.YZ2D;
            }
            else
            {
                CurrentMode = ViewMode.XYZ3D;
            }
        }
        
        public virtual Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }
        
        /// <summary>
        /// Target objects which will be affected by handle (for example if _targets array contains O1 and O2 objects and O1 is parent of O2 then _activeTargets array will contain only O1 object)
        /// </summary>
        private Transform[] _activeTargets;
        public virtual Transform[] ActiveTargets
        {
            get { return _activeTargets; }
        }

        private Transform[] _activeRealTargets;
        protected virtual Transform[] ActiveRealTargets
        {
            get { return _activeRealTargets; }
        }

        private Transform[] _realTargets;
        public virtual Transform[] RealTargets
        {
            get
            {
                if(_realTargets == null)
                {
                    return Targets;
                }
                return _realTargets;
            }
        }
        
        private Transform[] _commonCenter;
        private Transform[] _commonCenterTarget;
        
        private BaseHandleInput _input;

        private static List<BaseHandle> _allHandles = new List<BaseHandle>();
        protected static List<BaseHandle> AllHandles
        {
            get { return _allHandles; }
        }
        
        private void GetActiveRealTargets()
        {
            if(_realTargets == null)
            {
                _activeRealTargets = null;
                return;
            }

            _realTargets = _realTargets.Where(t => t != null && ((t.hideFlags & HideFlags.DontSave) == 0)).ToArray();
            HashSet<Transform> targetsHS = new HashSet<Transform>();
            for (int i = 0; i < _realTargets.Length; ++i)
            {
                if (_realTargets[i] != null && !targetsHS.Contains(_realTargets[i]))
                {
                    targetsHS.Add(_realTargets[i]);
                }
            }
            _realTargets = targetsHS.ToArray();
            if (_realTargets.Length == 0)
            {
                _activeRealTargets = new Transform[0];
                return;
            }
            else if (_realTargets.Length == 1)
            {
                _activeRealTargets = new[] { _realTargets[0] };
            }

            for (int i = 0; i < _realTargets.Length; ++i)
            {
                Transform target = _realTargets[i];
                Transform p = target.parent;
                while (p != null)
                {
                    if (targetsHS.Contains(p))
                    {
                        targetsHS.Remove(target);
                        break;
                    }

                    p = p.parent;
                }
            }

            _activeRealTargets = targetsHS.ToArray();
        }
        
        /// <summary>
        /// All Target objects
        /// </summary>
        [SerializeField]
        [FormerlySerializedAs("m_targets")]
        private Transform[] _targets;

        private Camera _cam;

        public virtual Transform[] Targets
        {
            get
            {
                return Targets_Internal;
            }
            set
            {
                DestroyCommonCenter(true);
                _realTargets = value;
                GetActiveRealTargets();
                Targets_Internal = value;
                if (Targets_Internal == null || Targets_Internal.Length == 0)
                {
                    return;
                }

                if (PivotMode == MEPivotMode.Center && ActiveTargets.Length > 1)
                {
                    Vector3 centerPosition = GetCommonCenterPosition();
                    _commonCenter = new Transform[1];
                    _commonCenter[0] = new GameObject { name = "CommonCenter" }.transform;
                    _commonCenter[0].SetParent(transform.parent, true);
                    _commonCenter[0].position = centerPosition;
                    _commonCenter[0].rotation = Rotation;
                    _commonCenterTarget = new Transform[_realTargets.Length];
                    for (int i = 0; i < _commonCenterTarget.Length; ++i)
                    {
                        GameObject target = new GameObject { name = "ActiveTarget " + _realTargets[i].name };
                        target.transform.SetParent(_commonCenter[0]);

                        target.transform.position = _realTargets[i].position;
                        target.transform.rotation = _realTargets[i].rotation;
                        target.transform.localScale = _realTargets[i].localScale;

                        _commonCenterTarget[i] = target.transform;
                    }
                    LockObject lockObject = SharedLockObject;
                    Targets_Internal = _commonCenter;
                    SharedLockObject = lockObject;
                }
            }
        }
        protected virtual Transform[] Targets_Internal
        {
            get { return _targets; }
            set
            {
             
                _targets = value;
                if(_targets == null)
                {
                    SharedLockObject = LockAxes.Eval(null);
                    _activeTargets = null;
                    return;
                }

                _targets = _targets.Where(t => t != null && ((t.hideFlags & HideFlags.DontSave) == 0)).ToArray();
                HashSet<Transform> targetsHS = new HashSet<Transform>();
                for (int i = 0; i < _targets.Length; ++i)
                {
                    if (_targets[i] != null && !targetsHS.Contains(_targets[i]))
                    {
                        targetsHS.Add(_targets[i]);
                    }
                }
                _targets = targetsHS.ToArray();
                if (_targets.Length == 0)
                {
                    SharedLockObject = LockAxes.Eval(new LockAxes[0]);
                    _activeTargets = new Transform[0];
                    return;
                }
                else if(_targets.Length == 1)
                {
                    _activeTargets = new [] { _targets[0] };
                }

                for(int i = 0; i < _targets.Length; ++i)
                {
                    Transform target = _targets[i];
                    Transform p = target.parent;
                    while(p != null)
                    {
                        if(targetsHS.Contains(p))
                        {
                            targetsHS.Remove(target);
                            break;
                        }

                        p = p.parent;
                    }
                }

                _activeTargets = targetsHS.ToArray();
                LockObject lockObject = LockAxes.Eval(_activeTargets.Where(t => t.GetComponent<LockAxes>() != null).Select(t => t.GetComponent<LockAxes>()).ToArray());
                if(_activeTargets.Any(target => target.gameObject.isStatic))
                {
                    lockObject.PositionX = lockObject.PositionY = lockObject.PositionZ = true;
                    lockObject.RotationX = lockObject.RotationY = lockObject.RotationZ = true;
                    lockObject.ScaleX = lockObject.ScaleY = lockObject.ScaleZ = true;
                    lockObject.RotationScreen = true;
                    lockObject.RotationFree = true;
                }
                SharedLockObject = lockObject;

                if (_activeTargets != null && _activeTargets.Length > 0)
                {
                    transform.position = _activeTargets[0].position;
                }

            }
        }
        
        public Transform Target
        {
            get
            {
                if(Targets == null || Targets.Length == 0)
                {
                    return null;
                }
                return Targets[0];
            }
        }

        protected virtual MEPivotMode PivotMode
        {
            get
            {
                LockObject lockObject = SharedLockObject;
                if (lockObject != null && lockObject.PivotMode != null)
                {
                    return lockObject.PivotMode.Value;
                }

                return Editor.Tools.PivotMode;
            }
        }
        
        protected virtual MEPivotRotation PivotRotation
        {
            get
            {
                LockObject lockObject = SharedLockObject;
                if (lockObject != null && lockObject.PivotRotation != null)
                {
                    return lockObject.PivotRotation.Value;
                }

                return Editor.Tools.PivotRotation;
            }
        }
        
        protected virtual Vector3 GetCenterPosition(Transform target)
        {
            return target.GetCenter();
        }

        protected virtual Vector3 GetCommonCenterPosition()
        {
            return TransformUtility.GetCommonCenter(Targets_Internal);
        }

        protected virtual void Awake()
        {
            _editor = MEBase.Instance;
            _allHandles.Add(this);
            
            HitTester = _editor.HitTester;
            appearance = _editor.Appearance;
            IMEGraphic graphic = _editor.Graphics;
            if (graphic != null)
            {
                _meCamera = graphic.GetOrCreateCamera(Camera.main, CameraEvent.AfterImageEffectsOpaque);
            }
            if (_targets != null && _targets.Length > 0 )
            {
                LockObject lockObject = SharedLockObject;
                if(_commonCenter == null || _commonCenter.Length == 0 || _commonCenter[0] != _targets[0])
                {
                    Targets = _targets;
                }
                SharedLockObject = lockObject;
            }
            
            if (Targets == null || Targets.Length == 0)
            {
                LockObject lockObject = SharedLockObject;
                Targets = new[] { transform };
                SharedLockObject = lockObject;
            }
            
        }

        protected virtual void Start()
        {
            _cam = Camera.main;
            _input =  GetComponent<BaseHandleInput>();

            if (_input == null || _input.Handle != this)
            {
                _input = gameObject.AddComponent<BaseHandleInput>();
                _input.Handle = this;
            }
            
            

            if (_meCamera != null)
            {
                _prevScale = transform.localScale;

                var transform1 = _meCamera.Camera.transform;
                _prevCamPosition = transform1.position;
                _prevCamRotation = transform1.rotation;
                _prevCamOrthographic = _meCamera.Camera.orthographic;
                _prevCamOrthographicsSize = _meCamera.Camera.orthographicSize;
                _prevCamRect = _meCamera.Camera.rect;
                
                // Subscription is owned by OnEnable/OnDisable. It used to be added here as well, which
                // left a second, never-removed handler: a handle deactivated by a tool switch kept
                // redrawing itself into the camera command buffer, so the previous handle stayed on
                // screen after W/E/R/T.
                _meCamera.RefreshCommandBuffer();
            }
        }

        protected virtual void OnEnable()
        {
            Editor.Tools.PivotRotationChanged += OnPivotRotationChanged;
            Editor.Tools.PivotModeChanged += OnPivotModeChanged;
            Editor.Tools.ToolChanged += OnRuntimeToolChanged;
            Editor.Tools.LockAxesChanged += OnLockAxesChanged;
            Editor.Undo.UndoCompleted += OnUndoCompleted;
            Editor.Undo.RedoCompleted += OnRedoCompleted;

            if (HitTester)
            {
                HitTester.Add(this);
            }
            
            if(_input != null)
            {
                _input.enabled = true;
            }
            
            if (_meCamera != null)
            {
                // Idempotent on purpose: exactly one subscription must exist per enabled handle, so the
                // single removal in OnDisable is always enough to stop it drawing.
                _meCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                _meCamera.CommandBufferRefresh += OnCommandBufferRefresh;
                _meCamera.RefreshCommandBuffer();
            }
        }

        protected virtual void OnDisable()
        {
            if (_meCamera != null)
            {
                _meCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                _meCamera.RefreshCommandBuffer();
            }

            if (HitTester)
            {
                HitTester.Remove(this);
            }
            
            Editor.Tools.PivotRotationChanged -= OnPivotRotationChanged;
            Editor.Tools.PivotModeChanged -= OnPivotModeChanged;
            Editor.Tools.ToolChanged -= OnRuntimeToolChanged;
            Editor.Tools.LockAxesChanged -= OnLockAxesChanged;
            Editor.Undo.UndoCompleted -= OnUndoCompleted;
            Editor.Undo.RedoCompleted -= OnRedoCompleted;
            
            
            DestroyCommonCenter(false);
            

            if ( Editor.Tools != null && Editor.Tools.ActiveTool == this)
            {
                Editor.Tools.ActiveTool = null;
            }

            if (_input != null)
            {
                _input.enabled = false;
            }
        }

        protected void OnDestroy()
        {
            _allHandles.Remove(this);
            
            if (_input != null && _input.Handle == this)
            {
                Destroy(_input);
            }
            
            if (_meCamera != null)
            {
                _meCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                _meCamera.RefreshCommandBuffer();
            }
            
            DestroyCommonCenter(false);


            if ( Editor.Tools != null && Editor.Tools.ActiveTool == this)
            {
                Editor.Tools.ActiveTool = null;
            }
        }

        private void DestroyCommonCenter(bool destroyImmediate)
        {
            if (_commonCenter != null)
            {
                for (int i = 0; i < _commonCenter.Length; ++i)
                {
                    if(_commonCenter[i])
                    {
                        if(destroyImmediate)
                        {
                            DestroyImmediate(_commonCenter[i].gameObject);
                        }
                        else
                        {
                            Destroy(_commonCenter[i].gameObject);
                        }
                    }
                    
                }
            }

            if (_commonCenterTarget != null)
            {
                for (int i = 0; i < _commonCenterTarget.Length; ++i)
                {
                    if(_commonCenterTarget[i])
                    {
                        if (destroyImmediate)
                        {
                            DestroyImmediate(_commonCenterTarget[i].gameObject);
                        }
                        else
                        {
                            Destroy(_commonCenterTarget[i].gameObject);
                        }
                    }
                }
            }

            _commonCenter = null;
            _commonCenterTarget = null;
        }

        protected virtual void Update()
        {

            if (IsDragging)
            {
                if (Editor.Tools.IsViewing)
                {
                    IsDragging = false;
                }
                else
                {
                    if (UnitSnapping)
                    {
                        EffectiveGridUnitSize = CurrentGridUnitSize;
                    }
                    else
                    {
                        EffectiveGridUnitSize = 0;
                    }
                    
                    OnDrag();
                }
            }
            
            UpdateOverride();
            
            if (IsDragging)
            {
                if (PivotMode == MEPivotMode.Center && _commonCenterTarget != null && _realTargets != null && _realTargets.Length > 1)
                {
                    for (int i = 0; i < _commonCenterTarget.Length; ++i)
                    {
                        Transform commonCenterTarget = _commonCenterTarget[i];
                        Transform target = _realTargets[i];
                        var transform1 = target.transform;
                        transform1.position = commonCenterTarget.position;
                        transform1.rotation = commonCenterTarget.rotation;
                        transform1.localScale = commonCenterTarget.lossyScale;
                    }
                }

                if (Drag != null)
                {
                    Drag.Invoke(this);
                }

                if (_commonCenter != null && _commonCenter.Length > 0)
                {
                    for (int i = 0; i < _allHandles.Count; ++i)
                    {
                        BaseHandle handle = _allHandles[i];
                        if ( handle.gameObject.activeSelf)
                        {
                            handle._commonCenter[0].position = _commonCenter[0].position;
                            handle._commonCenter[0].rotation = _commonCenter[0].rotation;
                            handle._commonCenter[0].localScale = _commonCenter[0].localScale;
                        }
                    }
                }
            }
            
            TryRefreshCommandBuffer();
        }
        protected virtual void UpdateOverride()
        {
            Transform target = Targets != null && Targets.Length > 0 && Targets[0] != null ? Targets[0] : null;
            if (target != null && (target.position != transform.position || target.rotation != transform.rotation || target.localScale != _prevScale))
            {
                _prevScale = transform.localScale;
                if (IsDragging)
                {
                    Vector3 offset = transform.position - Targets[0].position;
                    for (int i = 0; i < ActiveTargets.Length; ++i)
                    {
                        if (ActiveTargets[i] != null)
                        {
                            ActiveTargets[i].position += offset;
                        }
                    }
                }
                else
                {
                    transform.position = target.position;
                    transform.rotation = target.rotation;
                }

                TryRefreshCommandBuffer();
            }

            TrySelectAxis();
        }
        protected bool TrySelectAxis()
        {
            HandleAxis selectedAxis = SelectedAxis;
            if (Editor.Tools.IsViewing)
            {
                SelectedAxis = HandleAxis.None;
            }
            else
            {
                if (!IsDragging)
                {
                    SelectedAxis = HitTester.GetSelectedAxis(this);
                }
            }
            return SelectedAxis != selectedAxis;
        }

        protected bool TryRefreshCommandBuffer()
        {
            if (MeCamera != null)
            {
                MeCamera.RefreshCommandBuffer();
                return true;
            }
            return false;
        }
        protected virtual void OnDrag()
        {

        }
        
        protected virtual void LateUpdate()
        {            
            if(!IsDragging)
            {
                if (Editor.Tools.ActiveTool == this)
                {
                    Editor.Tools.ActiveTool = null;
                }
            }

            
            Camera camera = _meCamera.Camera;
            if (_prevCamPosition != camera.transform.position ||
                _prevCamRotation != camera.transform.rotation ||
                _prevCamOrthographic != camera.orthographic ||
                _prevCamOrthographicsSize != camera.orthographicSize ||
                _prevCamRect != camera.rect)
            {
                _prevCamPosition = camera.transform.position;
                _prevCamRotation = camera.transform.rotation;
                _prevCamOrthographic = camera.orthographic;
                _prevCamOrthographicsSize = camera.orthographicSize;
                _prevCamRect = camera.rect;
                TryRefreshCommandBuffer();
            }
        }
        

        public virtual void BeginDrag()
        {
            if(Editor.Tools.IsViewing)
            {
                return;
            }

            if (Editor.Tools.ActiveTool != null)
            {
                return;
            }

            IsDragging = OnBeginDrag();
            if (IsDragging)
            {
                if (BeforeDrag != null)
                {
                    BeforeDrag.Invoke(this);
                }

                Editor.Tools.ActiveTool = this;
                BeginRecordTransform();
            }
            else
            {
                if(Editor.Tools.ActiveTool == this)
                {
                    Editor.Tools.ActiveTool = null;
                }
            }
        }
        
        public virtual void EndDrag()
        {
            if (IsDragging)
            {
                OnDrop();
                EndRecordTransform();
                IsDragging = false;

                TryRefreshCommandBuffer();

                if (Drop != null)
                {
                    Drop.Invoke(this);
                }
            }
        }
        protected virtual void OnDrop()
        {

        }
        
        protected virtual bool OnBeginDrag()
        {
            if (Target == null)
            {
                return false;
            }

            SelectedAxis = HitTester.GetSelectedAxis(this);

            if (!_editor.HasChanged) _editor.HasChanged = true;
            return true;
        }
        
        protected virtual void OnRuntimeToolChanged()
        {
            EndDrag();
        }
        
        protected virtual void OnPivotModeChanged()
        {
            if (RealTargets != null)
            {
                Targets = RealTargets;
            }

            if (PivotMode != MEPivotMode.Center)
            {
                _realTargets = null;   
            }
            
            if(Target != null)
            {
                transform.position = Target.position;
            }

            TryRefreshCommandBuffer();
        }

        protected virtual void OnPivotRotationChanged()
        {
            TryRefreshCommandBuffer();

            if (_commonCenter is {Length: > 0})
            {
                Targets = RealTargets;
            }
        }
        
        protected virtual void OnLockAxesChanged()
        {
            if(SharedLockObject != null)
            {
                LockObject lockObject = SharedLockObject;
                SharedLockObject = lockObject;
            }

            TryRefreshCommandBuffer();
        }
        
        protected virtual void BeginRecordTransform()
        {
            if (!EnableUndo)
            {
                return;
            }
            Editor.Undo.BeginRecord();
            for (int i = 0; i < _activeRealTargets.Length; ++i)
            {
                Transform target = _activeRealTargets[i];
                if(target != null)
                {
                    Editor.Undo.BeginRecordTransform(target);
                }
            }
            Editor.Undo.EndRecord();
        }
        
        protected virtual void EndRecordTransform()
        {
            if(!EnableUndo)
            {
                return;
            }
            Editor.Undo.BeginRecord();
            for (int i = 0; i < _activeRealTargets.Length; ++i)
            {
                Transform target = _activeRealTargets[i];
                if (target != null)
                {
                    Editor.Undo.EndRecordTransform(target);
                }
            }
            Editor.Undo.EndRecord();
        }

        protected virtual void OnRedoCompleted()
        {
            if (PivotMode == MEPivotMode.Center)
            {
                if(_realTargets != null && (_realTargets.Length != 1 || _realTargets[0] != transform))
                {
                    Targets = _realTargets;
                }
            }
        }

        protected virtual void OnUndoCompleted()
        {
            if (PivotMode == MEPivotMode.Center)
            {
                if (_realTargets != null && (_realTargets.Length != 1 || _realTargets[0] != transform))
                {
                    Targets = _realTargets;
                }
            }
        }
        
        public virtual HandleAxis HitTest(out float distance)
        {
            distance = float.PositiveInfinity;
            return HandleAxis.None;
        }
        
        protected virtual Plane GetDragPlane(Matrix4x4 matrix, Vector3 axis)
        {
            Plane plane = new Plane(matrix.MultiplyVector(axis).normalized, matrix.MultiplyPoint(Vector3.zero));
            return plane;
        }
        
        protected virtual Plane GetDragPlane(Vector3 axis)
        {
            Vector3 toCam;
            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(_meCamera.Camera.transform.forward, Rotation * axis)), 1))
            {
                toCam = _meCamera.Camera.transform.position - transform.position;
            }
            else
            {
                toCam = _meCamera.Camera.cameraToWorldMatrix.MultiplyVector(Vector3.forward); 
            }
            
            Plane dragPlane = new Plane(toCam.normalized, transform.position);
            return dragPlane;
        }
        
        protected virtual bool GetPointOnDragPlane(Ray ray, out Vector3 point)
        {
            return GetPointOnDragPlane(DragPlane, ray, out point);
        }

        protected virtual bool GetPointOnDragPlane(Plane dragPlane, Ray ray, out Vector3 point)
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
        
        protected Vector3 GetGridOffset(float gridSize, Vector3 position)
        {
            Vector3 currentPosition = position;
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.y = Mathf.Round(position.y / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            Vector3 offset = position - currentPosition;
            return offset;
        }

        protected virtual void OnCommandBufferRefresh(IMECamera camera)
        {
            if(Target != null)
            {
                RefreshCommandBuffer(camera);
            }
        }

        protected virtual void RefreshCommandBuffer(IMECamera camera)
        {

        }
    }
}