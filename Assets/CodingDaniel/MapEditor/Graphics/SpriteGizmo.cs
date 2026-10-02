using System;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;

using UnityEngine.Serialization;

namespace CodingDaniel.MapEditor.Graphics
{
    public class SpriteGizmo : MonoBehaviour
    {
        public Mesh Mesh;

        [SerializeField, HideInInspector]
        private SphereCollider _collider;
        private SphereCollider _destroyedCollider;

        public event Action<SpriteGizmo> ComponentDestroyed;
        public Component Component;
        
        [SerializeField]
        [FormerlySerializedAs("m_scale")]
        private float _scale = 1.0f;
        public float Scale
        {
            get { return _scale; }
            set
            {
                if(_scale != value)
                {
                    _scale = value;
                    UpdateCollider();
                }
            }
        }
        
        private void OnEnable()
        {
            _collider = GetComponent<SphereCollider>();

            ExposeToEditor exposeToEditor = GetComponent<ExposeToEditor>();
            if (exposeToEditor == null || exposeToEditor.AddColliders)
            {
                if (_collider == null || _collider == _destroyedCollider)
                {
                    _collider = gameObject.AddComponent<SphereCollider>();
                }
                if (_collider != null)
                {
                    if (_collider.hideFlags == HideFlags.None)
                    {
                        _collider.hideFlags = HideFlags.HideInInspector;
                    }

                    UpdateCollider();
                }
            }
        }

        private void OnDisable()
        {
            if(_collider != null)
            {
                Destroy(_collider);
                _destroyedCollider = _collider;
                _collider = null;
            }
        }

        private void UpdateCollider()
        {
            if(_collider != null)
            {
                _collider.radius = 0.25f * _scale;
            }
        }

        private void Update()
        {
            if(Component == null)
            {
                if(ComponentDestroyed != null)
                {
                    ComponentDestroyed(this);
                }
                
                enabled = false;
            }
        }
    }
}