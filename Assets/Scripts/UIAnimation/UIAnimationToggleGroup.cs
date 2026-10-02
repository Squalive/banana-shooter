using UnityEngine;

namespace UIAnimation
{
    public class UIAnimationToggleGroup : MonoBehaviour
    {
        BaseUIAnimation SelectedUI { get; set; }

        public void SetUIAnimation(BaseUIAnimation uiAnimation)
        {
            if (SelectedUI != null)
            {
                SelectedUI.DeSelect();
            } 
            SelectedUI = uiAnimation;
            SelectedUI.Select();
        }
    }
}