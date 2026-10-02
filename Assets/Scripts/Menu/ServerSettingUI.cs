using System;
using System.Collections.Generic;
using System.Text;

using Cosmetic;
using Manager;
using Multiplayer;
using Multiplayer.Interface;
using Riptide;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class ServerSettingUI : MonoBehaviour
    {
        public static ServerSettingUI Instance;
    
        public List<AllowWeapon> allowWeapons = new List<AllowWeapon>();

        [Serializable]
        public class AllowWeapon
        {
            public string name;
            public Toggle toggle;
            public Button btn;
        }

        private void Awake()
        {
            Instance = this;

            
            
            
            
            
        }

        private bool isServer = false;
        private CSteamID lobbyId;

        private bool inited = false;
        public void Init()
        {
            if (inited) return;
            inited = true;
            isServer = NetworkServerManager.Instance.Server.IsRunning;

            for (short i = 0; i < allowWeapons.Count; i++)
            {
                var allowWeapon = allowWeapons[i];
                allowWeapon.toggle.interactable = isServer;

                //This UI list skips the weapons that have no toggle (DesertEagle, Boomer, Rocket Launcher,
                //Bac-20, Grenade Launcher), so the list position is NOT the weapon index. Resolve it by name.
                short weaponIndex = GetWeaponIndex(allowWeapon.name);
                if (weaponIndex < 0 || weaponIndex >= NetworkServerManager.AllowedWeapon.Count)
                {
                    allowWeapon.toggle.isOn = false;
                    allowWeapon.btn.interactable = false;
                    continue;
                }

                bool flag = NetworkServerManager.AllowedWeapon[weaponIndex];
                allowWeapon.toggle.isOn = flag;
                allowWeapon.btn.interactable = flag;
                
                if(isServer)
                {
                    //local copy: the listener must not capture the loop variable
                    short index = weaponIndex;
                    allowWeapon.toggle.onValueChanged.AddListener(delegate(bool arg0) { SetAllowedWeapon(index,arg0); });
                }
            }

            randomMap.interactable = isServer;
            randomGameMode.interactable = isServer;
            serverInput.interactable = isServer;
            disableSpecialWeaponToggle.interactable = isServer;
            
            lobbyId = LobbyManager.Instance.lobbyId;
            string name= SteamMatchmaking.GetLobbyData(lobbyId, "ashdaghj");
            SetServerText(name);
            serverName = name;

            randomMap.isOn = NetworkManager.ClientRandomMap;
            randomGameMode.isOn = NetworkManager.ClientRandomGameMode;
            disableSpecialWeaponToggle.isOn = NetworkManager.ClientDisableSpecialWeapon;
        
        
            serverInput.onDeselect.AddListener(SetServername);
            randomMap.onValueChanged.AddListener(SetRandomMap);
            randomGameMode.onValueChanged.AddListener(SetRandomGameMode);
            disableSpecialWeaponToggle.onValueChanged.AddListener(SetDisableSpecialWeapon);
            GameUIManager.Instance.gameModeText.SetText(NetworkManager.ClientGameMode + "\n" + NetworkManager.ClientServerType);
            enableTournamentToggle.isOn = LobbyManager.ClientTournamentStarted;
            enableTournamentToggle.onValueChanged.AddListener(TurnTournament);
            
            ulong steamId = NetworkManager.Instance.steamId.m_SteamID;
            if (!RolesManager.Instance.CheckIsAdmin(steamId) &&
                !RolesManager.Instance.CheckIsHelper(steamId) && !LobbyManager.ClientTournamentStarted)
            {
                foreach (var t in tournaments)
                {
                    t.SetActive(false);
                }
                
            }
            foreach (var obj in tournamentInfo)
            {
                obj.SetActive(LobbyManager.ClientTournamentStarted);
            }
            if (LobbyManager.ClientTournamentStarted)
            {
                enableTournamentToggle.interactable = false;
                tournamentStartButton.interactable = false;
                tourName.interactable = false;
                tourDesc.interactable = false;
                addRewardBtn.interactable = false;
                tourName.SetTextWithoutNotify(LobbyManager.ClientTournamentName.ToString());
                tourDesc.SetTextWithoutNotify(LobbyManager.ClientTournamentDesc.ToString());

                foreach (var prize in LobbyManager.ClientTournamentPrizes)
                {
                    AddObjToContent(CosmeticManager.ItemIdToItem[prize]);
                }
            }
            else if (!NetworkServerManager.Instance.Server.IsRunning)
            {
                enableTournamentToggle.interactable = false;
            }
            
        }

        private void SetDisableSpecialWeapon(bool arg0)
        {
            if (disableSpecialWeaponToggle.isOn != arg0)
                disableSpecialWeaponToggle.isOn = arg0;

            if (!isServer) return;
            NetworkServerManager.ServerDisableSpecialWeapon = arg0;
            NetworkServerManager.Instance.SendServerSetting(serverName);
        }

        public void SetServerSetting(string serverName)
        {
            randomGameMode.isOn = NetworkManager.ClientRandomGameMode;
            randomMap.isOn = NetworkManager.ClientRandomMap;
            disableSpecialWeaponToggle.isOn = NetworkManager.ClientDisableSpecialWeapon;
            SetServerText(serverName);
        }
        public Toggle randomMap, randomGameMode,disableSpecialWeaponToggle;
        void SetRandomMap(bool arg)
        {
            if (randomMap.isOn != arg)
                randomMap.isOn = arg;

            if (!isServer) return;
            NetworkServerManager.ServerRandomMap = arg;
            NetworkServerManager.Instance.SendServerSetting(serverName);
        }
        void SetRandomGameMode(bool arg)
        {
            if (randomGameMode.isOn != arg)
                randomGameMode.isOn = arg;

            if (!isServer) return;
            NetworkServerManager.ServerRandomGameMode = arg;
            NetworkServerManager.Instance.SendServerSetting(serverName);
        }
        public string serverName;
        public TMP_InputField serverInput;
        void SetServername(string name)
        {
            serverName = name;
            if (!isServer) return;
        
            SteamMatchmaking.SetLobbyData(LobbyManager.Instance.lobbyId,"LobbyName",serverName);
            NetworkServerManager.Instance.SendServerSetting(serverName);
        }
        void SetServerText(string name)
        {
            serverInput.text=name;
        }
        private static short GetWeaponIndex(string weaponName)
        {
            if (NetworkServerManager.Instance == null) return -1;

            var weaponInfo = NetworkServerManager.Instance.weaponInfo;
            for (short i = 0; i < weaponInfo.Count; i++)
            {
                if (weaponInfo[i].weaponName == weaponName) return i;
            }

            return -1;
        }

        void SetAllowedWeapon(short weapon,bool flag)
        {
            if (!NetworkServerManager.Instance.Server.IsRunning) return;
            if (weapon < 0 || weapon >= NetworkServerManager.AllowedWeapon.Count) return;
            int index = 0;
            foreach (var allowedWeapon in NetworkServerManager.AllowedWeapon)
            {
                if (allowedWeapon) index++;
            }

            if (!NetworkServerManager.AllowedWeapon[weapon])
            {
                //Currently disabled: enabling is always allowed.
                NetworkServerManager.AllowedWeapon[weapon] = true;
                flag = true;
            }
            else if (!flag && index > 4)
            {
                NetworkServerManager.AllowedWeapon[weapon] = false;
            }
            else
            {
                //Refused, it would leave fewer than 4 allowed weapons: keep it enabled.
                flag = true;
            }
            Message message = Message.Create(MessageSendMode.Reliable,(ushort)ServerToClientId.AllowedWeapon);
            message.Add(weapon);
            message.Add(flag);
            NetworkServerManager.Instance.Server.SendToAll(message);
        
        }
        [MessageHandler((ushort) ServerToClientId.AllowedWeapon, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void AllowedWeapon(Message message)
        {
            short weapon = message.GetShort();
            bool flag = message.GetBool();

            if (!NetworkServerManager.Instance.Server.IsRunning && weapon >= 0 &&
                weapon < NetworkServerManager.AllowedWeapon.Count)
            {
                NetworkServerManager.AllowedWeapon[weapon] = flag;
            }

            if (Instance == null || NetworkServerManager.Instance == null || weapon < 0 ||
                weapon >= NetworkServerManager.Instance.weaponInfo.Count) return;

            //weapon is a weaponInfo index, the UI list is a shorter list: match the entry by name.
            string weaponName = NetworkServerManager.Instance.weaponInfo[weapon].weaponName;
            for (int i = 0; i < Instance.allowWeapons.Count; i++)
            {
                if (Instance.allowWeapons[i].name != weaponName) continue;

                Instance.allowWeapons[i].toggle.isOn = flag;
                Instance.allowWeapons[i].btn.interactable = flag;
                break;
            }

        
        }
        [SerializeField] GameObject[] tournaments;
        [SerializeField] GameObject[] tournamentInfo;

        private bool _enableAddReward=false;
        [SerializeField] private GameObject rewardMenu;
        [SerializeField] private Transform addRewardContent;
        [SerializeField] private Transform btn;
        [SerializeField] private Toggle enableTournamentToggle;

        void TurnTournament(bool arg)
        {
            foreach (var obj in tournamentInfo)
            {
                obj.SetActive(arg);
            }

            if (arg)
            {
                enableTournamentToggle.interactable = false;
            }
        }

        private bool spawnItem;
        
        public void AddReward()
        {
            rewardMenu.transform.position = btn.position;
            _enableAddReward = !_enableAddReward;
            rewardMenu.SetActive(_enableAddReward);
            
            if (!spawnItem)
            {
                for (int i = 0; i < addRewardContent.childCount; i++)
                {
                    Destroy(addRewardContent.GetChild(i).gameObject);
                }
                if (InventoryManager.InventoryItems.Count > 0)
                {
                    spawnItem = true;
                    foreach (var itemsValue in InventoryManager.InventoryItems.Values)
                    {
                         CosmeticItem cosmeticItem = CosmeticManager.ItemIdToItem[itemsValue.itemDetails.m_iDefinition.m_SteamItemDef];
                         
                         Transform item = Instantiate(PrefabManager.Instance.GetPrefab("Cosmetic"),
                             addRewardContent).transform;
                         
                         item.GetChild(2).GetChild(0).GetComponent<RawImage>().texture = cosmeticItem.icon;
                         Transform child3 = item.GetChild(3);
                         child3.GetComponent<TextMeshProUGUI>().color = cosmeticItem.GetColor();
                         Transform child1 = item.GetChild(1);
                         switch (cosmeticItem.rarity)
                         {
                             case CosmeticItem.Rarity.Common:
                                 child1.GetComponent<RawImage>().color = Color.white;
                                 child3.GetComponent<TextMeshProUGUI>().color = Color.white;
                                 break;
                             case CosmeticItem.Rarity.Original:
                                 child1.GetComponent<RawImage>().color = new Color(255/255f,215/255f,0);
                                 child3.GetComponent<TextMeshProUGUI>().color = new Color(255/255f,215/255f,0);
                                 break;
                             case CosmeticItem.Rarity.Rare:
                                 child1.GetComponent<RawImage>().color = Color.red;
                                 child3.GetComponent<TextMeshProUGUI>().color = Color.red;
                                 break;
                             case CosmeticItem.Rarity.Legendary:
                                 child1.GetComponent<RawImage>().color = Color.yellow;
                                 child3.GetComponent<TextMeshProUGUI>().color = Color.yellow;
                                 break;
                             case CosmeticItem.Rarity.Uncommon:
                                 child1.GetComponent<RawImage>().color = Color.cyan;
                                 child3.GetComponent<TextMeshProUGUI>().color = Color.cyan;
                                 break;
                             case CosmeticItem.Rarity.Extraordinary:
                                 child1.GetComponent<RawImage>().color =new Color(1,105/255f,180/255f);
                                 child3.GetComponent<TextMeshProUGUI>().color =new Color(1,105/255f,180/255f);
                                 break;
                             case CosmeticItem.Rarity.Unique:
                                 child1.GetComponent<RawImage>().color =new Color(0,1,0);
                                 child3.GetComponent<TextMeshProUGUI>().color =new Color(0,1,0);
                                 break;
                             case CosmeticItem.Rarity.Epic:
                                 child1.GetComponent<RawImage>().color =new Color(255/255f,69/255f,0);
                                 child3.GetComponent<TextMeshProUGUI>().color =new Color(255/255f,69/255f,0);
                                 break;
                         }
         
                         child3.GetComponent<TextMeshProUGUI>().SetText(cosmeticItem.displayName);
                         
                         item.GetComponent<Button>().onClick.AddListener(delegate { AddActualReward(cosmeticItem,item.gameObject); });
                    }
                }
            }
            

            
        }

        
        public void CloseRewardMenu()
        {
            _enableAddReward = false;
            rewardMenu.SetActive(false);
        }


        [SerializeField] private Transform actualRewardContent;

        [SerializeField] private TMP_InputField tourName, tourDesc;
        void AddActualReward(CosmeticItem cosmeticItem,GameObject obj)
        {
            Destroy(obj);
            CloseRewardMenu();
            
            AddObjToContent(cosmeticItem);
            
            LobbyManager.ServerTournamentPrizes.Add(cosmeticItem.itemdefid);
        }

        void AddObjToContent(CosmeticItem cosmeticItem)
        {
            Transform item = Instantiate(PrefabManager.Instance.GetPrefab("Cosmetic"),
                actualRewardContent).transform;
            
            item.GetChild(2).GetChild(0).GetComponent<RawImage>().texture = cosmeticItem.icon;
            Transform child3 = item.GetChild(3);
            child3.GetComponent<TextMeshProUGUI>().color = cosmeticItem.GetColor();
            Transform child1 = item.GetChild(1);
            switch (cosmeticItem.rarity)
            {
                case CosmeticItem.Rarity.Common:
                    child1.GetComponent<RawImage>().color = Color.white;
                    child3.GetComponent<TextMeshProUGUI>().color = Color.white;
                    break;
                case CosmeticItem.Rarity.Original:
                    child1.GetComponent<RawImage>().color = new Color(255/255f,215/255f,0);
                    child3.GetComponent<TextMeshProUGUI>().color = new Color(255/255f,215/255f,0);
                    break;
                case CosmeticItem.Rarity.Rare:
                    child1.GetComponent<RawImage>().color = Color.red;
                    child3.GetComponent<TextMeshProUGUI>().color = Color.red;
                    break;
                case CosmeticItem.Rarity.Legendary:
                    child1.GetComponent<RawImage>().color = Color.yellow;
                    child3.GetComponent<TextMeshProUGUI>().color = Color.yellow;
                    break;
                case CosmeticItem.Rarity.Uncommon:
                    child1.GetComponent<RawImage>().color = Color.cyan;
                    child3.GetComponent<TextMeshProUGUI>().color = Color.cyan;
                    break;
                case CosmeticItem.Rarity.Extraordinary:
                    child1.GetComponent<RawImage>().color =new Color(1,105/255f,180/255f);
                    child3.GetComponent<TextMeshProUGUI>().color =new Color(1,105/255f,180/255f);
                    break;
                case CosmeticItem.Rarity.Unique:
                    child1.GetComponent<RawImage>().color =new Color(0,1,0);
                    child3.GetComponent<TextMeshProUGUI>().color =new Color(0,1,0);
                    break;
                case CosmeticItem.Rarity.Epic:
                    child1.GetComponent<RawImage>().color =new Color(255/255f,69/255f,0);
                    child3.GetComponent<TextMeshProUGUI>().color =new Color(255/255f,69/255f,0);
                    break;
            }

            child3.GetComponent<TextMeshProUGUI>().SetText(cosmeticItem.displayName);
        }


        [SerializeField] private Button tournamentStartButton,addRewardBtn;
        
        public void StartTournament()
        {
            tournamentStartButton.interactable = false;
            tourName.interactable = false;
            tourDesc.interactable = false;
            addRewardBtn.interactable = false;
            
            LobbyManager.ServerTournamentStarted = true;
            LobbyManager.ServerTournamentName = new StringBuilder(tourName.text);
            LobbyManager.ServerTournamentDesc = new StringBuilder(tourDesc.text);
            
            SteamMatchmaking.SetLobbyData(lobbyId,"tournament_enable","1");
            SteamMatchmaking.SetLobbyData(lobbyId,"tournament_name",LobbyManager.ServerTournamentName.ToString());
            SteamMatchmaking.SetLobbyData(lobbyId,"tournament_desc",LobbyManager.ServerTournamentDesc.ToString());

            StringBuilder p = new StringBuilder();
            foreach (var prize in LobbyManager.ServerTournamentPrizes)
            {
                p.Append(prize + ";");
            }
            SteamMatchmaking.SetLobbyData(lobbyId,"tournament_prize_pool",p.ToString());
            
        }
    }
}
