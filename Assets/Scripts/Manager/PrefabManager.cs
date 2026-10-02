using System;
using System.Collections.Generic;
using Menu;
using UnityEngine;
using UnityEngine.Rendering;

namespace Manager
{
    public class PrefabManager : MonoBehaviour
    {
        public static PrefabManager Instance;

        private void Awake()
        {
            Instance = this;

            foreach (var obj in objets)
            {
                if (obj != null)
                {
                    link.Add(obj.name,obj);
                }
            }
        }

        public List<GameObject> objets = new List<GameObject>();

        private Dictionary<string, GameObject> link = new Dictionary<string, GameObject>();

        public ServerItem serverPrefab;
        public GameObject playerListPrefab;
        public GameObject failedWindow;

        public LayerMask whatIsHittable;
        public AudioClip pewClip;
        public AudioClip[] walksSound;
        public AudioClip[] broadSwordHit;

        public AudioClip buttonPress;
        public Texture2D jackTexture;

        public Texture2D[] texture;
        public Texture2D[] throwObjTexture;

        public AudioClip[] hitWall;
        public GameObject impactAudio;

        public VolumeProfile waterVolume;

        public Texture2D errorTexture;

        public GameObject[] throwables = Array.Empty<GameObject>();
        
        public GameObject GetPrefab(string name)
        {
            if (link.TryGetValue(name,out var value))
                return value;
            
            // foreach (var objet in objets)
            // {
            //     if (objet.name == name)
            //     {
            //         return objet;
            //     }
            // }

            return null;
        }
    
        public Texture2D GetTexture2D(string name)
        {
            foreach (var objet in texture)
            {
                if (objet.name == name)
                {
                    return objet;
                }
            }

            return null;
        }
    }
}
