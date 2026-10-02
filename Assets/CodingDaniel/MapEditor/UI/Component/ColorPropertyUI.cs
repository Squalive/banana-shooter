using CodingDaniel.ColorPanel.Script;
using UnityEngine;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI.Component
{
    public class ColorPropertyUI : BaseProperty
    {
        [SerializeField] private Button btn;
        [SerializeField] private RawImage img;

        public override void UpdateValue(object o)
        {
            img.color = (Color) o;
        }

        public override void Init(string n, object o,bool c)
        {
            base.Init(n, o,c);
            btn.onClick.AddListener(OpenColorPanel);

            btn.interactable = c;
        }

        void OpenColorPanel()
        {
            ColorPickerControl.Instance.panel.SetActive(true);
            ColorPickerControl.Instance.OnColorChanged += (color =>
            {
                img.color = color;
                
                SetValue(color);
            });
        }
    }
}
