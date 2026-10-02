using System;
using System.Collections.Generic;
using System.Text;

using Manager;
using Multiplayer;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    [Serializable]
    public class ServerFilter
    {
        
        public enum SearchServerType
        {
            DlcOnly,
            Official,
            P2P,
            Community,
            Favourite,
            Friend,
            History,
            Lan,
        }
        //Search which type of server players need
        public SearchServerType searchServerType = SearchServerType.Official;
        
        //The server name players want to search
        public string searchServerName= String.Empty;

        public ServerType serverType = ServerType.None;

        //Closed Server Only
        public bool closedServerOnly = true;
        //Hot Server is the player amount / max players is bigger than 0.5f 
        public bool isHotServer=false;
        //Server is not full
        public bool isNotFull = false;
    }
    public class BrowseServerMenu : SingletonObject<BrowseServerMenu>
    {
        public static ServerFilter ServerFilter { get; } = new ServerFilter();

        [SerializeField] private TMP_InputField searchServerNameInput;

        [SerializeField] private Toggle closeServerOnlyToggle, hotServerToggle,notFullToggle;

        [SerializeField] private Transform content;

        [SerializeField] private SearchServerTypeItem[] serverTypeItems = new SearchServerTypeItem[6];

        private bool _ownedDlc;
        
        private SearchLobbyType _searchLobbyType;
        
        public Dictionary<CSteamID, ServerItem> ActiveServerItems = new();

        private Dictionary<CSteamID, ServerDetail> _playerHostedServer = new();
        private Dictionary<CSteamID, ServerDetail> _lanServer = new();
        private Dictionary<CSteamID, ServerDetail> _favouriteServers = new();
        private Dictionary<CSteamID, ServerDetail> _officialServers = new();
        private Dictionary<CSteamID, ServerDetail> _communityServers = new();
        private Dictionary<CSteamID, ServerDetail> _historyServers = new();
        private Dictionary<CSteamID, ServerDetail> _dlcOnlyServer = new();
        private Dictionary<CSteamID, ServerDetail> _friendServer = new();

        private bool IsQuickMatch { get; set; } = false;

        private void Awake()
        {
            Instance = this;

            _ownedDlc = SteamApps.BIsDlcInstalled(new AppId_t(2238100));
            
            //Reset the closed server only toggle
            closeServerOnlyToggle.isOn =
                LobbyManager.Instance.filter == ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault;
            closeServerOnlyToggle.onValueChanged.AddListener(SetCloseServerOnly);
            
            hotServerToggle.SetIsOnWithoutNotify(ServerFilter.isHotServer);
            hotServerToggle.onValueChanged.AddListener(SetHotServer);
            
            notFullToggle.SetIsOnWithoutNotify(ServerFilter.isNotFull);
            notFullToggle.onValueChanged.AddListener(SetNotFull);
            
            searchServerNameInput.SetTextWithoutNotify(ServerFilter.searchServerName);
            searchServerNameInput.onEndEdit.AddListener(SetSearchName);

            for (int i = 0; i < serverTypeItems.Length; i++)
            {
                int index = i;
                serverTypeItems[i].toggle.SetIsOnWithoutNotify((int)ServerFilter.searchServerType == index);
                
                serverTypeItems[i].toggle.onValueChanged.AddListener(delegate { SetServerType(index); });
                
                serverTypeItems[i].serverAmountText.StringReference.Arguments =
                    new List<object>() {0};
                serverTypeItems[i].serverAmountText.RefreshString();

                serverTypeItems[i].playerAmountText.StringReference.Arguments =
                    new List<object>() {0};
                serverTypeItems[i].playerAmountText.RefreshString();
            }
        }

        private void OnEnable()
        {
            ServerManager.Instance.OnServerRespond += DisplayServer;
        }

        private void OnDisable()
        {
            ServerManager.Instance.OnServerRespond -= DisplayServer;
        }

        
        public void RefreshServerList()
        {
            IsQuickMatch = false;
            _searchLobbyType = SearchLobbyType.Normal;
            
            for (int i = 0; i < serverTypeItems.Length; i++)
            {
                serverTypeItems[i].serverAmountText.StringReference.Arguments =
                    new List<object>() {0};
                serverTypeItems[i].serverAmountText.RefreshString();

                serverTypeItems[i].playerAmountText.StringReference.Arguments =
                    new List<object>() {0};
                serverTypeItems[i].playerAmountText.RefreshString();
            }
            
            LobbyManager.Instance.GetLobbiesList(LobbyManager.Instance.filter);
            
            ServerManager.Instance.RefreshServer();
        }
        
        
        public void QuickMatch()
        {
            IsQuickMatch = true;
            _searchLobbyType = SearchLobbyType.Normal;

            LobbyManager.Instance.GetLobbiesList(LobbyManager.Instance.filter);
            
            ServerManager.Instance.RefreshServer();
        }
        
        void RefreshTable()
        {
            DestroyLobbies();

            Dictionary<CSteamID, ServerDetail> details = _lanServer;

            switch (ServerFilter.searchServerType)
            {
                case ServerFilter.SearchServerType.P2P:
                    details = _playerHostedServer;
                    break;
                case ServerFilter.SearchServerType.Lan:
                    details = _lanServer;
                    break;
                case ServerFilter.SearchServerType.Favourite:
                    details = _favouriteServers;
                    break;
                case ServerFilter.SearchServerType.Official:
                    details = _officialServers;
                    break;
                case ServerFilter.SearchServerType.Community:
                    details = _communityServers;
                    break;
                case ServerFilter.SearchServerType.History:
                    details = _historyServers;
                    break;
                case ServerFilter.SearchServerType.DlcOnly:
                    details = _dlcOnlyServer;
                    break;
                case ServerFilter.SearchServerType.Friend:
                    details = _friendServer;
                    break;
            }
            foreach (var detail in details.Values)
            {
                if (IsAvailable(detail))
                {
                    ServerItem createdItem = Instantiate(PrefabManager.Instance.serverPrefab,content);
                                                    
                    createdItem.SetPlayerValues(detail);
                            
                    ActiveServerItems.Add(detail.Id,createdItem);
                }
            }
        }
        
        public void DisplayLobbiesP2P(List<CSteamID> lobbyIds,LobbyDataUpdate_t result)
        {
            for (int i = 0; i < lobbyIds.Count; i++)
            {
                switch (_searchLobbyType)
                {
                    case SearchLobbyType.Normal:
                        CSteamID id = (CSteamID) result.m_ulSteamIDLobby;
                        if (lobbyIds[i].m_SteamID == result.m_ulSteamIDLobby && !_playerHostedServer.ContainsKey(id))
                        {
                            string temp = SteamMatchmaking.GetLobbyData(id, "ServerType");
                            string serverMode="";
                            if (int.TryParse(temp, out int t))
                            {
                                ServerType type = (ServerType) t;
                                if (ServerType.None != ServerFilter.serverType&&
                                    type!=ServerFilter.serverType)
                                {
                                    continue;
                                }
                                serverMode = type.ToString();

                                // if (IsQuickMatch)
                                // {
                                //     IsQuickMatch = false;
                                //     LobbyMenu.Instance.JoinLobby(id.m_SteamID);
                                //     return;
                                // }
                            }
                            else
                            {
                                continue;
                            }
                            string serverName = SteamMatchmaking.GetLobbyData(id, "ashdaghj");
                            string gameMode = SteamMatchmaking.GetLobbyData(id, "GameMode");
    
                            bool workshopEnable =
                                SteamMatchmaking.GetLobbyData((CSteamID) result.m_ulSteamIDLobby,
                                    "WorkshopEnable") == "1";
                       
                            if (string.IsNullOrEmpty(serverName)
                                ||string.IsNullOrEmpty(gameMode)) continue;

                            string p = SteamMatchmaking.GetLobbyData(id, "ping");

                            int ping = -1;

                            if (SteamNetworkingUtils.ParsePingLocationString(p, out var pingLocation))
                            {
                                SteamNetworkingUtils.GetLocalPingLocation(out var localPingLocation);

                                ping = SteamNetworkingUtils.EstimatePingTimeBetweenTwoLocations(ref pingLocation,
                                    ref localPingLocation);
                            }

                            ServerDetail serverDetail = new ServerDetail(id, workshopEnable,
                                SteamMatchmaking.GetNumLobbyMembers(id),
                                SteamMatchmaking.GetLobbyMemberLimit(id), serverName,
                                SteamMatchmaking.GetLobbyData(id, "Version"), gameMode, serverMode,
                                SteamMatchmaking.GetLobbyData(id, "tournament_enable") == "1",ServerFilter.SearchServerType.P2P,ping,true,String.Empty);

                            if (IsAvailable(serverDetail))
                            {
                                ServerItem createdItem = Instantiate(PrefabManager.Instance.serverPrefab,content);
                                                    
                                createdItem.SetPlayerValues(serverDetail);
                                
                                ActiveServerItems.Add(id,createdItem);
                            }

                            _playerHostedServer.Add(id,serverDetail);
                            
                            int j = (int) ServerFilter.SearchServerType.P2P;
                            serverTypeItems[j].serverAmountText.StringReference.Arguments =
                                new List<object>() {_playerHostedServer.Count};
                            serverTypeItems[j].serverAmountText.RefreshString();

                            int playerAmount = 0;
                            foreach (var detail in _playerHostedServer.Values)
                            {
                                playerAmount += detail.PlayerAmount;
                            }
                            
                            serverTypeItems[j].playerAmountText.StringReference.Arguments =
                                new List<object>() {playerAmount};
                            serverTypeItems[j].playerAmountText.RefreshString();
                        }
                        break;
                    case SearchLobbyType.Tournament:
                        if (lobbyIds[i].m_SteamID == result.m_ulSteamIDLobby)
                        {
                            TournamentMenu.Instance.CheckTournament(lobbyIds[i]);
                        }
                        break;
                }
            }
        
        }
        
        void DisplayServer(gameserveritem_t gameserveritemT,ServerFilter.SearchServerType type)
        {
            string[] tags = gameserveritemT.GetGameTags().Split(';');

            string serverMode = ServerType.Normal.ToString();

            bool workshopEnable =false;
            
            string descShort = String.Empty;
            if (tags.Length >= 2)
            {
                if (int.TryParse(tags[0], out var index))
                {
                    serverMode = ((ServerType) index).ToString();
                }
                
                workshopEnable = tags[1] == "1";
                
                if (tags.Length >= 4)
                {
                    descShort = tags[3];
                }
            }

            StringBuilder version = new StringBuilder(gameserveritemT.m_nServerVersion.ToString());

            version = version.Insert(1, '.');

            ServerDetail serverDetail = new ServerDetail(gameserveritemT.m_steamID, workshopEnable,
                gameserveritemT.m_nPlayers,
                gameserveritemT.m_nMaxPlayers, gameserveritemT.GetServerName(),
                version.ToString(), "", serverMode,
                false,type,gameserveritemT.m_nPing,false,descShort)
            {
                NetAdr = gameserveritemT.m_NetAdr,
                LastTimePlayed = gameserveritemT.m_ulTimeLastPlayed,
                Favourited = type == ServerFilter.SearchServerType.Favourite || _favouriteServers.ContainsKey(gameserveritemT.m_steamID)
            };

            if (IsAvailable(serverDetail) && !ActiveServerItems.ContainsKey(serverDetail.Id))
            {
                if (IsQuickMatch)
                {
                    IsQuickMatch = false;
                    ServerManager.Instance.Connect(serverDetail.NetAdr.GetIP(),serverDetail.NetAdr.GetConnectionPort()); 
                    return;
                }
                ServerItem createdItem = Instantiate(PrefabManager.Instance.serverPrefab,content);
                    
                createdItem.SetPlayerValues(serverDetail);
                ActiveServerItems.Add(serverDetail.Id,createdItem);
            }

            Dictionary<CSteamID, ServerDetail> dictionary = _lanServer;
            
            switch (type)
            {
                case ServerFilter.SearchServerType.Official:
                    dictionary = _officialServers;
                    break;
                case ServerFilter.SearchServerType.Favourite:
                    dictionary = _favouriteServers;
                    break;
                case ServerFilter.SearchServerType.Community:
                    dictionary = _communityServers;
                    break;
                case ServerFilter.SearchServerType.History:
                    dictionary = _historyServers;
                    break;
                case ServerFilter.SearchServerType.DlcOnly:
                    dictionary = _dlcOnlyServer;
                    break;
                case ServerFilter.SearchServerType.Friend:
                    dictionary = _friendServer;
                    break;
            }

            if (dictionary.ContainsKey(serverDetail.Id))
                dictionary.Remove(serverDetail.Id);
            dictionary.Add(serverDetail.Id, serverDetail);

            int j = (int) type;
            serverTypeItems[j].serverAmountText.StringReference.Arguments =
                new List<object>() {dictionary.Count};
            serverTypeItems[j].serverAmountText.RefreshString();
            
            int playerAmount = 0;
            foreach (var detail in dictionary.Values)
            {
                playerAmount += detail.PlayerAmount;
            }
            
            serverTypeItems[j].playerAmountText.StringReference.Arguments =
                new List<object>() {playerAmount};
            serverTypeItems[j].playerAmountText.RefreshString();
            
        }
        static bool IsAvailable(ServerDetail detail)
        {
            bool flag = true;
            if (ServerFilter.searchServerType == detail.SearchServerType)
            {
                if (ServerFilter.isHotServer)
                {
                    flag &= (float) detail.PlayerAmount / 15 > 0.3f;
                }

                if (ServerFilter.isNotFull)
                {
                    flag &= detail.PlayerAmount < detail.PlayerMaxAmount;
                }

                flag &= detail.Name.Trim().ToLower().Contains(ServerFilter.searchServerName);
            }
            else
            {
                flag = false;
            }

            return flag;
        }

        public void CleanUp()
        {
            _playerHostedServer.Clear();
            _lanServer.Clear();
            _favouriteServers.Clear();
            _officialServers.Clear();
            _communityServers.Clear();
            _dlcOnlyServer.Clear();
            _friendServer.Clear();
            DestroyLobbies();
        }

        void DestroyLobbies()
        {
            foreach (var activeServerItem in ActiveServerItems.Values)
            {
                Destroy(activeServerItem.gameObject);
            }
            ActiveServerItems.Clear();
        }

        

        #region SetValues

        void SetServerType(int index)
        {
            ServerFilter.searchServerType = (ServerFilter.SearchServerType) (index);

            if (ServerFilter.searchServerType == ServerFilter.SearchServerType.DlcOnly && !_ownedDlc)
                ServerFilter.searchServerType = ServerFilter.SearchServerType.Official;

            RefreshTable();
        }
        void SetCloseServerOnly(bool flag)
        {
            //Set Filter
            LobbyManager.Instance.filter = flag
                ? ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault
                : ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide;
            _searchLobbyType = SearchLobbyType.Normal;
            LobbyManager.Instance.GetLobbiesList(LobbyManager.Instance.filter);
            ServerManager.Instance.RefreshServer();
        }
        void SetHotServer(bool flag)
        {
            ServerFilter.isHotServer = flag;
            
            RefreshTable();
        }
        void SetNotFull(bool flag)
        {
            ServerFilter.isNotFull = flag;
            
            RefreshTable();
        }

        private void SetSearchName(string str)
        {
            ServerFilter.searchServerName = str;
            
            RefreshTable();
        }
        #endregion
       
        [Serializable]
        public class SearchServerTypeItem
        {
            public Toggle toggle;
            public LocalizeStringEvent playerAmountText, serverAmountText;
        }
    }
}
