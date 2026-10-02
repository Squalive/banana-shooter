using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodingDaniel.MapEditor.Graphics;
using CodingDaniel.MapEditor.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
namespace CodingDaniel.MapEditor.MEEditor
{
    public delegate void ObjectEvent(ExposeToEditor obj);
    public delegate void ObjectEvent<T>(ExposeToEditor obj, T arg);
    public delegate void ObjectEvent<T, T2>(ExposeToEditor obj, T arg, T2 arg2);
    public delegate void ObjectParentChangedEvent(ExposeToEditor obj, ExposeToEditor oldValue, ExposeToEditor newValue);

    public interface IMEObjects
    {
        event ObjectEvent Awaked;
        event ObjectEvent Started;
        event ObjectEvent Enabled;
        event ObjectEvent Disabled;
        event ObjectEvent Destroying;
        event ObjectEvent Destroyed;
        event ObjectEvent MarkAsDestroyedChanging;
        event ObjectEvent MarkAsDestroyedChanged;
        event ObjectEvent TransformChanged;
        IEnumerable<ExposeToEditor> Get(bool rootsOnly, bool useCache = true);
    }
    public class MEObjects : MonoBehaviour,IMEObjects
    {
        public event ObjectEvent Awaked;
        public event ObjectEvent Started;
        public event ObjectEvent Enabled;
        public event ObjectEvent Disabled;
        public event ObjectEvent Destroying;
        public event ObjectEvent Destroyed;
        public event ObjectEvent MarkAsDestroyedChanging;
        public event ObjectEvent MarkAsDestroyedChanged;
        public event ObjectEvent TransformChanged;
        
        private IME _editor;
        private ExposeToEditor[] _enabledObjects;
        private UnityObject[] _selectedObjects;
        
        private HashSet<ExposeToEditor> _objects;
        
        
        public IEnumerable<ExposeToEditor> Get(bool rootsOnly, bool useCache)
        {
            if(rootsOnly)
            {
                if (!useCache)
                {
                    List<ExposeToEditor> objects = FindAll();
                    for (int i = 0; i < objects.Count; ++i)
                    {
                        objects[i].Init();
                    }
                    _objects = new HashSet<ExposeToEditor>(objects);
                }
                return _objects.Where(o => o.GetParent(true) == null);
            }


            return _objects;
        }
        
        private static List<ExposeToEditor> FindAll()
        {
            if (SceneManager.GetActiveScene().isLoaded)
            {
                return FindAllUsingSceneManagement();
            }
            List<ExposeToEditor> result = new List<ExposeToEditor>();
            ExposeToEditor[] objects = Resources.FindObjectsOfTypeAll<ExposeToEditor>();
            for (int i = 0; i < objects.Length; ++i)
            {
                ExposeToEditor obj = objects[i];
                if (obj == null)
                {
                    continue;
                }

                if(!HasValidState(obj))
                {
                    continue;
                }

                if (!obj.gameObject.IsPrefab())
                {
                    result.Add(obj);
                }
            }

            return result;
        }
        private static List<ExposeToEditor> FindAllUsingSceneManagement()
        {
            List<ExposeToEditor> result = new List<ExposeToEditor>();
            GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < rootGameObjects.Length; ++i)
            {
                ExposeToEditor[] exposedObjects = rootGameObjects[i].GetComponentsInChildren<ExposeToEditor>(true);
                for (int j = 0; j < exposedObjects.Length; ++j)
                {
                    ExposeToEditor obj = exposedObjects[j];
                    if (HasValidState(obj))
                    {
                        result.Add(obj);
                    }
                }
            }
            return result;
        }
        private static bool HasValidState(ExposeToEditor exposeToEditor)
        {
            return exposeToEditor != null &&
                   !exposeToEditor.MarkAsDestroyed &&
                   (exposeToEditor.hideFlags & HideFlags.HideInHierarchy) == 0 && 
                   (exposeToEditor.IsAwaked || !exposeToEditor.ActiveInHierarchy);
        }

