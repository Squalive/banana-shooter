using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CodingDaniel.ColorPanel.Script
{
    public class SVImageControl : MonoBehaviour,IDragHandler,IPointerClickHandler
    {
        [SerializeField] private RawImage pickerImage;

        private RawImage svImage;

        [SerializeField] private ColorPickerControl cc;

        private RectTransform rectTransform, pickerTransform;

        private void Start()
        {
            svImage = GetComponent<RawImage>();
            rectTransform = GetComponent<RectTransform>();
            pickerTransform = pickerImage.GetComponent<RectTransform>();
            pickerTransform.position =
                new Vector3(-(rectTransform.sizeDelta.x * 0.5f), -(rectTransform.sizeDelta.y * 0.5f));
        }

        void UpdateColor(PointerEventData eventData)
        {
            Vector3 pos = rectTransform.InverseTransformPoint(eventData.position);

            var sizeDelta = rectTransform.sizeDelta;
            float deltaX = sizeDelta.x * 0.5f;
            float deltaY = sizeDelta.y * 0.5f;

            if (pos.x < -deltaX) pos.x = -deltaX;
            else if (pos.x > deltaX) pos.x = deltaX;
            if (pos.y < -deltaY) pos.y = -deltaY;
            else if (pos.y > deltaY) pos.y = deltaY;

            float x = pos.x + deltaX;
            float y = pos.y + deltaY;

            float xNorm = x / sizeDelta.x;
            float yNorm = y / sizeDelta.y;

            pickerTransform.localPosition = pos;

            pickerImage.color = Color.HSVToRGB(0, 0, 1 - yNorm);
            
            cc.SetSv(xNorm,yNorm);
        }

        public void UpdatePicker()
        {
            float sat = cc.currentSat;
            float val = cc.currentVal;
            
            var sizeDelta = rectTransform.sizeDelta;

            float x = sat * sizeDelta.x;
            float y = val * sizeDelta.y;
            
            float deltaX = sizeDelta.x * 0.5f;
            float deltaY = sizeDelta.y * 0.5f;

            x -= deltaX;
            y -= deltaY;

            pickerTransform.localPosition = new Vector3(x, y, 0);
            
            pickerImage.color = Color.HSVToRGB(0, 0, 1 - val);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Debug.Log("aa");
            UpdateColor(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            UpdateColor(eventData);
        }
    }
}
