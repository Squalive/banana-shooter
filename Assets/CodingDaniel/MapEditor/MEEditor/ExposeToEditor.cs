using System;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.Events;
using INotifyPropertyChanged = System.ComponentModel.INotifyPropertyChanged;
using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;
using PropertyChangedEventHandler = System.ComponentModel.PropertyChangedEventHandler;
namespace CodingDaniel.MapEditor.MEEditor
{
    public enum BoundsType
    {
        Any,
        Mesh,
        SkinnedMesh,
        Custom,
        None,
        Sprite,
        RectTransform,
    }
    public delegate void ExposeToEditorChangeEvent<T>(ExposeToEditor obj, T oldValue, T newValue);
    public delegate void ExposeToEditorEvent(ExposeToEditor obj);
    public delegate void ExposeToEditorEvent<T>(ExposeToEditor obj, T arg);
    public delegate void ExposeToEditorEvent<T, T2>(ExposeToEditor obj, T arg, T2 arg2);
    [Serializable]
    public class ExposeToEditorUnityEvent : UnityEvent<ExposeToEditor> { }
    public class ExposeToEditor : MonoBehaviour, INotifyPropertyChanged
    {
        public const string HierarchyRootTag = "HierarchyRoot";

        public static event ExposeToEditorEvent Awaked;
        public static event ExposeToEditorEvent Destroying;
        public static event ExposeToEditorEvent Destroyed;
        public static event ExposeToEditorEvent MarkAsDestroyedChanging;
        public static event ExposeToEditorEvent MarkAsDestroyedChanged;
        public static event ExposeToEditorEvent NameChanged;
        public static event ExposeToEditorEvent TransformChanged;
        public static event ExposeToEditorEvent Started;
        public static event ExposeToEditorEvent Enabled;
        public static event ExposeToEditorEvent Disabled;

        public Collider[] Colliders { get; set; }
        
        private MeshFilter _filter;
        public MeshFilter MeshFilter
        {
            get { return _filter; }
        }
        
        private SkinnedMeshRenderer _skinned;
        public SkinnedMeshRenderer SkinnedMeshRenderer
        {
            get { return _skinned; }
        }
        
        private static readonly Bounds _none = new Bounds();
        public ExposeToEditorUnityEvent selected;
        public ExposeToEditorUnityEvent unselected;
        public GameObject boundsObject;
        public BoundsType boundsType;
        public Bounds customBounds;
        [SerializeField, HideInInspector]
        private bool _markAsDestroyed;
        public bool MarkAsDestroyed
        {
            get 
            {
                if(_markAsDestroyed)
                {
                    return true;
                }

                return false;
            
            }
            set
            {
                if (_markAsDestroyed != value)
                {
                    _markAsDestroyed = value;
                    if (MarkAsDestroyedChanging != null)
                    {
                        MarkAsDestroyedChanging(this);
                    }
                    gameObject.SetActive(!_markAsDestroyed);
                    if (MarkAsDestroyedChanged != null)
                    {
                        MarkAsDestroyedChanged(this);
                    }
                }
            }
        }
        public bool CanInspect = true;
        public bool CanDuplicate = true;
        public bool CanDelete = true;
        public bool CanTransform = true;
        public bool AddColliders = true;
        private BoundsType _effectiveBoundsType;
        public BoundsType EffectiveBoundsType
        {
            get { return _effectiveBoundsType; }
        }
        
        public Bounds Bounds
        {
            get
            {
                if (_effectiveBoundsType == BoundsType.Any)
                {
                    if (_filter != null && _filter.sharedMesh != null)
                    {
                        return _filter.sharedMesh.bounds;
                    }
                    else if (_skinned != null && _skinned.sharedMesh != null)
                    {
                        return _skinned.sharedMesh.bounds;
                    }
                    else if(boundsObject != null && boundsObject.transform is RectTransform)
                    {
                        return boundsObject.transform.CalculateRelativeRectTransformBounds();
                    }

                    return customBounds;
                }
                else if (_effectiveBoundsType == BoundsType.Mesh)
                {
                    if (_filter != null && _filter.sharedMesh != null)
                    {
                        return _filter.sharedMesh.bounds;
                    }
                    return _none;
                }
                else if (_effectiveBoundsType == BoundsType.SkinnedMesh)
                {
                    if (_skinned != null && _skinned.sharedMesh != null)
                    {
                        return _skinned.sharedMesh.bounds;
                    }
                }
                else if (_effectiveBoundsType == BoundsType.Sprite)
                {
                    if (_spriteRenderer != null)
                    {
                        return _spriteRenderer.sprite.bounds;
                    }
                }
                else if(_effectiveBoundsType == BoundsType.RectTransform)
                {
                    if (boundsObject != null && boundsObject.transform is RectTransform)
                    {
                        return boundsObject.transform.CalculateRelativeRectTransformBounds();
                    }
                }
                else if (_effectiveBoundsType == BoundsType.Custom)
                {
                    return customBounds;
                }
                return _none;
            }
        }
        
