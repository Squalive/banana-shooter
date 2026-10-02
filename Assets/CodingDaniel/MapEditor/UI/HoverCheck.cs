using System;
using System.Collections.Generic;
using CodingDaniel.MapEditor.MEEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CodingDaniel.MapEditor.UI
{
    public class HoverCheck : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public bool enableAlpha=false;
        private CanvasGroup _canvas;

        private float desiredAlpha = .9f;
        private void Start()
        {
            if (enableAlpha)
            {
                _canvas = gameObject.AddComponent<CanvasGroup>();

                desiredAlpha = .9f;
            }
            
        }

        [SerializeField] private List<HoverCheck> connectedCheck;

        private void Update()
        {
            if (enableAlpha)
            {
                _canvas.alpha = Mathf.Lerp(_canvas.alpha, GetConnectCheckHover()? 1f :desiredAlpha, Time.deltaTime * 20f);
            }
        }

        bool GetConnectCheckHover()
        {
            if (IsHovered) return true;
            foreach (var check in connectedCheck)
            {
                if (check.IsHovered)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsHovered { private set; get; }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (MEBase.Instance.Tools.IsViewing) return;
            IsHovered = true;
            desiredAlpha = 1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            desiredAlpha = .9f;
        }
    }
}
