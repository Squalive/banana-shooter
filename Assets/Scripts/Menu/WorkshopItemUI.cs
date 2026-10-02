using System;
using System.Collections;
using Manager;
using Steamworks;
using SteamWorkshop;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Menu
{
    public class WorkshopItemUI : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField] private CanvasGroup hoverDescription;
        [SerializeField] private RectTransform sizeTransform, authorTransform, installTransform,uninstallTransform;

        [SerializeField] private TextMeshProUGUI nameText, descriptionText, sizeText, authorText;

        [SerializeField] private RawImage previewImage,sizeImage,authorImage;

        [SerializeField] private Image bg;

        [SerializeField] private Button installBtn, uninstallBtn;

        private bool _isHovered = false;
        
        [SerializeField]
        private EItemState state = EItemState.k_EItemStateNone;

        [SerializeField] private Color[] noneColors = Array.Empty<Color>();
        [SerializeField] private Color[] needsUpdateColors = Array.Empty<Color>();
        [SerializeField] private Color[] installedColors = Array.Empty<Color>();
        [SerializeField] private Color[] downloadingColors = Array.Empty<Color>();

        private WorkshopItem _item;
        private void Start()
        {
            installBtn.onClick.AddListener(Install);
            uninstallBtn.onClick.AddListener(Uninstall);

            SteamWorkshopManager.OnWorkshopItemInstalled += OnItemInstalled;
            SteamWorkshopManager.OnItemStartDownloading += ItemStateChanged;
            SteamWorkshopManager.OnUnSubscribeItem += ItemStateChanged;
            SteamWorkshopManager.OnSubscribeItem += ItemStateChanged;

            GameManager.Instance.OnPersonaStateChanged += SetAuthorName;
        }

        private void OnDestroy()
        {
            SteamWorkshopManager.OnWorkshopItemInstalled -= OnItemInstalled;
            SteamWorkshopManager.OnItemStartDownloading -= ItemStateChanged;
            SteamWorkshopManager.OnUnSubscribeItem -= ItemStateChanged;
            SteamWorkshopManager.OnSubscribeItem -= ItemStateChanged;
            
            GameManager.Instance.OnPersonaStateChanged -= SetAuthorName;
        }

        public void Initialize(WorkshopItem item)
        {
            if(!gameObject.activeSelf)return;
            _item = item;
            nameText.SetText(item.Details.m_rgchTitle);
            descriptionText.SetText(item.Details.m_rgchDescription);
            sizeText.SetText(GameManager.GetObjectSize(item.Details.m_nFileSize));

            if (!SteamFriends.RequestUserInformation((CSteamID)item.Details.m_ulSteamIDOwner,true))
            {
                SetAuthorName((CSteamID)item.Details.m_ulSteamIDOwner, EPersonaChange.k_EPersonaChangeName);
            }
            else
            {
                authorText.SetText("Loading...");
            }

            state = item.state;

            if (state == (EItemState.k_EItemStateInstalled ^ EItemState.k_EItemStateSubscribed))
            {
                installTransform.gameObject.SetActive(false);
                uninstallTransform.gameObject.SetActive(true);
            }
            else
            {
                uninstallTransform.gameObject.SetActive(false);
                installTransform.gameObject.SetActive(true);
            }
            
            if(previewImage.texture!=null){
                Destroy(previewImage.texture);
            }

            previewImage.texture = null;
            StartCoroutine(LoadTextureToItem(item.previewUrl));
            
            SetColors();
        }

        void SetAuthorName(CSteamID id,EPersonaChange change)
        {
            if (id.m_SteamID != _item.Details.m_ulSteamIDOwner) return;
            authorText.SetText(SteamFriends.GetFriendPersonaName(id));
        }

        void OnItemInstalled(WorkshopItem item)
        {
            ItemStateChanged(item.fileId);
        }
        
        private void ItemStateChanged(PublishedFileId_t obj)
        {
            if (_item.fileId != obj) return;
            _item.state = (EItemState)SteamUGC.GetItemState(obj);
            
            state = _item.state;

            if (state == (EItemState.k_EItemStateInstalled ^ EItemState.k_EItemStateSubscribed))
            {
                installTransform.gameObject.SetActive(false);
                uninstallTransform.gameObject.SetActive(true);
            }
            else
            {
                uninstallTransform.gameObject.SetActive(false);
                installTransform.gameObject.SetActive(true);
            }
            
            SetColors();
        }

        void Install()
        {
            SteamUGC.SubscribeItem(_item.fileId);
        }
        
        void Uninstall()
        {
            SteamUGC.UnsubscribeItem(_item.fileId);
        }

        IEnumerator LoadTextureToItem(string url)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                previewImage.texture = texture;
            }
        }
        
        void SetColors()
        {
            Color[] colors = GetColors();

            Color textColor = colors[2];
            Color previewColor = colors[1];
            Color bgColor = colors[0];

            previewImage.color = previewColor;

            nameText.color = descriptionText.color = sizeText.color = authorText.color = textColor;

            bg.color =sizeImage.color =authorImage.color = bgColor;
        }

        Color[] GetColors()
        {
            switch (state)
            {
                case EItemState.k_EItemStateNone:
                    return noneColors;
                case EItemState.k_EItemStateNeedsUpdate ^ EItemState.k_EItemStateSubscribed:
                    return needsUpdateColors;
                case EItemState.k_EItemStateInstalled ^ EItemState.k_EItemStateSubscribed:
                    return installedColors;
                case EItemState.k_EItemStateDownloading ^ EItemState.k_EItemStateSubscribed:
                    return downloadingColors;
                case EItemState.k_EItemStateDownloadPending ^ EItemState.k_EItemStateSubscribed:
                    return downloadingColors;
                default:
                    return noneColors;
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        private void Update()
        {
            if (_isHovered)
            {
                hoverDescription.alpha = Mathf.Lerp(hoverDescription.alpha, 1, Time.deltaTime * 10f);
                sizeTransform.anchoredPosition = Vector2.Lerp(sizeTransform.anchoredPosition, new Vector2(40, 0),
                    Time.deltaTime * 10f);
                authorTransform.anchoredPosition = Vector2.Lerp(authorTransform.anchoredPosition, new Vector2(40, 0),
                    Time.deltaTime * 10f);

                installTransform.anchoredPosition = Vector2.Lerp(installTransform.anchoredPosition, new Vector2(-2, 2),
                    Time.deltaTime * 10f);
                uninstallTransform.anchoredPosition = Vector2.Lerp(installTransform.anchoredPosition, new Vector2(-2, 2),
                    Time.deltaTime * 10f);
            }
            else
            {
                hoverDescription.alpha = Mathf.Lerp(hoverDescription.alpha, 0, Time.deltaTime * 10f);
                sizeTransform.anchoredPosition = Vector2.Lerp(sizeTransform.anchoredPosition, Vector2.zero, 
                    Time.deltaTime * 10f);
                authorTransform.anchoredPosition = Vector2.Lerp(authorTransform.anchoredPosition, Vector2.zero, 
                    Time.deltaTime * 10f);
                
                installTransform.anchoredPosition = Vector2.Lerp(installTransform.anchoredPosition, new Vector2(-2, -30f),
                    Time.deltaTime * 10f);
                uninstallTransform.anchoredPosition = Vector2.Lerp(installTransform.anchoredPosition, new Vector2(-2, -30f),
                    Time.deltaTime * 10f);
            }
        }
    }
}
