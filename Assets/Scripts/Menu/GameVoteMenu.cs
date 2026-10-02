using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;

using Manager;
using Map;
using Multiplayer;
using Multiplayer.Interface;
using Riptide;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class GameVoteMenu : MonoBehaviour
    {
        public static GameVoteMenu Instance { get; private set; }

        public bool IsVoting { get; set; } = false;

        public GameMode SelectedGameMode { get; private set; } = GameMode.Brawl;

        private float _timer = 0;
        public GameObject map;
        public GameObject gameModeObj;
        private int _minimalPlayerCount = 2;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private LocalizeStringEvent timerTextLocalize;
        readonly Dictionary<string, MapPrefabItemUI> _voteMap = new();
        readonly Dictionary<string, ushort> _voteMapAmount = new();
        readonly Dictionary<ushort, ushort> _voteGameAmount = new();

        readonly Dictionary<ushort, GameModeVoteItem> _voteGameMode = new();
        
        [SerializeField] private Transform mapContent,gameModeContent;

        [SerializeField] private MapPrefabItemUI mapPrefab;
        [SerializeField] private GameModeVoteItem gameModePrefab;

        [SerializeField] private ToggleGroup mapGroup,gameModeGroup;

        [SerializeField] private PlayerLeaderboardItem playerPrefab;

        [SerializeField] private Transform playerListContent;

        [SerializeField] private GameObject forceStartObj;

        private Dictionary<ushort,LobbyDataManager.LobbyData> _list = new();

        private Dictionary<ushort,PlayerLeaderboardItem> _objects = new();

        [SerializeField] private GameObject lobby, server;
        private void Awake()
        {
            Instance = this;

            SteamFriends.SetRichPresence("steam_display", "#Status_WaitingForMatch");
            
            map.SetActive(!NetworkManager.ClientRandomMap);
            gameModeObj.SetActive(!NetworkManager.ClientRandomGameMode);

            if (NetworkManager.ClientEnableWorkshop)
            {
                foreach (var id in NetworkManager.WorkshopMaps)
                {
                    _voteMapAmount.Add(id.ToString(),0);
                }
            }
            else
            {
                foreach (var m in MapManager.Instance.maps)
                {
                    _voteMapAmount.Add(m.name,0);
                }
            }

            RefreshMaps();
            
            RefreshGameMode();
            
            if(MusicManager.Instance.music == MusicManager.MusicType.WinningMusic)
                Invoke(nameof(StopMusic),10f);
            else
                StopMusic();

            bool flag = LobbyManager.Instance.lobbyId.IsLobby();
            lobby.SetActive(flag);
            server.SetActive(!flag);
        }

        
        public void ForceStart()
        {
            NetworkServerManager.Instance.ForceStart();
        }

        void DisplayForceStartButton()
        {
            forceStartObj.SetActive(true);
        }
        
        void StopMusic()
        {
            MusicManager.Instance.ChangeMusic(MusicManager.MusicType.None);
        }

        void RefreshMaps()
        {
            for (int i = 0; i < mapContent.childCount; i++)
            {
                Destroy(mapContent.GetChild(i).gameObject);
            }

            if (NetworkManager.ClientEnableWorkshop)
            {
                foreach (var id in NetworkManager.WorkshopMaps)
                {
                    MapPrefabItemUI itemUI = Instantiate(mapPrefab, mapContent);
                    
                    itemUI.Initialize(id,mapGroup);
                    
                    itemUI.toggle.SetIsOnWithoutNotify(false);
                
                    itemUI.toggle.onValueChanged.AddListener(delegate(bool arg) { VoteMap(arg,id.ToString(),true); });

                    itemUI.toggle.interactable = !NetworkManager.LocalClientData.Eliminated;
                    
                    _voteMap.Add(id.ToString(),itemUI);
                }
            }
            else
            {
                foreach (var m in NetworkServerManager.Instance.Maps[SelectedGameMode])
                {
                    MapPrefabItemUI itemUI = Instantiate(mapPrefab, mapContent);
                
                    itemUI.Initialize(m,mapGroup);
                
                    itemUI.toggle.SetIsOnWithoutNotify(false);
                
                    itemUI.toggle.onValueChanged.AddListener(delegate(bool arg) { VoteMap(arg,m.name,false); });
                
                    itemUI.toggle.interactable = !NetworkManager.LocalClientData.Eliminated;
                    
                    _voteMap.Add(m.name,itemUI);
                }
            }
        }

        void RefreshGameMode()
        {
            for (int i = 0; i < gameModeContent.childCount; i++)
            {
                Destroy(gameModeContent.GetChild(i).gameObject);
            }

            foreach (var gameMode in NetworkServerManager.Instance.Maps.Keys)
            {
                if(gameMode == GameMode.None || gameMode == GameMode.SpecialGameMode || gameMode == GameMode.SpecialNormalGameMode)continue;
                GameModeVoteItem itemUI = Instantiate(gameModePrefab, gameModeContent);
                
                itemUI.Initialize(gameMode,gameModeGroup);
                
                itemUI.toggle.SetIsOnWithoutNotify(false);
                
                itemUI.toggle.onValueChanged.AddListener(delegate(bool arg) { VoteGameMode(arg,(ushort)gameMode); });
                
                itemUI.toggle.interactable = !NetworkManager.LocalClientData.Eliminated;
                
                _voteGameMode.Add((ushort)gameMode,itemUI);
                
                _voteGameAmount.Add((ushort)gameMode,0);
            }
        }

        void VoteMap(bool ar,string map,bool isWorkshopMap)
        {
            if (!IsVoting) return;
            if (ar)
            {
                Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.VoteMap);
                message.Add(isWorkshopMap);
                message.Add(map);
                NetworkManager.Instance.SendByte += message.WrittenLength;
                NetworkManager.Instance.Client.Send(message);
            }
            
        }
        void VoteGameMode(bool arg,ushort gameMode)
        {
            if (!IsVoting || !arg) return;
            Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.VoteGameMode);
            message.Add(gameMode);
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);
        }
        public void SetVotePage(float time,List<Tuple<ushort,LobbyDataManager.LobbyData>> datas,int minimalPlayerCount)
        {
            IsVoting = true;
            _minimalPlayerCount = minimalPlayerCount;
            
            if (NetworkServerManager.Instance.Server.IsRunning)
            {
                Invoke(nameof(DisplayForceStartButton), 2f);
            }
            
            _timer = time;

            timerTextLocalize.enabled = datas.Count <= 1;

            if (datas.Count < minimalPlayerCount)
            {
                timerTextLocalize.enabled = true;
                timerTextLocalize.SetEntry("waiting_for_players");
            }
            else
            {
                timerTextLocalize.enabled = false;
                timerText.SetText(time.ToString("F1"));
            }

            StartCoroutine(Ticking(time));

            foreach (var tuple in datas)
            {
                AddObject(tuple.Item1, tuple.Item2);
            }
            
            Refresh();
        }

        void Refresh()
        {
            foreach (var obj in _objects.Values)
            {
                Destroy(obj.gameObject);
            }
            
            _objects.Clear();
            
            // _list.Add(2, new LobbyDataManager.LobbyData(123129837, "askjdklasd", 214, 1,124124));
            // _list.Add(3, new LobbyDataManager.LobbyData(124125125, "asgsg", 255, 1,124124));

            uint i = 0;

            foreach (var data in _list.OrderByDescending(e => e.Value.kills))
            {
                if (NetworkManager.ClientData.TryGetValue(data.Key, out var clientData))
                {
                    ++i;
                    PlayerLeaderboardItem item = Instantiate(playerPrefab, playerListContent);

                    item.Initialize(data.Value, i, clientData.Eliminated);

                    _objects.Add(data.Key, item);
                }
            }
        }

        public void RemoveObject(ushort id)
        {
            if (_list.ContainsKey(id))
                _list.Remove(id);

            if (_objects.TryGetValue(id, out var item))
            {
                Destroy(item.gameObject);

                _objects.Remove(id);
            }
        }

        void AddObject(ushort id, LobbyDataManager.LobbyData data)
        {
            _list.Add(id,data);
        }

        IEnumerator Ticking(float time)
        {
            while (_list.Count < _minimalPlayerCount)
            {
                yield return null;
            }
            
            yield return new WaitForSeconds(time - 4);
            Tick();
            
            yield return new WaitForSeconds(1);
            Tick();
            
            yield return new WaitForSeconds(1);
            Tick();

            IsVoting = false;

            timerTextLocalize.enabled=true;
            timerTextLocalize.SetEntry("game_starting");
        }

        void Tick()
        {
            AudioManager.Instance.Play("ticking");
        }

        private void Update()
        {
            if (IsVoting && _list.Count >= _minimalPlayerCount)
            {
                _timer -= Time.deltaTime;
                if (timerTextLocalize.enabled)
                    timerTextLocalize.enabled = false;
                timerText.SetText(_timer.ToString("F1"));
            }
        }

        [MessageHandler((ushort) ServerToClientId.VoteMap, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        static void PlayerVoteMap(Message message)
        {
            if (Instance)
            {
                string lastMap = message.GetString();
                string currentMap = message.GetString();
                ushort lastMapVoteCount = message.GetUShort(), currentMapVoteCount = message.GetUShort();
                if (Instance._voteMapAmount.TryGetValue(lastMap, out _))
                {
                    Instance._voteMapAmount[lastMap] = lastMapVoteCount;
                    
                    if (Instance._voteMap.TryGetValue(lastMap,out var m))
                    {
                        m.SetCount(lastMapVoteCount);
                        
                    }
                }
                if (Instance._voteMapAmount.TryGetValue(currentMap, out _))
                {
                    Instance._voteMapAmount[currentMap] = currentMapVoteCount;
                    
                    if (Instance._voteMap.TryGetValue(currentMap,out var m))
                    {
                        m.SetCount(currentMapVoteCount);
                        m.Animate();
                    }
                }
            }
            
        }
        [MessageHandler((ushort) ServerToClientId.VoteGameMode, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        static void PlayerVoteGameMode(Message message)
        {
            if (Instance)
            {
                short lastGameMode = message.GetShort();
                ushort currentGameMode = message.GetUShort();
                ushort lastVoteCount = message.GetUShort();
                ushort currentVoteCount = message.GetUShort();
                if (Instance._voteGameAmount.TryGetValue((ushort)lastGameMode, out _))
                {
                    Instance._voteGameAmount[(ushort)lastGameMode] = lastVoteCount;
                    
                    if (Instance._voteGameMode.TryGetValue((ushort)lastGameMode,out var m))
                    {
                        m.SetCount(lastVoteCount);
                    }
                }
                if (Instance._voteGameAmount.TryGetValue(currentGameMode, out _))
                {
                    Instance._voteGameAmount[currentGameMode] = currentVoteCount;
                    
                    if (Instance._voteGameMode.TryGetValue(currentGameMode,out var m))
                    {
                        m.SetCount(currentVoteCount);
                        m.Animate();
                    }
                }
            }
        }
        
        [MessageHandler((ushort)ServerToClientId.AddVotingPlayer, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void AddVotingPlayer(Message message)
        {
            if (NetworkManager.GameState != GameState.Voting) return;

            if (Instance == null) return;
            
            ushort id = message.GetUShort();

            uint kills = message.GetUInt();

            if (NetworkManager.ClientData.TryGetValue(id, out var clientData))
            {
                string n = Chat.Instance.GetPlayerNameNetwork(clientData.Name,clientData.SteamId,clientData.DisplayTag, clientData.OwnedDlc);

                LobbyDataManager.LobbyData data = new LobbyDataManager.LobbyData(clientData.SteamId, n, kills, 0, (uint)clientData.Exp);

                Instance.RemoveObject(id);

                Instance.AddObject(id, data);

                Instance.Refresh();
            }
        }

        
        public void Disconnect()
        {
            LoadingManager.Instance.menuType = LoadingManager.MenuType.Normal;
            if (NetworkManager.Instance.Client.IsConnected)
            {
            
                LobbyManager.Instance.LeaveLobby();
            }
            else
            {
                LoadingManager.Instance.Menu();
            }
        }
        
        
        public void CopyLobbyId()
        {
            GUIUtility.systemCopyBuffer = LobbyManager.Instance.lobbyId.m_SteamID.ToString();
            NotificationMenu.Instance.NewItem("nc_message","nc_copy_complete");
        }

        
        public void Invite()
        {
            if (LobbyManager.Instance.lobbyId.IsLobby())
            {
                SteamFriends.ActivateGameOverlayInviteDialog(LobbyManager.Instance.lobbyId);
            }
        }
    }
}
