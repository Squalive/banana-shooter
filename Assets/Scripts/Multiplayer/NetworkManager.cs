using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Console;
using Level;
using Log;
using Manager;
using Menu;
using Mode;
using Movement;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Client.Enemy;
using Multiplayer.Entity.Server;
using Pool;
using Quest;
using Riptide;
using Riptide.Transports.Steam;
using Riptide.Utils;
using Steamworks;
using Steamworks.NET;
using SteamWorkshop.UI;
using UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using Utils;
using Weapon.WeaponStats;
using Random = UnityEngine.Random;
using SteamClient = Riptide.Transports.Steam.SteamClient;

namespace Multiplayer
{
    public enum ServerToClientId : ushort
    {
        Init=1,
        AddClientData,
        ServerProperties,
        CustomMessage,
        AddUI,
        Voting,
        AddVotingPlayer,
        Authorized,
        SetGameState,
        StartGame,
        BeforeGameStart,
        SpawnPlayer,
        PlayerMovement,
        PlayerAim,
        PlayerStartCrouch,
        PlayerStopCrouch,
        PlayerHasBanana,
        WeaponStartReloading,
        WeaponReloaded,
        TakeHealth,
        StartGrapple,
        StopGrapple,
        GetWeapon,
        TakeDamage,
        Dead,
        Respawn,
        UpdateWeaponIndex,
        StartRound,
        StopRound,
        Upgrade,
        VoiceChat,
        Shoot,
        TurnLight,
        VoteMap,
        ChangeCosmetic,
        OtherMode,
        Sync,
        SpecialWeapon,
        ManageToKick,
        AgreeKicking,
        DisAgreeKicking,
        ClearKick,
        VoteToKickFinish,
        VoteGameMode,
        DisableInvincible,
        ThrowObj,
        ThrowObjExplode,
        AllowedWeapon,
        SendMessage,
        ServerSetting,
        CrazyPlayer,
        SomeoneGetBox,
        Message,
        PlayerInfect,
        FlashBang,
        FireInit,
        PlayerFire,
        CollectableInit,
        GetConfirm,
        SpecialWeaponDisable,
        SpawnEnemy,
        EnemyMovement,
        HitEnemy,
        EnemyDead,
        EnemyState,
        BananaManMusic,
        SpecterMode,
        ObjectSpawn,
        ObjectDestroy,
        ObjectMovement,
        SendLatency,
        SendPerks,
        SetHill,
        SetTime,
        PlayerWeaponLevel,
        ObjHitWall,
        PickableSpawned,
        PickablePos,
        PickableRot,
        PickableDestroy,
        PickupWeapon,
        EndlessRefresh,
        EndlessWeaponStation,
        GrindCash,
        ClientEliminated,
        JumpPadSpawned,
        PveIterate,
    }
    public enum ClientToServerId : ushort
    {
        Init = 1,
        FragmentSerializeInventory,
        Authorize,
        RequestData,
        PlayerMovement,
        PlayerAim,
        PlayerStartCrouch,
        PlayerStopCrouch,
        WeaponReload,
        StartGrapple,
        StopGrapple,
        GetWeapon,
        UpdateWeaponIndex,
        Upgrade,
        VoiceChat,
        Shoot,
        TurnLight,
        VoteMap,
        ChangeCosmetic,
        SpecialWeapon,
        ManageToKick,
        AgreeKicking,
        DisAgreeKicking,
        Respawn,
        VoteGameMode,
        ThrowObj,
        SendMessage,
        GetOneBox,
        ManageServer,
        SpecialWeaponDisable,
        SpawnEnemy,
        SpecterMode,
        RequestSpawnObj,
        // SetCosmetics,
        SendPerks,    
        DoSelectTeam,
        PickPickable,
        BuyWeapon,
        AntiCheatData,
    }
    [Serializable]
    public enum GameMode : ushort
    {
        None=0,
        SpecialGameMode=1,
        SpecialNormalGameMode,
        Brawl,
        TeamDeathMatch,
        Infected,
        KillConfirm,
        Randomizer,
        KingOfTheHill,
        GunGame,
        CatchTheBanana,
        OneShotOneKill,
        RocketMode,
        PVE,
    }
    
    public enum RequestDataType
    {
        PlayerSpawn,
        Init,
        DisplayTag,
        InitRound,
    }
    public enum ManageType
    {
        Kick = 0,
        Ban
    }

    public enum MessageType
    {
        Infect = 0,
        KingOfTheHill,
        OneVsOne0,
        OneVsOne1,
        Wave
    }

    public enum ServerType
    {
        None=0,
        Normal,
        OneVsOne,
        Endless,
        ShootingRange,
        KnockoutRound,
    }
    

    [DefaultExecutionOrder(-99)]
    public class NetworkManager : MonoBehaviour
    {
        public const uint MaxSplitPacketSize = 256;
        
