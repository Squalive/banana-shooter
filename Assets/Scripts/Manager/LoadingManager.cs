using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using CodingDaniel.MapEditor.MEEditor.MESave;
using Demo;
using Manager.Interface;
using MapEditor;
using Menu;
using Mode;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Entity.Server;
using Newtonsoft.Json;
using ProceduralGeneration;
using PVE;
using Riptide;
using Save;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Manager
{
    public class LoadingManager : MonoBehaviour, ILoading
    {
        public static LoadingManager Instance { private set; get; }
        public static ILoading Iinstance;

        private void Awake()
        {
            percent = 1f / step;
            if (Instance == null)
            {
                Instance = this;
                Iinstance = this;
                group = GetComponent<CanvasGroup>();
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            SetAlpha(0);

            _titleText = title.GetComponent<TextMeshProUGUI>();
            _descText = desc.GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            _networkManager = NetworkManager.Instance;
        }

        private NetworkManager _networkManager;

        [SerializeField] RawImage backGround;
        [SerializeField] LocalizeStringEvent title, progress, desc;

        private TextMeshProUGUI _titleText, _descText;

        private CanvasGroup group;

        private float desiredAlpha = 0;

        void SetAlpha(float a)
        {
            desiredAlpha = a;
            if (Math.Abs(a - 1) < 0.01f)
            {
                @group.interactable = true;
                @group.blocksRaycasts = true;
            }
            else
            {
                @group.interactable = false;
                @group.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            @group.alpha = Mathf.Lerp(@group.alpha, desiredAlpha, Time.deltaTime * 10f);

            progressBar.value = Mathf.Lerp(progressBar.value, desiredProgress, Time.deltaTime * 10f);
        }

        [SerializeField] private Button menuBtn;

        public void Leave()
        {
            StopAllCoroutines();
            if (!NetworkManager.Instance.Client.IsConnected)
                Menu();
            LobbyManager.Instance.LeaveLobby();
        }

        /// <summary>
        /// Reset Everything Then Load a Scene
        /// </summary>
        public void ResetScene(bool isWorkshopMap = false)
        {
            Time.timeScale = 1f;
            PlayerState.DisplayPlayerName = true;
            SpectateMovement.Instance.StopSpect(false);
            AudioFreqController.Instance.ResetStat();
            UnderWaterSfx.Instance.SetUnderWater(false);
            PitchManager.Instance.SetPitch(1f);
            if (WarningUI.Instance)
                WarningUI.Instance.playerSpawn = false;
            ClientPlayer.existGroup.Clear();
            GameManager.Entities.Clear();

            PlayerParticle.Instance.DeInitialize();
            Interactor.Instance.DeInitialize();
            PickInteractor.Instance.DeInitialize();
            SlideAudio.Instance.DeInitialize();

            MapSaver.Instance.Cleanup();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            backGround.texture = shootingRange;
            _networkManager.StopAllCoroutines();
            _networkManager.boxes.Clear();
            if (GameUIManager.Instance)
            {
                foreach (var item in GameUIManager.Instance.PlayerList.Values)
                {
                    Destroy(item.gameObject);
                }
                GameUIManager.Instance.PlayerList.Clear();
            }

            DemoManager.Instance.DestroyEverything();

            foreach (var clientPlayer in ClientPlayer.list.Values)
            {
                if (clientPlayer != null)
                    Destroy(clientPlayer.gameObject);
            }
            ClientPlayer.list.Clear();
            foreach (var serverPlayer in ServerPlayer.list.Values)
            {
                if (serverPlayer != null)
                    serverPlayer.Destroy();
            }
            ServerPlayer.list.Clear();

            DemoManager.Instance.StopRecord();

            title.enabled = !isWorkshopMap;
            desc.enabled = !isWorkshopMap;
            WinnerPlayerDisplay.Instance.Disable();
        }

        public Texture2D shootingRange;
        public enum MenuType
        {
            None,
            Normal,
            Kick,
            ConnectionFailed,
            HostQuit,
            Cheat,
        }

        public MenuType menuType = MenuType.None;
        public string additionalMessage = String.Empty;

        public bool isLoading = false;

        private Coroutine loadingCoroutine = null;

        public void LoadGame(string map, Texture2D texture2D, string otherText, params int[] external)
        {
            SteamUGC.StopPlaytimeTrackingForAllItems();
            if (asyncOperation != null) asyncOperation.allowSceneActivation = true;
            if (loadingCoroutine != null)
                StopCoroutine(loadingCoroutine);
            loadingCoroutine = StartCoroutine(JoinGame(map, texture2D, otherText, external));

        }

        public void LoadWorkshopGame(PublishedFileId_t map, int external)
        {
            SteamUGC.StopPlaytimeTrackingForAllItems();
            SteamUGC.StartPlaytimeTracking(new[] { map }, 1);
            StartCoroutine(JoinWorkshopMap(map, external));
        }

        public void Menu()
        {
            if (asyncOperation != null) asyncOperation.allowSceneActivation = true;
            if (loadingCoroutine != null)
                StopCoroutine(loadingCoroutine);
            loadingCoroutine = StartCoroutine(BackToMenu());
        }
        IEnumerator BackToMenu()
        {
            TransitionUI.Instance.StartTransition();

            Chat.Instance.DestroyEveryThing();
            SteamUGC.StopPlaytimeTrackingForAllItems();
            ResetScene();
            isLoading = false;
            NotificationMenu.Instance.SetCanvas(false);

            NetworkServerManager.Iinstance.ResetProperties();

            LobbyDataManager.datas.Clear();
            int lastKill = GameManager.lastKill;

            GameManager.lastKill = 0;
            GameManager.lastDie = 0;
            GameManager.getBox = false;
            GameManager.win = false;
            GameManager.hitRecently = false;
            _networkManager.boxes.Clear();

            NetworkManager.ClientRandomMap = true;
            NetworkManager.ClientRandomGameMode = true;
            NetworkManager.ClientGameMode = GameMode.None;
            _networkManager.GameModeChanged?.Invoke(NetworkManager.ClientGameMode);

            foreach (var clientPlayer in ClientPlayer.list.Values)
            {
                if (clientPlayer != null)
                    Destroy(clientPlayer.gameObject);
            }
            ClientPlayer.list.Clear();

            DemoManager.Instance.StopPlayDemo();

            // yield return new WaitForSeconds(0.2f);

            title.SetEntry("Menu");
            title.RefreshString();

            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
            progress.SetEntry("progress");
            progress.StringReference.Arguments = new List<object>() { 0 };

            while (asyncOperation.progress < 0.9f || Mathf.Abs(@group.alpha - desiredAlpha) > 0.001f)
            {
                progress.StringReference.Arguments[0] = (int)(asyncOperation.progress * 100);
                progress.RefreshString();
                yield return null;
            }
            progress.StringReference.Arguments[0] = 100;
            progress.RefreshString();

            while (!asyncOperation.isDone)
                yield return null;

            SetAlpha(0);

            FailedWindow window = null;
            switch (menuType)
            {
                case MenuType.Kick:
                    window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                        .GetComponent<FailedWindow>();
                    window.SetTitle("Kicked");
                    window.SetReason(UIManager.IsItChinese() ? $"房主把你踢了, reason: {additionalMessage}" : $"Server kicked you, reason: {additionalMessage}");
                    break;
                case MenuType.ConnectionFailed:
                    window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                        .GetComponent<FailedWindow>();
                    window.SetTitle(UIManager.IsItChinese() ? "连接失败" : "connection failed");
                    window.SetReason(UIManager.IsItChinese() ? "网络问题或者服务器无了" : "network problem or server has been gone");
                    break;
                case MenuType.HostQuit:
                    window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                        .GetComponent<FailedWindow>();
                    window.SetTitle(UIManager.IsItChinese() ? "房主退出" : "Host Quit");
                    window.SetReason(UIManager.IsItChinese() ? "看起来腐竹好像跑路了" : "It seems server has been gone");

                    if (lastKill > 15)
                    {
                        InventoryManager.Instance.GetBox();
                    }
                    break;
                case MenuType.Cheat:
                    window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                        .GetComponent<FailedWindow>();
                    window.SetTitle("Eat shit");
                    window.SetReason(UIManager.IsItChinese() ? $"作弊了，书呆子, reason: {additionalMessage}" : $"Cheated nerd,reason: {additionalMessage}");
                    break;
                default:
                    if (!string.IsNullOrEmpty(additionalMessage))
                    {
                        window = Instantiate(PrefabManager.Instance.failedWindow, UIManager.Instance.transform)
                            .GetComponent<FailedWindow>();
                        window.SetTitle("Kicked");
                        window.SetReason(additionalMessage);
                    }

                    break;
            }

            menuType = MenuType.None;

            TransitionUI.Instance.ClearTransition();

            additionalMessage = String.Empty;
        }

        public bool requestDone = false;
        [SerializeField] private Slider progressBar;
        private float desiredProgress = 0;
        private static readonly int step = 5;
        private static float percent = 0;
        private AsyncOperation asyncOperation;
        IEnumerator JoinGame(string map, Texture2D texture2D, string otherText, params int[] external)
        {
            // TransitionUI.Instance.StartTransition();

            ResetScene();
            NotificationMenu.Instance.SetCanvas(false);
            desiredProgress = 0;
            progressBar.value = 0;
            requestDone = false;
            title.SetEntry(map);
            if (NetworkManager.ClientGameMode == GameMode.SpecialGameMode ||
                NetworkManager.ClientGameMode == GameMode.SpecialNormalGameMode)
            {
                desc.SetEntry($"gm_{map.ToLower()}_desc");
            }
            else desc.SetEntry($"gm_{NetworkManager.ClientGameMode.ToString().ToLower()}_desc");

            backGround.texture = texture2D;

            yield return new WaitForSeconds(.1f);

            SetAlpha(1f);

            #region Loading

            progress.SetEntry("progress");
            progress.StringReference.Arguments = new List<object>() { 0 };
            progress.RefreshString();

            yield return new WaitForSeconds(.3f);

            TransitionUI.Instance.ClearTransition();

            asyncOperation = SceneManager.LoadSceneAsync(map, LoadSceneMode.Single);
            asyncOperation.allowSceneActivation = false;
            isLoading = true;
            while (asyncOperation.progress < 0.9f || Mathf.Abs(@group.alpha - desiredAlpha) > 0.001f)
            {
                desiredProgress = (asyncOperation.progress * percent);
                progress.StringReference.Arguments[0] = (int)(asyncOperation.progress * 100);
                progress.RefreshString();
                yield return null;
            }
            progress.StringReference.Arguments[0] = 100;
            progress.RefreshString();
            desiredProgress = percent;
            #endregion

            #region Waitting for server

            progress.SetEntry(NetworkServerManager.Instance.Server.IsRunning ? "wait_players" : "wait_server");
            progress.RefreshString();

            while (NetworkManager.GameState != GameState.MidMatch && NetworkManager.GameState != GameState.Warmup && !NetworkServerManager.Instance.Server.IsRunning)
            {
                yield return null;
            }

            asyncOperation.allowSceneActivation = true;
            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            if (NetworkServerManager.Instance.Server.IsRunning)
            {
                NetworkServerManager.Iinstance.SetGameState(GameState.Warmup);
            }

            desiredProgress += percent;
            asyncOperation = null;
            #endregion
            // ReplayManager.Instance.SetRecord(true,map);

            desiredProgress += percent;

            progress.SetEntry("retrieving_server_info");
            Request(RequestDataType.Init);
            desiredProgress += percent;

            #region UI

            PowerInGameMenu.Instance.active = !NetworkManager.ClientDisableSpecialWeapon;

            if (NetworkManager.ClientGameMode == GameMode.GunGame ||
                NetworkManager.ClientGameMode == GameMode.RocketMode ||
                NetworkManager.ClientGameMode == GameMode.Randomizer)
                PowerInGameMenu.Instance.active = false;

            PowerInGameMenu.Instance.power.SetActive(PowerInGameMenu.Instance.active);

            if (ServerSettingUI.Instance)
            {
                ServerSettingUI.Instance.Init();
            }

            GameModeTip.Instance.SetTipText();

            BuyWeaponMenu.Instance.Init();

            #endregion

            InitNetwork(external[0]);
            if (NetworkManager.ClientGameMode != GameMode.None &&
                NetworkManager.ClientGameMode != GameMode.SpecialGameMode && NetworkManager.ClientGameMode != GameMode.SpecialNormalGameMode && NetworkManager.GameState == GameState.MidMatch)
            {
                requestDone = false;
                Request(RequestDataType.InitRound);
                while (!requestDone)
                {
                    yield return null;
                }
            }

            progress.SetEntry("init_player");
            requestDone = false;
            Request(RequestDataType.PlayerSpawn);
            while (!requestDone)
            {
                yield return null;
            }
            desiredProgress += percent;

            isLoading = false;

            yield return new WaitForSeconds(0.5f);

            SetAlpha(0);

            string gamemode = NetworkManager.ClientGameMode.ToString();

            if (map == "ShootingRange")
                gamemode = "ShootingRange";
            else if (map == "Endless")
                gamemode = "Endless";
            else if (map == "ProcMap")
            {
                GenerationTest.Instance.seed = (uint)external[1];

                GenerationTest.Instance.Generate(5);
            }

            GameUIManager.Instance.gameModeMap.SetEntry($"{gamemode}_Map");
            GameUIManager.Instance.gameModeMap.StringReference.Arguments = new List<object> { map };
            GameUIManager.Instance.gameModeMap.RefreshString();
        }
        IEnumerator JoinWorkshopMap(PublishedFileId_t map, int external)
        {
            ResetScene(true);
            NotificationMenu.Instance.SetCanvas(false);
            desiredProgress = 0;
            progressBar.value = 0;
            requestDone = false;

            SetAlpha(1f);
            yield return new WaitForSeconds(0.2f);

            MapData data;

            if (MapSaver.WorkshopMaps.TryGetValue(map, out var file))
            {
                CoroutineWithData cd = new CoroutineWithData(this, SaveSystem.ReadFileAsyncThread(file.FullName));

                yield return cd.coroutine;

                string content = cd.result.ToString();
                if (string.IsNullOrEmpty(content) || content == "{}" || (!content.Contains("{") || !content.Contains("}")))
                {
                    yield break;
                }

                // var jsonTask = Task.Run(() => JsonConvert.DeserializeObject<MapData>(content, new JsonSerializerSettings
                // {
                //     MaxDepth = 256
                // }));
                var jsonTask = Task.Run(() => JsonConvert.DeserializeObject<MapData>(content));

                while (!jsonTask.IsCompleted)
                {
                    yield return null;
                }

                data = jsonTask.Result;

                if (data == null)
                {
                    _networkManager.DisconnectClient();
                    yield break;
                }
                else if (string.IsNullOrEmpty(data.name))
                {
                    _networkManager.DisconnectClient();
                    yield break;
                }

                data.path = file.FullName.Substring(0, file.FullName.Length - file.Name.Length);

                _titleText.SetText(data.name);
                _descText.SetText(data.description);

                var t = SaveSystem.ReadByteFromFileAsync(data.path + "/" + data.GetNameString() + ".jpg");

                while (!t.IsCompleted)
                {
                    yield return null;
                }

                Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
                texture2D.LoadImage(t.Result);

                backGround.texture = texture2D;
            }
            else
            {
                yield break;
            }



            #region Loading

            progress.SetEntry("progress");
            progress.StringReference.Arguments = new List<object>() { 0 };
            progress.RefreshString();

            yield return new WaitForSeconds(.3f);

            TransitionUI.Instance.ClearTransition();
            asyncOperation = SceneManager.LoadSceneAsync("CustomMap", LoadSceneMode.Single);
            asyncOperation.allowSceneActivation = false;
            isLoading = true;
            while (asyncOperation.progress < 0.9f || Mathf.Abs(@group.alpha - desiredAlpha) > 0.001f)
            {
                desiredProgress = (asyncOperation.progress * percent);
                progress.StringReference.Arguments[0] = (int)(asyncOperation.progress * 100);
                progress.RefreshString();
                yield return null;
            }
            progress.StringReference.Arguments[0] = 100;
            progress.RefreshString();
            desiredProgress = percent;

            asyncOperation.allowSceneActivation = true;
            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            progress.SetEntry("Load WorkshopMap");
            progress.RefreshString();
            var task = MapSaver.Instance.LoadWorkshopMap(map, data);

            while (!task.IsCompleted)
            {
                yield return null;
            }
            #endregion

            #region Waitting for server

            progress.SetEntry(NetworkServerManager.Instance.Server.IsRunning ? "wait_players" : "wait_server");
            progress.RefreshString();

            while (NetworkManager.GameState != GameState.MidMatch && NetworkManager.GameState != GameState.Warmup && !NetworkServerManager.Instance.Server.IsRunning)
            {
                yield return null;
            }

            if (NetworkServerManager.Instance.Server.IsRunning)
            {
                NetworkServerManager.Iinstance.SetGameState(GameState.Warmup);
            }

            desiredProgress += percent;
            asyncOperation = null;
            #endregion
            // ReplayManager.Instance.SetRecord(true,map);


            desiredProgress += percent;

            progress.SetEntry("retrieving_server_info");
            Request(RequestDataType.Init);
            desiredProgress += percent;

            #region UI

            PowerInGameMenu.Instance.active = !NetworkManager.ClientDisableSpecialWeapon;
            PowerInGameMenu.Instance.power.SetActive(!NetworkManager.ClientDisableSpecialWeapon);

            if (ServerSettingUI.Instance)
            {
                ServerSettingUI.Instance.Init();
            }


            GameModeTip.Instance.SetTipText();

            BuyWeaponMenu.Instance.Init();

            #endregion

            InitNetwork(external);
            if (NetworkManager.ClientGameMode != GameMode.None &&
                NetworkManager.ClientGameMode != GameMode.SpecialGameMode && NetworkManager.GameState == GameState.MidMatch)
            {
                requestDone = false;
                Request(RequestDataType.InitRound);
                while (!requestDone)
                {
                    yield return new WaitForSeconds(0.1f);
                }

            }

            progress.SetEntry("init_player");
            requestDone = false;
            Request(RequestDataType.PlayerSpawn);
            while (!requestDone)
            {
                yield return new WaitForSeconds(0.1f);
            }
            desiredProgress += percent;
            progress.SetEntry("retrieving_player_info");

            isLoading = false;
            yield return new WaitForSeconds(0.5f);
            SetAlpha(0);

            GameUIManager.Instance.gameModeMap.SetEntry($"{NetworkManager.ClientGameMode.ToString()}_Map");
            GameUIManager.Instance.gameModeMap.StringReference.Arguments = new List<object> { data.name };
            GameUIManager.Instance.gameModeMap.RefreshString();


        }
        public IEnumerator PlayDemo(string map, Texture2D texture2D, Action onSceneLoaded = null)
        {
            if (isLoading) yield break;
            float p = 0.5f;

            ResetScene();
            NotificationMenu.Instance.SetCanvas(false);
            desiredProgress = 0;
            progressBar.value = 0;
            title.SetEntry(map);

            backGround.texture = texture2D;
            SetAlpha(1f);

            #region Loading

            progress.SetEntry("progress");
            progress.StringReference.Arguments = new List<object>() { 0 };
            progress.RefreshString();

            yield return new WaitForSeconds(.5f);
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(map, LoadSceneMode.Single);
            asyncOperation.allowSceneActivation = false;
            isLoading = true;
            while (asyncOperation.progress < 0.9f || Mathf.Abs(@group.alpha - desiredAlpha) > 0.001f)
            {
                desiredProgress = (asyncOperation.progress * p);
                progress.StringReference.Arguments[0] = (int)(asyncOperation.progress * 100);
                progress.RefreshString();
                yield return null;
            }
            progress.StringReference.Arguments[0] = 100;
            progress.RefreshString();
            desiredProgress = p;

            asyncOperation.allowSceneActivation = true;
            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            desiredProgress += p;
            #endregion

            isLoading = false;
            yield return new WaitForSeconds(0.5f);
            SetAlpha(0);

            SpectateMovement.Instance.StartSpect();

            onSceneLoaded?.Invoke();
        }
        public IEnumerator PlayWorkshopMapDemo(PublishedFileId_t map, Action onSceneLoaded = null)
        {
            TransitionUI.Instance.StartTransition();

            ResetScene(true);
            NotificationMenu.Instance.SetCanvas(false);
            desiredProgress = 0;
            progressBar.value = 0;
            float p = 0.5f;

            MapData data;

            if (MapSaver.WorkshopMaps.TryGetValue(map, out var file))
            {
                CoroutineWithData cd = new CoroutineWithData(this, SaveSystem.ReadFileAsyncThread(file.FullName));

                yield return cd.coroutine;

                string content = cd.result.ToString();
                if (string.IsNullOrEmpty(content) || content == "{}" || (!content.Contains("{") || !content.Contains("}")))
                {
                    TransitionUI.Instance.ClearTransition();
                    yield break;
                }

                var jsonTask = Task.Run(() => JsonConvert.DeserializeObject<MapData>(content, new JsonSerializerSettings()
                {
                    MaxDepth = 256
                }));

                while (!jsonTask.IsCompleted)
                {
                    yield return null;
                }

                data = jsonTask.Result;

                if (data == null)
                {
                    TransitionUI.Instance.ClearTransition();
                    yield break;
                }
                else if (string.IsNullOrEmpty(data.name))
                {
                    TransitionUI.Instance.ClearTransition();
                    yield break;
                }

                data.path = file.FullName.Substring(0, file.FullName.Length - file.Name.Length);

                _titleText.SetText(data.name);
                _descText.SetText(data.description);

                var t = SaveSystem.ReadByteFromFileAsync(data.path + "/" + data.GetNameString() + ".jpg");

                while (!t.IsCompleted)
                {
                    yield return null;
                }

                Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
                texture2D.LoadImage(t.Result);

                backGround.texture = texture2D;
            }
            else
            {
                TransitionUI.Instance.ClearTransition();
                yield break;
            }

            yield return new WaitForSeconds(0.2f);

            SetAlpha(1f);

            #region Loading

            progress.SetEntry("progress");
            progress.StringReference.Arguments = new List<object>() { 0 };
            progress.RefreshString();

            yield return new WaitForSeconds(.3f);

            TransitionUI.Instance.ClearTransition();
            asyncOperation = SceneManager.LoadSceneAsync("CustomMap", LoadSceneMode.Single);
            asyncOperation.allowSceneActivation = false;
            isLoading = true;
            while (asyncOperation.progress < 0.9f || Mathf.Abs(@group.alpha - desiredAlpha) > 0.001f)
            {
                desiredProgress = (asyncOperation.progress * p);
                progress.StringReference.Arguments[0] = (int)(asyncOperation.progress * 100);
                progress.RefreshString();
                yield return null;
            }
            progress.StringReference.Arguments[0] = 100;
            progress.RefreshString();
            desiredProgress = percent;

            asyncOperation.allowSceneActivation = true;
            while (!asyncOperation.isDone)
            {
                yield return null;
            }

            progress.SetEntry("Load WorkshopMap");
            progress.RefreshString();
            var task = MapSaver.Instance.LoadWorkshopMap(map, data);

            while (!task.IsCompleted)
            {
                yield return null;
            }
            #endregion

            desiredProgress = 1f;
            asyncOperation = null;

            isLoading = false;
            yield return new WaitForSeconds(0.5f);
            SetAlpha(0);

            SpectateMovement.Instance.StartSpect();

            onSceneLoaded?.Invoke();
        }

        void Request(RequestDataType dataType)
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.RequestData);
            message.Add((ushort)dataType);
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);
        }
        void InitNetwork(int external)
        {
            GameModes gameMode = GameModes.Create(NetworkManager.ClientGameMode, MapBound.Instance.gameObject);

            if (NetworkManager.ClientGameMode == GameMode.KingOfTheHill && external != -1)
                KingOfTheHill.Instance.SetNewHillClient(external);

            NetworkServerManager.Instance.game = gameMode;
            NetworkManager.Instance.game = gameMode;

            if (NetworkManager.ClientGameMode == GameMode.None || NetworkManager.ClientGameMode == GameMode.SpecialGameMode || NetworkManager.ClientGameMode == GameMode.SpecialNormalGameMode) return;
            if (NetworkServerManager.Instance.Server.IsRunning)
            {
                NetworkServerManager.Instance.StartGameFromLoad();
            }
        }


        public void StartLoadVotingScene(float time, List<Tuple<ushort, LobbyDataManager.LobbyData>> datas, int minimalPlayerCount)
        {
            StartCoroutine(LoadVotingScene(time, datas, minimalPlayerCount));
        }

        IEnumerator LoadVotingScene(float time, List<Tuple<ushort, LobbyDataManager.LobbyData>> datas, int minimalPlayerCount)
        {
            TransitionUI.Instance.StartTransition();
            ResetScene();
            NotificationMenu.Instance.SetCanvas(false);

            var ao = SceneManager.LoadSceneAsync("Voting");

            ao.allowSceneActivation = false;

            while (ao.progress < 0.9f)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            ao.allowSceneActivation = true;

            while (!ao.isDone)
            {
                yield return null;
            }


            TransitionUI.Instance.ClearTransition();

            GameVoteMenu.Instance.SetVotePage(time, datas, minimalPlayerCount);
        }
    }
}
