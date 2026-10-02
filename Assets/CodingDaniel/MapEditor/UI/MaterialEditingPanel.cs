using System;
using CodingDaniel.FileBrowser;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class MaterialEditingPanel : MonoBehaviour
    {
        public static MaterialEditingPanel Instance { private set; get; }

        private void Awake()
        {
            Instance = this;
        }

        public GameObject panel;
        
        public TextMeshProUGUI nameText;

        public Material selectMaterial;
        
        [SerializeField] private Button baseTextureBtn, heightTextureBtn, normalTextureBtn;
        [SerializeField] private RawImage baseTextureImg, heightTextureImg, normalTextureImg;
        private Texture2D baseTexture, heightTexture, normalTexture;

        [SerializeField] private Slider smoothnessSlider;
        [SerializeField] private TextMeshProUGUI smoothnessText;

        [SerializeField] private Slider metallicSlider;
        [SerializeField] private TextMeshProUGUI metallicText;

        [SerializeField] private Slider normalScaleSlider;
        [SerializeField] private TextMeshProUGUI normalScaleText;
        
        [SerializeField] private Slider heightSlider;
        [SerializeField] private TextMeshProUGUI heightText;
        private void Start()
        {
            baseTextureBtn.onClick.AddListener(delegate { SelectTexture(baseTextureImg,baseTexture,0); });
            heightTextureBtn.onClick.AddListener(delegate { SelectTexture(heightTextureImg,heightTexture,1); });
            normalTextureBtn.onClick.AddListener(delegate { SelectTexture(normalTextureImg,normalTexture,2); });
            
            smoothnessSlider.onValueChanged.AddListener(SetSmoothness);
            metallicSlider.onValueChanged.AddListener(SetMetallic);
            normalScaleSlider.onValueChanged.AddListener(SetNormalScale);
            heightSlider.onValueChanged.AddListener(SetHeight);
        }

        private void OnDestroy()
        {
            baseTextureBtn.onClick.RemoveListener(delegate { SelectTexture(baseTextureImg,baseTexture,0); });
            heightTextureBtn.onClick.RemoveListener(delegate { SelectTexture(heightTextureImg,heightTexture,1); });
            normalTextureBtn.onClick.RemoveListener(delegate { SelectTexture(normalTextureImg,normalTexture,2); });
            
            smoothnessSlider.onValueChanged.RemoveListener(SetSmoothness);
            metallicSlider.onValueChanged.RemoveListener(SetMetallic);
            normalScaleSlider.onValueChanged.RemoveListener(SetNormalScale);
            heightSlider.onValueChanged.RemoveListener(SetHeight);
        }
        void SetSmoothness(float value)
        {
            smoothnessText.SetText(value.ToString("F2"));

            if (selectMaterial)
            {
                selectMaterial.SetFloat(MapSaver.Smoothness,value);
            }
        }
        void SetMetallic(float value)
        {
            metallicText.SetText(value.ToString("F2"));
            
            if (selectMaterial)
            {
                selectMaterial.SetFloat(MapSaver.Metallic,value);
            }
        }
        
        void SetNormalScale(float value)
        {
            normalScaleText.SetText(value.ToString("F2"));
            
            if (selectMaterial)
            {
                selectMaterial.SetFloat(MapSaver.BumpScale,value);
            }
        }
        
        void SetHeight(float value)
        {
            heightText.SetText(value.ToString("F2"));
            
            if (selectMaterial)
            {
                selectMaterial.SetFloat(MapSaver.Parallax,value);
            }
        }
        void SelectTexture(RawImage img, Texture2D texture,int key)
        {
            string path = FileIOUtil.OpenFileDialog(FileType.Texture);
            if(!string.IsNullOrEmpty(path))
                LoadImage(path,img,texture,key);
        }
        async void LoadImage(string path,RawImage img, Texture2D texture,int key)
        {
            byte[] data = await SaveSystem.ReadByteFromFileAsync(path);
            
            texture.LoadImage(data);

            img.texture = texture;
            img.gameObject.SetActive(true);

            if (selectMaterial!=null)
            {
                switch (key)
                {
                    case 0:
                        selectMaterial.mainTexture = texture;
                        break;
                    case 1:
                        selectMaterial.SetTexture(MapSaver.ParallaxMap,texture);
                        break;
                    case 2:
                        selectMaterial.SetTexture(MapSaver.BumpMap,texture);
                        break;
                }
            }
            
        }

        public void OpenPanel(Material material)
        {
            selectMaterial = material;
            
            panel.SetActive(true);
            
            nameText.SetText(material.name);

            float smoothness = material.GetFloat(MapSaver.Smoothness);
            float metallic = material.GetFloat(MapSaver.Metallic);
            smoothnessSlider.SetValueWithoutNotify(smoothness);
            metallicSlider.SetValueWithoutNotify(metallic);
            smoothnessText.SetText(smoothness.ToString("F2"));
            metallicText.SetText(metallic.ToString("F2"));

            baseTexture = (Texture2D)material.mainTexture;

            if (baseTexture != null)
            {
                baseTextureImg.gameObject.SetActive(true);
                baseTextureImg.texture = baseTexture;
            }
            else
            {
                baseTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
                baseTextureImg.gameObject.SetActive(false);
            }

            heightTexture = (Texture2D)material.GetTexture(MapSaver.ParallaxMap);

            if (heightTexture != null)
            {
                heightTextureImg.gameObject.SetActive(true);
                heightTextureImg.texture = heightTexture;
                
                float parallax = material.GetFloat(MapSaver.Parallax);
                heightSlider.SetValueWithoutNotify(parallax);
                heightText.SetText(parallax.ToString("F2"));
            }
            else
            {
                heightTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
                heightTextureImg.gameObject.SetActive(false);
            }
            
            normalTexture = (Texture2D)material.GetTexture(MapSaver.BumpMap);

            if (normalTexture != null)
            {
                normalTextureImg.gameObject.SetActive(true);
                normalTextureImg.texture = normalTexture;

                float scale = material.GetFloat(MapSaver.BumpScale);
                normalScaleSlider.SetValueWithoutNotify(scale);
                normalScaleText.SetText(scale.ToString("F2"));
            }
            else
            {
                normalTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
                normalTextureImg.gameObject.SetActive(false);
            }
        }
    }
}
