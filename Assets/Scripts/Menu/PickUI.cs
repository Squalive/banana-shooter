
using System.Collections.Generic;
using Manager;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Server;
using Multiplayer.Server;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Weapon.WeaponStats;

namespace Menu
{
    public class PickUI : MonoBehaviour
    {
        public static PickUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            canvasRect = GetComponent<RectTransform>();
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

        [SerializeField] private GameObject pickObj;
        private bool _enable = false;

        private Camera camera;
        private Transform cameraTransform;
        private ClientPickable _pickable;
        private bool _init = false;
        [SerializeField] private RawImage icon;
        [SerializeField] private LocalizeStringEvent nameText;
        [SerializeField] private LocalizeStringEvent typeText;
        void SetCamera(Camera cam)
        {
            camera = cam;
            cameraTransform = cam.transform;
            _init = true;
        }
        public void EnablePickObj(ClientPickable pickable)
        {
            _pickable = pickable;
            pickObj.SetActive(true);
            _enable = true;

            switch (pickable.pickableType )
            {
                case PickableType.Weapon:
                   WeaponStat weaponTexture = NetworkManager.Instance.weaponInfo[pickable.ObjectIndex];
                    icon.texture = weaponTexture.texture;
                    
                    nameText.SetEntry("wt_"+ weaponTexture.weaponType);
                    nameText.StringReference.Arguments = new List<object>() {weaponTexture.name};
                    nameText.RefreshString();
                    break;
            }

           typeText.SetEntry("pt_" + pickable.pickableType);
        }

        public void DisablePickObj()
        {
            pickObj.SetActive(false);
            _enable = false;
        }
        void Disable()
        {
            _init = false;
        }

        private RectTransform canvasRect;
        Vector2 localPoint;
        float multiplier = 0.85f;
        private void Update()
        {
            if (!_enable || !_init) return;
            Vector3 desiredPos = _pickable.transform.position;
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

        [SerializeField] private Transform item;
    }
}