        public const byte PlayerHostedDemoMessageHandlerGroupId = 255;

        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get => _instance;
            private set
            {
                if (_instance == null)
                {
                    _instance = value;
                }
                else if (_instance != value)
                {
                    Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        public string ConnectionString { get; private set; } = String.Empty;
        
        [SerializeField] public GameObject playerPrefab;
        [SerializeField] public GameObject localPlayerPrefab;
        
        public GameObject PlayerPrefab => playerPrefab;
        public GameObject LocalPlayerPrefab => localPlayerPrefab;

        //The current gamemode going on
        public GameModes game;

        public Riptide.Client Client { get; private set; }

        public static GameState GameState { get; set; } = GameState.None;
        public static ServerType ClientServerType { get; set; } = ServerType.Normal;

        public static GameMode ClientGameMode { get; set; } = GameMode.Brawl;
        //Cheats
        public static bool ClientCheatsEnabled { get; private set; } = false;
        public short[] Weapons { set;get; } = new short[3] {0,1,2};

        public static int ClientDataCount = 0;
        //Store the client datas
        public static Dictionary<ushort, ClientData> ClientData = new Dictionary<ushort, ClientData>();

        public static ClientData LocalClientData;
        
        public List<WeaponStat> weaponInfo = new List<WeaponStat>();
        //The weapons that server allows to use
        public static Dictionary<string, bool> AllowedWeapon = new Dictionary<string, bool>();

        public CSteamID steamId;
        public CSteamID currentGroup;
        public static bool ClientRandomMap = true, ClientRandomGameMode = true;

        public Action<GameMode> GameModeChanged;

        public static ulong[] WorkshopMaps = Array.Empty<ulong>();

        public bool displayTag = true;
        public int SendByte { get; set; }
        public int ByteUp { get; private set; }
        private int ReadByte { get; set; }
        public int ByteDown { get; private set; }
        
        public bool IsWorkshopMap { private set; get; }

        public static bool ClientDisableSpecialWeapon { get; private set; } = false;
        public static bool ClientEnableWorkshop { get; private set; } = false;

        #region INITIALIZE

        private void Awake()
        {
            Instance = this;

            if (Instance == this)
            {
                foreach (var weapon in weaponInfo)
                {
                    AllowedWeapon.Add(weapon.name, true);
                }
            }
            
            // SteamNetworkingUtils.SetDebugOutputFunction(ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Everything,
            //     (type, msg) =>
            //     {
            //         //Debug.Log($"[{type}]\n{msg}\n");
            //         logFileCreator.SteamNetworkingLog($"[{type.ToString()}]\n{msg.ToString()}\n");
            //     });
            // Debug.Log(LevelUtils.GetExpFromLevel(90));
        }

        private void Start()
        {
            if (!SteamManager.Initialized)
            {
                Debug.LogError("Steam is not initialized!");
                return;
            }
#if UNITY_EDITOR
            RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
#else
            RiptideLogger.Initialize(Debug.Log, false);
#endif
            
            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.BANANA_GO_BRRRRRR);

            AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.START_GAME_1000);
            InitGame();
            steamId = SteamUser.GetSteamID();
            Chat.Instance.SetSteamId(steamId.m_SteamID);

            //SteamInventory.LoadItemDefinitions();

            ServerTick = 2;

            ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
            GetPlayerAvatar();

            displayTag = PlayerPrefs.GetInt("display_tag", 1) == 1;
            InvokeRepeating(nameof(ClearByte),1f,1f);

        }
        void ClearByte()
        {
            ByteUp = SendByte;
            ByteDown = ReadByte;
            SendByte = 0;
            ReadByte = 0;
        }
        #region STEAM

        public void SetCurrentServer(string server)
        {
            ConnectionString = server;

            // SteamFriends.SetRichPresence("server", server);
        }

        protected Callback<AvatarImageLoaded_t> ImageLoaded;

        void GetPlayerAvatar()
        {
            int ImageId = SteamFriends.GetLargeFriendAvatar(steamId);
            if (ImageId == -1) return;
            avatarLoaded = true;
            myAvatar = SteamTextureUtils.GetSteamImageAsTexture(ImageId);
            OnAvatarLoad?.Invoke(myAvatar);
        }

        public Texture2D myAvatar;

        public static bool avatarLoaded = false;
        public Action<Texture2D> OnAvatarLoad;

        public static Action<CSteamID,Texture2D> OnAvatarImageLoaded;

        private void OnImageLoaded(AvatarImageLoaded_t param)
        {
            OnAvatarImageLoaded?.Invoke(param.m_steamID,SteamTextureUtils.GetSteamImageAsTexture(param.m_iImage));
            
            if (param.m_steamID == steamId)
            {
                avatarLoaded = true;
                myAvatar = SteamTextureUtils.GetSteamImageAsTexture(param.m_iImage);
                OnAvatarLoad?.Invoke(myAvatar);
            }
        }


        #endregion

        #endregion

        internal int WinCount = 0;

        public bool connecting;

        private void Update()
        {
            Client.Update();
        }

        private void FixedUpdate()
        {
            if (!SteamManager.Initialized) return;
            
            if (ServerTick % uint.MaxValue == 0) ServerTick = 0;
            
            ServerTick++;
        }

        public bool testMode = false;
        public string MapId { get; set; }

        #region NETWORK
        
        void InitGame()
        {
            Client = new Riptide.Client(new SteamClient(NetworkServerManager.Instance.CashedSteamServer));
            
            Client.Connected += DidConnect;
            Client.ConnectionFailed += FailedToConnect;
            Client.Disconnected += DidDisconnect;
            Client.ClientDisconnected += ClientPlayerLeft;
            Client.ClientConnected += ClientPlayerJoin;

            Client.MessageReceived += MessageReceived;
        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ReadByte += e.Message.UnreadLength;
        }

        private void OnApplicationQuit()
        {
            Client.Connected -= DidConnect;
            Client.ConnectionFailed -= FailedToConnect;
            Client.Disconnected -= DidDisconnect;
            Client.ClientDisconnected -= ClientPlayerLeft;
            Client.ClientConnected -= ClientPlayerJoin;
            Client.MessageReceived -= MessageReceived;
            LobbyManager.Instance.LeaveLobby();
        }


