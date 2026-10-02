
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class TabListItem : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler
    {
        [SerializeField] private LocalizeStringEvent localizeText;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RawImage image;
        private Color _desiredColor,_desiredTextColor;

        private Color _defaultColor;
        public DropListItem item;

        private DropList _parent;
        private RectTransform _transform;
        private void Start()
        {
            _transform = GetComponent<RectTransform>();
            _defaultColor = image.color;
            _desiredColor = _defaultColor;
            _desiredTextColor=Color.white;
            _desiredColor.a = 0;
        }

        public void Init(DropListItem i,DropList dropList)
        {
            item = i;
            _parent = dropList;
            
            localizeText.SetEntry(item.name);
        }
        
        private void Update()
        {
            image.color = Color.Lerp(image.color, _desiredColor, Time.deltaTime * 15f);
            text.color = Color.Lerp(text.color, _desiredTextColor, Time.deltaTime * 15f);
        }

        public bool isSelect = false;
        public void OnPointerEnter(PointerEventData eventData)
        {
            _desiredColor = _defaultColor;
            _desiredColor.a = 0.8f;
            _desiredTextColor=Color.black;

            if (!isSelect&&item.type == DropListItemType.HasChild)
            {
                _parent.ClearSelection();
                _parent.DestroyDropList();
                isSelect = true;
                _parent.SpawnList(_transform,item.items,false,true);
                
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _desiredColor = _defaultColor;
            _desiredColor.a = 0;
            _desiredTextColor=Color.white;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (item.type == DropListItemType.Button)
            {
                TabList.Instance.ClearSelection();
                TabList.Instance.DestroyDropList();
                
                item.onClicked?.Invoke();
            }
        }
    }
}
