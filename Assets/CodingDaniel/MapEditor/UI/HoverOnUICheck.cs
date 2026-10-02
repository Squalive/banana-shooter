using UnityEngine;
using UnityEngine.EventSystems;

namespace CodingDaniel.MapEditor.UI
{
    public class HoverOnUICheck : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public bool Hover { private set; get; } = false;
        public void OnPointerEnter(PointerEventData eventData)
        {
            Hover = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hover = false;
        }
    }
}
