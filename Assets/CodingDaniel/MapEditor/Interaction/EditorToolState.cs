using System;
using UnityEngine;

using CodingDaniel.MapEditor.MECommon;
using UnityObject = UnityEngine.Object;
namespace CodingDaniel.MapEditor.Interaction
{
    public enum EditorTool
    {
        None,
        Move,
        Rotate,
        Scale,
        View,
        Rect,
        Custom
    }

    public enum MEPivotRotation
    {
        Local,
        Global
    }
    
    public enum MEPivotMode
    {
        Center = 0,
        Pivot = 1
    }

    public enum SnappingMode
    {
        BoundingBox,
        Vertex,
    }
    public delegate void EditorToolEvent<T1, T2>(T1 arg1, T2 arg2);
    public delegate void EditorToolEvent();
    public class EditorToolState
    {
        public event EditorToolEvent<EditorTool, object> ToolChanging;
        public event EditorToolEvent ToolChanged;

        public event EditorToolEvent PivotRotationChanging;
        public event EditorToolEvent PivotRotationChanged;
        public event EditorToolEvent PivotModeChanging;
        public event EditorToolEvent PivotModeChanged;
        public event EditorToolEvent SelectionModeChanging;
        public event EditorToolEvent SelectionModeChanged;

        public event EditorToolEvent IsViewingChanged;
        public event EditorToolEvent AutoFocusChanged;
        public event EditorToolEvent UnitSnappingChanged;
        public event EditorToolEvent IsSnappingChanged;
        public event EditorToolEvent SnappingModeChanged;
        public event EditorToolEvent LockAxesChanged;
        public event EditorToolEvent ActiveToolChanged;
        public event EditorToolEvent IsBoxSelectionEnabledChanged;

        private bool _isViewing;
        public bool IsViewing
        {
            get { return _isViewing; }
            set
            {
                if(_isViewing != value)
                {
                    _isViewing = value;
                    if(_isViewing)
                    {
                        ActiveTool = null;
                    }
                    IsViewingChanged?.Invoke();
                }
            }
        }

        private bool _autoFocus;
        public bool AutoFocus
        {
            get { return _autoFocus; }
            set
            {
                if(_autoFocus != value)
                {
                    _autoFocus = value;
                    AutoFocusChanged?.Invoke();
                }
            }
        }

        private bool _unitSnapping;
        public bool UnitSnapping
        {
            get { return _unitSnapping; }
            set
            {
                if(_unitSnapping != value)
                {
                    _unitSnapping = value;
                    UnitSnappingChanged?.Invoke();
                }
            }
        }

        private bool _isSnapping=false;
        public bool IsSnapping
        {
            get { return _isSnapping; }
            set
            {
                if(_isSnapping != value)
                {
                    _isSnapping = value;
                    IsSnappingChanged?.Invoke();
                }
            }
        }

        private SnappingMode _snappingMode = SnappingMode.BoundingBox;
        public SnappingMode SnappingMode
        {
            get { return _snappingMode; }
            set
            {
                if(_snappingMode != value)
                {
                    _snappingMode = value;
                    SnappingModeChanged?.Invoke();
                }
            }
        }


        private UnityObject _activeTool;
        public UnityObject ActiveTool
        {
            get { return _activeTool; }
            set
            {
                if (_activeTool != value)
                {
                    _activeTool = value;
                    ActiveToolChanged?.Invoke();
                }
            }
        }

        private LockObject _lockAxes;
        public LockObject LockAxes
        {
            get { return _lockAxes; }
            set
            {
                if(_lockAxes != value)
                {
                    _lockAxes = value;
                    LockAxesChanged?.Invoke();
                }
            }
        }

        private EditorTool _current = EditorTool.Rotate;
        public EditorTool Current
        {
            get { return _current; }
            set
            {
                if (_current != value)
                {
                    ToolChanging?.Invoke(value, null);
                    _current = value;
                    if(_current != EditorTool.Custom)
                    {
                        _isBoxSelectionEnabled = true;
                    }
                    _custom = null;
                    ToolChanged?.Invoke();
                }
            }
        }

        private object _custom;
        public object Custom
        {
            get { return _custom; }
            set
            {
                if(_custom != value)
                {
                    ToolChanging?.Invoke(EditorTool.Custom, value);
                    _current = EditorTool.Custom;
                    _custom = value;
                    ToolChanged?.Invoke();
                }
            }
        }

      
        private bool _isBoxSelectionEnabled = true;
        public bool IsBoxSelectionEnabled
        {
            get { return _isBoxSelectionEnabled; }
            set 
            { 
                if(_isBoxSelectionEnabled != value)
                {
                    _isBoxSelectionEnabled = value;
                    IsBoxSelectionEnabledChanged?.Invoke();
                }
            }
        }

        private MEPivotMode _pivotMode;
        public MEPivotRotation PivotRotation
        {
            get { return _pivotRotation; }
            set
            {
                if (_pivotRotation != value)
                {
                    PivotRotationChanging?.Invoke();
                    _pivotRotation = value;
                    PivotRotationChanged?.Invoke();
                }
            }
        }

        private MEPivotRotation _pivotRotation;
        public MEPivotMode PivotMode
        {
            get { return _pivotMode; }
            set
            {
                if(_pivotMode != value)
                {
                    PivotModeChanging?.Invoke();
                    _pivotMode = value;
                    PivotModeChanged?.Invoke();
                }
            }
        }


        public EditorToolState()
        {
            Reset();
        }

        public void Reset()
        {
            ActiveTool = null;
            LockAxes = null;
            Custom = null;
            _isViewing = false;
            _isSnapping = false;
            _unitSnapping = false;
            _pivotMode = MEPivotMode.Center;
        }

        
    }
}