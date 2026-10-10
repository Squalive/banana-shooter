
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Menu;
using Riptide.Transports.Udp;
using Steamworks;
using UnityEngine;
using Web;

namespace Multiplayer
{
    public class ServerManager : SingletonObject<ServerManager>
    {
        public Action<gameserveritem_t, ServerFilter.SearchServerType> OnServerRespond;

        private ISteamMatchmakingServerListResponse m_ServerListResponse;

        private ISteamMatchmakingPlayersResponse m_PlayersResponse;

        private HServerListRequest _lanRequest, _internetRequest, _favouriteRequest, _historyRequest, _friendRequest;

        public Callback<GameRichPresenceJoinRequested_t> OnServerJoinRequested;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            OnServerJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnServerJoinRequest);
            m_ServerListResponse =
                new ISteamMatchmakingServerListResponse(OnServerResponded, OnServerFailedToRespond, OnRefreshComplete);

            m_PlayersResponse = new ISteamMatchmakingPlayersResponse(OnAddPlayerToList, OnPlayerFailedToRespond, OnPlayersRefreshComplete);
        }

        private void OnDisable()
        {
            OnServerJoinRequested.Dispose();

            ReleaseRequest();
        }

        private void OnServerJoinRequest(GameRichPresenceJoinRequested_t param)
        {
            Debug.Log(param.m_rgchConnect);
            if (param.m_steamIDFriend.IsValid())
                Connect(param.m_rgchConnect);
        }

        public void RefreshServer()
        {
            AppId_t appIdT = SteamUtils.GetAppID();

            ReleaseRequest();

            MatchMakingKeyValuePair_t[] filters = {
                // new() { m_szKey = "appid", m_szValue = appIdT.m_AppId.ToString() }
            };

            //Request Favorite Server List
            _favouriteRequest = SteamMatchmakingServers.RequestFavoritesServerList(appIdT, filters, (uint)filters.Length, m_ServerListResponse);
            //Request LAN Server List
            _lanRequest = SteamMatchmakingServers.RequestLANServerList(appIdT, m_ServerListResponse);
            //Request INternet Server List
            _internetRequest = SteamMatchmakingServers.RequestInternetServerList(appIdT, filters, (uint)filters.Length, m_ServerListResponse);
            //Request History Server List
            _historyRequest = SteamMatchmakingServers.RequestHistoryServerList(appIdT, filters, (uint)filters.Length, m_ServerListResponse);
            //Request Friend Server List
            _friendRequest = SteamMatchmakingServers.RequestFriendsServerList(appIdT, filters, (uint)filters.Length, m_ServerListResponse);
        }

        void ReleaseRequest()
        {
            if (_lanRequest != HServerListRequest.Invalid)
            {
                SteamMatchmakingServers.ReleaseRequest(_lanRequest);
                _lanRequest = HServerListRequest.Invalid;
            }
            if (_favouriteRequest != HServerListRequest.Invalid)
            {
                SteamMatchmakingServers.ReleaseRequest(_favouriteRequest);
                _favouriteRequest = HServerListRequest.Invalid;
            }
            if (_internetRequest != HServerListRequest.Invalid)
            {
                SteamMatchmakingServers.ReleaseRequest(_internetRequest);
                _internetRequest = HServerListRequest.Invalid;
            }
            if (_historyRequest != HServerListRequest.Invalid)
            {
                SteamMatchmakingServers.ReleaseRequest(_historyRequest);
                _historyRequest = HServerListRequest.Invalid;
            }
            if (_friendRequest != HServerListRequest.Invalid)
            {
                SteamMatchmakingServers.ReleaseRequest(_friendRequest);
                _friendRequest = HServerListRequest.Invalid;
            }
        }
        private void OnRefreshComplete(HServerListRequest hRequest, EMatchMakingServerResponse response)
        {
            if (hRequest == _lanRequest) _lanRequest = HServerListRequest.Invalid;
            else if (hRequest == _favouriteRequest) _favouriteRequest = HServerListRequest.Invalid;
            else if (hRequest == _internetRequest) _internetRequest = HServerListRequest.Invalid;
            else if (hRequest == _historyRequest) _historyRequest = HServerListRequest.Invalid;
            else if (hRequest == _friendRequest) _friendRequest = HServerListRequest.Invalid;

            SteamMatchmakingServers.ReleaseRequest(hRequest);
            switch (response)
            {
                case EMatchMakingServerResponse.eServerFailedToRespond:
                    break;
                case EMatchMakingServerResponse.eNoServersListedOnMasterServer:
                    break;
            }
        }

        private void OnServerFailedToRespond(HServerListRequest hRequest, int iServer)
        {

            // ServerFilter.SearchServerType type = ServerFilter.SearchServerType.Official;
            //
            // if (hRequest == _lanRequest)
            // {
            //     type = ServerFilter.SearchServerType.Lan;
            // }
            // else if (hRequest == _favouriteRequest)
            // {
            //     type = ServerFilter.SearchServerType.Favourite;
            // }
            // else if (hRequest == _internetRequest)
            // {
            //     type = ServerFilter.SearchServerType.Community;
            // }
            // else if (hRequest == _friendRequest)
            // {
            //     type = ServerFilter.SearchServerType.Friend;
            // }
            // else if (hRequest == _historyRequest)
            // {
            //     type = ServerFilter.SearchServerType.History;
            // }
            // if(type!=ServerFilter.SearchServerType.History)
            //     Debug.LogError($"Server {iServer}, Type: {type} Failed to Respond");
        }

        private void OnServerResponded(HServerListRequest hRequest, int iServer)
        {
            var item = SteamMatchmakingServers.GetServerDetails(hRequest, iServer);

            // Debug.Log($"Server: {iServer} App Id: {item.m_nAppID}, Ip: {item.m_NetAdr.GetConnectionAddressString()} Response successfully");
            ServerFilter.SearchServerType type = ServerFilter.SearchServerType.Official;

            if (hRequest == _lanRequest)
            {
                type = ServerFilter.SearchServerType.Lan;
            }
            else if (hRequest == _favouriteRequest)
            {
                type = ServerFilter.SearchServerType.Favourite;
            }
            else if (hRequest == _internetRequest)
            {
                var tags = item.GetGameTags().Split(';');

                if (tags.Length > 2 && tags[2].Equals("1"))
                {
                    type = ServerFilter.SearchServerType.DlcOnly;
                }
                else
                {
                    type = Manifest.Current.IsOfficialServer(item.m_steamID.m_SteamID) ? ServerFilter.SearchServerType.Official : ServerFilter.SearchServerType.Community;
                }
            }
            else if (hRequest == _friendRequest)
            {
                type = ServerFilter.SearchServerType.Friend;
            }
            else if (hRequest == _historyRequest)
            {
                type = ServerFilter.SearchServerType.History;
            }
            OnServerRespond?.Invoke(item, type);
        }

        private void OnAddPlayerToList(string pchname, int nscore, float fltimeplayed)
        {
            Debug.Log($"{pchname}, {nscore}, {fltimeplayed}");
        }

        private void OnPlayersRefreshComplete()
        {

        }

        private void OnPlayerFailedToRespond()
        {
            Debug.Log("Player failed to respond");
        }

        private Coroutine _joinServerCoroutine;

        public void Connect(uint ip, ushort port)
        {
            UIManager.Instance.StartConnecting();
            foreach (var gameObject in UIManager.Instance.withoutLoading)
            {
                gameObject.SetActive(false);
            }
            UIManager.Instance.SetPos2Multiplayer();
            UIManager.Instance.SetButton(LobbyMenu.Instance.button);
            if (_joinServerCoroutine != null)
                StopCoroutine(_joinServerCoroutine);
            _joinServerCoroutine = StartCoroutine(JoinServerAsync(ip, port));
        }

        IEnumerator JoinServerAsync(uint ip, ushort port)
        {
            // Debug.Log(NetworkManager.Instance.connecting);
            while (NetworkManager.Instance.connecting)
            {
                if (NetworkManager.Instance.Client.IsConnected) yield break;

                yield return null;
            }
            NetworkManager.Instance.connecting = true;

            byte[] bytes = BitConverter.GetBytes(ip);

            Array.Reverse(bytes);

            NetworkManager.Instance.SetCurrentServer(new IPAddress(bytes) + ":" + port);

            // NetworkManager.Instance.Client.ChangeTransport(new UdpClient());

            NetworkManager.Instance.Client.Connect($"{ip}:{port}", 5, NetworkManager.PlayerHostedDemoMessageHandlerGroupId);
        }
        public void Connect(string hostAddress)
        {
            int index = hostAddress.IndexOf(':');
            IPAddress ipAddress = IPAddress.Parse(hostAddress.Substring(0, index));

            byte[] ipAddressBytes = ipAddress.GetAddressBytes();
            Array.Reverse(ipAddressBytes); // Need to reverse byte order for correct endianness

            UInt32 ipAddressAsUInt = BitConverter.ToUInt32(ipAddressBytes, 0);

            if (ushort.TryParse(hostAddress.Substring(index + 1), out var port))
            {
                Connect(ipAddressAsUInt, port);
            }
            //
            // NetworkManager.Instance.connecting = true;
            //
            // NetworkManager.Instance.Client.Connect($"{}:{}", 5,NetworkManager.PlayerHostedDemoMessageHandlerGroupId);
        }
    }


}
