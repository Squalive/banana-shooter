

using CodingDaniel.ColorPanel.Script;
using CodingDaniel.MapEditor.Graphics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class MapSettingMenu : MonoBehaviour
    {
        public static MapSettingMenu Instance { private set; get; }

        private void Awake()
        {
            Instance = this;

            RefreshSkybox();
            RefreshDirectionalLighting();
        }

        private void OnEnable()
        {
            enableDirectionalLighting.onValueChanged.AddListener(SetDirectionalLighting);

            boundXInput.onEndEdit.AddListener(delegate (string arg0) { OnBoundInput(arg0, 0); });
            boundYInput.onEndEdit.AddListener(delegate (string arg0) { OnBoundInput(arg0, 1); });
            boundZInput.onEndEdit.AddListener(delegate (string arg0) { OnBoundInput(arg0, 2); });

            boundCenterXInput.onEndEdit.AddListener(delegate (string arg0) { OnBoundCenterInput(arg0, 0); });
            boundCenterYInput.onEndEdit.AddListener(delegate (string arg0) { OnBoundCenterInput(arg0, 1); });
            boundCenterZInput.onEndEdit.AddListener(delegate (string arg0) { OnBoundCenterInput(arg0, 2); });
        }

        private void OnDisable()
        {
            enableDirectionalLighting.onValueChanged.RemoveListener(SetDirectionalLighting);

            boundXInput.onEndEdit.RemoveAllListeners();
            boundYInput.onEndEdit.RemoveAllListeners();
            boundZInput.onEndEdit.RemoveAllListeners();

            boundCenterXInput.onEndEdit.RemoveAllListeners();
            boundCenterYInput.onEndEdit.RemoveAllListeners();
            boundCenterZInput.onEndEdit.RemoveAllListeners();
        }

        [SerializeField] public RawImage skyboxImg;
        [SerializeField] public TextMeshProUGUI skyboxText;


        public void TryOpenSkyboxSource()
        {
            SourceUI.Instance.onApply += SetSkybox;

            SourceUI.Instance.DisplaySkybox();
        }

        void SetSkybox(SourceItemUI item)
        {
            SourceUI.Instance.onApply -= SetSkybox;

            Material skybox = (Material)item.source;


            RenderSettings.skybox = skybox;

            RefreshSkybox();
        }

        void RefreshSkybox()
        {
            skyboxColorImg.color = RenderSettings.ambientLight;
            Material skybox = RenderSettings.skybox;

            if (skybox == null)
            {

                skyboxImg.texture = null;
                skyboxText.SetText("None");
            }
            else
            {

                skyboxImg.texture = SourceUI.GetSkyboxTexture(skybox);
                skyboxText.SetText(skybox.name);
            }
        }

        void OnBoundInput(string str, int index)
        {
            if (float.TryParse(str, out var f))
            {
                switch (index)
                {
                    case 0:
                        _boundX = f;
                        break;
                    case 1:
                        _boundY = f;
                        break;
                    case 2:
                        _boundZ = f;
                        break;
                }

                MapBoundVisual.Instance.Bounds = new Bounds(Vector3.zero, new Vector3(_boundX, _boundY, _boundZ));
                MapBoundVisual.Instance.MECamera.RefreshCommandBuffer();
            }
        }

        void OnBoundCenterInput(string str, int index)
        {
            if (float.TryParse(str, out var f))
            {
                switch (index)
                {
                    case 0:
                        _boundCenterX = f;
                        break;
                    case 1:
                        _boundCenterY = f;
                        break;
                    case 2:
                        _boundCenterZ = f;
                        break;
                }

                MapBoundVisual.Instance.Bounds = new Bounds(new Vector3(_boundCenterX, _boundCenterY, _boundCenterZ), new Vector3(_boundX, _boundY, _boundZ));
                MapBoundVisual.Instance.MECamera.RefreshCommandBuffer();
            }
        }

        public void SetBound(Vector3 b, Vector3 center)
        {
            _boundX = b.x;
            _boundY = b.y;
            _boundZ = b.z;

            _boundCenterX = center.x;
            _boundCenterY = center.y;
            _boundCenterZ = center.z;

            boundXInput.SetTextWithoutNotify(_boundX.ToString("F1"));
            boundYInput.SetTextWithoutNotify(_boundY.ToString("F1"));
            boundZInput.SetTextWithoutNotify(_boundZ.ToString("F1"));

            boundCenterXInput.SetTextWithoutNotify(_boundCenterX.ToString("F1"));
            boundCenterYInput.SetTextWithoutNotify(_boundCenterY.ToString("F1"));
            boundCenterZInput.SetTextWithoutNotify(_boundCenterZ.ToString("F1"));

            MapBoundVisual.Instance.Bounds = new Bounds(new Vector3(_boundCenterX, _boundCenterY, _boundCenterZ), new Vector3(_boundX, _boundY, _boundZ));
            MapBoundVisual.Instance.MECamera.RefreshCommandBuffer();
        }

        [SerializeField] public Toggle enableDirectionalLighting;
        [SerializeField] private Light directionalLighting;

        [SerializeField] Color disabledLightingLight = Color.black;
        [SerializeField] Color enabledLightingLight = Color.white;


        [SerializeField] public RawImage skyboxColorImg;

        [SerializeField] private TMP_InputField boundXInput, boundYInput, boundZInput;
        [SerializeField] private TMP_InputField boundCenterXInput, boundCenterYInput, boundCenterZInput;
        private float _boundX = 20f, _boundY = 10f, _boundZ = 20f;
        private float _boundCenterX = 0f, _boundCenterY = 0f, _boundCenterZ = 0f;
        void SetDirectionalLighting(bool value)
        {
            directionalLighting.enabled = value;
            RenderSettings.ambientLight = value ? enabledLightingLight : disabledLightingLight;
            skyboxColorImg.color = RenderSettings.ambientLight;
        }

        void RefreshDirectionalLighting()
        {
            enableDirectionalLighting.SetIsOnWithoutNotify(directionalLighting.enabled);
        }


        public void TryEditSkyboxColor()
        {
            ColorPickerControl.Instance.OnColorChanged += (color =>
            {
                skyboxColorImg.color = color;

                RenderSettings.ambientLight = color;
            });
        }
        public Light GetDirectionalLight()
        {
            return directionalLighting;
        }
        public bool GetDirectionalLightEnable()
        {
            return directionalLighting.enabled;
        }
    }
}
