
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Menu
{
    public class UIHitDetection : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public UnityEvent pointerEnter;
        public UnityEvent pointerExit;

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerExit?.Invoke();
        }
    }
}
