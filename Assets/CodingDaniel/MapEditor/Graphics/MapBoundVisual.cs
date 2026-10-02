using System;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodingDaniel.MapEditor.Graphics
{
    [DefaultExecutionOrder(-50)]
    public class MapBoundVisual : MonoBehaviour
    {
        public static MapBoundVisual Instance { private set; get; }
        
        private MaterialPropertyBlock _lineProperties;
        protected MaterialPropertyBlock LineProperties
        {
            get { return _lineProperties; }
        }
        
        protected Camera SceneCamera
        {
            get { return MEBase.Instance.Camera; }
        }
        
        private Vector3 _prevPosition;
        private Quaternion _prevRotation;
        private Vector3 _prevScale;

        private Vector3 _prevCamPosition;
        private Quaternion _prevCamRotation;
        private bool _prevOrthographic;

        private bool _refreshOnCameraChanged;
        protected bool RefreshOnCameraChanged
        {
            get { return _refreshOnCameraChanged; }
            set { _refreshOnCameraChanged = value; }
        }
        
        private IMECamera _rteCamera;
        public IMECamera MECamera
        {
            get { return _rteCamera; }
        }

        public CameraEvent CameraEvent
        {
            get { return CameraEvent.AfterImageEffectsOpaque; }
        }

        protected virtual bool ForceCreateCamera
        {
            get { return false; }
        }
        
        
        private Vector3[] _handlesNormals;
        private Vector3[] _handlesPositions;
        protected virtual Vector3[] HandlesPositions
        {
            get { return _handlesPositions; }
        }

        protected virtual Vector3[] HandlesNormals
        {
            get { return _handlesNormals; }
        }
        
        private IME me;
        void Awake()
        {
            Instance = this;
            _handlesPositions = GizmoUtility.GetHandlesPositions();
            _handlesNormals = GizmoUtility.GetHandlesNormals();

            _lineProperties = new MaterialPropertyBlock();
            
            me = MEBase.Instance;

        }

        private void OnDestroy()
        {
            if (_rteCamera != null)
            {
                _rteCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                _rteCamera.RefreshCommandBuffer();
            }
        }
        
        void Start()
        {

            if (_rteCamera == null && SceneCamera != null)
            {
                IMEGraphic graphics = MEBase.Instance.Graphics;
                if(graphics != null)
                {
                    _rteCamera = graphics.GetOrCreateCamera(SceneCamera, CameraEvent);
                }
                                
                if(_rteCamera == null)
                {
                    _rteCamera = SceneCamera.gameObject.AddComponent<MECamera>();
                    _rteCamera.Event = CameraEvent;
                }
            }

            if(_rteCamera != null)
            {
                _prevPosition = transform.position;
                _prevRotation = transform.rotation;
                _prevScale = transform.localScale;

                _prevCamPosition = _rteCamera.Camera.transform.position;
                _prevCamRotation = _rteCamera.Camera.transform.rotation;
                _prevOrthographic = _rteCamera.Camera.orthographic;

                // Subscribing here as well as in OnEnable left the single removal in OnDisable one short,
                // so a disabled MapBoundVisual kept drawing. See BaseHandle for the same fix.
                _rteCamera.RefreshCommandBuffer();
            }
            
            enabled = false;
        }

        void OnEnable()
        {
            if (_rteCamera != null)
            {
                // Idempotent: exactly one subscription while enabled, so OnDisable's single removal
                // always stops it drawing.
                _rteCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                _rteCamera.CommandBufferRefresh += OnCommandBufferRefresh;
                _rteCamera.RefreshCommandBuffer();
            }

            // me.Undo.StateChanged += OnUndo;
            // me.Undo.UndoCompleted += OnUndo;
            // me.Undo.RedoCompleted += OnUndo;
        }
        
        void OnDisable()
        {
            if (_rteCamera != null)
            {
                _rteCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                _rteCamera.RefreshCommandBuffer();
            }

            // if(me != null && me.Undo != null)
            // {
            //     me.Undo.StateChanged -= OnUndo;
            //     me.Undo.UndoCompleted -= OnUndo;
            //     me.Undo.RedoCompleted -= OnUndo;
            // }
        }

        private void Update()
        {
            if (_rteCamera != null)
            {
                if (_prevPosition != transform.position || _prevRotation != transform.rotation || _prevScale != transform.localScale)
                {
                    _prevPosition = transform.position;
                    _prevRotation = transform.rotation;
                    _prevScale = transform.localScale;

                    _rteCamera.RefreshCommandBuffer();
                }  
            }
        }

        private void LateUpdate()
        {
            if (_rteCamera != null && _rteCamera.Camera != null && _refreshOnCameraChanged)
            {
                Camera camera = _rteCamera.Camera;
                if (_prevCamPosition != camera.transform.position || _prevCamRotation != camera.transform.rotation || _prevOrthographic != camera.orthographic)
                {
                    _prevCamPosition = camera.transform.position;
                    _prevCamRotation = camera.transform.rotation;
                    _prevOrthographic = camera.orthographic;

                    _rteCamera.RefreshCommandBuffer();
                }
            }
        }

        public Bounds Bounds { get; set; } = new Bounds(Vector3.zero, new Vector3(20, 10, 20));

        void OnCommandBufferRefresh(IMECamera camera)
        {
            LineProperties.SetColor("_Color", MEBase.Instance.Appearance.Colors.BoundsColor);
            
            Bounds bounds = Bounds;
            Vector3 scale = bounds.extents;
            
            GizmoUtility.DrawWireCube(camera.CommandBuffer, bounds, bounds.center, Quaternion.identity, scale, LineProperties);

        }
    }
}
