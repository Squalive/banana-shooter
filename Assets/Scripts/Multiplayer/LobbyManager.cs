using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Manager;
using Menu;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Multiplayer.Interface;
using Riptide.Transports.Steam;
using Steamworks;
using Steamworks.NET;
using UnityEngine;
using SteamClient = Riptide.Transports.Steam.SteamClient;

namespace Multiplayer
{
    public class LobbyManager : MonoBehaviour
    {
        private static LobbyManager _instance;
        public static LobbyManager Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                    _instance = value;
                else if (_instance != value)
                {
                    Debug.Log($"{nameof(LobbyManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        protected Callback<LobbyCreated_t> lobbyCreated;
        protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
        protected Callback<LobbyEnter_t> lobbyEnter;
        protected Callback<LobbyMatchList_t> lobbyList;
        protected Callback<LobbyDataUpdate_t> lobbyDataUpdated;
        protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;

        public List<CSteamID> lobbyIds = new List<CSteamID>();

        private const string HostAddressKey = "HostAddress";
        internal CSteamID lobbyId;

        public static bool ServerTournamentStarted = false;
        public static StringBuilder ServerTournamentName;
        public static StringBuilder ServerTournamentDesc;
        public static List<int> ServerTournamentPrizes = new List<int>();

        public static bool ClientTournamentStarted = false;
        public static StringBuilder ClientTournamentName;
        public static StringBuilder ClientTournamentDesc;
        public static List<int> ClientTournamentPrizes = new List<int>();

        public static Action<LobbyDataUpdate_t> OnLobbyDataUpdate;
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam is not initialized!");
                return;
            }

            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            lobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
            lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnGetLobbyData);
            lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(PlayerJoinOrLeave);


            int bandwidth = 750000;
            GCHandle receivedData = GCHandle.Alloc(bandwidth, GCHandleType.Pinned);
            IntPtr pointer = receivedData.AddrOfPinnedObject();
            SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin,
                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, pointer);


            SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMax,
                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global, IntPtr.Zero,
                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32, pointer);

            SteamNetworkingUtils.InitRelayNetworkAccess();
        }

        public CSteamID owner;
        private void PlayerJoinOrLeave(LobbyChatUpdate_t param)
        {

            if (!NetworkServerManager.Instance.Server.IsRunning)
            {
                if (owner == (CSteamID)param.m_ulSteamIDUserChanged &&
                    ((EChatMemberStateChange)param.m_rgfChatMemberStateChange ==
                     EChatMemberStateChange.k_EChatMemberStateChangeDisconnected ||
                     (EChatMemberStateChange)param.m_rgfChatMemberStateChange ==
                     EChatMemberStateChange.k_EChatMemberStateChangeLeft))
                {
                    foreach (ClientPlayer player in ClientPlayer.list.Values)
                        Destroy(player.gameObject);

                    ClientPlayer.list.Clear();
                    if (LoadingManager.Instance.menuType == LoadingManager.MenuType.None) LoadingManager.Instance.menuType = LoadingManager.MenuType.HostQuit;
                    LeaveLobby();
                }
                return;
            }
            string state =
                ((EChatMemberStateChange)(param.m_rgfChatMemberStateChange)) ==
                EChatMemberStateChange.k_EChatMemberStateChangeEntered
                    ? "join server"
                    : "left server";
            string content = $"Server : {SteamFriends.GetFriendPersonaName((CSteamID)param.m_ulSteamIDUserChanged)} {state}";
            Chat.Instance.AddMessage(content, Color.yellow);
        }

        public void CreateLobby()
        {
            switch (NetworkServerManager.ServerType)
            {
                case ServerType.Endless:
                    lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
                    break;
                default:
                    lobbyType = NetworkServerManager.ServerGameMode == GameMode.SpecialGameMode
                        ? ELobbyType.k_ELobbyTypePrivate
                        : CreateServerMenu.Instance.lobbyType;
                    break;
            }

            SteamMatchmaking.CreateLobby(lobbyType, CreateServerMenu.Instance.maxPlayer);
        }

        public ELobbyType lobbyType;
        private void OnLobbyCreated(LobbyCreated_t callback)
        {
            if (callback.m_eResult != EResult.k_EResultOK)
            {
                FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                    .GetComponent<FailedWindow>();
                window.SetTitle("Failed");
                window.SetReason(callback.m_eResult.ToString());
                return;
            }

            lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyData(lobbyId, HostAddressKey, SteamUser.GetSteamID().ToString());
            string serverName = string.IsNullOrEmpty(CreateServerMenu.Instance.serverName)
                ? NetworkManager.Instance.PersonalName + "'s Banana Lobby"
                : Chat.Instance.SwearCheck(CreateServerMenu.Instance.serverName);
            SteamMatchmaking.SetLobbyData(lobbyId, "ashdaghj", serverName);
            SteamMatchmaking.SetLobbyData(lobbyId, "Version", Application.version);
            owner = SteamMatchmaking.GetLobbyOwner(lobbyId);
            SteamMatchmaking.SetLobbyData(lobbyId, "LobbyOwner", owner.ToString());

            SteamMatchmaking.SetLobbyData(lobbyId, "ServerType", ((int)NetworkServerManager.ServerType).ToString());

            #region Workshop

            SteamMatchmaking.SetLobbyData(lobbyId, "WorkshopEnable", NetworkServerManager.ServerEnableWorkshop ? "1" : "0");

            // if (NetworkServerManager.ServerEnableWorkshop)
            // { 
            //     StringBuilder workshopMaps = new StringBuilder();
            //
            //     foreach (var id in NetworkServerManager.EnabledWorkshopMaps)
            //     {
            //         workshopMaps.Append(id.ToString());
            //         workshopMaps.Append(';');
            //     }
            //
            //     SteamMatchmaking.SetLobbyData(lobbyId, "workshopmap",workshopMaps.ToString());
            // }

            SteamNetworkingUtils.GetLocalPingLocation(out var result);
            SteamNetworkingUtils.ConvertPingLocationToString(ref result, out var buf, 1024);
            SteamMatchmaking.SetLobbyData(lobbyId, "ping", buf);
            #endregion



            // string weaponSetting = "";
            // foreach (var allowedWeapon in NetworkManager.AllowedWeapon.Values)
            // {
            //     weaponSetting += allowedWeapon ? "1" : "0";
            //     weaponSetting += " ";
            // }
            // SteamMatchmaking.SetLobbyData(lobbyId,"WeaponSetting",weaponSetting);
            SteamFriends.SetRichPresence("server", lobbyId.ToString());
            NetworkManager.Instance.SetCurrentServer(lobbyId.ToString());
            NetworkManager.Instance.connecting = true;
            NetworkServerManager.ClientData.Clear();
            NetworkServerManager.SetIsPlaying(false);
            NetworkServerManager.Instance.SetCheatsEnabled(false);
            NetworkServerManager.Instance.Server.Start(0, (ushort)CreateServerMenu.Instance.maxPlayer, NetworkManager.PlayerHostedDemoMessageHandlerGroupId);

            // NetworkManager.Instance.Client.ChangeTransport(new SteamClient(NetworkServerManager.Instance.CashedSteamServer));
            NetworkManager.Instance.Client.Connect("127.0.0.1", 5, NetworkManager.PlayerHostedDemoMessageHandlerGroupId);

            NetworkServerManager.SetWorkshopMapVote();
        }

        public void JoinLobby(ulong lobbyId)
        {
            SteamMatchmaking.JoinLobby((CSteamID)lobbyId);
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            UIManager.Instance.StartConnecting();
            UIManager.Instance.SetButton(LobbyMenu.Instance.button);
            UIManager.Instance.SetPos2Multiplayer();
            foreach (var go in UIManager.Instance.withoutLoading)
            {
                go.SetActive(false);
            }
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        private void OnLobbyEnter(LobbyEnter_t callback)
        {
            if (NetworkServerManager.Instance.Server.IsRunning)
                return;
            lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            owner = SteamMatchmaking.GetLobbyOwner(lobbyId);
            // Debug.Log(owner.m_SteamID);
            string version = SteamMatchmaking.GetLobbyData(lobbyId, "Version");
            if (version != Application.version)
            {
                LeaveLobby();
                if (UIManager.Instance)
                {
                    UIManager.Instance.disConnectBtn.onClick.Invoke();
                    FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                        .GetComponent<FailedWindow>();
                    window.SetTitle("Join Failed");
                    window.SetReason("Incorrect version");
                }

                return;
            }
            string own = SteamMatchmaking.GetLobbyData(lobbyId, "LobbyOwner");
            if (ulong.TryParse(own, out var checkOwner))
            {

                if (owner.m_SteamID != checkOwner)
                {
                    LeaveLobby();
                    if (UIManager.Instance)
                    {
                        UIManager.Instance.disConnectBtn.onClick.Invoke();
                        FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                            .GetComponent<FailedWindow>();
                        window.SetTitle("Join Failed");
                        window.SetReason("Room doesnt exist anymore");
                    }

                    return;
                }
            }
            else
            {
                LeaveLobby();
                if (UIManager.Instance)
                {
                    UIManager.Instance.disConnectBtn.onClick.Invoke();
                    FailedWindow window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                        .GetComponent<FailedWindow>();
                    window.SetTitle("Join Failed");
                    window.SetReason("Room doesnt exist anymore");
                }

                return;
            }
            SteamFriends.SetRichPresence("server", lobbyId.ToString());
            Connect();
        }

        void Connect()
        {
            NetworkManager.Instance.SetCurrentServer(lobbyId.ToString());
            hostAddress = SteamMatchmaking.GetLobbyData(lobbyId, HostAddressKey);

            NetworkManager.Instance.connecting = true;
            // NetworkManager.Instance.Client.ChangeTransport(new SteamClient(NetworkServerManager.Instance.CashedSteamServer));
            NetworkManager.Instance.Client.Connect(hostAddress, 5, NetworkManager.PlayerHostedDemoMessageHandlerGroupId);
        }

        [HideInInspector]
        public string hostAddress;

        public void LeaveLobby()
        {
            SteamMatchmaking.LeaveLobby(lobbyId);
            NetworkManager.Instance.DisconnectClient();
            NetworkServerManager.Instance.StopServer();
            NetworkManager.Instance.StopServer();
            owner = CSteamID.Nil;
            lobbyId = CSteamID.Nil;
        }

        public ELobbyDistanceFilter filter = ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault;
        public void GetLobbiesList(ELobbyDistanceFilter f)
        {
            BrowseServerMenu.Instance.CleanUp();
            if (lobbyIds.Count > 0)
            {
                lobbyIds.Clear();
            }

            SteamMatchmaking.AddRequestLobbyListDistanceFilter(f);
            SteamMatchmaking.AddRequestLobbyListResultCountFilter(500);
            SteamMatchmaking.RequestLobbyList();
        }
        void OnGetLobbyList(LobbyMatchList_t result)
        {
            for (int i = 0; i < result.m_nLobbiesMatching; i++)
            {
                CSteamID id = SteamMatchmaking.GetLobbyByIndex(i);
                lobbyIds.Add(id);
                SteamMatchmaking.RequestLobbyData(id);
            }
        }

        void OnGetLobbyData(LobbyDataUpdate_t result)
        {
            BrowseServerMenu.Instance.DisplayLobbiesP2P(lobbyIds, result);
        }

        public void SetLobbyGameMode()
        {
            if (!SteamManager.Initialized) return;
            if (owner.m_SteamID == Chat.Instance.steamId)
            {
                SteamMatchmaking.SetLobbyData(lobbyId, "GameMode", NetworkServerManager.ServerGameMode.ToString());
            }
        }

        public void SetLobbyType(ELobbyType type)
        {
            if (!SteamManager.Initialized) return;
            if (SteamMatchmaking.SetLobbyType(lobbyId, type))
                Debug.Log($"Change lobby type to {type} successfully");
        }
    }
}
