using System;
using System.Collections.Generic;
using Audio;

using Manager;
using Multiplayer;
using Steamworks;
using Steamworks.NET;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Menu
{
    public class UIManager : MonoBehaviour
    {

        public static UIManager Instance;

        [SerializeField] private GameObject notConnectedToSteamServers;

        [SerializeField] private LocalizeStringEvent connectStateText;

        [SerializeField] private GameObject dlcServerTab;

        private void Awake()
        {
            Instance = this;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            version.text = Application.version;

            dlcServerTab.SetActive(SteamApps.BIsDlcInstalled(new AppId_t(2238100)));
        }
        
        private void OnEnable()
        {
            if (SteamManager.SteamServersConnected) OnSteamServerConnected();
            else OnSteamServerNotConnected();

            SteamManager.OnSteamServersConnectedAction += OnSteamServerConnected;
            SteamManager.OnSteamServersNotConnectedAction += OnSteamServerNotConnected;
        }

        private void OnDisable()
        {
            SteamManager.OnSteamServersConnectedAction -= OnSteamServerConnected;
            SteamManager.OnSteamServersNotConnectedAction -= OnSteamServerNotConnected;
        }

        public void SetConnectingState(string state)
        {
            connectStateText.SetEntry(state);
        }

        public void StartConnecting()
        {
            SetConnectingState("Connect");
            loadingMenu.SetActive(true);
        }

        void OnSteamServerConnected()
        {
            notConnectedToSteamServers.SetActive(false);
        }

        void OnSteamServerNotConnected()
        {
            notConnectedToSteamServers.SetActive(true);
        }

        [SerializeField] private Transform cam;

        private Vector3 desiredPos,defaultPos;

        private void Start()
        {
            if (!SteamManager.Initialized)
            {
                foreach (var obj in adminPanel)
                {
                    obj.SetActive(false);
                }
                return;
            }
            else
            {
                ulong steamId = SteamUser.GetSteamID().m_SteamID;
                bool active = RolesManager.Instance.CheckIsAdmin(steamId) ||
                              RolesManager.Instance.CheckIsHelper(steamId);
                foreach (var obj in adminPanel)
                {
                    obj.SetActive(active);
                }
            }
            // GameManager.Instance.LoadSetting();

            defaultPos = cam.localPosition;
            desiredPos = defaultPos;
        
            SteamFriends.SetRichPresence("steam_display", "#Status_AtMainMenu");

            foreach (var reBindUi in reBindUis)
            {
                reBindUi.Refresh();
            }
        
            MusicManager.Instance.ChangeMusic(MusicManager.MusicType.MainMenu);
        }

        [SerializeField] public Button shootingRangeBtn;
        [SerializeField] public Button endLessBtn;
        [SerializeField] public Button tryToCreateCancel;

        [SerializeField] private List<GameObject> adminPanel = new List<GameObject>();
        
        public void ShootingRange()
        {
            shootingRangeBtn.interactable = false;
            NetworkServerManager.SetServerType(ServerType.ShootingRange);
            NetworkServerManager.SetServerEnableWorkshop(false);
            CreateServerMenu.Instance.CreateLobby(true);
        }
        
        public void Endless()
        {
            endLessBtn.interactable = false;
            AudioManager.Instance.PlayButton();
            NetworkServerManager.SetServerType(ServerType.Endless);
            NetworkServerManager.SetServerEnableWorkshop(false);
            CreateServerMenu.Instance.CreateLobby(false);
        }

        void Back()
        {
            if (btn!=null && Input.GetKeyDown(KeyCode.Escape))
            {
                btn.onClick.Invoke();
            }
        }

        Button btn;

        
        public void SetButton(Button button)
        {
            btn = button;
        }

        [SerializeField] private Vector3 weaponPos = new Vector3(4.25f,2.5f,26.75f);
        [SerializeField] private Vector3 multiplayerPos = new Vector3(4.25f,2.5f,26.75f);

        public void SetPos2Weapon()
        {
            desiredPos = weaponPos;
        }

        
        public void SetPos2Multiplayer()
        {
            desiredPos = multiplayerPos;
        }
    
        
        public void SetPos2Default()
        {
            desiredPos = defaultPos;
        }
        private void Update()
        {
            cam.localPosition = Vector3.Lerp(cam.localPosition,desiredPos,Time.deltaTime*5f);
            Back();
        }

        
        public void Quit()
        {
            Application.Quit();
        }

        
        public void ButtonSound()
        {
            AudioManager.Instance.PlayButton();
        }

        // [SerializeField] public GameObject border;/


    
        [Serializable]
        public class WeaponUI
        {
            public TextMeshProUGUI text; 
            public RawImage weaponImage;
        }

        static readonly string Chinese = "Chinese (Simplified) (zh-Hans)";
        public static bool IsItChinese()
        {
            return LocalizationSettings.Instance.GetSelectedLocale().LocaleName == Chinese;
        }

        public List<GameObject> withoutLoading = new List<GameObject>();
        [SerializeField]private GameObject loadingMenu;

        [SerializeField] GameObject browseServer,mainMenu;
        
        public void Disconnect()
        {
            LobbyManager.Instance.LeaveLobby();

            if (LobbyMenu.Instance.isMatching)
            {
                LobbyMenu.Instance.isMatching = false;
                mainMenu.SetActive(true);
                browseServer.SetActive(false);
                // border.SetActive(false);
            }
            else
            {
                mainMenu.SetActive(false);
                browseServer.SetActive(true);
            }
        }
    
        [SerializeField] private TextMeshProUGUI version;
        public Button disConnectBtn;

        public GameObject newItem;
        public TextMeshProUGUI newItemNameText;
        public RawImage NewItemImage;

        [SerializeField] private GameObject chineseVersionAbout;
        public void AboutMenu()
        {
            chineseVersionAbout.SetActive(IsItChinese());
        }
        
        public void OpenDiscordUrl()
        {
            Application.OpenURL("https://discord.gg/DtfVqyP6WX");
        }

        public void OpenYoutubeUrl()
        {
            Application.OpenURL("https://www.youtube.com/channel/UCJtaYNVq4jTaiPkFFTIqNRg");
        }

        public void OpenBiliBiliUrl()
        {
            Application.OpenURL("https://space.bilibili.com/413405976");
        }

        public void OpenBsDocUrl()
        {
            Application.OpenURL("https://bs-docs.readthedocs.io/en/latest/");
        }

        public ReBindUI[] reBindUis;

        
        public void SetDontShowMe(bool flag)
        {
            GameManager.hostGameDontShowMeThisAgain = flag;
        
            PlayerPrefs.SetInt("hostGameDontShowMeThisAgain",flag ? 1 : 0);
        }

        public GameObject enableWorkshopPanel;
    }
}
