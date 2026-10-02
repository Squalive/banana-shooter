
using Manager;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Menu
{
    public class TutorialInGameUI : MonoBehaviour
    {
        public static TutorialInGameUI Instance { get; private set; }
        public enum TutorialState
        {
            None,
            EndlessStart,
            BuyStation
        }

        private void Awake()
        {
            Instance = this;
        }

        public TutorialState state = TutorialState.None;
        Vector2 localPoint;
        private Camera camera;
        private bool init = false;
        private Transform cameraTransform;
    
        private RectTransform canvasRect;
        float multiplier = 0.85f;

        [SerializeField] private CanvasGroup canvasGroup;
        private float _desiredAlpha=0;
        private void Start()
        {
            canvasRect = GetComponent<RectTransform>();
        }
        void SetCamera(Camera cam)
        {
            camera = cam;
            cameraTransform = cam.transform;
            init = true;
        }
        void Disable()
        {
            init = false;
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

        private Vector3 desiredPos;
        private Transform _trackTarget;
        [SerializeField] private Transform item;
        [SerializeField] private LocalizeStringEvent text;
        public void ChangeState(TutorialState s,Vector3 pos,Transform target = null)
        {
            if (!GameManager.Instance.setting.enableTutorial)
            {
                return;
            }
            state = s;
            switch (s)
            {
                case TutorialState.None:
                    item.gameObject.SetActive(false);
                    text.gameObject.SetActive(false);
                    break;
                case TutorialState.EndlessStart:
                    item.gameObject.SetActive(true);
                    desiredPos = pos;
                    text.gameObject.SetActive(true);
                    text.SetEntry("Endless_tutorial_1");
                    break;  
                case TutorialState.BuyStation:
                    item.gameObject.SetActive(true);
                    desiredPos = pos;
                    text.gameObject.SetActive(true);
                    text.SetEntry("Endless_tutorial_2");
                    break;
            }
        }
        private void LateUpdate()
        {

            switch (state)
            {
                case TutorialState.None:
                    return;
            }

            if (!init) return;
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
            _desiredAlpha = Mathf.Clamp(localPoint.magnitude / 700, .25f,1f);

            if (Vector3.Distance(cameraTransform.position, desiredPos) < 6)
            {
                _desiredAlpha = 0;
            }

            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, _desiredAlpha, Time.deltaTime * 10f);
            
            item.localPosition = localPoint;
        }
        
        
    }
}
