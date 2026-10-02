
using System;
using System.Collections.Generic;
using CodingDaniel.FileBrowser;
using CodingDaniel.MapEditor.Extension.ProBuilderIntegration;
using CodingDaniel.MapEditor.MEEditor;
using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.UI.AddObject;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class MaterialPaletteUI : MonoBehaviour,IPointerDownHandler
    {
        public static  MaterialPaletteUI Instance { private set; get; }
        [SerializeField] private RectTransform ui;
        [SerializeField] private ToggleGroup toggleGroup;

        [SerializeField] private MaterialPaletteUIITem prefab;
        [SerializeField] private Transform content;

        [SerializeField] private List<Material> materials;
        public List<Tuple<string,string,string,Material>> externalMats;
        private Vector2 _desiredPos;

        private float offset;
        
        public Material SelectedMaterial { private set; get; }
        
        public bool DisplayingUI { private set; get; }

        [SerializeField] private Button applyBtn,tryToAddMatBtn,addMatBtn;

        [SerializeField] public Transform option;

        private IME me;
        private Texture2D _loadTexture;
        private void Awake()
        {
            Instance = this;
            
            _loadTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            
            applyBtn.onClick.AddListener(Apply);
            tryToAddMatBtn.onClick.AddListener(TryToAddMaterial);
            addMatBtn.onClick.AddListener(AddMaterial);
            
            baseTextureBtn.onClick.AddListener(delegate { SelectTexture(baseTextureImg,baseTexture,0); });
            heightTextureBtn.onClick.AddListener(delegate { SelectTexture(heightTextureImg,heightTexture,1); });
            normalTextureBtn.onClick.AddListener(delegate { SelectTexture(normalTextureImg,normalTexture,2); });
            
            smoothnessSlider.onValueChanged.AddListener(SetSmoothness);
            metallicSlider.onValueChanged.AddListener(SetMetallic);
            normalScaleSlider.onValueChanged.AddListener(SetNormalScale);
            heightSlider.onValueChanged.AddListener(SetHeight);
            
            selectBtn.onClick.AddListener(Select);
            editBtn.onClick.AddListener(Edit);
            removeBtn.onClick.AddListener(Remove);

            MapSaver.Instance.OnMapLoaded += InitExternalMat;
        }

        private void OnDestroy()
        {
            applyBtn.onClick.RemoveListener(Apply);
            tryToAddMatBtn.onClick.RemoveListener(TryToAddMaterial);
            addMatBtn.onClick.RemoveListener(AddMaterial);
            
            baseTextureBtn.onClick.RemoveAllListeners();
            heightTextureBtn.onClick.RemoveAllListeners();
            normalTextureBtn.onClick.RemoveAllListeners();

            smoothnessSlider.onValueChanged.RemoveListener(SetSmoothness);
            metallicSlider.onValueChanged.RemoveListener(SetMetallic);
            normalScaleSlider.onValueChanged.RemoveListener(SetNormalScale);
            heightSlider.onValueChanged.RemoveListener(SetHeight);
            
            selectBtn.onClick.RemoveListener(Select);
            editBtn.onClick.RemoveListener(Edit);
            removeBtn.onClick.RemoveListener(Remove);
            
            
            MapSaver.Instance.OnMapLoaded -= InitExternalMat;
        }

        private void Start()
        {
            me=MEBase.Instance;

            materials = MapSaver.Instance.materials;
            
            foreach (var material in materials)
            {
                MaterialPaletteUIITem item = Instantiate(prefab, content);
                
                item.Init(material.name,material.mainTexture,material,toggleGroup,this);
            }
            
            InvokeRepeating(nameof(Refresh),0,5f);
        }

        void InitExternalMat()
        {
            externalMats = MapSaver.Instance.externalMaterials;
            foreach (var tuple in externalMats)
            {
                var material = tuple.Item4;
                MaterialPaletteUIITem item = Instantiate(prefab, content);
                
                item.Init(material.name,material.mainTexture,material,toggleGroup,this,true);
            }
        }
        void Refresh()
        {
            offset = Screen.height * 1 / 10f;

        }
        void Apply()
        {
            ProBuilderTool.Instance.ApplyMaterial(SelectedMaterial);
        }
        public bool enableUI = true;
        private void Update()
        {
            DisplayingUI = !MEBase.Instance.Tools.IsViewing && (Input.mousePosition.y < offset || option.gameObject.activeSelf);
            _desiredPos = DisplayingUI ? new Vector2(0,0) : new Vector2(0,-100);
            
            
            ui.anchoredPosition = Vector2.Lerp(ui.anchoredPosition,_desiredPos,Time.deltaTime*15f);

            if (DisplayingUI)
            {
                bool flag = me.Selection.ActiveGameObject;

                if (flag)
                {
                    flag = me.Selection.ActiveGameObject.GetComponent<Renderer>();
                    if (!flag)
                    {
                        flag = me.Selection.ActiveGameObject == ProBuilderTool.Instance._pivot.gameObject;
                    }
                }
                

                applyBtn.interactable = flag;
            }
        }

        public void SelectMaterial(MaterialPaletteUIITem item=null)
        {
            if (item == null)
            {
                SelectedMaterial = null;
                // nameText.SetText("null");
                // textureImage.texture = null;
            }
            else
            {
                SelectedMaterial = item.material;
                // nameText.SetText(item.material.name);
                // textureImage.texture = item.material.mainTexture;
            }
        }

        [SerializeField] private GameObject addMatPanel;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button baseTextureBtn, heightTextureBtn, normalTextureBtn;
        [SerializeField] private RawImage baseTextureImg, heightTextureImg, normalTextureImg;
        [Serializable]
        public class TextureWithPath
        {
            public string path;
            public Texture2D texture;
        }
        private TextureWithPath baseTexture = new (), heightTexture = new(), normalTexture = new();

        private bool setBase=false, setHeight=false, setNormal=false;

        [SerializeField] private Slider smoothnessSlider;
        [SerializeField] private TextMeshProUGUI smoothnessText;

        [SerializeField] private Slider metallicSlider;
        [SerializeField] private TextMeshProUGUI metallicText;
        
        [SerializeField] private Slider normalScaleSlider;
        [SerializeField] private TextMeshProUGUI normalScaleText;
        
        [SerializeField] private Slider heightSlider;
        [SerializeField] private TextMeshProUGUI heightText;
        void SetSmoothness(float value)
        {
            smoothnessText.SetText(value.ToString("F2"));
        }
        void SetMetallic(float value)
        {
            metallicText.SetText(value.ToString("F2"));
        }
        void SetNormalScale(float value)
        {
            normalScaleText.SetText(value.ToString("F2"));
        }
        void SetHeight(float value)
        {
            heightText.SetText(value.ToString("F2"));
        }
        void TryToAddMaterial()
        {
            addMatPanel.SetActive(true);

            setBase = setHeight = setNormal = false;
            
            baseTextureImg.gameObject.SetActive(false);
            heightTextureImg.gameObject.SetActive(false);
            normalTextureImg.gameObject.SetActive(false);

            smoothnessSlider.value = 0.5f;
            metallicSlider.value = 0;
            heightSlider.value = 0.005f;
            normalScaleSlider.value = 1;
            
            nameInput.SetTextWithoutNotify("");

            baseTextureImg.texture = null;
            heightTextureImg.texture = null;
            normalTextureImg.texture = null;
            baseTexture.texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            heightTexture.texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            normalTexture.texture = new Texture2D(512, 512, TextureFormat.RGB24, false);
        }

        void SelectTexture(RawImage img, TextureWithPath texture,int key)
        {
            string path = FileIOUtil.OpenFileDialog(FileType.Texture);
            if(!string.IsNullOrEmpty(path))
                LoadImage(path,img,texture,key);
        }
        async void LoadImage(string path,RawImage img, TextureWithPath texture,int key)
        {
            byte[] data = await SaveSystem.ReadByteFromFileAsync(path);
            
            texture.texture.LoadImage(data);
            texture.path = path;

            img.texture = texture.texture;
            img.gameObject.SetActive(true);

            switch (key)
            {
                case 0:
                    setBase = true;
                    break;
                case 1:
                    setHeight = true;
                    break;
                case 2:
                    setNormal = true;
                    break;
            }
        }
        void AddMaterial()
        {
            Material material = new Material(MapSaver.Instance.dummyMat);

            
            material.EnableKeyword("_METALLICGLOSSMAP");


            material.SetFloat(MapSaver.Smoothness, smoothnessSlider.value);
            material.SetFloat(MapSaver.Metallic, metallicSlider.value);

            material.name = nameInput.text;
            
            material.mainTexture = setBase ? baseTexture.texture : null;

            if (setHeight)
            {
                material.SetTexture(MapSaver.ParallaxMap, heightTexture.texture);
                material.EnableKeyword ("_PARALLAXMAP");
            }
            else
            {
                material.SetTexture(MapSaver.ParallaxMap, null);
            }

            if (setNormal)
            {
                material.SetTexture(MapSaver.BumpMap, normalTexture.texture);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.SetTexture(MapSaver.BumpMap, null);
                
            }
            
            material.SetFloat(MapSaver.BumpScale,normalScaleSlider.value);
            material.SetFloat(MapSaver.Parallax,heightSlider.value);
            
            MapSaver.Instance.externalMaterials.Add(new Tuple<string, string, string, Material>(baseTexture.path,heightTexture.path,normalTexture.path,material));
            
            MaterialPaletteUIITem item = Instantiate(prefab, content);
                
            item.Init(material.name,material.mainTexture,material,toggleGroup,this,true);
        }

        private MaterialPaletteUIITem _selectedItem;
        public void SetSelectMaterial(MaterialPaletteUIITem item)
        {
            _selectedItem = item;
            
            option.gameObject.SetActive(true);
            option.position = Input.mousePosition;
        }

        void Select()
        {
            option.gameObject.SetActive(false);

            if (_selectedItem != null)
            {
                _selectedItem._toggle.isOn = true;
            }
        }

        void Edit()
        {
            option.gameObject.SetActive(false);
            
            MaterialEditingPanel.Instance.OpenPanel(_selectedItem.material);
        }

        void Remove()
        {
            option.gameObject.SetActive(false);

            for (int i = 0; i < externalMats.Count; i++)
            {
                var mat = externalMats[i];

                if (mat.Item4 == _selectedItem.material)
                {
                    externalMats.Remove(mat);
                    break;
                }
            }

            for (int i = 0; i < MapSaver.Instance.externalMaterials.Count; i++)
            {
                var mat = MapSaver.Instance.externalMaterials[i];

                if (mat.Item4 == _selectedItem.material)
                {
                    MapSaver.Instance.externalMaterials.Remove(mat);
                    break;
                }
            }

            Destroy(_selectedItem.gameObject);
        }

        [SerializeField] private Button selectBtn, editBtn, removeBtn;
        public void OnPointerDown(PointerEventData eventData)
        {
            option.gameObject.SetActive(false);
            _selectedItem = null;
            
            AddExternalObjectMenu.Instance.CloseEditBar();
        }
    }
}
