using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class DragWindow : MonoBehaviour,IDragHandler, IPointerEnterHandler ,IPointerExitHandler,IPointerDownHandler
    {
        public static Action<DragWindow> WindowFocused;
        public UnityEvent onPointerClick= new UnityEvent();
        private RectTransform _canvasRect;
        [SerializeField]
        private RectTransform _transform;
        private Canvas _canvas;

        public static bool IsDragging { private set; get; }

        [SerializeField] private Canvas canvas;
        private const int NormalSorting = 2;

        [SerializeField] private bool allowScaling = true;
        int GetSorting()
        {
            return NormalSorting;
        }
        
        private void Awake()
        {
            _canvasRect = canvas.GetComponent<RectTransform>();
            
            _canvas = GetComponent<Canvas>();

            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();

                _canvas.overrideSorting = true;
                _canvas.sortingOrder = GetSorting();

                gameObject.AddComponent<GraphicRaycaster>();
            }
        }
        
        private void OnEnable()
        {
            WindowFocused?.Invoke(this);
            WindowFocused += OnWindowFocused;
            _canvas.sortingOrder = GetSorting()+1;
        }

        private void OnDisable()
        {
            IsDragging = false;
            WindowFocused -= OnWindowFocused;
            _canvas.sortingOrder = GetSorting();
        }
        
        private void OnWindowFocused(DragWindow obj)
        {
            if (obj != this)
            {
                _canvas.sortingOrder = GetSorting();
            }
        }

        private Vector2 startPos, endPos;

        private bool isChangingSize = false;

        public Vector2 BorderLeftUp
        {
            get
            {
                var anchoredPosition = _transform.anchoredPosition;
                var delta = _transform.sizeDelta;

                return anchoredPosition + new Vector2(-delta.x / 2f, delta.y / 2f);
            }
        }
        
        public Vector2 BorderLeftDown
        {
            get
            {
                var anchoredPosition = _transform.anchoredPosition;
                var delta = _transform.sizeDelta;

                return anchoredPosition + new Vector2(-delta.x / 2f, -delta.y / 2f);
            }
        }
        
        public Vector2 BorderRightUp
        {
            get
            {
                var anchoredPosition = _transform.anchoredPosition;
                var delta = _transform.sizeDelta;

                return anchoredPosition + new Vector2(delta.x / 2f, delta.y / 2f);
            }
        }
        
        public Vector2 BorderRightDown
        {
            get
            {
                var anchoredPosition = _transform.anchoredPosition;
                var delta = _transform.sizeDelta;

                return anchoredPosition + new Vector2(delta.x / 2f, -delta.y / 2f);
            }
        }

        private const float Min = 50;

        public void OnDrag(PointerEventData eventData)
        {
            if (!Input.GetMouseButton(0)) return;

            endPos = eventData.position / canvas.scaleFactor - new Vector2(1920 / 2f, 1080 / 2f);

            var anchoredPosition = _transform.anchoredPosition;
            if (allowScaling)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(),
                    Input.mousePosition, null, out endPos);
                if (isChangingSize)
                {
                    return;
                }

                if (Vector2.Distance(endPos, BorderLeftUp) < Min)
                {
                    BeginChange(BorderRightDown);
                    return;
                }

                if (Vector2.Distance(endPos, BorderLeftDown) < Min)
                {
                    BeginChange(BorderRightUp);
                    return;
                }

                if (Vector2.Distance(endPos, BorderRightDown) < Min)
                {
                    BeginChange(BorderLeftUp);
                    return;
                }

                if (Vector2.Distance(endPos, BorderRightUp) < Min)
                {
                    BeginChange(BorderLeftDown);
                    return;
                }
            }

            float halfWidth = _transform.sizeDelta.x/2f;
            float halfHeight = _transform.sizeDelta.y/2f;
            isChangingSize = false;
            anchoredPosition += eventData.delta / canvas.scaleFactor;

            Vector2 sizeDelta = _canvasRect.sizeDelta;

            Vector2 max = new Vector2(sizeDelta.x / 2f, sizeDelta.y / 2f);
            Vector2 min = new Vector2(-sizeDelta.x / 2f, -sizeDelta.y / 2f);

            if (anchoredPosition.x + halfWidth > max.x)
            {
                anchoredPosition.x = max.x-halfWidth;
            }
            if (anchoredPosition.x -halfWidth < min.x)
            {
                anchoredPosition.x = min.x+halfWidth;
            }
            if (anchoredPosition.y+halfHeight > max.y)
            {
                anchoredPosition.y = max.y-halfHeight;
            }
            if (anchoredPosition.y-halfHeight < min.y)
            {
                anchoredPosition.y = min.y+halfHeight;
            }

            _transform.anchoredPosition = anchoredPosition;
            // Debug.Log(canvas.pixelRect.width);
            // _transform.anchoredPosition =
            //     new Vector2(Mathf.Clamp(_transform.anchoredPosition.x, 0, canvas.pixelRect.width),
            //         Mathf.Clamp(_transform.anchoredPosition.y, 0, canvas.pixelRect.height));
        }

        void BeginChange(Vector2 start)
        {
            if (!isChangingSize)
            {
                isChangingSize = true;
                startPos = start;
            }
        }

        private void Update()
        {
            if (!allowScaling) return;
            if (isChangingSize)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    isChangingSize = false;
                }

                Vector2 size = endPos - startPos;
                
                _transform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
                _transform.anchoredPosition=(endPos + startPos) / 2f;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsDragging = true;
            
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsDragging = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            WindowFocused?.Invoke(this);
            _canvas.sortingOrder = GetSorting()+1;
            onPointerClick?.Invoke();
        }
    }
}
