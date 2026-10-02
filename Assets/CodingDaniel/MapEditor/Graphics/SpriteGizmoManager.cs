using System;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Graphics
{
    public interface ISpriteGizmoManager
    {
        void Register(Type type, Material material);
        Material Unregister(Type type);
        void Refresh();
    }
    [DefaultExecutionOrder(-1)]
    public class SpriteGizmoManager : MonoBehaviour,ISpriteGizmoManager
    {
        public static SpriteGizmoManager Instance { private set; get; }
        
        private readonly Dictionary<Type, string> _builtIn = new Dictionary<Type, string>
        {
            {  typeof(Light), "LightGizmoMat" },
            // {  typeof(PlayerSpawnPoint), "PlayerSpawnPoint" },
            {  typeof(DecalProjector), "Decal" },
            {  typeof(AudioSource), "AudioSource" },
        };
        
        private Dictionary<Type, Tuple<Mesh, Material>> _registered = new Dictionary<Type, Tuple<Mesh, Material>>();
        private Dictionary<Type, Tuple<Mesh, Material>> _typeToMeshAndMaterial;
        private Type[] _types;
        private IME _editor;
        private IMEGraphic _graphics;
        private IMeshesCache _meshesCache;
        
        [SerializeField]
        [FormerlySerializedAs("m_gizmoScale")]
        private float _gizmoScale = 1;
        public float GizmoScale
        {
            get { return _gizmoScale; }
            set { _gizmoScale = value; }
        }

        private void Awake()
        {
            Instance = this;
            _editor = MEBase.Instance;
            _graphics = MEBase.Instance.Graphics;
            if(_editor == null)
            {
                Debug.LogError("ME is null");
            }

        }
        private void OnEnable()
        {
            _meshesCache = _graphics.CreateSharedMeshesCache(CameraEvent.BeforeImageEffects);
            _meshesCache.RefreshMode = CacheRefreshMode.OnTransformChange;
            Refresh();
            
        }

        private void OnDisable()
        {
            Cleanup();

            foreach (Type type in _registered.Keys.ToArray())
            {
                Unregister(type);
            }

            if (_graphics != null)
            {
                _graphics.DestroySharedMeshesCache(_meshesCache);
            }

            _typeToMeshAndMaterial = null;
            _types = null;
        }
        public void Refresh()
        {
            Cleanup();
            Initialize();
        }
        
        public void Register(Type type, Material material)
        {
            if(!material.enableInstancing)
            {
                Debug.LogWarning("material enableInstance == false");
                return;
            }

            _registered[type] = new Tuple<Mesh, Material>(GraphicsUtility.CreateQuad(), material);
        }

        public Material Unregister(Type type)
        {
            Tuple<Mesh, Material> tuple;
            if (!_registered.TryGetValue(type, out tuple))
            {
                return null;
            }

            if (tuple.Item1 != null)
            {
                Destroy(tuple.Item1);
            }

            return tuple.Item2;
        }
        
        protected virtual void GreateGizmo(GameObject go,  Component component, Type type)
        {
            Tuple<Mesh, Material> tuple;
            if (_typeToMeshAndMaterial.TryGetValue(type, out tuple))
            {
                SpriteGizmo gizmo = go.GetComponent<SpriteGizmo>();
                if (!gizmo)
                {
                    gizmo = go.AddComponent<SpriteGizmo>();
                    gizmo.Component = component;

                    if (go.GetComponent<DecalProjector>())
                    {
                        gizmo.Scale = 2f;
                    }
                    gizmo.ComponentDestroyed += OnComponentDestroyed;
                }

                gizmo.Mesh = tuple.Item1;
                _meshesCache.Add(gizmo.Mesh, gizmo.transform);
                _meshesCache.SetMaterial(tuple.Item1, tuple.Item2);
            }
        }

        protected virtual void DestroyGizmo(GameObject go)
        {
            SpriteGizmo gizmo = go.GetComponent<SpriteGizmo>();
            if (gizmo)
            {
                Destroy(gizmo);
                _meshesCache.Remove(gizmo.Mesh, gizmo.transform);
            }
        }

        private void OnComponentDestroyed(SpriteGizmo gizmo)
        {
            gizmo.ComponentDestroyed -= OnComponentDestroyed;
            Destroy(gizmo);
            _meshesCache.Remove(gizmo.Mesh, gizmo.transform);
            _meshesCache.Refresh();
        }

        private void Initialize()
        {
            if (_types != null)
            {
                Debug.LogWarning("Already initialized");
                return;
            }

            _typeToMeshAndMaterial = new Dictionary<Type, Tuple<Mesh, Material>>();
            foreach(KeyValuePair<Type, Tuple<Mesh, Material>> kvp in _registered)
            {
                if (kvp.Value != null)
                {
                    _typeToMeshAndMaterial.Add(kvp.Key, kvp.Value);
                }   
            }

            foreach (KeyValuePair<Type, string> kvp in _builtIn)
            {
                if(_typeToMeshAndMaterial.ContainsKey(kvp.Key))
                {
                    continue;
                }

                Material material = Resources.Load<Material>(kvp.Value);
                if (material != null)
                {
                    _typeToMeshAndMaterial.Add(kvp.Key, new Tuple<Mesh, Material>(GraphicsUtility.CreateQuad(), material));
                }
            }

            int index = 0;
            _types = new Type[_typeToMeshAndMaterial.Count];
            foreach (Type type in _typeToMeshAndMaterial.Keys)
            {
                _types[index] = type;
                index++;
            }

            _types = GetTypes(_types);
            SubscribeAndCreate();
        }
        protected virtual Type[] GetTypes(Type[] types)
        {
            return types;
        }
        private void Cleanup()
        {
            if(_typeToMeshAndMaterial != null)
            {
                foreach(var kvp in _typeToMeshAndMaterial)
                {
                    if(_registered.ContainsKey(kvp.Key))
                    {
                        continue;
                    }

                    Mesh mesh = kvp.Value.Item1;
                    if(mesh != null)
                    {
                        Destroy(mesh);
                    }
                }
            }

            _types = null;
            _typeToMeshAndMaterial = null;
            UnsubscribeAndDestroy();
        }
        private void UnsubscribeAndDestroy()
        {
            Unsubscribe();

            SpriteGizmo[] objs = Resources.FindObjectsOfTypeAll<SpriteGizmo>();
            for (int j = 0; j < objs.Length; ++j)
            {
                SpriteGizmo obj = objs[j];
                if (!obj.gameObject.IsPrefab())
                {
                    DestroyGizmo(obj.gameObject);
                }
            }
        }
        private void SubscribeAndCreate()
        {
            IEnumerable<ExposeToEditor> objects = _editor.Object.Get(false);

            for (int i = 0; i < _types.Length; ++i)
            {
                IEnumerable<ExposeToEditor> objectsOfType = objects;
                foreach (ExposeToEditor obj in objectsOfType)
                {
                    Component component = obj.GetComponent(_types[i]);
                    if (component != null)
                    {
                        GreateGizmo(obj.gameObject, component, _types[i]);
                    }
                }
            }

            _meshesCache.Refresh();
            Subscribe();
        }

        private void Subscribe()
        {
            _editor.Object.Awaked += OnAwaked;
            _editor.Object.Destroyed += OnDestroyed;
            _editor.Object.MarkAsDestroyedChanged += OnMarkAsDestroyedChanged;
        }

        private void Unsubscribe()
        {
            if(_editor != null && _editor.Object != null)
            {
                _editor.Object.Awaked -= OnAwaked;
                _editor.Object.Destroyed -= OnDestroyed;
                _editor.Object.MarkAsDestroyedChanged -= OnMarkAsDestroyedChanged;
            }
        }
        
        private void OnAwaked(ExposeToEditor obj)
        {
            bool refresh = false;
            for (int i = 0; i < _types.Length; ++i)
            {
                Component component = obj.GetComponent(_types[i]);
                if (component != null)
                {
                    GreateGizmo(obj.gameObject, component, _types[i]);
                    refresh = true;
                }
            }

            if(refresh)
            {
                _meshesCache.Refresh();
            }
        }
        
        private void OnDestroyed(ExposeToEditor obj)
        {
            bool refresh = false;
            for (int i = 0; i < _types.Length; ++i)
            {
                Component component = obj.GetComponent(_types[i]);
                if (component != null)
                {
                    DestroyGizmo(obj.gameObject);
                    refresh = true;
                }
            }

            if(refresh)
            {
                _meshesCache.Refresh();
            }
        }

        private void OnMarkAsDestroyedChanged(ExposeToEditor obj)
        {
            bool refresh = false;
            if (obj.MarkAsDestroyed)
            {
                for (int i = 0; i < _types.Length; ++i)
                {
                    Component component = obj.GetComponent(_types[i]);
                    if (component != null)
                    {
                        DestroyGizmo(obj.gameObject);
                        refresh = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < _types.Length; ++i)
                {
                    Component component = obj.GetComponent(_types[i]);
                    if (component != null)
                    {
                        GreateGizmo(obj.gameObject, component, _types[i]);
                        refresh = true;
                    }
                }
            }

            if (refresh)
            {
                _meshesCache.Refresh();
            }
        }
    }
}