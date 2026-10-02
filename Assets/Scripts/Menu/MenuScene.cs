using System;
using Manager;
using UnityEngine;

namespace Menu
{
    public class MenuScene : MonoBehaviour
    {
        public static MenuScene Instance;
        
        [Serializable]
        public class MapScene
        {
            public GameObject obj;
            
            public Color fogColor = Color.black;

            public float fogDensity = 0;
        }

        [SerializeField] private MapScene[] maps;

        [Range(0, 2)] public int index;

        private void Awake()
        {
            Instance = this;

            index = InventoryManager.Instance.cosmeticIndex.menuSceneIndex;
            Refresh();
        }

        private void OnDestroy()
        {
            Instance = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(!Application.isPlaying)
                Refresh();
        }
#endif

        public void Refresh()
        {
            if (index >= maps.Length)
                return;
            
            RenderSettings.fogColor = maps[index].fogColor;
            RenderSettings.fogDensity = maps[index].fogDensity;

            foreach (var mapScene in maps)
            {
                mapScene.obj.SetActive(false);
            }

            maps[index].obj.SetActive(true);
        }
    }
}
