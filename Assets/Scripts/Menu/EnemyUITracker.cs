

using TMPro;
using UnityEngine;

namespace Menu
{
    public class EnemyUITracker : MonoBehaviour
    {
        Vector2 localPoint;
        private Camera camera;
        private bool init = false;
        private Transform cameraTransform;
        public Transform target;
    
    
        private RectTransform canvasRect;

        private CanvasGroup _canvasGroup;
        [SerializeField]private CanvasGroup nameCanvas;
        private float desiredAlpha = 0.5f;
        private float nameAlpha = 0.5f;
        float multiplier = 0.85f;
        [SerializeField] private TrackerType type = TrackerType.None;

        [HideInInspector]
        public Vector3 offset = Vector3.zero;

        public TextMeshProUGUI text;
        
        public enum TrackerType
        {
            None,
            Enemy,
            Player,
        }
        private void Start()
        {
            camera = GameStart.Instance.camera;
            cameraTransform = GameStart.Instance.cameraTransform;
            canvasRect = transform.root.GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void LateUpdate()
        {
            if (!target) return;
            Vector3 desiredPos = target.position+offset;
            Vector3 targetPos = VectorExtension.CalculateWorldPosition(desiredPos, cameraTransform);
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

            float f = localPoint.magnitude / 300f;
            desiredAlpha = Mathf.Clamp(f, .9f, .25f);
            if (type == TrackerType.Player)
            {
                nameAlpha = desiredAlpha > 0.8f ? 1f : 0;
                nameCanvas.alpha = Mathf.Lerp(nameCanvas.alpha, nameAlpha, Time.deltaTime * 10f);
            }
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, desiredAlpha, Time.deltaTime * 10f);
            transform.localPosition = localPoint;
        }
    
    }
}
