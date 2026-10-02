
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class WindowCustomization : MonoBehaviour
    {
        [SerializeField] private Color backGroundColor = new(67 / 255f, 67 / 255f, 67 / 255f);
        [SerializeField] private Color secondBackGroundColor = new(48 / 255f, 48 / 255f, 48 / 255f);

        [SerializeField] private RawImage backGround,secondBackGround;

        private void OnValidate()
        {
            if (backGround)
            {
                backGround.color = backGroundColor;
            }

            if (secondBackGround)
            {
                secondBackGround.color = secondBackGroundColor;
            }
        }
    }
}
