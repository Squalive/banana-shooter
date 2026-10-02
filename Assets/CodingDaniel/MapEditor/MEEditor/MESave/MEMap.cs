using System;
using UnityEngine;

namespace CodingDaniel.MapEditor.MEEditor.MESave
{
    public class MEMap : MonoBehaviour
    {
        public static MEMap Instance;

        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] private Light directionalLight;
        [SerializeField] private Transform groundRoot, lightRoot;


        public Light GetDirectionalLight()
        {
            return directionalLight;
        }

        public Transform GetGroundRoot()
        {
            return groundRoot;
        }
        public Transform GetLightRoot()
        {
            return lightRoot;
        }
    }
}
