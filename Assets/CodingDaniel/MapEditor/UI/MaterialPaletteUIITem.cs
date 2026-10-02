using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class MaterialPaletteUIITem : MonoBehaviour,IPointerDownHandler
    {
        private MaterialPaletteUI _materialPaletteUI;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private RawImage image;
        public Material material;

        [HideInInspector]
        public Toggle _toggle;

        private bool external = false;

        public void Init(string name, Texture texture, Material material,ToggleGroup toggleGroup,MaterialPaletteUI materialPaletteUI,bool e=false)
        {
            nameText.SetText(name);
            image.texture = texture;
            this.material = material;
            _toggle = GetComponent<Toggle>();
            _toggle.group = toggleGroup;
            _materialPaletteUI = materialPaletteUI;
            external = e;
            
            _toggle.onValueChanged.AddListener(Click);
        }

        void Click(bool flag)
        {
            EventSystem.current.SetSelectedGameObject(null);
            _materialPaletteUI.SelectMaterial(flag ? this : null);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!external) return;
            if (Input.GetMouseButtonDown(1))
            {
                MaterialPaletteUI.Instance.SetSelectMaterial(this);
            }
        }
    }
}
