using System;
using UnityEngine;

namespace Cosmetic
{
    public class CosmeticRender : MonoBehaviour
    {
        public static CosmeticRender Instance { get; private set; }

        public Camera itemCam;
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public Light light;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
