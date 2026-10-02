using System.Collections;
using System.Collections.Generic;
using Manager;
using Multiplayer;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class GameStart : MonoBehaviour
    {
        public static GameStart Instance;
        public LocalizeStringEvent text;
        public RawImage icon;
    
        public List<Texture2D> texture = new List<Texture2D>();
        public List<Texture2D> serverTypeTexture = new List<Texture2D>();

        public Transform item;
        public CanvasGroup canvas;
        private float speed = 30f;

        private float desiredAlpha = 0f;
        private Vector3 desiredSize = Vector3.one* 1000f;
        private void Awake()
        {
            Instance = this;
            canvasRect = GetComponent<RectTransform>();

            item.localScale = Vector3.one * 1000f;
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
            init = true;
        }
        public void SetValues()
        {
            GameMode gameMode = NetworkManager.ClientGameMode;
            // icon.texture = texture[(int) (gameMode - 3)];
            icon.texture = PrefabManager.Instance.GetTexture2D($"gm_{gameMode.ToString().ToLower()}");
            text.SetEntry(gameMode.ToString());

            item.localScale = Vector3.one * 1000f;
            desiredAlpha = 1f;
            desiredSize=Vector3.one;

        
            Invoke(nameof(Clear),3f);
        }

        public void SetSpecialMode()
        {
            icon.texture = serverTypeTexture[(int) (NetworkManager.ClientServerType - 3)];
            text.SetEntry(NetworkManager.ClientServerType.ToString());
        
            item.localScale = Vector3.one * 1000f;
            desiredAlpha = 1f;
            desiredSize=Vector3.one;

        
            Invoke(nameof(Clear),3f);
        }

        bool target=false;
        void Clear()
        {
            if (NetworkManager.ClientGameMode == GameMode.KingOfTheHill)
            {
                target = true;
                desiredSize=Vector3.one*0.35f;

                StartCoroutine(MoveToTarget());
            
                return;
            }
            desiredAlpha = 0f;
        }

        IEnumerator MoveToTarget()
        {
            int frame = 0;
            speed = 10f;
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

            while (Vector2.Distance(localPoint,item.localPosition) > 7f && frame < 500)
            {
                targetPos = VectorExtension.CalculateWorldPosition(desiredPos, cameraTransform);
                screenPoint = camera.WorldToScreenPoint(targetPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);

                sizeDelta = canvasRect.sizeDelta;

                max = new Vector2(sizeDelta.x / 2f, sizeDelta.y / 2f) * multiplier;
                min = new Vector2(-sizeDelta.x / 2f, -sizeDelta.y / 2f) * multiplier;
        
        
        
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
                item.localPosition = Vector3.Lerp(item.localPosition,localPoint,Time.deltaTime*3f);
                frame++;
                yield return null;
            }
            follow = true;
        }

        Vector2 localPoint;
        public Camera camera;
        private bool init = false;
        public Transform cameraTransform;
        private void Update()
        {
            canvas.alpha = Mathf.Lerp(canvas.alpha, desiredAlpha, Time.deltaTime * 5f);
            item.localScale = Vector3.Lerp(item.localScale,desiredSize,Time.deltaTime*speed);
        
       
        }
        public Vector3 desiredPos;

        private RectTransform canvasRect;
        float multiplier = 0.85f;
        private void LateUpdate()
        {
            if (!init || !target || !follow) return;
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


            desiredAlpha = GameUIManager.Instance.ticking ? 0 : Mathf.Clamp(localPoint.magnitude / 400, 0, 1f);

            item.localPosition = localPoint;
        }

        private bool follow = false;
        void Disable()
        {
            init = false;
        }
    }
}