        private uint _serverTick;

        public uint ServerTick
        {
            get => _serverTick;
            private set
            {
                _serverTick = value;
                InterpolationTick = (uint) (value - TicksBetweenPositionUpdates);
            }
        }

        public uint InterpolationTick { get; private set; }
        private uint _ticksBetweenPositionUpdates = 2;

        public uint TicksBetweenPositionUpdates
        {
            get => _ticksBetweenPositionUpdates;
            private set
            {
                _ticksBetweenPositionUpdates = value;
                InterpolationTick = (uint) (ServerTick - value);
            }
        }

        [Space(10)] [SerializeField] private uint tickDivergenceTolerance = 1;

        public void StopServer()
        {
            ClientGameMode = GameMode.Brawl;
            ClientServerType = ServerType.Normal;
        }

        public void DisconnectClient()
        {
            Client.Disconnect();
        }

        public bool IsTeamMode()
        {
            return ClientGameMode == GameMode.TeamDeathMatch || ClientServerType == ServerType.Endless;
        }

        private HAuthTicket _currentTicket;

        void ClearClientData()
        {
            ClientData.Clear();
            
            VoiceChatUIManager.Instance.Clear();
        }

        void DidConnect(object sender, EventArgs e)
        {
            ClearClientData();
            connecting = false;
        }

        void FailedToConnect(object sender, EventArgs e)
        {
            connecting = false;
            if (SceneManager.GetActiveScene().name != "Menu")
            {
                if (LoadingManager.Instance.menuType == LoadingManager.MenuType.None)
                    LoadingManager.Instance.menuType = LoadingManager.MenuType.HostQuit;
                LoadingManager.Instance.Menu();
            }
            else UIManager.Instance.disConnectBtn.onClick.Invoke();
        }

        void ClientPlayerLeft(object sender, ClientDisconnectedEventArgs e)
        {
            if (ClientData.ContainsKey(e.Id))
            {
                VoiceChatUIManager.Instance.RemovePlayer(e.Id);
                ClientData.Remove(e.Id);
            }
            
            if (GameUIManager.Instance&&GameUIManager.Instance.PlayerList.ContainsKey(e.Id) && GameUIManager.Instance.PlayerList[e.Id] != null)
            {
                Destroy(GameUIManager.Instance.PlayerList[e.Id].gameObject);
                GameUIManager.Instance.PlayerList.Remove(e.Id);
            }

            if (ClientPlayer.list.ContainsKey(e.Id))
            {
                Destroy(ClientPlayer.list[e.Id].gameObject);
                ClientPlayer.list.Remove(e.Id);
            }

            if (GameState == GameState.Voting)
            {
                if (GameVoteMenu.Instance)
                {
                    GameVoteMenu.Instance.RemoveObject(e.Id);
                }
            }
        }

        void ClientPlayerJoin(object sender, ClientConnectedEventArgs e)
        {
        }

        void DidDisconnect(object sender, DisconnectedEventArgs e)
        {
            connecting = false;
            LocalClientData = null;
            SteamFriends.SetRichPresence("server", null);
            SetCurrentServer(String.Empty);
            SteamUser.CancelAuthTicket(_currentTicket);
            LobbyManager.Instance.LeaveLobby();
            
            string additionalMessage = String.Empty;
            if (e.Message != null)
            {
                additionalMessage = e.Message.GetString();
                Debug.Log("Disconnect Message Found");
            }
            else
            {
                Debug.Log("Not Found any Disconnect Messages");
            }

            if (SceneManager.GetActiveScene().name != "Menu")
            {
                foreach (ClientPlayer player in ClientPlayer.list.Values)
                    Destroy(player.gameObject);

                ClientPlayer.list.Clear();

                if(additionalMessage!=String.Empty)
                    LoadingManager.Instance.additionalMessage = additionalMessage;
                if (LoadingManager.Instance.menuType == LoadingManager.MenuType.None)
                {
                    if (e.Reason == DisconnectReason.ServerStopped)
                        LoadingManager.Instance.menuType = LoadingManager.MenuType.HostQuit;
                    if (e.Reason == DisconnectReason.Kicked)
                        LoadingManager.Instance.menuType = LoadingManager.MenuType.Cheat;
                    if (e.Reason == DisconnectReason.TransportError)
                        LoadingManager.Instance.menuType = LoadingManager.MenuType.ConnectionFailed;
                    // if (e.Reason == DisconnectReason.Disconnected)
                    //     LoadingManager.Instance.menuType = LoadingManager.MenuType.ConnectionFailed;
                }
            
                LoadingManager.Instance.Menu();
            }
            else
            {
                UIManager.Instance.disConnectBtn.onClick.Invoke();

                UIManager.Instance.endLessBtn.interactable = true;
                UIManager.Instance.shootingRangeBtn.interactable = true;
            }

        }

        

        #endregion

        #region GAME

        public static void SetClientEnableWorkshop(bool flag)
        {
            ClientEnableWorkshop = flag;
        }
        [Serializable]
        public class BoxListItem
        {
            public ushort playerId;
            public int itemdefid;

            public BoxListItem(ushort _playerId, int _itemdefid)
            {
                playerId = _playerId;
                itemdefid = _itemdefid;
            }
        }

        public Queue<BoxListItem> boxes = new Queue<BoxListItem>();
        
