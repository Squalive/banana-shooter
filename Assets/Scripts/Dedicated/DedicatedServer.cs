#if UNITY_SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using MapEditor;
using Multiplayer;
using Multiplayer.Entity.Server;
using Newtonsoft.Json;
using Save;
using Steamworks;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dedicated
{
    /// <summary>
    /// Entry point of the Dedicated Server build. The normal boot scene still loads every manager;
    /// this logs the server into Steam as a game server, starts Riptide and loads maps itself,
    /// which on a listen server is done by the host's client.
    /// </summary>
    public class DedicatedServer : MonoBehaviour
    {
        public static DedicatedServer Instance { get; private set; }

        const float UpdateCheckInterval = 300f;
        const float UpdateShutdownDelay = 180f;
        const float SteamLogOnTimeout = 30f;
        const float WorkshopDownloadTimeout = 300f;
        static readonly AppId_t AppId = new(1949740);

        string _serverId = "MyServer";
        ushort _port = 27015;
        ushort _maxPlayers = 40;
        Config _config;
        string _serverDir;
        bool _loggedOn;

        readonly StringBuilder _consoleInput = new();
        bool _consoleOpen = true;

        Callback<SteamServersConnected_t> _steamConnected;
        Callback<SteamServerConnectFailure_t> _steamFailed;
        Callback<DownloadItemResult_t> _workshopDownloaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Instance = new GameObject(nameof(DedicatedServer)).AddComponent<DedicatedServer>();
            DontDestroyOnLoad(Instance.gameObject);
        }

        IEnumerator Start()
        {
            ParseArgs();
            _serverDir = Path.Combine(Directory.GetCurrentDirectory(), "Servers", _serverId);
            _config = LoadJson<Config>(Path.Combine(_serverDir, "Config.json"));

            // Managers come from the boot scene; GameManager also applies the client frame cap when it finishes.
            float deadline = Time.realtimeSinceStartup + 10f;
            yield return new WaitUntil(() => GameManager.Initialized || Time.realtimeSinceStartup > deadline);
            yield return new WaitUntil(() => NetworkServerManager.Instance != null && NetworkServerManager.Instance.Server != null);

            // No rendering, so without a cap the main loop spins a whole core.
            QualitySettings.vSyncCount = 0;
            // Unity starts a job worker per extra core and each wakes every frame; on a 6-core box
            // that was half the idle CPU (19% vs 10% of a core) for a server with almost no jobs.
            // ponytail: one worker; raise it if profiling under load shows the main thread waiting on jobs.
            JobsUtility.JobWorkerCount = 1;
            Application.targetFrameRate = Mathf.RoundToInt(1f / Time.fixedDeltaTime);
            Time.maximumDeltaTime = 0.1f;

            // Only round-based types have a server-side flow; the others need a host client.
            NetworkServerManager.SetServerType(_config.Game.ServerType == ServerType.KnockoutRound ? ServerType.KnockoutRound : ServerType.Normal);
            NetworkServerManager.ServerRandomMap = _config.Game.RandomMap;
            NetworkServerManager.ServerRandomGameMode = _config.Game.RandomGameMode;
            NetworkServerManager.MinimalPlayerCount = _config.Game.MinimalPlayerAmount;
            NetworkServerManager.DlcOnly = _config.Server.DlcOnly;

            if (!StartSteam())
            {
                Application.Quit(1);
                yield break;
            }

            RolesManager.Instance.TryToInitialize();

            // Workshop downloads need the Steam login, and players need the maps before they can vote on them.
            float logOnDeadline = Time.realtimeSinceStartup + SteamLogOnTimeout;
            yield return new WaitUntil(() => _loggedOn || Time.realtimeSinceStartup > logOnDeadline);
            yield return DownloadWorkshopMaps();
            SetGameTags();

            NetworkServerManager.ClientData.Clear();
            NetworkServerManager.SetIsPlaying(false);
            NetworkServerManager.Instance.Server.Start(_port, _maxPlayers, NetworkManager.PlayerHostedDemoMessageHandlerGroupId);

            Debug.Log($"[Dedicated] '{_config.Browser.ServerName}' listening on port {_port}, {_maxPlayers} slots");

            if (_config.Server.UpdateRestart)
                InvokeRepeating(nameof(CheckForUpdate), UpdateCheckInterval, UpdateCheckInterval);
        }

        bool StartSteam()
        {
            var mode = _config.Server.VacSecure ? EServerMode.eServerModeAuthenticationAndSecure : EServerMode.eServerModeAuthentication;
            if (!GameServer.Init(0, _port, (ushort)(_port + 1), mode, Application.version))
            {
                Debug.LogError("[Dedicated] GameServer.Init failed. Is steam_appid.txt next to the executable and the port free?");
                return false;
            }

            SteamGameServer.SetModDir("Banana Shooter");
            SteamGameServer.SetProduct("1949740");
            SteamGameServer.SetGameDescription("Banana Shooter");
            SteamGameServer.SetDedicatedServer(true);
            SteamGameServer.SetServerName(_config.Browser.ServerName);
            SetGameTags();
            SteamGameServer.SetMaxPlayerCount(_maxPlayers);

            _steamConnected = Callback<SteamServersConnected_t>.CreateGameServer(_ =>
            {
                _loggedOn = true;
                Debug.Log($"[Dedicated] Logged in to Steam as {SteamGameServer.GetSteamID()}");
            });
            _steamFailed = Callback<SteamServerConnectFailure_t>.CreateGameServer(r => Debug.LogError($"[Dedicated] Steam login failed: {r.m_eResult}"));

            if (string.IsNullOrEmpty(_config.Server.LoginToken))
                SteamGameServer.LogOnAnonymous();
            else
                SteamGameServer.LogOn(_config.Server.LoginToken);

            SteamGameServer.SetAdvertiseServerActive(true);
            return true;
        }

        // The server browser reads these by position: server type; workshop; DLC only; description.
        void SetGameTags()
        {
            SteamGameServer.SetGameTags($"{(int)NetworkServerManager.ServerType};{(NetworkServerManager.ServerEnableWorkshop ? 1 : 0)};{(_config.Server.DlcOnly ? 1 : 0)};{_config.Browser.DescriptionShort}");
        }

        IEnumerator DownloadWorkshopMaps()
        {
            var workshop = LoadJson<WorkshopConfig>(Path.Combine(_serverDir, "SteamWorkshopConfig.json"));
            if (!workshop.Enabled || workshop.Items.Count == 0) yield break;

            string content = Path.Combine(_serverDir, "Workshop", "Content");
            Directory.CreateDirectory(content);
            if (!SteamGameServerUGC.BInitWorkshopForGameServer(new DepotId_t(AppId.m_AppId), content))
            {
                Debug.LogError("[Dedicated] Could not start the Steam Workshop for this server, workshop maps are off");
                yield break;
            }

            var pending = new HashSet<ulong>(workshop.Items);
            _workshopDownloaded = Callback<DownloadItemResult_t>.CreateGameServer(result =>
            {
                if (result.m_unAppID == AppId && pending.Remove(result.m_nPublishedFileId.m_PublishedFileId))
                    RegisterWorkshopMap(result.m_nPublishedFileId, result.m_eResult);
            });

            Debug.Log($"[Dedicated] Downloading {pending.Count} workshop maps");
            foreach (var id in workshop.Items)
                if (!SteamGameServerUGC.DownloadItem(new PublishedFileId_t(id), true) && pending.Remove(id))
                    RegisterWorkshopMap(new PublishedFileId_t(id), EResult.k_EResultFail);

            float deadline = Time.realtimeSinceStartup + WorkshopDownloadTimeout;
            yield return new WaitUntil(() => pending.Count == 0 || Time.realtimeSinceStartup > deadline);
            foreach (var id in pending)
                RegisterWorkshopMap(new PublishedFileId_t(id), EResult.k_EResultTimeout);

            NetworkServerManager.SetServerEnableWorkshop(NetworkServerManager.EnabledWorkshopMaps.Count > 0);
            NetworkServerManager.SetWorkshopMapVote();
            Debug.Log($"[Dedicated] {NetworkServerManager.EnabledWorkshopMaps.Count}/{workshop.Items.Count} workshop maps ready");
        }

        // A failed download can still leave a copy from an earlier run, which is better than dropping the map.
        static void RegisterWorkshopMap(PublishedFileId_t id, EResult result)
        {
            if (SteamGameServerUGC.GetItemInstallInfo(id, out _, out var folder, 1024, out _) && MapSaver.Instance.LoadWorkshopMap(folder, id))
                NetworkServerManager.EnabledWorkshopMaps.Add(id);
            else
                Debug.LogError($"[Dedicated] Workshop item {id} is not available: {result}");
        }

        void Update()
        {
            if (_steamConnected != null)
                GameServer.RunCallbacks();

            PollConsole();
        }

        void OnApplicationQuit()
        {
            NetworkServerManager.Instance.StopServer();
            SteamGameServer.SetAdvertiseServerActive(false);
            SteamGameServer.LogOff();
            GameServer.Shutdown();
        }

        /// <summary>Called by NetworkServerManager right after it tells the clients which map to load.</summary>
        public void LoadMap() => StartCoroutine(LoadMapRoutine());

        IEnumerator LoadMapRoutine()
        {
            var server = NetworkServerManager.Instance;
            ClearPlayers();

            // Game modes enable themselves by comparing against the client-side mode.
            NetworkManager.ClientGameMode = NetworkServerManager.ServerGameMode;

            if (server.IsWorkshopMap)
            {
                yield return LoadWorkshopMap(server.WorkshopMap);
            }
            else
            {
                SteamGameServer.SetMapName(server.CurrentMap);
                yield return SceneManager.LoadSceneAsync(server.CurrentMap);
            }

            server.SetGameState(GameState.Warmup);

            var game = GameModes.Create(NetworkServerManager.ServerGameMode, MapBound.Instance.gameObject);
            server.game = game;
            NetworkManager.Instance.game = game;

            server.StartGameFromLoad();
        }

        // Same steps as the client's LoadingManager.JoinWorkshopMap: read the map file, load the empty
        // CustomMap scene, then build the map's geometry (and colliders) into it.
        IEnumerator LoadWorkshopMap(PublishedFileId_t id)
        {
            if (!MapSaver.WorkshopMaps.TryGetValue(id, out var file))
            {
                Debug.LogError($"[Dedicated] Workshop map {id} is not downloaded");
                yield break;
            }

            var read = new CoroutineWithData(this, SaveSystem.ReadFileAsyncThread(file.FullName));
            yield return read.coroutine;
            var parse = Task.Run(() => JsonConvert.DeserializeObject<MapData>(read.result.ToString()));
            yield return new WaitUntil(() => parse.IsCompleted);

            MapData data = parse.IsFaulted ? null : parse.Result;
            if (data == null || string.IsNullOrEmpty(data.name))
            {
                Debug.LogError($"[Dedicated] Workshop map {id} could not be read");
                yield break;
            }

            data.path = file.FullName.Substring(0, file.FullName.Length - file.Name.Length);
            SteamGameServer.SetMapName(data.name);

            yield return SceneManager.LoadSceneAsync("CustomMap");
            var build = MapSaver.Instance.LoadWorkshopMap(id, data);
            yield return new WaitUntil(() => build.IsCompleted);
        }

        /// <summary>On a listen server the host's scene reset does this when voting starts or a map loads.</summary>
        public static void ClearPlayers()
        {
            foreach (var player in ServerPlayer.list.Values)
                if (player != null) player.Destroy();
            ServerPlayer.list.Clear();
            GameManager.Entities.Clear();
        }

        void CheckForUpdate()
        {
            if (!SteamGameServer.WasRestartRequested()) return;

            CancelInvoke(nameof(CheckForUpdate));
            float delay = NetworkServerManager.Instance.Server.ClientCount == 0 ? 0f : UpdateShutdownDelay;
            Debug.Log($"[Dedicated] Steam requested a restart for a game update, shutting down in {delay}s");
            Invoke(nameof(Quit), delay);
        }

        void Quit() => Application.Quit();

        // A thread blocked in Console.ReadLine keeps IL2CPP from finishing shutdown while stdin is open
        // (terminal, docker -it), so stdin is polled on the main thread instead.
#if UNITY_STANDALONE_LINUX
        [StructLayout(LayoutKind.Sequential)]
        struct PollFd
        {
            public int Fd;
            public short Events;
            public short Revents;
        }

        const short PollIn = 1;

        [DllImport("libc.so.6", EntryPoint = "poll")]
        static extern int Poll(ref PollFd fds, uint count, int timeoutMs);

        [DllImport("libc.so.6", EntryPoint = "read")]
        static extern IntPtr Read(int fd, byte[] buffer, IntPtr count);

        readonly byte[] _readBuffer = new byte[1024];

        void PollConsole()
        {
            var stdin = new PollFd { Fd = 0, Events = PollIn };
            while (_consoleOpen && Poll(ref stdin, 1, 0) > 0)
            {
                int read = (int)Read(0, _readBuffer, (IntPtr)_readBuffer.Length);
                if (read <= 0)
                {
                    _consoleOpen = false;
                    return;
                }

                _consoleInput.Append(Encoding.UTF8.GetString(_readBuffer, 0, read));
                for (int newline; (newline = _consoleInput.ToString().IndexOf('\n')) >= 0;)
                {
                    RunCommand(_consoleInput.ToString(0, newline));
                    _consoleInput.Remove(0, newline + 1);
                }
            }
        }
#else
        void PollConsole() { }
#endif

        void RunCommand(string line)
        {
            string[] args = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0) return;

            var server = NetworkServerManager.Instance;
            switch (args[0].ToLowerInvariant())
            {
                case "status":
                    string map = server.IsWorkshopMap ? $"workshop {server.WorkshopMap}" : server.CurrentMap;
                    Debug.Log($"[Dedicated] {NetworkServerManager.GameState}, map '{map}', {server.Server.ClientCount}/{_maxPlayers} players");
                    foreach (var data in NetworkServerManager.ClientData.Values)
                        Debug.Log($"  {data.Id}  {data.SteamId}  {data.Name}");
                    break;
                case "kick" when args.Length > 1 && ushort.TryParse(args[1], out var id):
                    server.Server.DisconnectClient(id, NetworkServerManager.GetDisconnectMessage("Kicked"));
                    break;
                case "start":
                    server.ForceStart();
                    break;
                case "quit":
                    Application.Quit();
                    break;
                default:
                    Debug.Log("[Dedicated] Commands: status, kick <id>, start, quit");
                    break;
            }
        }

        void ParseArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                string value = args[i + 1];
                switch (args[i].ToLowerInvariant())
                {
                    case "+server":
                        _serverId = value;
                        break;
                    case "+port":
                        if (ushort.TryParse(value, out var port)) _port = port;
                        break;
                    case "+maxplayercount":
                        if (ushort.TryParse(value, out var max)) _maxPlayers = (ushort)Mathf.Clamp(max, 2, 80);
                        break;
                }
            }
        }

        static T LoadJson<T>(string path) where T : new()
        {
            if (File.Exists(path))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) ?? new T();
                }
                catch (JsonException e)
                {
                    Debug.LogError($"[Dedicated] {path} is invalid, using defaults: {e.Message}");
                    return new T();
                }
            }

            var config = new T();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
            Debug.Log($"[Dedicated] Wrote default config to {path}");
            return config;
        }

        // Same file and keys as the old dedicated server's SteamWorkshopConfig.json.
        class WorkshopConfig
        {
            [JsonProperty("enabled")] public bool Enabled;
            [JsonProperty("fileUlongIds")] public List<ulong> Items = new();
        }

        // Same keys as the old dedicated server's Config.json, so existing files keep working.
        class Config
        {
            [JsonProperty("Server")] public ServerSection Server = new();
            [JsonProperty("Browser")] public BrowserSection Browser = new();
            [JsonProperty("Game")] public GameSection Game = new();

            public class ServerSection
            {
                [JsonProperty("Login_Token")] public string LoginToken = "";
                [JsonProperty("VAC_Secure")] public bool VacSecure = true;
                [JsonProperty("DLC_Only")] public bool DlcOnly;
                [JsonProperty("Enable_Update_Restart")] public bool UpdateRestart = true;
            }

            public class BrowserSection
            {
                [JsonProperty("Server_Name")] public string ServerName = "Banana Shooter";
                [JsonProperty("Description_Short")] public string DescriptionShort = "";
            }

            public class GameSection
            {
                [JsonProperty("Random_Map")] public bool RandomMap;
                [JsonProperty("Random_GameMode")] public bool RandomGameMode;
                [JsonProperty("Minimal_Player_Amount")] public ushort MinimalPlayerAmount = 2;
                [JsonProperty("Server_Type")] public ServerType ServerType = ServerType.Normal;
            }
        }
    }
}
#endif
