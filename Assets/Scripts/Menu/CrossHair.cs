using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class CrossHair : MonoBehaviour
    {
        public static CrossHair Instance;

        [Header("准星的长度")]
        public float width;
        [Header("准星的高度")]
        public float height;
        [Header("上下（左右）两条准星之间的距离")]
        public float distance;
 
        private Texture tex;            //  准星背景辅助参数
 
        public Color color = Color.white;

        public RectTransform left, up, right, bottom,dotTransform;
        public RawImage[] rawImages;

        public bool enableDot=true;

        public GameObject dot;
        private void Awake()
        {
            Instance = this;

            dotTransform = dot.GetComponent<RectTransform>();
        }

        private void Start()
        {
            SetWidth(GameManager.Instance.setting.crossHairWidth);
            SetHeight(GameManager.Instance.setting.crossHairHeight);
            SetDistance(GameManager.Instance.setting.crossHairDistance);
            SetDot(GameManager.Instance.setting.enableDot);
            SetColor(new Color(GameManager.Instance.setting.csR/255f,  GameManager.Instance.setting.csG/255f, GameManager.Instance.setting.csB/255f));
            SetValue();
        }

        public void SetDot(bool arg)
        {
            enableDot = arg;
            SetValue();
        } 
        void SetValue()
        {
            float costant = 0;
            left.anchoredPosition = new Vector2(costant - distance / 2, 0);
            up.anchoredPosition = new Vector2(0, costant - distance / 2);
            right.anchoredPosition = new Vector2(costant + distance / 2, 0);
            bottom.anchoredPosition = new Vector2(0, costant + distance / 2);

            left.sizeDelta = new Vector2(width, height);
            right.sizeDelta = new Vector2(width, height);
            up.sizeDelta = new Vector2(height, width);
            bottom.sizeDelta = new Vector2(height, width);

            if(dotTransform!=null)
                dotTransform.sizeDelta = new Vector2(height, height);

            foreach (var i in rawImages)
            {
                i.color = color;
            }
            dot.SetActive(enableDot);
        }

        public void SetColor(Color c)
        {
            color = c;
            SetValue();
        }

        public void SetWidth(float w)
        {
            width = w;
            SetValue();
        }
        public void SetHeight(float h)
        {
            height = h;
            SetValue();
        }
        public void SetDistance(float d)
        {
            distance = d;
            SetValue();
        }

        private void OnValidate()
        {
            SetValue();
        }
    }
}
