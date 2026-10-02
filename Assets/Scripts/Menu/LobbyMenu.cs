using System.Collections;
using System.Collections.Generic;
using Audio;

using Manager;
using Multiplayer;
using Multiplayer.Interface;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    
    public enum SearchLobbyType
    {
        Normal,
        Tournament,
    }
    public class LobbyMenu : MonoBehaviour
    {
        public static LobbyMenu Instance;

        private void Awake()
        {
            Instance = this;
        
        
        }
        
        private void Start()
        {
            // closeServerOnlyToggle.isOn =
            //     LobbyManager.Instance.filter == ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault;
            // closeServerOnlyToggle.onValueChanged.AddListener(SetCloseServerOnly);
            
            NetworkServerManager.SetServerType(ServerType.Normal);
        }

        #region UI

        // public List<GameObject> lobbies = new List<GameObject>();
        // [SerializeField] private ServerType displayServerType=ServerType.None;
        // private int _currentDisplayServerTypeIndex = 0;
        // [SerializeField] private LocalizeStringEvent displayServerTypeText;
        //
        // public void NextDisplayServerType()
        // {
        //     _currentDisplayServerTypeIndex += 1;
        //     if (_currentDisplayServerTypeIndex > 2) _currentDisplayServerTypeIndex = 0;
        //     displayServerType = (ServerType) (_currentDisplayServerTypeIndex);
        //     string text;
        //     if (displayServerType == ServerType.None)
        //     {
        //         text = "all";
        //     }
        //     else
        //     {
        //         text = $"sm_{displayServerType.ToString().ToLower()}";
        //     }
        //
        //     displayServerTypeText.SetEntry(text);
        //     
        //     AudioManager.Instance.PlayButton();
        //     
        //     GetLobbyList();
        // }
        //
        // 
        // public void QuickMatch(int serverType)
        // {
        //     if (!isMatching)
        //     {
        //         _serverTypeFilter = (ServerType) serverType;
        //         NetworkManager.ServerType = _serverTypeFilter;
        //         isMatching = true;
        //         matchTime = 0;
        //         matchCount = 0;
        //         GetLobbyList(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide,SearchLobbyType.Normal);
        //     }
        // }
        // public void JoinLobby(TMP_InputField lobbyIDInput)
        // {
        //     if (string.IsNullOrEmpty(lobbyIDInput.text)) return;
        //     UIManager.Instance.loadingMenu.SetActive(true);
        //     foreach (var gameObject in UIManager.Instance.withoutLoading)
        //     {
        //         gameObject.SetActive(false);
        //     }
        //     UIManager.Instance.SetButton(button);
        //     if(joinLobbyAsync!=null)
        //         StopCoroutine(joinLobbyAsync);
        //     joinLobbyAsync = StartCoroutine(JoinLobbyAsync(ulong.Parse(lobbyIDInput.text)));
        // }
        
        public LocalizeStringEvent loadingText;
        #endregion

        #region Lobby

        private Coroutine joinLobbyAsync;

        public bool isMatching = false;
        public float matchTime = 0;
    
        public Button button;
        public void JoinLobby(ulong lobbyId)
        {
            UIManager.Instance.StartConnecting();
            
            foreach (var gameObject in UIManager.Instance.withoutLoading)
            {
                gameObject.SetActive(false);
            }
            UIManager.Instance.SetButton(button);
            if(joinLobbyAsync!=null)
                StopCoroutine(joinLobbyAsync);
            joinLobbyAsync = StartCoroutine(JoinLobbyAsync(lobbyId));

        }

        
        public void JoinLobbyThroughCode(TMP_InputField inputField)
        {
            if (ulong.TryParse(inputField.text, out var id))
            {
                JoinLobby(id);
            }
        }
        
        IEnumerator JoinLobbyAsync(ulong lobbyId)
        {
            // Debug.Log(NetworkManager.Instance.connecting);
            while (NetworkManager.Instance.connecting)
            {
                if(NetworkManager.Instance.Client.IsConnected)yield break;
                yield return null;
            }
            LobbyManager.Instance.JoinLobby(lobbyId);
        }
    
        #endregion

        private void Update()
        {
            if (isMatching)
            {
                matchTime += Time.deltaTime;

                if (matchTime >= 10)
                {
                    isMatching = false;
                
                    UIManager.Instance.enableWorkshopPanel.SetActive(true);
                }

            
                if (matchTime >= 7)
                {
                    loadingText.SetEntry("creating");
                    loadingText.StringReference.Arguments = new List<object>() {matchTime.ToString("F2")};
                    loadingText.RefreshString();
                }
                else
                {
                    loadingText.SetEntry("searching");
                    loadingText.StringReference.Arguments = new List<object>() {matchTime.ToString("F2")};
                    loadingText.RefreshString();
                }
            }
            else if (NetworkManager.Instance.Client.IsConnecting)
            {
                loadingText.SetEntry("Connect");
                loadingText.RefreshString();
            }
        }

        [SerializeField] private GameObject warning;
        
        public void TryToCreate(bool flag)
        {
            warning.SetActive(!GameManager.hostGameDontShowMeThisAgain && !flag);
            if (GameManager.hostGameDontShowMeThisAgain || flag)
            {
                UIManager.Instance.enableWorkshopPanel.SetActive(true);
                UIManager.Instance.SetButton(UIManager.Instance.tryToCreateCancel);
            }
        }
    }
}
