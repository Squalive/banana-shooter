using System;
using System.Collections.Generic;
using CodingDaniel.MapEditor.MEEditor.MESave;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodingDaniel.MapEditor.UI
{
    public class SourceUI : MonoBehaviour
    {
        public static SourceUI Instance { private set; get; }

        [SerializeField] private GameObject ui;

        [SerializeField] private Transform content;
        [SerializeField] private SourceItemUI itemPrefab;
        
        [SerializeField] private List<Material> skyboxes;

        private void Awake()
        {
            Instance = this;
        }

        private List<SourceItemUI> _items = new List<SourceItemUI>();
        public static readonly int FrontTex = Shader.PropertyToID("_FrontTex");

        public void DisplaySkybox()
        {
            ClearItems();
            skyboxes = MapSaver.Instance.skyboxes;
            foreach (var skybox in skyboxes)
            {
                SourceItemUI item = Instantiate(itemPrefab, content);
                
                Texture texture = GetSkyboxTexture(skybox);
                item.Init(skybox.name,texture,skybox);
                
                _items.Add(item);
            }
            
            
        }
        public static Texture GetSkyboxTexture(Material skybox)
        {
            Texture texture;
            if (skybox.shader.name == "Skybox/Cubemap")
            {
                // texture = skybox.GetTexture("_Tex");
                texture = null;
            }
            else if (skybox.shader.name == "Skybox/Procedural")
            {
                texture = null;
            }
            else if (skybox.shader.name == "Skybox/Panoramic")
            {
                texture = skybox.GetTexture("_MainTex");
            }
            else
            {
                texture = skybox.GetTexture(FrontTex);
            }

            return texture;
        }
        void ClearItems()
        {
            foreach (var item in _items)
            {
                Destroy(item.gameObject);
            }
            
            _items.Clear();
        }

        public Action<SourceItemUI> onApply;
        public void Apply(SourceItemUI selectItem)
        {
            onApply?.Invoke(selectItem);
            ui.SetActive(false);
        }
    }
}
