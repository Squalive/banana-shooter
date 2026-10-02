using System;
using System.Collections;
using System.Threading.Tasks;
using CodingDaniel.MapEditor.Handle;
using CodingDaniel.MapEditor.MECommon;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.UI;
using UnityEngine;
using UnityEngine.Rendering;

using CodingDaniel.MapEditor.Interaction;

using CodingDaniel.MapEditor.Graphics;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.MEEditor
{
    [Serializable]
    public struct CameraLayerSettings
    {
        public int ResourcePreviewLayer;
        public int RuntimeGraphicsLayer;
        public int MaxGraphicsLayers;
        public int AllScenesLayer;
        public int ExtraLayer2;
        public int ExtraLayer;
        public int UIBackgroundLayer;

        public int RaycastMask
        {
            get
            {
                return ~((((1 << MaxGraphicsLayers) - 1) << RuntimeGraphicsLayer) | (1 << AllScenesLayer) | (1 << ExtraLayer) | (1 << ExtraLayer2) | (1 << ResourcePreviewLayer));
            }
        }

        public CameraLayerSettings(int resourcePreviewLayer, int runtimeGraphicsLayer, int maxLayers, int allSceneLayer, int extraLayer, int hiddenLayer, int uiBackgroundLayer)
        {
            ResourcePreviewLayer = resourcePreviewLayer;
            RuntimeGraphicsLayer = runtimeGraphicsLayer;
            MaxGraphicsLayers = maxLayers;
            AllScenesLayer = allSceneLayer;
            ExtraLayer = extraLayer;
            ExtraLayer2 = hiddenLayer;
            UIBackgroundLayer = uiBackgroundLayer;
        }
    }
    public interface IME
    {
        EditorToolState Tools
        {
            get;
        }

        IMESelection Selection
        {
            get;
        }

        IMEObjects Object
        {
            get;
        }

        IRuntimeUndo Undo
        {
            get;
        }

        Camera Camera
        {
            get;
        }

        Pointer Pointer
        {
            get;
        }

        /// <summary>
        /// Interaction-layer services. The editor root owns and resolves them, so the handles no
        /// longer reach for the components' own static Instance properties.
        /// </summary>
        MEHandleComponent Appearance
        {
            get;
        }

        MEHitTester HitTester
        {
            get;
        }

        /// <summary>Camera/graphics service the interaction layer draws through.</summary>
        IMEGraphic Graphics
        {
            get;
        }
        CameraLayerSettings CameraLayerSettings
        {
            get;
        }

        GameObject EditedObject
        {
            get;
        }

        GameObject PlayModeObject
        {
            get;
        }

        bool IsPlayMode
        {
            set;
            get;
        }

        bool HasChanged
        {
            get;
            set;
        }
    }
    [DefaultExecutionOrder(-91)]
    public class MEBase : MonoBehaviour,IME
    {
        public static IME Instance { private set; get; }
        
        private void Awake()
        {
            Instance = this;

            if (useBuiltinUndo)
            {
                _undo = new RuntimeUndo(this);
            }
            else
            {
                _undo = new DisabledUndo();
            }

            _selection = new MESelection(this);
            _object = gameObject.AddComponent<MEObjects>();
            
            _camera = Camera.main;
            
            QualitySettings.SetQualityLevel(3);

            _editedObject = new GameObject("Edit Object Root");
            _playModeObject = new GameObject("Play Mode Object Root");

            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            while (MapSettingMenu.Instance==null ||TabHolder.Instance==null  )
            {
                yield return null;
            }
            yield return MapSaver.Instance.LoadEditorMap();
        }
        public EditorToolState Tools { get; } = new EditorToolState();
        
        private IMESelection _selection;
        private IMEObjects _object;
        private Camera _camera;
        [SerializeField] private Pointer pointer;
        
        public Camera Camera => _camera;
        private IRuntimeUndo _undo;
        public virtual IMESelection Selection
        {
            get { return _selection; }
        }
        
        public virtual IMEObjects Object
        {
            get { return _object; }
        }
        public virtual IRuntimeUndo Undo
        {
            get { return _undo; }
        }
        public virtual Pointer Pointer => pointer;

        private MEHandleComponent _appearance;
        private MEHitTester _hitTester;

        /// <summary>
        /// Resolved from the scene on first use and cached. Lazy on purpose: the handles Awake at
        /// execution order -50, well after this root (-91), so a lookup here is always safe.
        /// </summary>
        public virtual MEHandleComponent Appearance
        {
            get
            {
                if (_appearance == null)
                {
                    _appearance = FindObjectOfType<MEHandleComponent>();
                }

                return _appearance;
            }
        }

        public virtual MEHitTester HitTester
        {
            get
            {
                if (_hitTester == null)
                {
                    _hitTester = FindObjectOfType<MEHitTester>();
                }

                return _hitTester;
            }
        }

        private IMEGraphic _graphics;

        public virtual IMEGraphic Graphics
        {
            get
            {
                if (_graphics == null)
                {
                    _graphics = FindObjectOfType<MEGraphic>();
                }

                return _graphics;
            }
        }
        [SerializeField]
        [FormerlySerializedAs("m_cameraLayerSettings")]
        private CameraLayerSettings _cameraLayerSettings = new CameraLayerSettings(20, 21, 4, 17, 18, 19, 16);
        public virtual CameraLayerSettings CameraLayerSettings
        {
            get { return _cameraLayerSettings; }
        }

        public bool useBuiltinUndo;

        private bool _isPlayMode;

        public bool IsPlayMode
        {
            set => _isPlayMode = value;
            get => _isPlayMode;
        }
        private bool _hasChanged;

        public bool HasChanged
        {
            set => _hasChanged = value;
            get => _hasChanged;
        }
        private GameObject _editedObject, _playModeObject;

        public GameObject EditedObject
        {
            get => _editedObject;
        }
        
        public GameObject PlayModeObject
        {
            get => _playModeObject;
        }
    }
}