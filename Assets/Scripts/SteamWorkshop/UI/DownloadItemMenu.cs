using System.Collections.Generic;
using Audio;

using CodingDaniel.MapEditor.MEEditor.MESave;
using Menu;
using Multiplayer;
using Multiplayer.Interface;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SteamWorkshop.UI
{
    public class DownloadItemMenu : MonoBehaviour
    {
        public static DownloadItemMenu Instance { private set; get; }

        private Dictionary<PublishedFileId_t, NeedToDownloadItemUI> items =
            new Dictionary<PublishedFileId_t, NeedToDownloadItemUI>();

        private void Awake()
        {
            Instance = this;

            _cancel = backBtn.GetComponent<Button>();
            
            SteamWorkshopManager.OnItemDownloaded += OnItemDownloaded;
            SteamWorkshopManager.OnItemStartDownloading += OnItemStartDownloading;
        }
        
        private void OnDestroy()
        {
            SteamWorkshopManager.OnItemDownloaded -= OnItemDownloaded;
            SteamWorkshopManager.OnItemStartDownloading -= OnItemStartDownloading;
        }

        [SerializeField] private GameObject panel;
        [SerializeField] private Transform content;

        [SerializeField] private NeedToDownloadItemUI prefab;

        [SerializeField] private GameObject backBtn, joinBtn, downloadBtn, downloadingPanel;

        [SerializeField] private Slider downloadingSlider;
        [SerializeField] private TextMeshProUGUI downloadProgressText;

        private ulong _totalOfDownloadingMapBytes = 0,_downloadBytes=0;

        private HashSet<PublishedFileId_t> _downloadingMaps = new HashSet<PublishedFileId_t>();

        private float progress = 0;

        private Button _cancel;

        public void NeedToDownload(ulong[] files)
        {
            foreach (var item in items.Values)
            {
                Destroy(item.gameObject);
            }
            items.Clear();

            bool canJoin = true;
            bool downloading = false;

            foreach (var s in files)
            {
                PublishedFileId_t fileIdT = new PublishedFileId_t(s);

                NeedToDownloadItemUI itemUI = Instantiate(prefab, content);

                itemUI.Init(fileIdT);

                items.Add(fileIdT, itemUI);

                if (canJoin)
                {
                    canJoin = (itemUI.itemState & EItemState.k_EItemStateInstalled) == EItemState.k_EItemStateInstalled && (itemUI.itemState & EItemState.k_EItemStateNeedsUpdate) != EItemState.k_EItemStateNeedsUpdate;
                }

                if (!downloading)
                {
                    downloading = (itemUI.itemState & EItemState.k_EItemStateDownloading) != EItemState.k_EItemStateNone;
                }
            }

            UIManager.Instance.SetButton(_cancel);
            panel.SetActive(true);

            backBtn.SetActive(canJoin || !downloading);
            joinBtn.SetActive(canJoin);
            downloadBtn.SetActive(!canJoin && !downloading);
            downloadingPanel.SetActive(downloading);
        }

        
        public void Cancel()
        {
            AudioManager.Instance.PlayButton();

            panel.SetActive(false);

            LobbyManager.Instance.LeaveLobby();

            UIManager.Instance.disConnectBtn.onClick.Invoke();
        }

        
        public void Connect()
        {
            panel.SetActive(false);
            AudioManager.Instance.PlayButton();
            
            // NetworkServerManager.EnabledWorkshopMaps.Clear();

            foreach (var item in items)
            {
                if (SteamUGC.GetItemInstallInfo(item.Key, out _, out var path, 1000, out _))
                {
                    // DateTime date = DateTimeOffset.FromUnixTimeSeconds(timeStamp).LocalDateTime;
            
                    MapSaver.Instance.LoadWorkshopMap(path,item.Key);
                }
            }

            NetworkManager.Instance.TryToAuthorize();
        }

        
        public void Download()
        {
            AudioManager.Instance.PlayButton();

            _downloadBytes = 0;
            _totalOfDownloadingMapBytes = 0;
            progress = 0;
            _downloadingMaps.Clear();
            
            foreach (var item in items)
            {
                if ((item.Value.itemState & EItemState.k_EItemStateInstalled) == EItemState.k_EItemStateNone || 
                    (item.Value.itemState & EItemState.k_EItemStateNeedsUpdate) == EItemState.k_EItemStateNeedsUpdate)
                {
                    SteamWorkshopManager.Instance.DownloadItemTemp(item.Key);
                }
            }
        }

        public void AddBytes(PublishedFileId_t id,ulong down, ulong total)
        {
            if (_downloadingMaps.Contains(id))
            {
                _downloadBytes += down;
            }
            else
            {
                if (total != 0)
                {
                    _downloadingMaps.Add(id);
                    _downloadBytes += down;
                    _totalOfDownloadingMapBytes += total;
                }
            }

            if (_totalOfDownloadingMapBytes == 0)
                progress = 0;
            else progress = _downloadBytes / (float) _totalOfDownloadingMapBytes;
            
            downloadProgressText.SetText((progress*100f).ToString("F0")+"%");
        }

        private void Update()
        {
            downloadingSlider.value = Mathf.Lerp(downloadingSlider.value, progress, Time.deltaTime * 20f);
        }

        private void OnItemDownloaded(PublishedFileId_t fileId)
        {
            if (items.TryGetValue(fileId, out var item))
            {
                item.InitState(EItemState.k_EItemStateInstalled);
                ResetPanel();
            }
        }
        
        private void OnItemStartDownloading(PublishedFileId_t fileId)
        {
            if (items.TryGetValue(fileId, out var item))
            {
                item.InitState(EItemState.k_EItemStateDownloading);
                
                item.StartGetDownloadProgress();
                ResetPanel();
            }
        }

        void ResetPanel()
        {
            bool canJoin = true;
            bool downloading = false;

            foreach (var itemUI in items.Values)
            {
                if (canJoin)
                {
                    canJoin = (itemUI.itemState & EItemState.k_EItemStateInstalled) == EItemState.k_EItemStateInstalled && (itemUI.itemState & EItemState.k_EItemStateNeedsUpdate) == EItemState.k_EItemStateNone;
                }

                if (!downloading)
                {
                    downloading = (itemUI.itemState & EItemState.k_EItemStateDownloading) == EItemState.k_EItemStateDownloading;
                }
            }
            
            backBtn.SetActive(canJoin || !downloading);
            joinBtn.SetActive(canJoin);
            downloadBtn.SetActive(!canJoin && !downloading);
            downloadingPanel.SetActive(downloading);
        }
    }
}