        public void SetRichPreference(string gamemodeName, string mapName)
        {
            SteamFriends.SetRichPresence("gamemode", gamemodeName);
            SteamFriends.SetRichPresence("map", mapName);
            SteamFriends.SetRichPresence("steam_display", "#Playing");
        }

        [SerializeField] public Locale[] language;
        [MessageHandler((ushort) ServerToClientId.StartRound,PlayerHostedDemoMessageHandlerGroupId)]
        private static void StartRound(Message message)
        {
            uint endTick = message.GetUInt();
            
            switch (ClientServerType)
            {
                case ServerType.Endless:
                    if(Endless.Instance) Endless.Instance.StartGameClient();
                    break;
                default:
                    if (Instance.game != null)
                    {
                        Instance.game.StartGameClient(endTick);

                        if (!LoadingManager.Instance.requestDone)
                        {
                            LoadingManager.Instance.requestDone = true;
                        }
                    }
                    break;
            }
            
        }
        [MessageHandler((ushort) ServerToClientId.StopRound,PlayerHostedDemoMessageHandlerGroupId)]
        private static void StopRound(Message message)
        {
            ServerType serverType = (ServerType) message.GetUShort();
            switch (serverType)
            {
                case ServerType.Endless:
                    if (Endless.Instance)
                    {
                        Endless.Instance.ClientStop();
                    }
                    break;
                default:
                    GameMode gameMode = (GameMode) message.GetUShort();
                    
                    if (serverType == ServerType.KnockoutRound)
                    {
                        bool end = message.GetBool();

                        ushort[] eliminatedPlayerIds = message.GetUShorts();

                        GameManager.survive = true;

                        for (int i = 0; i < eliminatedPlayerIds.Length; i++)
                        {
                            if (ClientData.TryGetValue(eliminatedPlayerIds[i], out var clientData))
                            {
                                clientData.SetEliminated(true);
                                if (clientData == LocalClientData)
                                {
                                    GameManager.survive = false;
                                }
                                if (ClientPlayer.list.TryGetValue(eliminatedPlayerIds[i], out var clientPlayer))
                                {
                                    clientPlayer.playerState.WeaponManager.DisableAllWeapons();
                                    clientPlayer.player.gameObject.SetActive(false);
                                    clientPlayer.Dead = true;
                                    clientPlayer.playerState.ExplodeRagdoll();
                                    Debug.Log($"{clientData.Name} Eliminated");
                                }

                                if (GameUIManager.Instance.PlayerList.TryGetValue(clientData.Id, out var playerListItem))
                                {
                                    playerListItem.SetState(PlayerListItem.PlayerItemState.Dead);
                                }
                            }
                        }

                        GameManager.finalRound = end;
                        
                        Debug.Log($"Round: {end}");
                    }

                    int count = message.GetInt();

                    List<Tuple<ushort, string, string>> list = new List<Tuple<ushort, string, string>>();

                    for (int i = 0; i < count; i++)
                    {
                        list.Add(new Tuple<ushort, string, string>(message.GetUShort(),message.GetString(),message.GetString()));
                    }

                    var boxReceivedIds = message.GetUShorts();

                    var winner = list[0];

                    ushort winnerId = winner.Item1;
                    string name = winner.Item2;
                    string description = winner.Item3;
                    if (ClientPlayer.list.ContainsKey(Instance.Client.Id))
                    {
                        GameManager.lastKill = ClientPlayer.list[Instance.Client.Id].Kills;
                        GameManager.lastKill = ClientPlayer.list[Instance.Client.Id].Deaths;
                    }
                    else
                    {
                        GameManager.lastKill = 0;
                        GameManager.lastKill = 0;
                    }
                
                    GameManager.win = winnerId == Instance.Client.Id;

                    int j = 0;
                    foreach (var item in list)
                    {
                        if (ClientPlayer.list.TryGetValue(item.Item1, out var p))
                        {
                            switch (j)
                            {
                                case 0:
                                    WinnerPlayerDisplay.Instance.Enable(new []{WinnerPlayerDisplay.Instance.firstPlayer, GameUIManager.Instance.firstWinner});
                                    WinnerPlayerDisplay.Instance.SetPlayerCosmetics(WinnerPlayerDisplay.Instance.firstCosmetics, p.playerState.CosmeticIndex);
                                    break;
                                case 1:
                                    WinnerPlayerDisplay.Instance.Enable(new []{WinnerPlayerDisplay.Instance.secondPlayer, GameUIManager.Instance.secondWinner});
                                    WinnerPlayerDisplay.Instance.SetPlayerCosmetics(WinnerPlayerDisplay.Instance.secondCosmetics, p.playerState.CosmeticIndex);
                                    GameUIManager.Instance.winSecondText.SetText(item.Item2 + "\n<size=12>" + item.Item3 + "</size>");
                                    break;
                                case 2:
                                    WinnerPlayerDisplay.Instance.Enable(new []{WinnerPlayerDisplay.Instance.thirdPlayer, GameUIManager.Instance.thirdWinner});
                                    WinnerPlayerDisplay.Instance.SetPlayerCosmetics(WinnerPlayerDisplay.Instance.thirdCosmetics, p.playerState.CosmeticIndex);
                                    GameUIManager.Instance.winThirdText.SetText(item.Item2 + "\n<size=12>" + item.Item3 + "</size>");
                                    break;
                            }
                        }

                        ++j;
                    }

                    if (ClientPlayer.list.TryGetValue(winnerId,out var player))
                    {
                        ClientPlayer myPlayer = ClientPlayer.list[Instance.Client.Id];
                        string team;
                        switch (gameMode)
                        {
                            case GameMode.Infected:
                                if (GameUIManager.Instance)
                                {
                                    GameManager.win = player.playerState.IsInfected == myPlayer.playerState.IsInfected;
                                    GameUIManager.Instance.gameScene.SetActive(false);
                                    GameUIManager.Instance.mainMenu.SetActive(false);

                                    GameUIManager.Instance.winFirstText.SetText(name + "\n<size=24>" + description + "</size>");
                                    // string infectWinStr = player.playerState.IsInfected
                                    //     ? UIManager.IsItChinese() ?"<color=green>感染者</color> <color=yellow>获胜</color>" : "<color=green>Infected dave</color> <color=yellow>Win</color>"
                                    //     :  UIManager.IsItChinese() ?"幸存者<color=yellow>获胜</color>" :"Normal Dave <color=yellow>Win</color>";
                                    // GameUIManager.Instance.winner.SetText(infectWinStr);
                                    // GameUIManager.Instance.endScreen.SetActive(true);
                                }
                                break;
                            case GameMode.TeamDeathMatch:
                                team = player.playerState.Team == Team.Rebel ? myPlayer.playerState.Team == Team.Rebel
                                        ?
                                        "RebelWon_rebelside"
                                        : "RebelWon_allianceside" :
                                    myPlayer.playerState.Team == Team.Rebel ? "AllianceWon_rebelside" : "AllianceWon_allianceside";

                                GameManager.win = player.playerState.Team == myPlayer.playerState.Team;
                                
                                if (GameUIManager.Instance)
                                {
                                    GameUIManager.Instance.gameScene.SetActive(false);
                                    GameUIManager.Instance.mainMenu.SetActive(false);
                                    // GameUIManager.Instance.winnerNameText.SetText($"{name} : {description}");
                    
                                    GameUIManager.Instance.winFirstText.SetText(name + "\n<size=24>" + description + "</size>");
                                    // GameUIManager.Instance.winner.GetComponent<LocalizeStringEvent>().SetEntry(team);
                                    // GameUIManager.Instance.endScreen.SetActive(true);
                                }
                                break;
                            case GameMode.KingOfTheHill:
                                if (GameUIManager.Instance)
                                {
                                    GameUIManager.Instance.gameScene.SetActive(false);
                                    GameUIManager.Instance.mainMenu.SetActive(false);
                                    // GameUIManager.Instance.winnerNameText.SetText($"{name} : {description}");
                    
                                    GameUIManager.Instance.winFirstText.SetText(name + "\n<size=24>" + description + "</size>");

                                    // GameUIManager.Instance.winner.SetText("The best monke");
                                    // GameUIManager.Instance.endScreen.SetActive(true);
                                }
                                break;
                            case GameMode.CatchTheBanana:
                                
                                if (GameUIManager.Instance)
                                {
                                    GameUIManager.Instance.gameScene.SetActive(false);
                                    GameUIManager.Instance.mainMenu.SetActive(false);
                                    // GameUIManager.Instance.winnerNameText.SetText($"{name} : {description}");
                    
                                    GameUIManager.Instance.winFirstText.SetText(name + "\n<size=24>" + description + "</size>");
                                    // GameUIManager.Instance.winner.SetText("The best player in this match");
                                    // GameUIManager.Instance.endScreen.SetActive(true);
                                }

                                break;
                            default:
                                if (GameUIManager.Instance)
                                {
                                    GameUIManager.Instance.gameScene.SetActive(false);
                                    GameUIManager.Instance.mainMenu.SetActive(false);
                    
                                    GameUIManager.Instance.winFirstText.SetText(name + "\n<size=24>" + description + "</size>");
                                    // GameUIManager.Instance.winner.SetText("The best player in this match");
                                    // GameUIManager.Instance.endScreen.SetActive(true);
                                }
                                break;
                        }
                        
                        EndScreenUI.Instance.SetEndScreenValue(name, description);
                    }
                    
                    if (GameManager.win || GameManager.survive)
                    {
                        QuestManager.Instance.GetProgress(QuestType.Win);

                        AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.SUCK_MY_BANANA);

                        if (ClientPlayer.list.ContainsKey(winnerId) && !ClientPlayer.list[winnerId].hitted)
                        {
                            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.I_AM_A_ROCK);
                        }
                    
                        if (ClientPlayer.list.ContainsKey(winnerId) && !ClientPlayer.list[winnerId].moved)
                        {
                            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.RED_LIGHT_GREEN_LIGHT);
                        }
                    
                    
                        int wins= AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.WINS);
                        LeaderboardManager.Instance.UploadWin(wins);

