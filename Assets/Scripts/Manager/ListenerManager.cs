using System;
using UnityEngine;

namespace Manager
{
    public class ListenerManager : MonoBehaviour
    {
        public static ListenerManager Instance;

        public static Action<Transform> OnCameraChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _transform = transform;
            
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public Transform cameraTransform;

        private Transform _transform;
        
        private void LateUpdate()
        {
            if (cameraTransform != null)
            {
                _transform.position = cameraTransform.position;
                _transform.rotation = cameraTransform.rotation;
            }
        }

        public void SetCamera(Transform t)
        {
            cameraTransform = t;
            OnCameraChanged?.Invoke(t);
        }
    }
}
