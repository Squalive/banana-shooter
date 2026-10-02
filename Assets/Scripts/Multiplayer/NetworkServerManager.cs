using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using Map;
using Mode;
using Multiplayer.Entity.Interface;
using Multiplayer.Entity.Server;
using Multiplayer.Entity.Server.Enemy;
using Multiplayer.Interface;
using Multiplayer.LagCompensation;
using Multiplayer.Server;
using Multiplayer.ServerTypes;
using Pool;
using Riptide;
using Riptide.Transports.Steam;
using Riptide.Utils;
using Steamworks;
using Steamworks.NET;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using Weapon;
using Weapon.WeaponStats;
using Random = UnityEngine.Random;

namespace Multiplayer
{
    public class ClientData
    {
        public ushort Id { get; }
        public bool Eliminated { get; private set; }
        public bool OwnedDlc { get; private set; }
        public string Name { get; }
        public ulong SteamId { get; }
        public ulong GroupId { get; }
        public string Desc { get; }
        public int Exp { get; }
        public bool DisplayTag { get; private set; }
        public int DesiredTeam { get; set; } = -1;

        public short[] Weapons { get; set; }

        public InventoryManager.CosmeticIndex CosmeticIndex;
        public List<Perk> Perks { get; set; } = new(){Perk.None,Perk.None,Perk.None} ;

        public int InventoryLength { get; set; } = -1;

        public byte[] SerializeInventory { get; set; }

        public bool SerializeInventoryInitialized { get; set; } = false;
        
        public ClientData(ushort id,string name,ulong steamId, bool ownedDlc,ulong groupId,string desc,int exp,bool displayTag, bool eliminated,short[] weapons,ushort[] perks)
        {
            Id = id;
            OwnedDlc = ownedDlc;
            Name = name;
            SteamId = steamId;
            GroupId = groupId;
            Desc = desc;
            Exp = exp;
            DisplayTag = displayTag;
            Weapons = weapons;
            Eliminated = eliminated;
            for (int i = 0; i < perks.Length; i++)
            {
                Perks[i] = (Perk) perks[i];
            }
        }
        
        public void InitializeCosmetics(InventoryManager.CosmeticIndex cosmeticIndex)
        {
            CosmeticIndex = cosmeticIndex;
            
            SendToAllClient();
        }

        public ushort GetIndex()
        {
            var index = NetworkServerManager.ClientData.Where(pair => pair.Value == this).Select(pair => pair.Key).FirstOrDefault();

            return index;
        }

        public void SetDisplayTag(bool t)
        {
            DisplayTag = t;
        }

        public void SetEliminated(bool eliminated)
        {
            Eliminated = eliminated;
        }

        public void SendEliminated()
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.ClientEliminated);

            message.Add(Id);
            message.Add(Eliminated);
            
            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        public void SendToClient(ushort client)
        {
            Message message = GetMessage();
            
            NetworkServerManager.Instance.Server.Send(message,client);
        }
        