        public Vector3 LocalPosition
        {
            get { return transform.localPosition; }
            set { transform.localPosition = value; }
        }

        public Vector3 LocalScale
        {
            get { return transform.localScale; }
            set { transform.localScale = value; }
        }

        private Vector3 _localEulerAngles;
        public Vector3 LocalEuler
        {
            get => GetLocalEulerAngles(true);
            set => SetLocalEulerAngles(value, true);
        }
        Vector3 GetLocalEulerAngles(bool syncWithTransform = false)
        {
            float angle = Quaternion.Angle(transform.localRotation, Quaternion.Euler(_localEulerAngles));
            if (syncWithTransform && angle != 0)
            {
                _localEulerAngles = transform.localEulerAngles;
            }
            return _localEulerAngles;
        }
        public void SetLocalEulerAngles(Vector3 value, bool updateLocalRotation = false)
        {
            bool anglesChanged = _localEulerAngles != value;

            _localEulerAngles = value;
            if (updateLocalRotation && anglesChanged)
            {
                transform.localRotation = Quaternion.Euler(_localEulerAngles);
            }

        }
        
        public bool ActiveInHierarchy => gameObject.activeInHierarchy;
        
        public bool ActiveSelf
        {
            get => gameObject.activeSelf;
            set
            {
                if(ActiveSelf != value)
                {
                    gameObject.SetActive(value);
                    RaisePropertyChanged(nameof(ActiveSelf));
                    RaisePropertyChanged(nameof(ActiveInHierarchy));
                }
            }
        }
        
        private event PropertyChangedEventHandler _propertyChanged;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }
        
        void RaisePropertyChanged(string name)
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        public bool IsAwaked
        {
            get;
            private set;
        }

        private bool _initialized;
        public void Init()
        {
            if (_initialized)
            {
                return;
            }
            if (boundsObject == null)
            {
                boundsObject = gameObject;
            }
            _initialized = true;
        }
        private SpriteRenderer _spriteRenderer;
        public SpriteRenderer SpriteRenderer
        {
            get { return _spriteRenderer; }
        }
        private void Awake()
        {
            Init();
            IsAwaked = true;

            _effectiveBoundsType = boundsType;
            _filter = boundsObject.GetComponent<MeshFilter>();
            _skinned = boundsObject.GetComponent<SkinnedMeshRenderer>();
            
            if (_filter == null && _skinned == null)
            {
                _spriteRenderer = boundsObject.GetComponent<SpriteRenderer>();
            }

            bool visible = (hideFlags & HideFlags.HideInHierarchy) == 0;
            if (visible)
            {
                if (transform.parent != null && transform.parent.GetComponent<ExposeToEditor>() == null && transform.parent.tag != HierarchyRootTag)
                {
                    //gameObject.hideFlags = HideFlags.HideInHierarchy;
                    visible = false;
                    // Debug.LogWarning(gameObject.name + ": parent GameObject is not exposed to editor");
                }
            }

            if (visible)
            {
                Awaked?.Invoke(this);
            }
        }
        
        private void Start()
        {
            if ((hideFlags & HideFlags.HideInHierarchy) == 0)
            {
                Started?.Invoke(this);
            }
        }
        
        private void OnEnable()
        {
            if ((hideFlags & HideFlags.HideInHierarchy) == 0)
            {
                Enabled?.Invoke(this);
            }
        }

        private void OnDisable()
        {
            if ((hideFlags & HideFlags.HideInHierarchy) == 0)
            {
                Disabled?.Invoke(this);
            }
        }

        private void OnDestroy()
        {
            
            if ((hideFlags & HideFlags.HideInHierarchy) == 0)
            {
                Destroying?.Invoke(this);
            }

            if ((hideFlags & HideFlags.HideInHierarchy) == 0)
            {
                Destroyed?.Invoke(this);
            }
        }
        
        private void Update()
        {
            if (TransformChanged != null)
            {
                if (transform.hasChanged)
                {
                    transform.hasChanged = false;

                    if ((hideFlags & HideFlags.HideInHierarchy) == 0)
                    {
                        if (TransformChanged != null)
                        {
                            TransformChanged(this);
                        }
                    }
                }
            }
        }
        public ExposeToEditor GetParent(bool includeDestroyed = false)
        {
            if (transform.parent != null)
            {
                ExposeToEditor parent = transform.parent.GetComponent<ExposeToEditor>();
                if (parent != null && (includeDestroyed || !parent.MarkAsDestroyed))
                {
                    return parent;
                }
            }
            return null;
        }
        
    }
}