        private void Awake()
        {
            _editor = MEBase.Instance;
            
            List<ExposeToEditor> objects = FindAll();
            for(int i = 0; i < objects.Count; ++i)
            {
                objects[i].Init();
            }
            _objects = new HashSet<ExposeToEditor>(objects);
            
            foreach (ExposeToEditor obj in _objects)
            {
                TryToAddColliders(obj);
                obj.SendMessage("OnRuntimeEditorOpened", SendMessageOptions.DontRequireReceiver);
            }
            
            ExposeToEditor.Awaked += OnAwaked;
            ExposeToEditor.Enabled += OnEnabled;
            ExposeToEditor.Started += OnStarted;
            ExposeToEditor.Disabled += OnDisabled;
            ExposeToEditor.Destroying += OnDestroying;
            ExposeToEditor.Destroyed += OnDestroyed;
            ExposeToEditor.MarkAsDestroyedChanging += OnMarkAsDestroyedChanging;
            ExposeToEditor.MarkAsDestroyedChanged += OnMarkAsDestroyedChanged;
            ExposeToEditor.TransformChanged += OnTransformChanged;

        }
        
        private void OnDestroy()
        {

            ExposeToEditor.Awaked -= OnAwaked;
            ExposeToEditor.Enabled -= OnEnabled;
            ExposeToEditor.Started -= OnStarted;
            ExposeToEditor.Disabled -= OnDisabled;
            ExposeToEditor.Destroying -= OnDestroying;
            ExposeToEditor.Destroyed -= OnDestroyed;
            ExposeToEditor.MarkAsDestroyedChanging -= OnMarkAsDestroyedChanging;
            ExposeToEditor.MarkAsDestroyedChanged -= OnMarkAsDestroyedChanged;

            ExposeToEditor.TransformChanged -= OnTransformChanged;

        }
        private void TryToAddColliders(ExposeToEditor obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj.Colliders == null || obj.Colliders.Length == 0)
            {
                List<Collider> colliders = new List<Collider>();
                Rigidbody rigidBody = obj.boundsObject.GetComponentInParent<Rigidbody>();

                bool isRigidBody = rigidBody != null;
                if (obj.EffectiveBoundsType == BoundsType.Any)
                {
                    if (obj.MeshFilter != null)
                    {
                        if (!isRigidBody)
                        {
                            MeshCollider collider = obj.boundsObject.GetComponent<MeshCollider>();
                            if (collider == null)
                                collider=obj.boundsObject.AddComponent<MeshCollider>();
                            collider.isTrigger = false;
                            collider.convex = false;
                            collider.sharedMesh = obj.MeshFilter.sharedMesh;
                            colliders.Add(collider);
                        }
                    }
                    else if (obj.SkinnedMeshRenderer != null)
                    {
                        if ( !isRigidBody)
                        {
                            MeshCollider collider = obj.boundsObject.AddComponent<MeshCollider>();
                            collider.convex = false;
                            collider.sharedMesh = obj.SkinnedMeshRenderer.sharedMesh;
                            colliders.Add(collider);
                        }
                    }
                    else if (obj.SpriteRenderer != null && obj.SpriteRenderer.sprite != null)
                    {
                        if (obj.AddColliders && !isRigidBody)
                        {
                            BoxCollider collider = obj.boundsObject.AddComponent<BoxCollider>();
                            collider.size = obj.SpriteRenderer.sprite.bounds.size;
                            colliders.Add(collider);
                        }
                    }
                }
                else if (obj.EffectiveBoundsType == BoundsType.Mesh)
                {
                    if (obj.MeshFilter != null)
                    {
                        if (!isRigidBody)
                        {
                            MeshCollider collider = obj.boundsObject.GetComponent<MeshCollider>();
                            if (collider == null)
                                collider=obj.boundsObject.AddComponent<MeshCollider>();
                            collider.convex = false;
                            collider.sharedMesh = obj.MeshFilter.sharedMesh;
                            colliders.Add(collider);
                        }
                    }
                }
                else if (obj.EffectiveBoundsType == BoundsType.SkinnedMesh)
                {
                    if (obj.SkinnedMeshRenderer != null)
                    {
                        if (!isRigidBody)
                        {
                            MeshCollider collider = obj.boundsObject.AddComponent<MeshCollider>();
                            collider.isTrigger = false;
                            collider.convex = false;
                            collider.sharedMesh = obj.SkinnedMeshRenderer.sharedMesh;
                            colliders.Add(collider);
                        }
                    }
                }
                else if (obj.EffectiveBoundsType == BoundsType.Sprite)
                {
                    if (obj.SpriteRenderer != null)
                    {
                        if (obj.AddColliders && !isRigidBody)
                        {
                            BoxCollider collider = obj.boundsObject.AddComponent<BoxCollider>();
                            collider.size = obj.SpriteRenderer.sprite.bounds.size;
                            colliders.Add(collider);
                        }
                    }
                }
                else if (obj.EffectiveBoundsType == BoundsType.Custom)
                {
                    if (!isRigidBody)
                    {
                        Mesh box = GraphicsUtility.CreateCube(Color.black, obj.customBounds.center, 1,
                            obj.customBounds.extents.x * 2, obj.customBounds.extents.y * 2,
                            obj.customBounds.extents.z * 2);

                        MeshCollider collider = obj.boundsObject.AddComponent<MeshCollider>();
                        collider.convex = false;

                        collider.sharedMesh = box;
                        colliders.Add(collider);
                    }
                }

                obj.Colliders = colliders.ToArray();
            }
        }
        
        private void TryToDestroyColliders(ExposeToEditor obj)
        {
            if (obj != null && obj.Colliders != null)
            {
                for (int i = 0; i < obj.Colliders.Length; ++i)
                {
                    Collider collider = obj.Colliders[i];
                    if (collider != null)
                    {
                        Destroy(collider);
                    }
                }
                obj.Colliders = null;
            }
        }
        
        private void OnAwaked(ExposeToEditor obj)
        {
            
            obj.SendMessage("EditorAwake", SendMessageOptions.DontRequireReceiver);

            if (!_objects.Contains(obj))
            {
                _objects.Add(obj);
            }

            if (Awaked != null)
            {
                Awaked(obj);
            }
        }

        private void OnDestroying(ExposeToEditor obj)
        {
            if (Destroying != null)
            {
                Destroying(obj);
            }
        }

        private void OnDestroyed(ExposeToEditor obj)
        {
            obj.SendMessage("OnEditorDestroy", SendMessageOptions.DontRequireReceiver);
            if (_objects.Contains(obj))
            {
                _objects.Remove(obj);
                TryToDestroyColliders(obj);
            }

            if(_editor.Selection.IsSelected(obj.gameObject))
            {
                _editor.Selection.Objects = _editor.Selection.Objects.Where(o => o != obj.gameObject).ToArray();
            }

            if (Destroyed != null)
            {
                Destroyed(obj);
            }
        }

        private void OnMarkAsDestroyedChanging(ExposeToEditor obj)
        {
            if (obj.MarkAsDestroyed)
            {
                _objects.Remove(obj);
                SendMessageTo(obj.gameObject, "OnMarkAsDestroyed");
            }
            else
            {
                _objects.Add(obj);
                SendMessageTo(obj.gameObject, "OnMarkAsRestored");
            }

            if(MarkAsDestroyedChanging != null)
            {
                MarkAsDestroyedChanging(obj);
            }
        }
        
        void SendMessageTo(GameObject gameobject, string methodName, params object[] parameters)
        {
            MonoBehaviour[] components = gameobject.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour m in components)
            {
                InvokeIfExists(m, methodName, parameters);
            }
        }
        
        private void InvokeIfExists(object objectToCheck, string methodName, params object[] parameters)
        {
            Type type = objectToCheck.GetType();
            
            MethodInfo methodInfo = type.GetMethod(methodName);
            if (methodInfo != null)
            {
                methodInfo.Invoke(objectToCheck, parameters);
            }
        }
        private void OnMarkAsDestroyedChanged(ExposeToEditor obj)
        {
            if (MarkAsDestroyedChanged != null)
            {
                MarkAsDestroyedChanged(obj);
            }
        }

        private void OnEnabled(ExposeToEditor obj)
        {
            if (Enabled != null)
            {
                Enabled(obj);
            }
        }
        
        private void OnStarted(ExposeToEditor obj)
        {
            obj.SendMessage("EditorStart", SendMessageOptions.DontRequireReceiver);

            TryToDestroyColliders(obj);
            TryToAddColliders(obj);

            if (Started != null)
            {
                Started(obj);
            }
        }

        private void OnDisabled(ExposeToEditor obj)
        {
            if (Disabled != null)
            {
                Disabled(obj);
            }
        }

        private void OnTransformChanged(ExposeToEditor obj)
        {
            if (TransformChanged != null)
            {
                TransformChanged(obj);
            }
        }
    }
    
    
}