        void SendToAllClient()
        {
            Message message = GetMessage();
            
            NetworkServerManager.Instance.Server.SendToAll(message);

            if (NetworkServerManager.GameState == GameState.Voting)
            {
                Message addplayerVotingMessage = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.AddVotingPlayer);

                addplayerVotingMessage.AddUShort(Id);
                // addplayerVotingMessage.Add(SteamId);
                addplayerVotingMessage.AddUInt(LobbyDataManager.datas[SteamId].kills);
                // addplayerVotingMessage.Add(LobbyDataManager.datas[SteamId].exp);

                NetworkServerManager.Instance.Server.SendToAll(addplayerVotingMessage, Id);
            }
        }

        Message GetMessage()
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.AddClientData);

            message.Add(Id);
            message.Add(Name);
            message.Add(SteamId);
            message.Add(GroupId);
            message.Add(OwnedDlc);
            message.Add(Desc);
            message.Add(Exp);
            message.Add(DisplayTag);
            message.Add(Eliminated);
            message.Add(Weapons);

            ushort[] perks = new ushort[3];

            for (int i = 0; i < perks.Length; i++)
            {
                perks[i] = (ushort)Perks[i];
            }

            message.Add(perks);
            message.Add(CosmeticIndex);

            return message;
        }

        public void Remove()
        {
        }
    }

    public enum GameState
    {
        None=0,
        Voting,
        Loading,
        Warmup,
        MidMatch,
    }
    
    [DefaultExecutionOrder(-105)]
    public class NetworkServerManager : MonoBehaviour, INetworkServer
    {
        public const byte PlayerHostedDemoMessageHandlerGroupId = 255;

        private static NetworkServerManager _instance;
        public static INetworkServer Iinstance { private set; get; }

        public static NetworkServerManager Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                {
                    _instance = value;
                    Iinstance = value;
                }
                else if (_instance != value)
                {
                    Debug.Log($"{nameof(NetworkServerManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }
        
        //Server
        public Riptide.Server Server { get; private set; }

        public SteamServer CashedSteamServer { get; private set; }
        
        //The type of this server
        public static ServerType ServerType { get;private set; } = ServerType.Normal;
        
        //The current game mode of the server
        public static GameMode ServerGameMode { get; set; } = GameMode.None;
        
        //Cheats
        public static bool CheatsEnabled { get; private set; } = false;

        //The current voted player amount 
        public static int VoteCount { get; set; } = 0;

        public static bool IsPlaying { get; private set; } = false;

        private const float MaxWallBangDistance = 3f;
        
        //The banned player from server
        public static List<ulong> BannedPlayer = new List<ulong>();
        //The weapons that server allows to use
        public static List<bool> AllowedWeapon = new List<bool>();
        //Store the client datas
        public static Dictionary<ushort, ClientData> ClientData = new Dictionary<ushort, ClientData>();
        
        //A dictionary of map voting
        public static Dictionary<string, ushort> MapVote = new Dictionary<string, ushort>();
        //A dictionary of gamemode voting
        public static Dictionary<ushort, ushort> GameModeVote = new Dictionary<ushort, ushort>();
        
        //A dictionary of workshop map voting
        public static Dictionary<ulong, ushort> WorkshopMapVote = new();

        private static Dictionary<ulong, EUserHasLicenseForAppResult> _userHasDlc = new();
        
        public static bool ServerRandomMap = false, ServerRandomGameMode = false;
        
        //A Boolean which set the special weapon enable or disable
        public static bool ServerDisableSpecialWeapon { get; set; } = false;
        
        
        //The Server player prefab
        [SerializeField] public GameObject serverPlayerPrefab;
        public GameObject ServerPlayerPrefab => serverPlayerPrefab;
        
        //Maps
        public Dictionary<GameMode, List<Map.Map>> Maps { get; } = new();

        //The current gamemode going on
        public GameModes game;

        //A list of the weapon
        public List<WeaponStat> weaponInfo = new List<WeaponStat>();

        #region Tick

        public uint CurrentTick { get; private set; } = 0;

        #endregion

        #region Workshop
        
        //Workshop supported?
        public static bool ServerEnableWorkshop { get; private set; } = false;

        public static HashSet<PublishedFileId_t> EnabledWorkshopMaps = new HashSet<PublishedFileId_t>();

        public static Action OnEnabledMapsChanged;
        
        public PublishedFileId_t WorkshopMap { private set; get; }
        
        public bool IsWorkshopMap { private set; get; }

        #endregion

        public static IServerType CurrentServer { get; private set; }

        /// <summary>
        /// Entity
        /// </summary>
        public static Entity.Entity Entity = new();

        public static GameState GameState { private set; get; } = GameState.None;
        
        public string CurrentMap { private set; get; } = String.Empty;
        
        static readonly List<CSteamID> AuthorizedUsers = new();
        
        public static Dictionary<CSteamID, Tuple<ushort,string>> SteamIDToClient = new Dictionary<CSteamID, Tuple<ushort,string>>();
        
        protected Callback<ValidateAuthTicketResponse_t> m_ValidateAuthTicketResponse;

        private float _votingTime;

        public static Dictionary<ushort, Tuple<short, string>> PlayerVoteList = new();

        private static Queue<ushort> _ids = new();

        #region Unity Function

        private void Awake()
        {
            Instance = this;
            if (Instance == this)
            {
                //Special Gamemode
                Maps.Add(GameMode.SpecialGameMode,new List<Map.Map>(){MapManager.Instance.NameToMap["Endless"]});
                
                //Special Normal Gamemode
                Maps.Add(GameMode.SpecialNormalGameMode, new List<Map.Map>(){MapManager.Instance.NameToMap["ShootingRange"]});
                
                //Brawl
                List<Map.Map> m = new List<Map.Map>();   

                for (int i = 2; i < MapManager.Instance.maps.Count; i++)
                {
                    m.Add(MapManager.Instance.maps[i]);
                }
                
                Maps.Add(GameMode.Brawl,m);
                
                //TDM
                Maps.Add(GameMode.TeamDeathMatch,m);

                //INfected
                Maps.Add(GameMode.Infected,m);
                
                //Kill Confirm
                Maps.Add(GameMode.KillConfirm,m);

                //Randomizer
                Maps.Add(GameMode.Randomizer,m);
                
                //King of the hill
                Maps.Add(GameMode.KingOfTheHill,m);
                
                //Gun Game
                Maps.Add(GameMode.GunGame,m);
                
                //Catch The Banana
                Maps.Add(GameMode.CatchTheBanana,m);
                
                //One shoe one kill
                Maps.Add(GameMode.OneShotOneKill,m);
                
                //Rocket mode
                Maps.Add(GameMode.RocketMode,m);
                
                // PVE
                // Maps.Add(GameMode.PVE,m);
                
                foreach (var map in MapManager.Instance.maps)
                {
                    MapVote.TryAdd(map.name, 0);
                }

                GameModeVote.Add((ushort)GameMode.Brawl, 0);
                GameModeVote.Add((ushort)GameMode.CatchTheBanana, 0);
                GameModeVote.Add((ushort)GameMode.TeamDeathMatch, 0);
                GameModeVote.Add((ushort)GameMode.Infected, 0);
                GameModeVote.Add((ushort)GameMode.KillConfirm, 0);
                GameModeVote.Add((ushort)GameMode.Randomizer, 0);
                GameModeVote.Add((ushort)GameMode.KingOfTheHill, 0);
                GameModeVote.Add((ushort)GameMode.GunGame, 0);
                GameModeVote.Add((ushort)GameMode.OneShotOneKill, 0);
                GameModeVote.Add((ushort)GameMode.RocketMode, 0);
                // GameModeVote.Add((ushort)GameMode.PVE, 0);

                foreach (var _ in weaponInfo)
                {
                    AllowedWeapon.Add(true);
                }
            }
        }

        private void Start()
        {
            if (!SteamManager.Initialized)
            {
                return;
            }
            
#if UNITY_EDITOR
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
#else
            RiptideLogger.Initialize(Debug.Log, true);
#endif
            
            m_ValidateAuthTicketResponse = Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicketResponse);
            
            InitGame();
        }

        private void Update()
        {
            if (!SteamManager.Initialized) return;
            if (GameState == GameState.Voting && ClientData.Count >= CurrentServer.OverrideMinimalPlayerCount(2))
            {
                _votingTime -= Time.deltaTime;

                if (_votingTime <= 0)
                {
                    SendStartGame();
                }
            }
        }

        private void FixedUpdate()
        {
            if (!SteamManager.Initialized) return;
            
            if (Server.IsRunning)
            {
                Server.Update();
                if (CurrentTick % uint.MaxValue == 0)
                {
                    CurrentTick = 0;
                    Iinstance.SendSync();

                    foreach (var player in ServerPlayer.list.Values)
                    {
                        foreach (var weapon in player.ActiveWeapons)
                        {
                            weapon.ResetTick();
                        }
                    }
                }

                if (CurrentTick % 200 == 0)
                    Iinstance.SendSync();

                CurrentTick++;
            }
        }

        private void OnApplicationQuit()
        {
            Server.ClientConnected -= NewPlayerConnect;
            Server.ClientDisconnected -= ClientDisconnect;
        }

        #endregion

        #region Networking
        
        void InitGame()
        {
            CashedSteamServer = new SteamServer();
            Server = new Riptide.Server(CashedSteamServer);

            Server.ClientConnected += NewPlayerConnect;
            Server.ClientDisconnected += ClientDisconnect;
            
            InitializeIds();
        }
        
        public void StopServer()
        {
            foreach (var id in AuthorizedUsers)
            {
                SteamUser.EndAuthSession(id);
            }
            AuthorizedUsers.Clear();
            Server.Stop();
            SetGameState(GameState.None,false);
            ServerGameMode = GameMode.Brawl;
            SetServerType(ServerType.Normal);
            BannedPlayer.Clear();
            SteamIDToClient.Clear();
            _userHasDlc.Clear();
        }

        private static void InitializeIds()
        {
            for (ushort i = 41; i < ushort.MaxValue; i++)
            {
                _ids.Enqueue(i);
            }
        }

        public static ushort GetId()
        {
            return _ids.Dequeue();
        }

        public static void AddEntity(ushort id, IEntity entity)
        {
            Entity.Entities[id] = entity;
        }

        public static void RemoveEntity(ushort id)
        {
            _ids.Enqueue(id);

            Entity.Entities.Remove(id);
        }

        #region Pakcet Sent

        public void SendSync()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.Sync);
            message.AddUInt(CurrentTick);

            Server.SendToAll(message);
        }
        
        public void StartRound()
        {
            Server.SendToAll(GetRoundMessage());
        }
        public void StartRound(ushort toClient)
        {
            Server.Send(GetRoundMessage(),toClient);
        }

        Message GetRoundMessage()
        {
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.StartRound);

            message.Add(game.EndTick);

            return message;
        }
        
        public bool SetInfectedPlayer(ushort id, ushort fromClient)
        {
            if (ServerPlayer.list.TryGetValue(id,out var infectedPlayer))
            {
                infectedPlayer.Kills = 0;
                infectedPlayer.IsInfected = true;
                infectedPlayer.MaxHealth = 200;
                if (infectedPlayer.HasPerk(Perk.Fat))
                    infectedPlayer.MaxHealth =
                        (int) (infectedPlayer.MaxHealth * PerkManager.FatMultiplier);
                infectedPlayer.Health = infectedPlayer.MaxHealth;


                Message message = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.PlayerInfect);

                message.Add(id);
                message.Add(fromClient);

                Server.SendToAll(message);

                int cnt = 0;
                foreach (var player in ServerPlayer.list.Values)
                {
                    if (player.IsInfected) cnt++;
                }

                if (cnt == ServerPlayer.list.Count)
                {
                    StopGame();
                }

                return true;
            }

            return false;
        }

        public void BananaManMusic()
        {
            
        }

        public void SendServerSetting(string serverName)
        {
            Message message = Message.Create(MessageSendMode.Reliable,(ushort)ServerToClientId.ServerSetting);
            message.Add(ServerRandomMap);
            message.Add(ServerRandomGameMode);
            message.Add(ServerDisableSpecialWeapon);
            message.Add(serverName);
            Server.SendToAll(message);
        }
        
        public void SendClientInitialized(ushort client)
        {
            Message msg;
            switch (ServerType)
            {
                case ServerType.ShootingRange:
                    msg = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.OtherMode);

                    msg.Add("ShootingRange");

                    Server.Send(msg, client);
                    break;
                case ServerType.Endless:
                    msg = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.OtherMode);

                    msg.Add("Endless");

                    Server.Send(msg, client);
                    break;
                default:
                    if (GameState == GameState.Voting)
                    {
                        if (!VerifyVote())
                        {
                            msg = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.Voting);

                            msg.Add(_votingTime);

                            msg.AddUShort(CurrentServer.OverrideMinimalPlayerCount(2));

                            msg.Add((ushort) ClientData.Count);

                            foreach (var d in ClientData.Values)
                            {
                                if (LobbyDataManager.datas.TryGetValue(d.SteamId, out var lobbyData))
                                {
                                    msg.Add(d.Id);
                                    msg.Add(lobbyData.kills);
                                }
                            }

                            Server.Send(msg, client);
                        }
                    }
                    else if(GameState == GameState.MidMatch)
                    {
                        msg = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.StartGame);

                        msg.Add((ushort)GameState);
                        msg.Add(IsWorkshopMap);
                        
                        msg.Add((ushort) ServerGameMode);
                        msg.Add(IsWorkshopMap
                            ? WorkshopMap.ToString()
                            : CurrentMap);
                        if (ServerGameMode == GameMode.KingOfTheHill)
                        {
                            msg.Add(KingOfTheHill.Instance.serverIndex);
                        }

                        if ( CurrentMap == "ProcMap" )
                        {
                            msg.Add( Seed );
                        }
                        if (IsWorkshopMap)
                        {
                            if (MapSaver.WorkshopMaps.TryGetValue(WorkshopMap, out var file))
                            {
                                msg.Add(file.GetMD5ChecksumForFile());
                            }
                        }
                        Server.Send(msg, client);
                    }
                    break;
            }
        }
        #endregion

        #region Packet Handle
        
        [MessageHandler((ushort) ClientToServerId.VoteMap, PlayerHostedDemoMessageHandlerGroupId)]
        private static void VoteMap(ushort fromClient, Message message)
        {
            if (ServerRandomMap) return;
            if (GameState != GameState.Voting) return;

            if (PlayerVoteList.TryGetValue(fromClient,out var item))
            {
                bool workshopMap = message.GetBool();
                string map = message.GetString();
                ushort lastMapVoteCount = 0;
                ushort currnetMapVoteCount = 0;
                if (workshopMap)
                {
                    ulong id ;
                    if(!string.IsNullOrEmpty(item.Item2))
                    {
                        id = ulong.Parse(item.Item2);
                        if(WorkshopMapVote.ContainsKey(id))
                            lastMapVoteCount = --WorkshopMapVote[id];
                    }
                    id = ulong.Parse(map);
                    if (WorkshopMapVote.ContainsKey(id))
                    {
                        currnetMapVoteCount = ++WorkshopMapVote[id];
                    }
                }
                else
                {
                    if(!string.IsNullOrEmpty(item.Item2))
                    {
                        if(MapVote.ContainsKey(item.Item2))
                            lastMapVoteCount = --MapVote[item.Item2];
                    }
                    if (MapVote.ContainsKey(map))
                    {
                        currnetMapVoteCount = ++MapVote[map];
                    }
                }

                if (string.IsNullOrEmpty(item.Item2))
                    VoteCount++;
            
                Message msg = Message.Create(MessageSendMode.Reliable,(ushort)ServerToClientId.VoteMap);
                msg.Add(item.Item2);
                msg.Add(map);
                msg.Add(lastMapVoteCount);
                msg.Add(currnetMapVoteCount);

                PlayerVoteList[fromClient] = new Tuple<short, string>(item.Item1, map);
                Instance.Server.SendToAll(msg);
                
                Instance.VerifyVote();
            }
        }
        
        [MessageHandler((ushort) ClientToServerId.VoteGameMode, PlayerHostedDemoMessageHandlerGroupId)]
        private static void VoteGameMode(ushort fromClient, Message message)
        {
            if (ServerRandomGameMode) return;
            if (GameState != GameState.Voting) return;
            if (PlayerVoteList.TryGetValue(fromClient,out var item))
            {
                ushort gameMode = message.GetUShort();
                ushort lastVoteCount = 0, currentVoteCount = 0;
                if(item.Item1 != -1 && GameModeVote.ContainsKey((ushort)item.Item1))
                    lastVoteCount = --GameModeVote[(ushort)item.Item1];

                if(GameModeVote.ContainsKey(gameMode))
                    currentVoteCount = ++GameModeVote[gameMode];
                if (item.Item1==-1)
                    VoteCount++;

                //Debug.Log($"{gameMode} : {NetworkServerManager.Instance.gameModeVote[gameMode]}");
            
                Message msg = Message.Create(MessageSendMode.Reliable,(ushort)ServerToClientId.VoteGameMode);
                msg.Add(item.Item1);
                msg.Add(gameMode);
                msg.AddUShort(lastVoteCount);
                msg.AddUShort(currentVoteCount);
                PlayerVoteList[fromClient] = new Tuple<short, string>((short)gameMode, item.Item2);
                Instance.Server.SendToAll(msg);
                
                Instance.VerifyVote();
            }

        }

        [MessageHandler((ushort) ClientToServerId.GetOneBox, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        public static void SomeOneGotBox(ushort fromClient, Message message)
        {
            int itemdefid = message.GetInt();
        
            Message msg = Message.Create(MessageSendMode.Reliable,(ushort)ServerToClientId.SomeoneGetBox);
            msg.Add(fromClient);
            msg.Add(itemdefid);
            Instance.Server.SendToAll(msg);
        }

        [MessageHandler((ushort) ClientToServerId.ManageServer, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        public static void ManageServer(ushort fromClient, Message message)
        {
            if (!RolesManager.Instance.CheckIsAdmin(ServerPlayer.list[fromClient].SteamId)) return;
        
            ManageType type = (ManageType) message.GetUShort();

            ushort id = 0;
            ulong steamId = 0;
            switch (type)
            {
                case ManageType.Kick:
                    id = message.GetUShort();
                    // if (id == NetworkManager.Instance.Client.Id)
                    // {
                    //     if (LoadingManager.Instance.menuType == LoadingManager.MenuType.None)LoadingManager.Instance.menuType= LoadingManager.MenuType.Kick;
                    //     LobbyManager.Instance.LeaveLobby();
                    // }
                    Instance.Server.DisconnectClient(id,Message.Create());
                    break;
                case ManageType.Ban:
                    id = message.GetUShort();
                    if (id == NetworkManager.Instance.Client.Id)
                    {
                        if (LoadingManager.Instance.menuType == LoadingManager.MenuType.None)LoadingManager.Instance.menuType= LoadingManager.MenuType.Kick;
                        LobbyManager.Instance.LeaveLobby();
                    }
                    steamId = message.GetULong();
                    BannedPlayer.Add(steamId);
                    Instance.Server.DisconnectClient(id);
                    break;
            }
        }
        
        [MessageHandler((ushort) ClientToServerId.Authorize, PlayerHostedDemoMessageHandlerGroupId)]
        public static void AuthorizeClient(ushort fromClient, Message message)
        {
            Debug.Log($"Client {fromClient} trys to authorize");

            byte[] ticket = message.GetBytes();
            CSteamID id =new CSteamID( message.GetULong());
            string userName = message.GetString();
            
            if(!SteamIDToClient.ContainsKey(id))
                SteamIDToClient.Add(id,new Tuple<ushort, string>(fromClient, userName));

            var result = SteamUser.BeginAuthSession(ticket, ticket.Length, id);

            switch (result)
            {
                case EBeginAuthSessionResult.k_EBeginAuthSessionResultOK:
                    _userHasDlc[id.m_SteamID] = SteamUser.UserHasLicenseForApp(id, new AppId_t(2238100));
                    Debug.Log($"Ticket is valid for this game {1949740} and this Steam ID {id}.");
                    break;
                case EBeginAuthSessionResult.k_EBeginAuthSessionResultInvalidTicket:
                    Debug.Log("The ticket is invalid.");
                    break;
                case EBeginAuthSessionResult.k_EBeginAuthSessionResultDuplicateRequest:
                    Debug.Log($"A ticket has already been submitted for this Steam ID {id}.");
                    // Instance.OnValidateAuthTicketResponse(new ValidateAuthTicketResponse_t(){m_eAuthSessionResponse = EAuthSessionResponse.k_EAuthSessionResponseOK, m_SteamID = id,m_OwnerSteamID = id});
                    break;
                case EBeginAuthSessionResult.k_EBeginAuthSessionResultInvalidVersion:
                    Debug.Log("Ticket is from an incompatible interface version.");
                    break;
                case EBeginAuthSessionResult.k_EBeginAuthSessionResultGameMismatch:
                    Debug.Log("Ticket is not for this game.");
                    break;
                case EBeginAuthSessionResult.k_EBeginAuthSessionResultExpiredTicket:
                    Debug.Log("Ticket has expired.");
                    break;
            }
        }
        #endregion

        #endregion
        
        #region CallBack

        private void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t param)
        {
            bool kick = false;
            string reason = String.Empty;
            
            Tuple<ushort, string> tuple;

            switch (param.m_eAuthSessionResponse)
            {
                case EAuthSessionResponse.k_EAuthSessionResponseOK:
                    Debug.Log($"{param.m_SteamID} Got Authorized");
                    if (AuthorizedUsers.Contains(param.m_SteamID))
                        AuthorizedUsers.Remove(param.m_SteamID);
                    AuthorizedUsers.Add(param.m_SteamID);
                    
                    if (SteamIDToClient.TryGetValue(param.m_SteamID, out tuple))
                    {
                        Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.Authorized);
                            
                        Instance.Server.Send(message,tuple.Item1);
                            
                        if (PlayerVoteList.ContainsKey(tuple.Item1))
                            PlayerVoteList.Remove(tuple.Item1);
            
                        PlayerVoteList.Add(tuple.Item1, new Tuple<short, string>(-1,String.Empty));
            
                        if (GameState == GameState.None)
                        {
                            SetGameState(GameState.Voting,false);

                            int playerCount = Server.ClientCount;
                
                            _votingTime = playerCount > 20 ? 75 : playerCount > 10 ? 60 : playerCount>5 ? 45 : 30;
                
                            VoteCount = 0;
                        }
                    }
                    
                    break;
                case EAuthSessionResponse.k_EAuthSessionResponseAuthTicketCanceled:
                    Debug.Log($"{param.m_SteamID}'s Tick Got Canceled");
                    break;
                case EAuthSessionResponse.k_EAuthSessionResponseVACBanned:
                    Debug.Log($"{param.m_SteamID} Got VAC Banned");
                    
                    // if (SteamIDToClient.TryGetValue(param.m_SteamID, out tuple))
                    //     Instance.Server.DisconnectClient(tuple.Item1);
                    break;
                case EAuthSessionResponse.k_EAuthSessionResponseUserNotConnectedToSteam:
                    Debug.Log($"{param.m_SteamID} Not Connected to Steam");
                    
                    kick = true;
                    reason = "Not Connected to Steam";
                    break;
                case EAuthSessionResponse.k_EAuthSessionResponsePublisherIssuedBan:
                    Debug.Log($"{param.m_SteamID} Got Game Banned");

                    kick = true;
                    reason = "Game Banned";
                    break;
                case EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalid:
                    Debug.Log($"{param.m_SteamID}'s Ticket Invalid");
                    
                    kick = true;
                    reason = "Ticket Invalid";
                    break;
                case EAuthSessionResponse.k_EAuthSessionResponseVACCheckTimedOut:
                    Debug.Log($"{param.m_SteamID} Vac Check Timeout");
                    
                    kick = true;
                    reason = "Vac Check Timeout";
                    break;
                
            }

            if (kick)
            {
                if (AuthorizedUsers.Contains(param.m_SteamID))
                    AuthorizedUsers.Remove(param.m_SteamID);
                    
                if (SteamIDToClient.TryGetValue(param.m_SteamID, out tuple))
                    Instance.Server.DisconnectClient(tuple.Item1, GetDisconnectMessage(reason));
            }
        }

        #endregion

        #region Server Callback Function

        public void NewPlayerConnect(object sender, ServerConnectedEventArgs e)
        {
            AnticheatManager.Instance.Heartbeats[e.Client.Id] = Time.time;

            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.Init);

            message.Add((ushort)ServerType);
            message.Add(ServerRandomMap);
            message.Add(ServerRandomGameMode);
            message.Add(ServerDisableSpecialWeapon);
            message.Add(ServerEnableWorkshop);
            message.Add(CheatsEnabled);
            message.Add(ClientData.Count);

            foreach (var weaponValue in AllowedWeapon)
            {
                message.Add(weaponValue);
            }

            message.Add(EnabledWorkshopMaps.Count);

            foreach (var id in EnabledWorkshopMaps)
            {
                message.Add((ulong)id);
            }
            
            Server.Send(message,e.Client.Id);

            foreach (var data in ClientData.Values)
            {
                if(data.SerializeInventoryInitialized)
                    data.SendToClient(e.Client.Id);
            }
        }

        public void ClientDisconnect(object sender, ServerDisconnectedEventArgs e)
        {
            AnticheatManager.Instance.Heartbeats.Remove(e.Client.Id);
            PlayerVoteList.Remove(e.Client.Id);
            _userHasDlc.Remove(e.Client.Id);
            
            if (ClientData.TryGetValue(e.Client.Id,out var data))
            {
                CSteamID id = new CSteamID(data.SteamId);
                SteamUser.EndAuthSession(id);
                if (AuthorizedUsers.Contains(id))
                {
                    AuthorizedUsers.Remove(id);
                }

                if (InventoryManager.Instance.PendingUserSteamIds.Contains(data))
                {
                    InventoryManager.Instance.PendingUserSteamIds.Remove(data);
                }

                if (SteamIDToClient.ContainsKey(id))
                {
                    SteamIDToClient.Remove(id);
                }
                
                ClientData.Remove(e.Client.Id);
            }
            if (SceneManager.GetActiveScene().name != "Menu")
            {
                if (ServerPlayer.list.ContainsKey(e.Client.Id))
                {
                    if (IsTeamMode())
                    {
                        if (ServerPlayer.list[e.Client.Id].Team == Team.Rebel)
                        {
                            ServerPlayer.RebelPlayers--;
                        }
                        else
                        {
                            ServerPlayer.AlliancePlayers--;
                        }
                    }

                    ServerCollectable.RemoveItem(e.Client.Id);

                    bool infected = ServerPlayer.list[e.Client.Id].IsInfected;

                    if ((ServerGameMode != GameMode.SpecialGameMode
                         && ServerGameMode != GameMode.SpecialNormalGameMode))
                    {
                        if (Server.ClientCount < 2)
                        {
                            StopGame();
                        }

                        switch (ServerGameMode)
                        {
                            case GameMode.Infected:
                                bool isInfect = true;
                                int index = 0;
                                foreach (var player in ServerPlayer.list.Values)
                                {
                                    if (!player.IsInfected)
                                    {
                                        isInfect = false;
                                        index++;
                                    }
                                }

                                if (isInfect || (index == ServerPlayer.list.Count && infected))
                                {
                                    StopGame();
                                }

                                break;
                        }
                    }
                    
                    ServerPlayer.list[e.Client.Id].Destroy();
                    ServerPlayer.list.Remove(e.Client.Id);
                }

            }

        }

        #endregion

        #region Game Logic

        bool VerifyVote()
        {
            if (ClientData.Count < CurrentServer.OverrideMinimalPlayerCount(2))
            {
                return false;
            }
            
            int playerCount = GetAvailableClientCount();
            
            if (playerCount == 1)
            {
                return false;
            }
            int a = playerCount == 2 ? 2 : (int)Mathf.Floor(playerCount * 8f / 10f);
            int b = 0;
            if (!ServerRandomMap) b++;
            if (!ServerRandomGameMode) b++;
            if (b == 0)
            {
                _votingTime = 0;
                return true;
            }
            if (VoteCount / b >= a)
            {
                _votingTime = 0;
                return true;
            }
            return false;
        }

        public static void SetWorkshopMapVote()
        {
            WorkshopMapVote.Clear();

            foreach (var id in EnabledWorkshopMaps)
            {
                WorkshopMapVote.Add((ulong)id, 0);
            }
        }

        /// <summary>
        /// Set the workshop boolean
        /// </summary>
        /// <param name="flag"></param>
        public static void SetServerEnableWorkshop(bool flag)
        {
            ServerEnableWorkshop = flag;
        }

        public uint Seed { get; private set; }

        void SendStartGame()
        {
            SetGameState(GameState.Loading);
            
            PlayerRagdoll.ragdolls.Clear();
            ServerPlayer.AlliancePlayers = 0;
            ServerPlayer.RebelPlayers = 0;
            GameMode g = ServerRandomGameMode ? GetGameMode() : GameMode.Brawl;

            CurrentMap = String.Empty;
            WorkshopMap = PublishedFileId_t.Invalid;

            ushort cnt = 0;
            if (!ServerRandomGameMode)
            {
                cnt = 0;
                if (GameModeVote[(ushort)GameMode.Brawl] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.Brawl];
                    g = GameMode.Brawl;
                }

                if (GameModeVote[(ushort)GameMode.TeamDeathMatch] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.TeamDeathMatch];
                    g = GameMode.TeamDeathMatch;
                }

                if (GameModeVote[(ushort)GameMode.Infected] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.Infected];
                    g = GameMode.Infected;
                }

                if (GameModeVote[(ushort)GameMode.KillConfirm] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.KillConfirm];
                    g = GameMode.KillConfirm;
                }

                if (GameModeVote[(ushort)GameMode.Randomizer] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.Randomizer];
                    g = GameMode.Randomizer;
                }

                if (GameModeVote[(ushort)GameMode.KingOfTheHill] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.KingOfTheHill];
                    g = GameMode.KingOfTheHill;
                }

                if (GameModeVote[(ushort)GameMode.GunGame] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.GunGame];
                    g = GameMode.GunGame;
                }
                
                if (GameModeVote[(ushort)GameMode.CatchTheBanana] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.CatchTheBanana];
                    g = GameMode.CatchTheBanana;
                }
                
                if (GameModeVote[(ushort)GameMode.OneShotOneKill] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.OneShotOneKill];
                    g = GameMode.OneShotOneKill;
                }
                
                if (GameModeVote[(ushort)GameMode.RocketMode] > cnt)
                {
                    cnt = GameModeVote[(ushort)GameMode.RocketMode];
                    g = GameMode.RocketMode;
                }
                
                // if (GameModeVote[(ushort)GameMode.PVE] > cnt)
                // {
                //     cnt = GameModeVote[ (ushort)GameMode.PVE ];
                //     g = GameMode.PVE;
                // }
            }

            // bool isWorkshopMap = ServerEnableWorkshop && Random.Range(0, 10) < 6;
            if ( g == GameMode.PVE )
            {
                IsWorkshopMap = false;

                CurrentMap = "ProcMap";
                Seed = (uint)( DateTime.Now.Ticks % uint.MaxValue );
            }
            else
            {
                IsWorkshopMap = ServerEnableWorkshop;
                //Play workshop maps
                if (IsWorkshopMap)
                {
                    if (g == GameMode.KingOfTheHill) g = GameMode.Brawl;
                    PublishedFileId_t fileIdT = ServerRandomMap ? GetRandomWorkshopMap() : EnabledWorkshopMaps.FirstOrDefault();
                    if (!ServerRandomMap)
                    {
                        cnt = 0;
                        foreach (var map in WorkshopMapVote)
                        {
                            if (map.Value > cnt)
                            {
                                cnt = map.Value;
                                fileIdT = new PublishedFileId_t(map.Key);
                            }
                        }
                    }

                    WorkshopMap = fileIdT;
                }
                else
                {
                    string m = ServerRandomMap ?  GetRandomMap(g) : "Water";
                    if (!ServerRandomMap)
                    {
                        cnt = 0;
                        foreach (var map in Maps)
                        {
                            if (map.Key == g)
                            {
                                foreach (var scene in map.Value)
                                {
                                    if (MapVote[scene.name] > cnt)
                                    {
                                        cnt = MapVote[scene.name];
                                        m = scene.name;

                                    }
                                }
                            }
                        }
                    }
                    CurrentMap = m;
                    
                    Seed = (uint)( DateTime.Now.Ticks % uint.MaxValue );
                }
            }
                
            ServerGameMode = g;

            Message message = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.StartGame);
            message.Add((ushort)GameState);
            message.Add(IsWorkshopMap);
            message.Add((ushort) ServerGameMode);
            message.Add(IsWorkshopMap ? WorkshopMap.ToString() : CurrentMap);

            if (ServerGameMode == GameMode.KingOfTheHill)
            {
                message.AddInt( 0 );
            }
            
            if ( CurrentMap == "ProcMap" )
            {
                message.AddUInt( Seed );
            }

            if (IsWorkshopMap)
            {
                if (MapSaver.WorkshopMaps.TryGetValue(WorkshopMap, out var file))
                {
                    message.Add(file.GetMD5ChecksumForFile());
                }
            }
            foreach (var data in ClientData.Values)
            {
                if (data.SerializeInventoryInitialized)
                {
                    Server.Send( message, data.Id, false );
                }
            }
            message.Release();
        }
        
        public void StopGame()
        {
            if (ServerType == ServerType.Endless) return;
            if (GameState == GameState.None) return;

            SetGameState(GameState.None);
            game.StopGameServer();
        
            StopAllCoroutines();

            foreach (var map in Maps)
            {
                if (map.Key != GameMode.None)
                {
                    foreach (var scene in map.Value)
                    {
                        if (MapVote.ContainsKey(scene.name))
                        {
                            MapVote[scene.name] = 0;
                        }
                    }
                }
            }

            GameModeVote[(ushort)GameMode.Brawl] = 0;
            GameModeVote[(ushort)GameMode.CatchTheBanana] = 0;
            GameModeVote[(ushort)GameMode.TeamDeathMatch] = 0;
            GameModeVote[(ushort)GameMode.Infected] = 0;
            GameModeVote[(ushort)GameMode.KillConfirm] = 0;
            GameModeVote[(ushort)GameMode.Randomizer] = 0;
            GameModeVote[(ushort)GameMode.KingOfTheHill] = 0;
            GameModeVote[(ushort)GameMode.GunGame] = 0;

            ServerGrenade.nextId = 0;
            ServerCollectable.nextId = 0;
            
            StartCoroutine(GotoReady());
        }

        IEnumerator GotoReady()
        {
            yield return new WaitForSeconds(15f);
            SetGameState(GameState.Voting,false);
            int playerCount = Server.ClientCount;
                
            _votingTime = playerCount > 20 ? 75 : playerCount > 10 ? 60 : playerCount>5 ? 45 : 30;
                
            VoteCount = 0;

            if (!VerifyVote())
            {
                Message message = Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.Voting);

                message.Add(_votingTime);
                
                message.AddUShort(CurrentServer.OverrideMinimalPlayerCount(2));

                message.Add((ushort) ClientData.Count);

                foreach (var d in ClientData.Values)
                {
                    if (LobbyDataManager.datas.TryGetValue(d.SteamId, out var lobbyData))
                    {
                        message.Add(d.Id);
                        message.Add(lobbyData.kills);
                    }
                }

                Server.SendToAll(message);
            }
        }
        
        IEnumerator KickLoadFailedPlayer()
        {
            yield return new WaitForSeconds(10);
            
            foreach (var client in ClientData.Values)
            {
                if (!ServerPlayer.list.ContainsKey(client.Id))
                {
                    Server.DisconnectClient(client.Id);
                }
            }
        }
        IEnumerator StartGame()
        {
            StartCoroutine(KickLoadFailedPlayer());
            while (ServerPlayer.list.Count < ClientData.Count)
            {
                yield return null;
            }
            
            if (!IsPlaying) IsPlaying = true;
            if (!Server.IsRunning) yield break;
            
            game.leftTime = CurrentServer.OverrideGameTime(game.leftTime);
            game.SetTick();
            
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.BeforeGameStart);

            message.AddUInt(game.StartTick);
            message.AddUInt(game.EndTick);
            
            if (ServerType == ServerType.KnockoutRound)
            {
                message.AddBool(GetAvailableClientCount() <= 2);
                message.AddInt(((KnockoutServer)CurrentServer).Round);
            }
            
            Server.SendToAll(message);
        }
        
        public void StartGameFromLoad()
        {
            StartCoroutine(StartGame());
        }
        
        public void ShootLagCompensation(uint tick, Vector3 lookDir,Vector3 raycastPos,ushort fromClient, ActiveWeapon weapon)
        {
            uint finalTick = tick;

            if (ServerPlayer.list.TryGetValue(fromClient, out var fromPlayer))
            {
                bool aiming = fromPlayer.IsAiming;
                if (weapon.Stat.explosiveAmmo)
                {
                    for (int i = 0; i < weapon.Stat.bulletAmount; i++)
                    {
                        Vector3 dir = lookDir.normalized;

                        StartCoroutine(ExplosiveAmmoLagCompensation(finalTick, raycastPos, dir, fromClient,
                            weapon.Stat.bUseGravity ? 1 : 0, fromPlayer));
                    }
                }
                else
                {
                    List<LagCompensationHitbox> hitboxes = new List<LagCompensationHitbox>();
                    //Player hitbox
                    foreach (var serverPlayer in ServerPlayer.list.Values)
                    {
                        TransformUpdate transformUpdate = serverPlayer.TransformBuffer[finalTick % ServerPlayer.MaxTickStore];

                        if (transformUpdate != null)
                        {
                            Vector3 predictPos = transformUpdate.Position;

                            if (ShouldSpawnHitbox(serverPlayer,fromPlayer))
                            {
                                LagCompensationHitbox hitbox = ObjectPooler.Instance.SpawnFromPool("PlayerHitbox", predictPos, Quaternion.identity).GetComponent<LagCompensationHitbox>();
                                
                                hitbox.Initialize(serverPlayer.Id,finalTick);
                                
                                hitboxes.Add(hitbox);
                            }
                        }
                    }
                    
                    //Enemy Hitbox
                    foreach (var serverEnemy in ServerEnemy.list.Values)
                    {
                        TransformUpdate transformUpdate = serverEnemy.TransformBuffer[finalTick % ServerPlayer.MaxTickStore];
                        
                        if (transformUpdate != null)
                        {
                            Vector3 predictPos = transformUpdate.Position;

                            string enemy;

                            switch (serverEnemy.enemyType)
                            {
                                case ServerEnemy.EnemyType.Jack:
                                    enemy = "JackHitbox";
                                    break;
                                case ServerEnemy.EnemyType.Kat:
                                    enemy = "KatHitbox";
                                    break;
                                case ServerEnemy.EnemyType.Turret:
                                    enemy = "TurretHitbox";
                                    break;
                                case ServerEnemy.EnemyType.Zombie:
                                    enemy = "ZombieHitbox";
                                    break;
                                default:
                                    enemy = "JackHitbox";
                                    break;
                            }

                            LagCompensationHitbox hitbox = ObjectPooler.Instance.SpawnFromPool(enemy, predictPos, Quaternion.identity).GetComponent<LagCompensationHitbox>();
                                
                            hitbox.Initialize(serverEnemy.Id,finalTick);
                                
                            hitboxes.Add(hitbox);
                        }  
                    }
                    
                    Physics.SyncTransforms();
                    
                    RaycastHit[] hits = new RaycastHit[10];
                    Vector3 dir;
                    int cnt;

                    if (fromPlayer.IsInfected)
                    {
                        dir = lookDir.normalized;
                        cnt = Physics.RaycastNonAlloc(raycastPos, dir, hits, 4f,
                            GameManager.Instance.lagCompensationHitboxLayer, QueryTriggerInteraction.Ignore);

                        bool flag = false;
                        
                        for (int i = 0; i < cnt; i++)
                        {
                            RaycastHit hit = hits[i];

                            LagCompensationHitbox hitbox = hit.transform.root.GetComponent<LagCompensationHitbox>();

                            if (hitbox != null)
                            {
                                hitbox.TakeDamage(fromClient,finalTick,false, aiming,LagCompensationHitbox.HitboxType.Player, hit.point,hit.collider.gameObject.CompareTag("Head"),3);
                                flag = true;
                                break;
                            }
                        }
                        
                        cnt = Physics.SphereCastNonAlloc(raycastPos, 1.3f, dir, hits, 4f,
                            GameManager.Instance.lagCompensationHitboxLayer, QueryTriggerInteraction.Ignore);

                        if (!flag)
                        {
                            for (int i = 0; i < cnt; i++)
                            {
                                RaycastHit hit = hits[i];

                                LagCompensationHitbox hitbox = hit.transform.root.GetComponent<LagCompensationHitbox>();

                                if (hitbox != null)
                                {
                                    hitbox.TakeDamage(fromClient,finalTick,false, aiming,LagCompensationHitbox.HitboxType.Player, hit.point, hit.collider.gameObject.CompareTag("Head"),3);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        switch (weapon.Stat.weaponType)
                        {
                            case WeaponStat.WeaponType.Knife:
                                dir = lookDir.normalized;
                                cnt = Physics.RaycastNonAlloc(raycastPos, dir, hits, 4f,
                                    GameManager.Instance.lagCompensationHitboxLayer, QueryTriggerInteraction.Ignore);

                                bool flag = false;
                                
                                for (int i = 0; i < cnt; i++)
                                {
                                    RaycastHit hit = hits[i];

                                    LagCompensationHitbox hitbox = hit.transform.root.GetComponent<LagCompensationHitbox>();

                                    if (hitbox != null)
                                    {
                                        hitbox.TakeDamage(fromClient,finalTick,false, aiming,LagCompensationHitbox.HitboxType.Player, hit.point,hit.collider.gameObject.CompareTag("Head"));
                                        flag = true;
                                        break;
                                    }
                                }
                                
                                cnt = Physics.SphereCastNonAlloc(raycastPos, 1.3f, dir, hits, 4f,
                                    GameManager.Instance.lagCompensationHitboxLayer, QueryTriggerInteraction.Ignore);

                                if (!flag)
                                {
                                    for (int i = 0; i < cnt; i++)
                                    {
                                        RaycastHit hit = hits[i];

                                        LagCompensationHitbox hitbox = hit.transform.root.GetComponent<LagCompensationHitbox>();

                                        if (hitbox != null)
                                        {
                                            hitbox.TakeDamage(fromClient,finalTick,false,aiming, LagCompensationHitbox.HitboxType.Player, hit.point, hit.collider.gameObject.CompareTag("Head"));
                                            break;
                                        }
                                    }
                                }
                                

                                
                                break;
                            case WeaponStat.WeaponType.Taser:
                                dir = lookDir.normalized;
                                cnt = Physics.RaycastNonAlloc(raycastPos, dir, hits, 6f,
                                    GameManager.Instance.lagCompensationHitboxLayer, QueryTriggerInteraction.Ignore);

                                for (int i = 0; i < cnt; i++)
                                {
                                    RaycastHit hit = hits[i];

                                    LagCompensationHitbox hitbox = hit.transform.root.GetComponent<LagCompensationHitbox>();

                                    if (hitbox != null)
                                    {
                                        hitbox.TakeDamage(fromClient,finalTick,false, aiming,LagCompensationHitbox.HitboxType.Player, hit.point, hit.collider.gameObject.CompareTag("Head"));
                                        break;
                                    }
                                }
                            
                                break;
                            default:
                                for (int i = 0; i < weapon.Stat.bulletAmount; i++)
                                {
                                    Vector3 offset = weapon.Stat.spreadAngle / 80f * Random.insideUnitCircle;

                                    dir = (lookDir + offset).normalized;

                                    cnt = Physics.SphereCastNonAlloc(raycastPos, 0.25f, dir, hits, 1000f,
                                        GameManager.Instance.lagCompensationHitboxLayer, QueryTriggerInteraction.Ignore);

                                    // Debug.Log($"Client tick: {tick} , Server tick: {CurrentTick} , Raycast Pos: {raycastPos}");

                                    Dictionary<GameObject, RaycastHit> objToHits =
                                        new Dictionary<GameObject, RaycastHit>();

                                    for (int j = 0; j < cnt; j++)
                                    {
                                        RaycastHit hit = hits[j];

                                        objToHits[hit.collider.gameObject] = hit;

                                        LagCompensationHitbox hitbox = hit.transform.root.GetComponent<LagCompensationHitbox>();

                                        if (hitbox != null)
                                        {
                                            Vector3 hitboxPos = hitbox.transform.position;
                                            RaycastHit[] wallHits = new RaycastHit[10];
                                            
                                            int wallCnt = Physics.RaycastNonAlloc(hitboxPos, (raycastPos - hitboxPos).normalized,
                                                wallHits, Vector3.Distance(hitboxPos, raycastPos),
                                                GameManager.Instance.lagCompensationHitboxLayer,
                                                QueryTriggerInteraction.Ignore);

                                            float wallDistance = 0;

                                            for (int k = 0; k < wallCnt; k++)
                                            {
                                                RaycastHit wallHit = wallHits[k];

                                                if (objToHits.TryGetValue(wallHit.collider.gameObject, out var ogHit))
                                                {
                                                    wallDistance += Vector3.Distance(ogHit.point, wallHit.point);
                                                }
                                            }

                                            // if (wallDistance > MaxWallBangDistance) continue;
                                            
                                            hitbox.TakeDamage(fromClient,finalTick,wallDistance > 0, aiming,LagCompensationHitbox.HitboxType.Player, hit.point, hit.collider.gameObject.CompareTag("Head"));

                    #if UNITY_EDITOR
                                            // if (false)
                                            // {
                                            //     LagCompensationTest newTest = new GameObject($"Player {fromClient} Shoot Player {hitbox.Id}").AddComponent<LagCompensationTest>();
                                            //
                                            //     newTest.shootPlayerPos = raycastPos - ServerPlayer.HeadOffset;
                                            //     newTest.hitPlayerPos = hitbox.transform.position;
                                            //     newTest.hitPos = hit.point;
                                            //     newTest.hitPlayerOgPos = ServerPlayer.list[hitbox.Id].PlayerTransform.position;
                                            // }       
                    #endif
                                            break;
                                        }
                                        
                                    }
                                }
                                break;
                        }
                    }
                    
                    foreach (var hitbox in hitboxes)
                    {
                        hitbox.gameObject.SetActive(false);
                    }
                    
                    hitboxes.Clear();
                }
            }
            
        }
        
        IEnumerator ExplosiveAmmoLagCompensation(uint tick,Vector3 startPos,Vector3 dir,ushort fromClient, int useGravity,IPlayerServer fromPlayer)
        {
            List<LagCompensationHitbox> hitboxes = new List<LagCompensationHitbox>();
            bool aiming = fromPlayer.IsAiming;

            Vector3 startVel = dir * 200f;

            Collider[] cols = new Collider[15];

            uint predictTick = tick;

            for (float t = 0; t < 50; t+=Time.fixedDeltaTime)
            {
                Vector3 newPoint = startPos + t * startVel;
                newPoint.y = startPos.y + startVel.y * t + Physics.gravity.y / 2f * t * t * useGravity;
                
                int cnt = Physics.OverlapSphereNonAlloc(newPoint, 0.5f,cols, GameManager.Instance.lagCompensationHitboxLayer,QueryTriggerInteraction.Ignore);

                if (cnt > 0)
                {
                    // Debug.Log(cols[0].bounds.ClosestPoint(newPoint));
                    yield return new WaitForSeconds(t);

                    predictTick += (uint)(t / 0.02f);
                    
                    //Player hitbox
                    foreach (var serverPlayer in ServerPlayer.list.Values)
                    {
                        TransformUpdate transformUpdate = serverPlayer.TransformBuffer[predictTick % ServerPlayer.MaxTickStore];

                        if (transformUpdate != null)
                        {
                            Vector3 predictPos = transformUpdate.Position;

                            if (ShouldSpawnHitbox(serverPlayer,fromPlayer))
                            {
                                LagCompensationHitbox hitbox = ObjectPooler.Instance.SpawnFromPool("PlayerHitbox", predictPos, Quaternion.identity).GetComponent<LagCompensationHitbox>();
                                
                                hitbox.Initialize(serverPlayer.Id,tick);
                                
                                hitboxes.Add(hitbox);
                            }
                        }  
                    }
                    
                    //Enemy Hitbox
                    foreach (var serverEnemy in ServerEnemy.list.Values)
                    {
                        TransformUpdate transformUpdate = serverEnemy.TransformBuffer[predictTick % ServerPlayer.MaxTickStore];
                        
                        if (transformUpdate != null)
                        {
                            Vector3 predictPos = transformUpdate.Position;

                            string enemy;

                            switch (serverEnemy.enemyType)
                            {
                                case ServerEnemy.EnemyType.Jack:
                                    enemy = "JackHitbox";
                                    break;
                                case ServerEnemy.EnemyType.Kat:
                                    enemy = "KatHitbox";
                                    break;
                                case ServerEnemy.EnemyType.Turret:
                                    enemy = "TurretHitbox";
                                    break;
                                case ServerEnemy.EnemyType.Zombie:
                                    enemy = "ZombieHitbox";
                                    break;
                                default:
                                    enemy = "JackHitbox";
                                    break;
                            }

                            LagCompensationHitbox hitbox = ObjectPooler.Instance.SpawnFromPool(enemy, predictPos, Quaternion.identity).GetComponent<LagCompensationHitbox>();
                                
                            hitbox.Initialize(serverEnemy.Id,tick);
                                
                            hitboxes.Add(hitbox);
                        }  
                    }
                    
                    Physics.SyncTransforms();
                    
                    cnt = Physics.OverlapSphereNonAlloc(newPoint, 18f,cols, GameManager.Instance.lagCompensationHitboxLayer,QueryTriggerInteraction.Ignore);

                    HashSet<ushort> alreadyHitted = new HashSet<ushort>();

                    for (int i = 0; i < cnt; i++)
                    {
                        if (cols[i].transform.root.TryGetComponent(out LagCompensationHitbox hitbox) && alreadyHitted.Add(hitbox.Id))
                        {
                            hitbox.TakeDamage(fromClient,tick,false, aiming,LagCompensationHitbox.HitboxType.Player, cols[i].bounds.ClosestPoint(newPoint));
                        }
                    }
                    
                    foreach (var hitbox in hitboxes)
                    {
                        hitbox.gameObject.SetActive(false);
                    }
                
                    hitboxes.Clear();
                    
                    yield break;
                }
            }
        }
        
        public bool ShouldSpawnHitbox(IPlayerServer serverPlayer, IPlayer fromPlayer) 
        {
            return serverPlayer.Id != fromPlayer.Id && !serverPlayer.Dead
                                                    && (ServerGameMode != GameMode.TeamDeathMatch ||
                                                        serverPlayer.Team != fromPlayer.Team)
                                                    && (ServerGameMode != GameMode.Infected ||
                                                        serverPlayer.IsInfected != fromPlayer.IsInfected);
        }
        
        #endregion

        #region Returns of the server properties

        public int GetAvailablePlayerCount()
        {
            int count = 0;
            foreach (var serverPlayer in ServerPlayer.list.Values)
            {
                if (!serverPlayer.Eliminated) count++;
            }

            return count;
        }
        
        public bool IsTeamMode()
        {
            return ServerGameMode == GameMode.TeamDeathMatch || ServerType == ServerType.Endless;
        }
        GameMode GetGameMode()
        {
            int rand = Random.Range(0, 10);

            switch (rand)
            {
                case 0:
                    return GameMode.Brawl;
                case 1:
                    return GameMode.TeamDeathMatch;
                case 2:
                    return GameMode.Infected;
                case 3:
                    return GameMode.KillConfirm;
                case 4:
                    return GameMode.Randomizer;
                case 5:
                    return GameMode.KingOfTheHill;
                case 6:
                    return GameMode.GunGame;
                case 7:
                    return GameMode.CatchTheBanana;
                case 8:
                    return GameMode.OneShotOneKill;
                case 9:
                    return GameMode.RocketMode;
                default:
                    return GameMode.Brawl;
            }
        }

        PublishedFileId_t GetRandomWorkshopMap()
        {
            int rand = Random.Range(0, EnabledWorkshopMaps.Count);

            return EnabledWorkshopMaps.ElementAt(rand);
        }

        string GetRandomMap(GameMode mode)
        {
            foreach (var map in Maps)
            {
                if (map.Key == mode && map.Key!= GameMode.None && map.Key!=GameMode.SpecialGameMode)
                {
                    int rand = Random.Range(0, map.Value.Count);

                    return map.Value.ToArray()[rand].name;
                }
            }
            return String.Empty;
        }

        public ushort GetAvailableEntityId()
        {
            for (ushort i = (ushort)(Server.MaxClientCount + 1); i < Multiplayer.Entity.Entity.MaxEntityAmount; i++)
            {
                if (!Entity.Entities.ContainsKey(i))
                {
                    return i;
                }
            }

            return 0;
        }
        
        public static Message GetDisconnectMessage(string reason)
        {
            Message message = Message.Create();

            message.Add(reason);

            return message;
        }

        public static int GetAvailableClientCount()
        {
            int clientCount = 0;

            foreach (var data in ClientData.Values)
            {
                if (!data.Eliminated) clientCount++;
            }

            return clientCount;
        }

        public static EUserHasLicenseForAppResult GetLicenseResult(ulong steamID)
        {
            return _userHasDlc.GetValueOrDefault(steamID, EUserHasLicenseForAppResult.k_EUserHasLicenseResultNoAuth);
        }
        
        #endregion

        #region Set server properties
        
        public enum ServerPropertiesType : ushort
        {
            CheatsEnabled = 0,
            
        }

        public void SetIsWorkshopMap(bool flag)
        {
            IsWorkshopMap = flag;
        }

        public void SetCheatsEnabled(bool flag)
        {
            CheatsEnabled = flag;

            if (Server.IsRunning)
            {
                SendServerProperty(ServerPropertiesType.CheatsEnabled);

                var oldLobbyType = LobbyManager.Instance.lobbyType;
                LobbyManager.Instance.SetLobbyType(flag && oldLobbyType != ELobbyType.k_ELobbyTypePrivate ? ELobbyType.k_ELobbyTypeFriendsOnly : oldLobbyType);
            }
        }

        void SendServerProperty(ServerPropertiesType type)
        {
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.ServerProperties);

            message.Add((ushort)type);

            switch (type)
            {
                case ServerPropertiesType.CheatsEnabled:
                    message.Add(CheatsEnabled);
                    break;
            }
            
            Server.SendToAll(message);
        }

        #endregion

        #region Set server properties outside

        public void ResetProperties()
        {
            foreach (var map in Maps)
            {
                if (map.Key != GameMode.None)
                {
                    foreach (var scene in map.Value)
                    {
                        if (MapVote.ContainsKey(scene.name))
                        {
                            MapVote[scene.name] = 0;
                        }
                    }
                }
            }
            
            GameModeVote[(ushort)GameMode.Brawl] = 0;
            GameModeVote[(ushort)GameMode.TeamDeathMatch] = 0;
            GameModeVote[(ushort)GameMode.Infected] = 0;
            GameModeVote[(ushort)GameMode.KillConfirm] = 0;
            GameModeVote[(ushort)GameMode.Randomizer] = 0;
            GameModeVote[(ushort)GameMode.KingOfTheHill] = 0;
            GameModeVote[(ushort)GameMode.GunGame] = 0;
            GameModeVote[(ushort)GameMode.CatchTheBanana] = 0;
            StopAllCoroutines();
            SetGameState(GameState.None,false);
            ServerRandomMap = false;
            ServerRandomGameMode = false;
            ServerGameMode = GameMode.None;
            
            for (int i = 0; i < weaponInfo.Count; i++)
            {
                AllowedWeapon[i] = true;
            }
            
            foreach (var serverPlayer in ServerPlayer.list.Values)
            {
                if(serverPlayer!=null)
                    serverPlayer.Destroy();
            }
            ServerPlayer.list.Clear();
        }

        public void SetGameState(GameState state,bool send=true)
        {
            GameState = state;

            if (state == GameState.Voting)
            {
                foreach (var v in MapVote.Keys.ToList())
                {
                    MapVote[v] = 0;
                }
                foreach (var v in GameModeVote.Keys.ToList())
                {
                    GameModeVote[v] = 0;
                }

                foreach (var v in WorkshopMapVote.Keys.ToList())
                {
                    WorkshopMapVote[v] = 0;
                }

                foreach (var id in PlayerVoteList.Keys.ToList())
                {
                    PlayerVoteList[id] = new Tuple<short, string>(-1,String.Empty);
                }
                
                
            }

            if (send)
            {
                Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.SetGameState);

                message.Add((ushort) state);
            
                Server.SendToAll(message);
            }
        }

        public void ForceStart()
        {
            _votingTime = 0;
            SendStartGame();
        }

        public static void SetServerType(ServerType serverType)
        {
            ServerType = serverType;

            switch (ServerType)
            {
                case ServerType.KnockoutRound:
                    CurrentServer = new KnockoutServer();
                    break;
                default:
                    CurrentServer = new NormalServer();
                    break;
            }
        }

        public static void SetIsPlaying(bool flag)
        {
            IsPlaying = flag;
            if (!flag)
            {
                switch (ServerType)
                {
                    case ServerType.KnockoutRound:
                        ((KnockoutServer)CurrentServer).ResetRound();
                        break;
                }
            }
        }
        #endregion
    }
}