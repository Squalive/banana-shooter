
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class TabItem : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler
    {
        [SerializeField] private RawImage image;
        [SerializeField] private TextMeshProUGUI text;
        private Color _desiredColor,_desiredTextColor;

        private Color _defaultColor;

        [SerializeField] private List<DropListItem> items = new List<DropListItem>();

        private bool _isSelect = false;
        private void Start()
        {
            _defaultColor = image.color;
            _desiredColor = _defaultColor;
            _desiredTextColor = Color.white;
            _desiredColor.a = 0;

            _transform = GetComponent<RectTransform>();
        }


        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isSelect && TabList.Instance.isSelected)
            {
                TabList.Instance.ClearSelection();
                Click();
                
            }
            _desiredColor = _defaultColor;
            _desiredColor.a = 0.8f;
            _desiredTextColor = Color.black;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TabList.Instance.isSelected) return;
            _desiredColor = _defaultColor;
            _desiredColor.a = 0;
            _desiredTextColor = Color.white;
        }

        private void Update()
        {
            image.color = Color.Lerp(image.color, _desiredColor, Time.deltaTime * 15f);
            text.color = Color.Lerp(text.color, _desiredTextColor, Time.deltaTime * 15f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isSelect)
            {
                TabList.Instance.isSelected = false;
                ClearSelect();
                TabList.Instance.DestroyDropList();
                return;
            }
            Click();
        }

        private RectTransform _transform;
        void Click()
        {
            TabList.Instance.DestroyDropList();
            _isSelect = true;
            TabList.Instance.isSelected = true;
            TabList.Instance.SpawnList(_transform,items);
        }
        public void ClearSelect()
        {
            _isSelect = false;
            _desiredColor = _defaultColor;
            _desiredColor.a = 0;
            
            _desiredTextColor = Color.white;
        }
    }
}
