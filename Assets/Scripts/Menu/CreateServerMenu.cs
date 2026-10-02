using System;
using System.Collections;

using Multiplayer;
using Multiplayer.Interface;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class CreateServerMenu : SingletonObject<CreateServerMenu>
    {
        
        [SerializeField] private TextMeshProUGUI maxPlayerText;
        public string serverName;
        public int maxPlayer=10;
        public ELobbyType lobbyType;

        private int currentLobbyTypeIndex = 0;
        private int _currentServerTypeIndex = 0;
        
        private Coroutine createLobbyAsync;
        private void Awake()
        {
            Instance = this;

            randomMap.onValueChanged.AddListener(SetRandomMap);
            randomGameMode.onValueChanged.AddListener(SetRandomGameMode);
        }
        
        public void SetServername(string name)
        {
            serverName = name;
        }
        
        public void SetMaxPlayer(float c)
        {
            maxPlayer = (int)c;
            maxPlayerText.SetText(maxPlayer.ToString());
        }
        
        
        public void NextLobbyType(int i)
        {
            currentLobbyTypeIndex = i;
            // if (currentLobbyTypeIndex > 1) currentLobbyTypeIndex = 0;
            // else if (currentLobbyTypeIndex < 0) currentLobbyTypeIndex = 1;
            // string text = "public";
            ELobbyType type = ELobbyType.k_ELobbyTypePublic;

            switch (currentLobbyTypeIndex)
            {
                case 1:
                    type = ELobbyType.k_ELobbyTypeFriendsOnly;
                    break;
                case 2:
                    type = ELobbyType.k_ELobbyTypePrivate;
                    break;
            }
            lobbyType = type;
        }
        
        public void NextServerType(int i)
        {
            _currentServerTypeIndex = i;
            // if (_currentServerTypeIndex > 1) _currentServerTypeIndex = 0;
            // else if (_currentServerTypeIndex < 0) _currentServerTypeIndex = 1;
            ServerType t = (ServerType) (1+_currentServerTypeIndex);

            NetworkServerManager.SetServerType(t);
        
            // string text = $"sm_{t.ToString().ToLower()}";
        }
        
        public void CreateLobby(bool shootingRange)
        {
            GameMode g = GameMode.Brawl;
            switch (NetworkServerManager.ServerType)
            {
                case ServerType.Endless:
                    g = GameMode.SpecialNormalGameMode;
                    break;
            }

            UIManager.Instance.StartConnecting();
            
            foreach (var gameObject in UIManager.Instance.withoutLoading)
            {
                gameObject.SetActive(false);
            }
            UIManager.Instance.SetButton(LobbyMenu.Instance.button);
            NetworkServerManager.ServerGameMode = shootingRange ? GameMode.SpecialGameMode : g;
            NetworkManager.Instance.GameModeChanged?.Invoke(NetworkServerManager.ServerGameMode);
            if(createLobbyAsync!=null)
                StopCoroutine(createLobbyAsync);
            createLobbyAsync = StartCoroutine(CreateLobbyAsync());
        }
        IEnumerator CreateLobbyAsync()
        {
            while (NetworkManager.Instance.connecting)
            {
            
                yield return null;
            }
            LobbyManager.Instance.CreateLobby();
        }
        public Toggle randomMap, randomGameMode;
        void SetRandomMap(bool arg)
        {
            if (randomMap.isOn != arg)
                randomMap.isOn = arg;

            NetworkServerManager.ServerRandomMap = arg;
        }
        void SetRandomGameMode(bool arg)
        {
            if (randomGameMode.isOn != arg)
                randomGameMode.isOn = arg;

            NetworkServerManager.ServerRandomGameMode = arg;
        }
    }
}
