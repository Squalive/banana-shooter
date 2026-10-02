using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIAnimation
{
    public abstract class BaseUIAnimation : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        public bool isToggle = false;
        
        public bool IsSelected { get; set; }

        public UIAnimationToggleGroup group;

        private Canvas _canvas;

        [SerializeField] private GameObject[] parent = Array.Empty<GameObject>();

        private Canvas[] _parentCanvas;

        public bool highPriority = true;
        protected virtual void Awake()
        {
            if (highPriority)
            {
                _canvas = GetComponent<Canvas>();

                if (_canvas == null)
                {
                    _canvas = gameObject.AddComponent<Canvas>();

                    _canvas.overrideSorting = false;

                    gameObject.AddComponent<GraphicRaycaster>();
                
                }

                _parentCanvas = new Canvas[parent.Length];

                for (int i = 0; i < parent.Length; i++)
                {
                    _parentCanvas[i] = parent[i].GetComponent<Canvas>();

                    if (_parentCanvas[i] == null)
                    {
                        _parentCanvas[i] = parent[i].AddComponent<Canvas>();

                        _parentCanvas[i].overrideSorting = false;
                    
                        parent[i].AddComponent<GraphicRaycaster>();
                    }
                }
            }
            
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if (highPriority)
            {
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = 5;

                foreach (var c in _parentCanvas)
                {
                    c.sortingOrder = 5;
                }
            }
            
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (highPriority)
            {
                _canvas.overrideSorting = false;
                // _canvas.sortingOrder = 1;
                // foreach (var c in _parentCanvas)
                // {
                //     c.sortingOrder = 1;
                // }
            }
            
        }


        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (highPriority)
            {
                _canvas.overrideSorting = false;
            }

            if (isToggle)
            {
                if (!IsSelected)
                {
                    group.SetUIAnimation(this);
                }
                else
                {
                    DeSelect();
                }
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            
        }

        public void Select()
        {
            IsSelected = true;
        }

        public virtual void DeSelect()
        {
            IsSelected = false;
        }
    }
}