                        switch (gameMode)
                        {
                            case GameMode.TeamDeathMatch:
                                if (ClientPlayer.list.ContainsKey(winnerId) && ClientPlayer.list[winnerId].Kills > 10)
                                {
                                    //winner winner banana dinner;
                                    VoiceLine.Instance.PlayVoice(VoiceKey.tdm_win_A);
                                }
                                else
                                {
                                    //your teammate is good,but don’t you
                                    VoiceLine.Instance.PlayVoice(VoiceKey.tdm_win_B);
                                }
                                break;
                            default:
                                if (ClientPlayer.list.ContainsKey(winnerId) && ClientPlayer.list[winnerId].Kills > 20)
                                {
                                    //On the surface you won, but you didn't
                                    VoiceLine.Instance.PlayVoice(VoiceKey.bw_A);
                                }
                                break;
                        }
                    }
                    else
                    {
                        if(Random.Range(0,10) < 5)
                            //don’t worry,you will banana next time
                            VoiceLine.Instance.PlayVoice(VoiceKey.bw_B);
                    }

                    switch (serverType)
                    {
                        case ServerType.KnockoutRound:
                            AudioManager.Instance.Play(GameManager.survive ? "game_end" : "ShootingTargetFailed");
                            break;
                        default:
                            AudioManager.Instance.Play("game_end");
                            break;
                    }

                    if(Instance.game!=null)
                        Instance.game.StopGameClient(boxReceivedIds);
                    break;
            }
            
        }


        public void SetIsWorkshopMap(bool flag)
        {
            IsWorkshopMap = flag;
        }
        
    
        #endregion
    
        #region SENDMESSAGE

        private string _personalName="";

        public string PersonalName
        {
            get
            {
                if (string.IsNullOrEmpty(_personalName))
                {
                    _personalName = SteamFriends.GetPersonaName();
                }

                return _personalName;
            }
        }
        void Init()
        {
            if (UIManager.Instance)
            {
                UIManager.Instance.SetConnectingState("Initializing");
            }
            Debug.Log("trying to initialize");
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.Init);
            
            message.Add(PersonalName);
            message.Add(currentGroup.m_SteamID);
            var desc = GameManager.Instance.setting.description.Length > 100 ? GameManager.Instance.setting.description.Substring(0, 100) : GameManager.Instance.setting.description;
            message.Add(desc);
            message.Add(LevelManager.Instance.levelSystem.GetExp());
            message.Add(displayTag);
        
            message.Add(Weapons);
            ushort[] perks = 
            {
                (ushort) PerkManager.Instance.perks[0], (ushort) PerkManager.Instance.perks[1],
                (ushort) PerkManager.Instance.perks[2]
            };
            message.Add(perks);

            int len = InventoryManager.SerializeInventory.Length;
            
            message.Add(len);
            
            if (len <= MaxSplitPacketSize)
            {
                message.Add(InventoryManager.SerializeInventory);
            }
            else
            {
                byte[] bytes = new byte[MaxSplitPacketSize];
                
                Array.Copy(InventoryManager.SerializeInventory, 0, bytes, 0 , MaxSplitPacketSize);
                message.Add(bytes);
            }

            SendByte += message.WrittenLength;
            Client.Send(message);
            
            if(len > MaxSplitPacketSize) 
                Invoke(nameof(SendFragmentSerializeInventory), 0.3f);
        }

        private void SendFragmentSerializeInventory()
        {
            int len = InventoryManager.SerializeInventory.Length;
            int offset = len % MaxSplitPacketSize == 0 ? 0 : 1;

            int loop = (int)(len / MaxSplitPacketSize + offset - 1);
            
            for (int i = 0; i < loop; i++)
            {
                int d = (int)((i + 1) * MaxSplitPacketSize);
                int index = i + 1 >= loop ? len - d : (int)MaxSplitPacketSize;
                byte[] bytes = new byte[index];

                Array.Copy(InventoryManager.SerializeInventory, d, bytes, 0 , index);

                Message message  = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.FragmentSerializeInventory);

                message.Add(bytes);
                
                SendByte += message.WrittenLength;
                Client.Send(message);
            }
        }
        public void HitEnemy(ushort id,Vector3 normal)
        {
            if (!ClientEnemy.list.ContainsKey(id)) return;
            Quaternion rot = Quaternion.LookRotation(normal);
            Vector3 pos = ClientEnemy.list[id].transform.position;
            if (GameManager.Instance.setting.spawnParticle)
            {
                // Instantiate(PrefabManager.Instance.GetPrefab("robotHit"), pos, rot);
                ObjectPooler.Instance.SpawnFromPool("BulletHit",pos,rot);
                // ObjectPooler.Instance.SpawnFromPool("Blood",pos,rot);
                Instantiate(PrefabManager.Instance.GetPrefab("robotHIt2"), pos, rot);
            }

            //Debug.Log($"Client Pos: {pos}");
            HitMarker.Instance.StartHitMarkerRobot(Color.white);
        }
        
        //0 -- player
        //1 -- achievement
        //2 -- crate open
        public void SendMsg(string msg,int type)
        {
            Message message = Message.Create(MessageSendMode.Unreliable,(ushort)ClientToServerId.SendMessage);
            message.Add(msg);
            message.Add(type);
            SendByte += message.WrittenLength;
            Instance.Client.Send(message);
        }

        #endregion
    
        #region HANDLEMESSAGE

        void SetTick(uint serverTick)
        {
            uint sT = serverTick;
            if (Mathf.Abs(ServerTick - sT) > tickDivergenceTolerance)
            {
                Debug.Log($"Client Tick: {ServerTick} -> {sT}");
                ServerTick = sT;
            }
        }

        [MessageHandler((ushort) ServerToClientId.Init, PlayerHostedDemoMessageHandlerGroupId)]
        private static void Init(Message message)
        {
            ServerType serverType = (ServerType) message.GetUShort();
            ClientServerType = serverType;
            
            bool randomMap = message.GetBool();
            bool randomGameMode = message.GetBool();
            bool disableSpecialWeapon = message.GetBool();
            bool enableWorkshop = message.GetBool();
            bool cheatsEnable = message.GetBool();
            int clientDataCount = message.GetInt();
            
            ClientRandomMap = randomMap;
            ClientRandomGameMode = randomGameMode;
            ClientDisableSpecialWeapon = disableSpecialWeapon;
            ClientEnableWorkshop = enableWorkshop;
            ClientCheatsEnabled = cheatsEnable;
            ClientDataCount = clientDataCount;
            
            for (int i = 0; i < Instance.weaponInfo.Count;i++)
            {
                AllowedWeapon[Instance.weaponInfo[i].weaponName] = message.GetBool();
            }

            if (enableWorkshop)
            {
                int workshopMapCount = message.GetInt();

                WorkshopMaps = new ulong[workshopMapCount];

                for (int i = 0; i < workshopMapCount; i++)
                {
                    WorkshopMaps[i] = message.GetULong();
                }
                DownloadItemMenu.Instance.NeedToDownload(WorkshopMaps);
                // if(NetworkServerManager.Instance.Server.IsRunning)
                //     Instance.TryToAuthorize();
                // else
                //     DownloadItemMenu.Instance.NeedToDownload(WorkshopMaps);
            }
            else
            {
                Instance.TryToAuthorize();
            }
        }

        [MessageHandler((ushort)ServerToClientId.AddClientData, PlayerHostedDemoMessageHandlerGroupId)]
        private static void NewClientDataReceived(Message message)
        {
            ushort id = message.GetUShort();
            string name = message.GetString();
            ulong steamId = message.GetULong();
            ulong groupId = message.GetULong();
            bool ownedDlc = message.GetBool();
            string desc = message.GetString();
            int exp = message.GetInt();
            bool displayTag = message.GetBool();
            bool eliminated = message.GetBool();
            short[] weapons = message.GetShorts();
            ushort[] perks = message.GetUShorts();
            InventoryManager.CosmeticIndex cosmeticIndex = message.GetCosmeticIndex();

            ClientData data = new ClientData(id, name, steamId,ownedDlc ,groupId, desc, exp, displayTag,eliminated, weapons, perks)
            {
                CosmeticIndex = cosmeticIndex
            };
            
            SteamFriends.SetPlayedWith((CSteamID)steamId);

            ClientData[id] = data;

            if (id == Instance.Client.Id)
            {
                LocalClientData = data;
            }
            
            VoiceChatUIManager.Instance.NewPlayer(data);
            
            // Debug.Log($"{id } {name}");
        }
        public void TryToAuthorize()
        {
            if(_authorizeCoroutine != null)
                StopCoroutine(_authorizeCoroutine);
            _authorizeCoroutine = StartCoroutine(TryToAuthorizeCoroutine());
        }

        private Coroutine _authorizeCoroutine;

        IEnumerator TryToAuthorizeCoroutine()
        {
            if (UIManager.Instance)
            {
                UIManager.Instance.SetConnectingState("Syncing");
            }
            
            while (ClientData.Count < ClientDataCount)
            {
                yield return null;
            }
            
            if (UIManager.Instance)
            {
                UIManager.Instance.SetConnectingState("Authorizing");
            }
            
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.Authorize);

            byte[] mTicket = new byte[1024];

            SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
            // identity.SetSteamID(((SteamConnection)Client.Connection).SteamId);
            _currentTicket=SteamUser.GetAuthSessionTicket(mTicket, 1024, out var pcbTicket, ref identity);

            byte[] realTicket = new byte[pcbTicket];

            for (int i = 0; i < pcbTicket; i++)
            {
                realTicket[i] = mTicket[i];
            }

            message.Add(realTicket);
            message.Add(SteamUser.GetSteamID().m_SteamID);
            message.Add(SteamFriends.GetPersonaName());
            
            Client.Send(message);
        }

        [MessageHandler((ushort)ServerToClientId.ServerProperties, PlayerHostedDemoMessageHandlerGroupId)]
        private static void ServerProperties(Message message)
        {
            NetworkServerManager.ServerPropertiesType type =
                (NetworkServerManager.ServerPropertiesType)message.GetUShort();
            
            string value = String.Empty;
            string valueType = String.Empty;

            switch (type)
            {
                case NetworkServerManager.ServerPropertiesType.CheatsEnabled:
                    ClientCheatsEnabled = message.GetBool();

                    valueType = "sv_cheats";
                    value = ClientCheatsEnabled.ToString().ToLower();

                    if (!ClientCheatsEnabled && PlayerMovement.Instance && PlayerMovement.Instance.IsNoclip())
                    {
                        PlayerMovement.Instance.ChangeNoClip();
                    }
                    
                    break;
            }
            
            Chat.Instance.AddMessage($"Server: {valueType} is now set to {value}", Color.white);
        }
        
        [MessageHandler((ushort) ServerToClientId.Authorized, PlayerHostedDemoMessageHandlerGroupId)]
        private static void Authorized(Message message)
        {
            Instance.Init();
        }
        
        [MessageHandler((ushort) ServerToClientId.Sync, PlayerHostedDemoMessageHandlerGroupId)]
        private static void Sync(Message message)
        {
            Instance.SetTick(message.GetUInt());
        }

        [MessageHandler((ushort) ServerToClientId.SetGameState, PlayerHostedDemoMessageHandlerGroupId)]
        private static void SetGameState(Message message)
        {
            GameState = (GameState) message.GetUShort();
        }
        [MessageHandler((ushort) ServerToClientId.ServerSetting, PlayerHostedDemoMessageHandlerGroupId)]
        private static void ServerSetting(Message message)
        {
            ClientRandomMap = message.GetBool();
            ClientRandomGameMode = message.GetBool();
            ClientDisableSpecialWeapon = message.GetBool();

            string serverName = message.GetString();

            if (ServerSettingUI.Instance)
            {
                ServerSettingUI.Instance.SetServerSetting(serverName);
            }
        }
        
        [MessageHandler((ushort) ServerToClientId.SomeoneGetBox, PlayerHostedDemoMessageHandlerGroupId)]
        private static void SomeoneGetBox(Message message)
        {
            ushort playerId = message.GetUShort();
            int itemdefid = message.GetInt();
            BoxListItem item = new BoxListItem(playerId, itemdefid);
            Instance.boxes.Enqueue(item);
        }
        [MessageHandler((ushort) ServerToClientId.Message, PlayerHostedDemoMessageHandlerGroupId)]
        private static void GetMessage(Message message)
        {
            MessageType type = (MessageType) message.GetUShort();
            ushort se;
            float clearTimer = 1.5f;
            float waitTimer = 0;
            switch (type)
            {
                case MessageType.Infect:
                    se = message.GetUShort();
                    if (GameUIManager.Instance)
                    {
                        GameUIManager.Instance.message.SetEntry("Infected_Message");
                        GameUIManager.Instance.message.StringReference.Arguments = new List<object>() {se.ToString()};
                    }
                    break;
                case MessageType.KingOfTheHill:
                    se = message.GetUShort();
                    if (se <= 5)
                    {
                        AudioManager.Instance.Play("ticking");
                    }
                    if (GameUIManager.Instance)
                    {
                        GameUIManager.Instance.message.SetEntry("KingOfTheHill_Message");
                        GameUIManager.Instance.message.StringReference.Arguments = new List<object>() {se.ToString()};
                    }
                    break;
                case MessageType.OneVsOne0:
                    waitTimer = 1.5f;
                    clearTimer = 5f;
                    if (GameUIManager.Instance)
                    {
                        GameUIManager.Instance.message.SetEntry("onevsone_0");
                    }
                    break;
                case MessageType.OneVsOne1:
                    waitTimer = 1.5f;
                    clearTimer = 5f;
                    if (GameUIManager.Instance)
                    {
                        GameUIManager.Instance.message.SetEntry("onevsone_1");
                    }
                    break;
                case MessageType.Wave:
                    
                    se = message.GetUShort();
                    if (se <= 3)
                    {
                        AudioManager.Instance.Play("ticking");
                    }
                    if (GameUIManager.Instance)
                    {
                        GameUIManager.Instance.message.SetEntry("wave_fresh");
                        GameUIManager.Instance.message.StringReference.Arguments = new List<object>() {se.ToString()};
                    }

                    break;
            }

            Instance.DisplayMessage(waitTimer,clearTimer);
        }

        [MessageHandler((ushort)ServerToClientId.ClientEliminated, PlayerHostedDemoMessageHandlerGroupId)]
        private static void ClientEliminated(Message message)
        {
            ushort id = message.GetUShort();
            bool eliminated = message.GetBool();

            if (ClientData.TryGetValue(id, out var clientData))
            {
                clientData.SetEliminated(eliminated);
            }
        }
        public void DisplayMessage(float waitTimer, float clearTimer)
        {
            StartCoroutine(ShowMessage(waitTimer, clearTimer));
        }
        IEnumerator ShowMessage( float waitTimer,float clearTimer)
        {
            yield return new WaitForSeconds(waitTimer);
        
            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.message.RefreshString();
                GameUIManager.Instance.ShowMessage(clearTimer);
            }
        }   
        
        #endregion
    
        #region REFERENCE
        public bool IsTeamMode(PlayerState player)
        {
            return (ClientGameMode == GameMode.TeamDeathMatch && ClientPlayer.list.ContainsKey(Client.Id) &&
                    player.Team == ClientPlayer.list[Client.Id].playerState.Team) || 
                   (ClientServerType == ServerType.Endless);
        }
        
    
        public bool CantPlay(bool cursor = true)
        {
            cursor &= Cursor.lockState != CursorLockMode.Locked;
            return (GameUIManager.Instance&&GameUIManager.Instance.pause)||
                   (TeamSelector.Instance&&TeamSelector.Instance.IsSelecting) ||
                   cursor || Chat.Instance.IsChat() 
                   || DeveloperConsoleUI.Instance.uiCanvas.activeSelf ||
                   (BuyWeaponMenu.Instance&&BuyWeaponMenu.Instance.Buying);
        }
    
        public bool CheckMultiplayerGameModeStarted()
        {
            switch (ClientServerType)
            {
                case ServerType.Endless:
                    break;  
                default:
                    if (Instance.game)
                    {
                        if(!Instance.game.started || Instance.game.stopped)
                            return true;
                    }
                    break;
            }
            

            return false;
        }

        public Texture2D GetWeaponTexture(string name)
        {
            foreach (var weapon in weaponInfo)
            {
                if (weapon.weaponName == name)
                {
                    return weapon.texture;
                }
            }

            return PrefabManager.Instance.GetTexture2D("KillSecured");
        }
        public string GetWeaponChineseName(string name)
        {
            foreach (var weapon in weaponInfo)
            {
                if (weapon.weaponName == name)
                {
                    return weapon.chineseName;
                }
            }

            return "";
        }

        public PublishedFileId_t GetCurrentWorkshopMap()
        {
            if (IsWorkshopMap)
            {
                if (ulong.TryParse(MapId, out var id))
                {
                    return (PublishedFileId_t)id;
                }
            }
            return PublishedFileId_t.Invalid;
        }
        #endregion
    }
}