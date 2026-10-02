using System;

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodingDaniel.ColorPanel.Script
{
    public class ColorPickerControl : MonoBehaviour
    {
        public static ColorPickerControl Instance { private set; get; }

        public Action<Color> OnColorChanged;

        public float currentHue, currentSat, currentVal;

        public GameObject panel;

        [SerializeField] private RawImage hueImage, satValImage, outputImage;

        [SerializeField] private Slider hueSlider;

        [SerializeField] private TMP_InputField hexInputField;

        private Texture2D hueTexture, svTexture, outputTexture;
        
        // [SerializeField] MeshRenderer changeThis

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CreatHueImage();
            CreateSvImage();
            CreateOutputImage();
            UpdateOutputImage();
            
            hexInputField.onEndEdit.AddListener(OnTextInput);
        }

        private void OnDestroy()
        {
            hexInputField.onEndEdit.RemoveListener(OnTextInput);
        }

        void CreatHueImage()
        {
            hueTexture = new Texture2D(16, 1);
            hueTexture.wrapMode = TextureWrapMode.Clamp;
            hueTexture.name = "HueTexture";

            for (int i = 0; i < hueTexture.width; i++)
            {
                hueTexture.SetPixel(i,0,Color.HSVToRGB((float)i/hueTexture.width,1,0.95f));
            }
            hueTexture.Apply();

            currentHue = 0;

            hueImage.texture = hueTexture;
        }

        void CreateSvImage()
        {
            svTexture = new Texture2D(16, 16);
            svTexture.wrapMode = TextureWrapMode.Clamp;
            svTexture.name = "SatValTexture";

            for (int i = 0; i < svTexture.width; i++)
            {
                for (int j = 0; j < svTexture.height; j++)
                {
                    svTexture.SetPixel(i,j,Color.HSVToRGB(currentHue,(float)i / svTexture.height, (float)j / svTexture.width));
                }
            }
            
            svTexture.Apply();
            currentSat = 0;
            currentVal = 0;
            satValImage.texture = svTexture;
        }

        void CreateOutputImage()
        {
            outputTexture = new Texture2D(1, 16);
            outputTexture.wrapMode = TextureWrapMode.Clamp;
            outputTexture.name = "OutputTexture";

            Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);

            for (int i = 0; i < outputTexture.height; i++)
            {
                outputTexture.SetPixel(0,i,currentColor);
            }
            
            outputTexture.Apply();

            outputImage.texture = outputTexture;
        }

        void UpdateOutputImage()
        {
            Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);

            for (int i = 0; i < outputTexture.height; i++)
            {
                outputTexture.SetPixel(0,i,currentColor);
            }
            
            outputTexture.Apply();
            
            hexInputField.SetTextWithoutNotify(ColorUtility.ToHtmlStringRGB(currentColor));
            
            OnColorChanged?.Invoke(currentColor);
        }

        public void SetSv(float s, float v)
        {
            currentSat = s;
            currentVal = v;

            UpdateOutputImage();
        }

        
        public void UpdateSvImage()
        {
            currentHue = hueSlider.value;
            for (int i = 0; i < svTexture.width; i++)
            {
                for (int j = 0; j < svTexture.height; j++)
                {
                    svTexture.SetPixel(i,j,Color.HSVToRGB(currentHue,(float)i / svTexture.height, (float)j / svTexture.width));
                }
            }

            svTexture.Apply();
            
            UpdateOutputImage();
        }

        [SerializeField] private SVImageControl control;
        void OnTextInput(string str)
        {
            if (str.Length < 6)
            {
                Color currentColor = Color.HSVToRGB(currentHue, currentSat, currentVal);
                hexInputField.SetTextWithoutNotify(ColorUtility.ToHtmlStringRGB(currentColor));
                return;
            }

            if (ColorUtility.TryParseHtmlString("#" + str, out var newColor))
            {
                Color.RGBToHSV(newColor,out currentHue,out currentSat,out currentVal);
            }
            
            hueSlider.SetValueWithoutNotify(currentHue);
            
            control.UpdatePicker();
            UpdateOutputImage();
        }

        
        public void Close()
        {
            OnColorChanged = null;
        }
    }
}
