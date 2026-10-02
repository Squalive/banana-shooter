using Manager;
using TMPro;
using UnityEngine;

namespace Menu
{
    public class InteractKeyTip : MonoBehaviour
    {
        public static InteractKeyTip Instance { get; private set; }
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private GameObject obj;
        [SerializeField] private Transform item;
        
        private Camera camera;
        private Transform cameraTransform;
        private bool _init = false;

        private bool _enable = false;
        private void Awake()
        {
            Instance = this;
            canvasRect = GetComponent<RectTransform>();
            
            keyText.SetText(GameManager.GetBindingName("Interact",0));
        }
        private void OnEnable()
        {
            GameManager.Instance.PlayerSpawn += SetCamera;
            GameManager.Instance.PlayerDestroy += Disable;
        }

        private void OnDisable()
        {
            GameManager.Instance.PlayerSpawn -= SetCamera;
            GameManager.Instance.PlayerDestroy -= Disable;
        }

        void SetCamera(Camera cam)
        {
            camera = cam;
            cameraTransform = cam.transform;
            _init = true;
        }
        void Disable()
        {
            _init = false;
        }
        public void EnableObj(Vector3 pos)
        {
            obj.SetActive(true);
            _enable = true;
            desiredPos = pos;

        }

        public void DisableObj()
        {
            obj.SetActive(false);
            _enable = false;
        }
        
        private RectTransform canvasRect;
        Vector2 localPoint;
        float multiplier = 0.85f;

        private Vector3 desiredPos;
        private void Update()
        {
            if (!_enable||!_init) return;
            Vector3 targetPos = VectorExtension.CalculateWorldPosition(desiredPos,cameraTransform);
            Vector2 screenPoint = camera.WorldToScreenPoint(targetPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);

            Vector2 sizeDelta = canvasRect.sizeDelta;

            Vector2 max = new Vector2(sizeDelta.x / 2f, sizeDelta.y / 2f) * multiplier;
            Vector2 min = new Vector2(-sizeDelta.x / 2f, -sizeDelta.y / 2f) * multiplier;
        
        
        
            if (localPoint.x > max.x)
            {
                localPoint.x = max.x;
            }
            if (localPoint.x < min.x)
            {
                localPoint.x = min.x;
            }
            if (localPoint.y > max.y)
            {
                localPoint.y = max.y;
            }
            if (localPoint.y < min.y)
            {
                localPoint.y = min.y;
            }

            if (targetPos != desiredPos)
            {
                localPoint = -localPoint;
                localPoint.y = min.y;
            }

            item.localPosition = localPoint;
        }
    }
}
