
using System.Collections.Generic;
using System.Text;
using Manager;
using Multiplayer;
using Steamworks;
using SteamWorkshop;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class ServerDetail
    {
        public CSteamID Id;

        public bool WorkshopEnable = false;
        public int PlayerAmount = 0;
        public int PlayerMaxAmount = 0;
        
        public string Name;
        
        public string Version;
        
        public string Mode;

        public string DescShort;
        
        public string ServerMode;

        public bool TournamentEnable = false;

        public ServerFilter.SearchServerType SearchServerType;

        public int Ping=0;

        public servernetadr_t NetAdr = new servernetadr_t();

        public uint LastTimePlayed = 0;

        public bool Favourited = false;

        public bool IsPlayerHost = false;

        public List<Friend> FriendsPlaying = new List<Friend>();
        public ServerDetail(CSteamID id, bool workshopEnable, int playerAmount,int playerMaxAmount,string name,string version,string mode,string serverMode,bool tournamentEnable, ServerFilter.SearchServerType searchServerType,int ping,bool isPlayerHost,string descShort)
        {
            Id = id;
            DescShort = descShort;
            WorkshopEnable=workshopEnable;
            PlayerAmount = playerAmount;
            PlayerMaxAmount = playerMaxAmount;
            Name = name;
            Version = version;
            Mode = mode;
            ServerMode = serverMode;
            TournamentEnable = tournamentEnable;
            SearchServerType = searchServerType;
            Ping = ping;
            IsPlayerHost = isPlayerHost;

            foreach (var friend in FriendManager.Friends.Values)
            {
                if (friend.CurrentServer == id)
                {
                    FriendsPlaying.Add(friend);
                }
            }
        }
    }
    public class ServerItem : MonoBehaviour
    {
        public ServerDetail ServerDetail;

        public ulong lobbyId;
        [SerializeField] TextMeshProUGUI playerCount,version,nameText,pingText,descShortText;
        [SerializeField] Button joinBtn;

        [SerializeField] RawImage versionImage,serverSetting, serverModeBg;

        [SerializeField] LocalizeStringEvent editText, friendsText,serverModeText;

        [SerializeField] private RawImage icon;

        [SerializeField] private Texture2D[] texture2Ds;
        [SerializeField] private GameObject tournament,workshopIcon,friends;

        [SerializeField] private Toggle favouriteToggle;

        public void SetPlayerValues(ServerDetail serverDetail)
        {
            ServerDetail = serverDetail;
            lobbyId = serverDetail.Id.m_SteamID;
            workshopIcon.SetActive(serverDetail.WorkshopEnable);
            playerCount.SetText($"{serverDetail.PlayerAmount}/{serverDetail.PlayerMaxAmount}");
            nameText.SetText(serverDetail.Name);
            pingText.SetText($"{serverDetail.Ping} ms" );

            string version = serverDetail.Version;
            // versionImage.color =
            //     version == Application.version ? Color.green : Color.red;
            // this.version.SetText(version);
            joinBtn.interactable = version == Application.version;
            joinBtn.onClick.AddListener(UIManager.Instance.ButtonSound);
            
            if (serverDetail.IsPlayerHost)
            {
                favouriteToggle.interactable = false;
                joinBtn.onClick.AddListener(delegate {LobbyMenu.Instance.JoinLobby(lobbyId);});
            }
            else
            {
                favouriteToggle.SetIsOnWithoutNotify(serverDetail.Favourited);
                favouriteToggle.onValueChanged.AddListener(SetFavourite);
                
                joinBtn.onClick.AddListener(delegate
                {
                    SteamFriends.SetRichPresence("server", serverDetail.Id.ToString());
                    ServerManager.Instance.Connect(serverDetail.NetAdr.GetIP(),serverDetail.NetAdr.GetConnectionPort());
                });
            }
            
            // serverSetting.color = serverDetail.WorkshopEnable ? new Color(0,140/255f,219/255f) : new Color(45/255f,205/255f,0);
            // editText.SetEntry(serverDetail.WorkshopEnable ? "modded" : "default");

            Texture2D texture = GetTexture($"gm_{serverDetail.Mode.ToLower()}");

            if (texture == null)
            {
                icon.gameObject.SetActive(false);
            }
            else
            {
                icon.texture = texture;
            }

            string serverType = "sm_" + serverDetail.ServerMode.ToLower();

            switch (serverType)
            {
                case "sm_knockoutround":
                    serverModeBg.gameObject.SetActive(true);
                    serverModeText.SetEntry(serverType);
                    break;
            }
            tournament.SetActive(serverDetail.TournamentEnable);

            if (!string.IsNullOrEmpty(serverDetail.DescShort))
            {
                descShortText.gameObject.SetActive(true);
                descShortText.SetText(serverDetail.DescShort);
            }
            
            friends.SetActive(ServerDetail.FriendsPlaying.Count > 0);

            StringBuilder friendStr = new StringBuilder();

            for (int i = 0; i < ServerDetail.FriendsPlaying.Count; i++)
            {
                if (i +1 >= ServerDetail.FriendsPlaying.Count)
                {
                    friendStr.Append(ServerDetail.FriendsPlaying[i].Name);
                    break;
                }
                friendStr.Append(ServerDetail.FriendsPlaying[i].Name + ", ");
            }

            friendsText.StringReference.Arguments = new List<object>() {friendStr};
            friendsText.RefreshString();
        }

        private void OnDisable()
        {
            favouriteToggle.onValueChanged.RemoveListener(SetFavourite);
        }

        Texture2D GetTexture(string n)
        {
            foreach (var texture in texture2Ds)
            {
                if (texture.name == n)
                {
                    return texture;
                }
            }

            return null;
        }

        void SetFavourite(bool flag)
        {
            if (flag)
            {
                int i= SteamMatchmaking.AddFavoriteGame(SteamWorkshopManager._appId, ServerDetail.NetAdr.GetIP(),
                    ServerDetail.NetAdr.GetConnectionPort(), ServerDetail.NetAdr.GetQueryPort(), 255,
                    ServerDetail.LastTimePlayed);
                
                Debug.Log("Add " + ServerDetail.NetAdr.GetConnectionAddressString() + " to the favourite " + i);
            }
            else
            {
                SteamMatchmaking.RemoveFavoriteGame(SteamWorkshopManager._appId, ServerDetail.NetAdr.GetIP(),
                    ServerDetail.NetAdr.GetConnectionPort(), ServerDetail.NetAdr.GetQueryPort(), 255);
            }
        }
    }
}
