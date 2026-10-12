using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Riptide;
using Riptide.Transports.Steam;
using Steamworks;
using UnityEngine;

namespace Multiplayer.Migration
{
    public class HostMigration : MonoBehaviour
    {
        public static HostMigration Instance { get; private set; }

        public static int PendingResumes => Instance != null ? Instance._awaiting.Count : 0;

        private const string PingKey = "ping";
        private const string SuccessorsKey = "successors";
        private const ushort ResumeMagic = 0xB4A1;
        private const float SnapshotInterval = 0.25f;
        private const float ElectionInterval = 10f;
        private const float ReconnectWindow = 10f;
        private const float CandidateWindow = 6f;
        private const int ChunkSize = 1000;

        public bool Migrating { get; private set; }
        public bool Resuming { get; private set; }

        public bool HandingOff => _handingOff;

        private readonly List<ulong> _successors = new();
        private readonly SnapshotAssembler _assembler = new();
        private readonly HashSet<ushort> _awaiting = new();
        private readonly HashSet<ushort> _resumed = new();
        private readonly List<ushort> _dropped = new();

        private byte[] _lastSnapshot;
        private ulong _handoffTarget;
        private bool _oldHostStays;
        private ulong _currentHost;
        private ulong _newHost;
        private ushort _myId;
        private uint _snapshotSeq;
        private uint _session;
        private float _snapshotTimer;
        private float _electionTimer;
        private float _awaitDeadline;
        private float _pingRetry;
        private float _ownerWatchUntil;
        private CSteamID _pingLobby = CSteamID.Nil;
        private bool _takeOverLobby;
        private bool _leaving;
        private bool _handingOff;
        private bool _quitAllowed;
        private ushort _handoffClient;
        private byte[] _promoteNext;
        private byte[] _pendingFinal;
        private float _pendingFinalDeadline;
        private bool _ackReceived;
        private CSteamID _handedOffLobby = CSteamID.Nil;
        private CSteamID _knownLobby = CSteamID.Nil;
        private DisconnectedEventArgs _disconnect;

        private static ulong Me => SteamUser.GetSteamID().m_SteamID;

        private static Riptide.Server Server => NetworkServerManager.Instance.Server;

        private static Riptide.Client Client => NetworkManager.Instance.Client;

        private static CSteamID Lobby => LobbyManager.Instance != null ? LobbyManager.Instance.lobbyId : CSteamID.Nil;

        private void Awake()
        {
            Instance = this;
            _session = (uint)new System.Random().Next(1, int.MaxValue);
            Server.HandleConnection = HandleConnection;
        }

        private void Update()
        {
            if (!Steamworks.NET.SteamManager.Initialized) return;

            if (_pendingFinal != null && Time.unscaledTime > _pendingFinalDeadline)
            {
                _promoteNext = _pendingFinal;
                _pendingFinal = null;
            }

            if (_promoteNext != null)
            {
                var data = _promoteNext;
                _promoteNext = null;
                Client.Disconnect();
                Promote(data);
            }

            if (Client.IsConnected)
            {
                _myId = Client.Id;
                if (!Migrating) _leaving = false;
            }

            var lobby = Lobby;
            if (lobby != _knownLobby && !Migrating && !Resuming)
            {
                _knownLobby = lobby;
                _lastSnapshot = null;
                _pendingFinal = null;
                _successors.Clear();
                _handoffTarget = 0;
                _oldHostStays = false;
            }
            PublishPing(lobby);
            WatchOwner(lobby);
            if (_takeOverLobby) TryTakeOverLobby(lobby);

            if (!Server.IsRunning || lobby == CSteamID.Nil) return;

            if (_awaiting.Count > 0 && Time.unscaledTime > _awaitDeadline)
                DropAwaiting();

            if (NetworkServerManager.ServerType == ServerType.ShootingRange || Server.ClientCount < 2) return;

            _electionTimer -= Time.unscaledDeltaTime;
            if (_electionTimer <= 0f)
            {
                _electionTimer = ElectionInterval;
                Elect();
            }

            _snapshotTimer -= Time.unscaledDeltaTime;
            if (_snapshotTimer <= 0f && _successors.Count > 0)
            {
                _snapshotTimer = SnapshotInterval;
                SendSnapshot(false, _successors);
            }
        }

        #region Host

        private void PublishPing(CSteamID lobby)
        {
            if (lobby == CSteamID.Nil)
            {
                _pingLobby = CSteamID.Nil;
                return;
            }

            if (_pingLobby == lobby || Time.unscaledTime < _pingRetry) return;
            _pingRetry = Time.unscaledTime + 1f;

            if (SteamNetworkingUtils.GetLocalPingLocation(out var location) < 0f) return;
            SteamNetworkingUtils.ConvertPingLocationToString(ref location, out var text, 1024);
            SteamMatchmaking.SetLobbyMemberData(lobby, PingKey, text);
            _pingLobby = lobby;
        }

        private void Elect()
        {
            var lobby = Lobby;
            ulong me = Me;
            var candidates = NetworkServerManager.ClientData.Values
                .Where(d => d.SteamId != me && Server.TryGetClient(d.Id, out _))
                .Select(d => d.SteamId)
                .ToList();

            var locations = new Dictionary<ulong, SteamNetworkPingLocation_t>();
            foreach (var id in candidates)
            {
                string text = SteamMatchmaking.GetLobbyMemberData(lobby, new CSteamID(id), PingKey);
                if (!string.IsNullOrEmpty(text) && SteamNetworkingUtils.ParsePingLocationString(text, out var location))
                    locations[id] = location;
            }

            var ranked = SuccessorElection.Rank(candidates, (a, b) =>
            {
                if (!locations.TryGetValue(a, out var la) || !locations.TryGetValue(b, out var lb)) return -1;
                return SteamNetworkingUtils.EstimatePingTimeBetweenTwoLocations(ref la, ref lb);
            }).Take(2).ToList();

            if (ranked.SequenceEqual(_successors)) return;

            _successors.Clear();
            _successors.AddRange(ranked);
            SteamMatchmaking.SetLobbyData(lobby, SuccessorsKey, string.Join(",", _successors));

            var message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.MigrationSuccessors);
            message.AddULongs(_successors.ToArray());
            Server.SendToAll(message);

            Debug.Log($"[Migration] successors: {string.Join(", ", _successors)}");
        }

        private void SendSnapshot(bool final, IEnumerable<ulong> targets, bool oldHostStays = false)
        {
            byte[] data;
            try
            {
                data = ServerSnapshot.Capture();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            uint seq = ++_snapshotSeq;
            var chunks = SnapshotChunks.Split(data, ChunkSize);
            foreach (var steamId in targets)
            {
                if (!TryGetClientId(steamId, out ushort clientId)) continue;
                for (int i = 0; i < chunks.Count; i++)
                {
                    var message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.MigrationSnapshot);
                    message.AddUInt(_session);
                    message.AddUInt(seq);
                    message.AddUShort((ushort)i);
                    message.AddUShort((ushort)chunks.Count);
                    message.AddBool(final);
                    message.AddBool(oldHostStays);
                    message.AddBytes(chunks[i]);
                    Server.Send(message, clientId);
                }
            }

            if (final || seq % 120 == 1)
                Debug.Log($"[Migration] snapshot {seq}{(final ? " (final)" : string.Empty)}: {data.Length} bytes, {chunks.Count} parts");
        }

        private static bool TryGetClientId(ulong steamId, out ushort clientId)
        {
            foreach (var data in NetworkServerManager.ClientData.Values)
            {
                if (data.SteamId == steamId)
                {
                    clientId = data.Id;
                    return true;
                }
            }

            clientId = 0;
            return false;
        }

        public bool OnLeaveLobby()
        {
            if (_handingOff) return true;
            _leaving = !Migrating;
            if (!HandOffIfHosting()) return false;

            StartCoroutine(AfterHandOff(() => LobbyManager.Instance.LeaveLobby()));
            return true;
        }

        private bool CanHandOff()
        {
            return Server.IsRunning && Lobby != CSteamID.Nil && Server.ClientCount >= 2
                   && NetworkServerManager.ServerType != ServerType.ShootingRange && _handedOffLobby != Lobby;
        }

        private bool HandOffIfHosting()
        {
            if (!CanHandOff()) return false;

            Elect();
            if (_successors.Count == 0) return false;
            HandOff(_successors[0], false);
            return true;
        }

        private IEnumerator AfterHandOff(Action then)
        {
            _handingOff = true;
            float deadline = Time.unscaledTime + 1.5f;
            while (Time.unscaledTime < deadline && Server.IsRunning && !_ackReceived && Server.TryGetClient(_handoffClient, out _))
                yield return null;
            _handingOff = false;
            then();
        }

        private bool WantsToQuit()
        {
            if (_quitAllowed || !CanHandOff() || !HandOffIfHosting()) return true;

            StartCoroutine(AfterHandOff(() =>
            {
                _quitAllowed = true;
                Application.Quit();
            }));
            return false;
        }

        private void OnEnable() => Application.wantsToQuit += WantsToQuit;

        private void OnDisable() => Application.wantsToQuit -= WantsToQuit;

        public bool HandOffTo(ushort clientId)
        {
            if (!Server.IsRunning || Lobby == CSteamID.Nil) return false;
            if (!NetworkServerManager.ClientData.TryGetValue(clientId, out var data) || data.SteamId == Me) return false;
            if (!Server.TryGetClient(clientId, out _)) return false;

            HandOff(data.SteamId, true);
            StartCoroutine(AfterHandOff(() =>
            {
                NetworkServerManager.Instance.StopServer();
                ServerSnapshot.ClearServerState();
                NetworkServerManager.ClientData.Clear();
                if (LobbyManager.Instance != null)
                    LobbyManager.Instance.owner = new CSteamID(data.SteamId);
                RefreshHostUi();
            }));
            return true;
        }

        private void HandOff(ulong target, bool oldHostStays)
        {
            Debug.Log($"[Migration] {ServerSnapshot.Describe()}");
            var message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.MigrationHandoff);
            message.AddULong(target);
            message.AddBool(oldHostStays);
            Server.SendToAll(message);

            SendSnapshot(true, new[] { target }, oldHostStays);
            SteamMatchmaking.SetLobbyOwner(Lobby, new CSteamID(target));

            foreach (var connection in Server.Clients)
            {
                if (connection is SteamConnection steam)
                    SteamNetworkingSockets.FlushMessagesOnConnection(steam.SteamNetConnection);
            }

            _handedOffLobby = Lobby;
            _handingOff = true;
            _ackReceived = false;
            _handoffClient = TryGetClientId(target, out var targetClient) ? targetClient : (ushort)0;
            _lastSnapshot = null;
            _handoffTarget = target;
            _newHost = target;
            _ownerWatchUntil = Time.unscaledTime + 30f;
            _oldHostStays = oldHostStays;
            _successors.Clear();
            Debug.Log($"[Migration] handing host to {target}, old host stays: {oldHostStays}");
        }

        #endregion

        #region New host

        private void HandleConnection(Connection connection, Message connectMessage)
        {
            ulong steamId = connection is SteamConnection steam ? steam.SteamId.m_SteamID : 0;

            if (TryReadResume(connectMessage, out ushort oldId))
            {
                if (_awaiting.Contains(oldId)
                    && NetworkServerManager.ClientData.TryGetValue(oldId, out var data)
                    && data.SteamId == steamId
                    && Server.Accept(connection, oldId))
                {
                    _awaiting.Remove(oldId);
                    _resumed.Add(oldId);
                    Debug.Log($"[Migration] client {oldId} ({steamId}) is back");
                    return;
                }

                Debug.LogWarning($"[Migration] rejected resume of {oldId} from {steamId}");
                Server.Reject(connection);
                return;
            }

            if (_awaiting.Count == 0)
            {
                Server.Accept(connection);
                return;
            }

            for (ushort id = 1; id <= Server.MaxClientCount; id++)
            {
                if (_awaiting.Contains(id) || NetworkServerManager.ClientData.ContainsKey(id) || Server.TryGetClient(id, out _)) continue;
                if (Server.Accept(connection, id)) return;
                break;
            }

            Server.Reject(connection);
        }

        private static bool TryReadResume(Message message, out ushort oldId)
        {
            oldId = 0;
            if (message == null || message.UnreadLength < 4) return false;
            if (message.GetUShort() != ResumeMagic) return false;
            oldId = message.GetUShort();
            return true;
        }

        private static Message ResumeMessage(ushort oldId)
        {
            var message = Message.Create();
            message.AddUShort(ResumeMagic);
            message.AddUShort(oldId);
            return message;
        }

        public bool OnResumedConnect(ushort clientId)
        {
            if (!_resumed.Contains(clientId)) return false;

            var message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.MigrationResumed);
            message.AddULong(Me);
            message.AddUShorts(_dropped.ToArray());
            Server.Send(message, clientId);
            return true;
        }

        public bool WasResumed(ushort clientId) => _resumed.Contains(clientId);

        private void Promote(byte[] snapshot)
        {
            RestoreInfo info;
            try
            {
                info = ServerSnapshot.Restore(snapshot);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Migrating = false;
                NetworkManager.Instance.FinishDisconnect(_disconnect);
                return;
            }

            ushort me = _myId;
            _awaiting.Clear();
            _resumed.Clear();
            _dropped.Clear();
            foreach (var id in NetworkServerManager.ClientData.Keys)
                _awaiting.Add(id);
            _awaitDeadline = Time.unscaledTime + ReconnectWindow;

            Server.Start(0, info.MaxClients, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId);

            if (!_oldHostStays && info.HostClientId != me)
                Drop(info.HostClientId);

            Resuming = true;
            Migrating = false;
            Client.Connect("127.0.0.1", 5, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId, ResumeMessage(me));

            _handoffTarget = 0;
            _oldHostStays = false;
            _successors.Clear();
            _electionTimer = 1f;
            _takeOverLobby = true;
            _newHost = Me;
            _lastSnapshot = null;
            _handedOffLobby = CSteamID.Nil;
            if (LobbyManager.Instance != null)
                LobbyManager.Instance.hostAddress = Me.ToString();
            if (LobbyManager.Instance != null)
                LobbyManager.Instance.owner = new CSteamID(Me);

            RefreshHostUi();
            Debug.Log($"[Migration] took over as host: {ServerPlayerCount()} players, {NetworkServerManager.ClientData.Count} clients, waiting for {_awaiting.Count}");
            Debug.Log($"[Migration] {ServerSnapshot.Describe()}");
        }

        private static int ServerPlayerCount() => Multiplayer.Entity.Server.ServerPlayer.list.Count;

        private void Drop(ushort clientId)
        {
            _awaiting.Remove(clientId);
            if (!_dropped.Contains(clientId))
                _dropped.Add(clientId);

            NetworkServerManager.Instance.RemoveClient(clientId);
            NetworkManager.Instance.RemovePlayer(clientId);

            var message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.MigrationDropped);
            message.AddUShorts(new[] { clientId });
            Server.SendToAll(message);

            Debug.Log($"[Migration] dropped client {clientId}");
        }

        private void DropAwaiting()
        {
            foreach (var id in _awaiting.ToList())
                Drop(id);
        }

        private void TryTakeOverLobby(CSteamID lobby)
        {
            if (lobby == CSteamID.Nil)
            {
                _takeOverLobby = false;
                return;
            }

            if (SteamMatchmaking.GetLobbyOwner(lobby).m_SteamID != Me) return;

            string me = Me.ToString();
            SteamMatchmaking.SetLobbyData(lobby, LobbyManager.HostAddressKey, me);
            SteamMatchmaking.SetLobbyData(lobby, "LobbyOwner", me);
            if (SteamNetworkingUtils.GetLocalPingLocation(out var location) >= 0f)
            {
                SteamNetworkingUtils.ConvertPingLocationToString(ref location, out var text, 1024);
                SteamMatchmaking.SetLobbyData(lobby, PingKey, text);
            }

            _takeOverLobby = false;
            Debug.Log("[Migration] lobby now points to this host");
        }

        #endregion

        private static void RefreshHostUi()
        {
            if (Menu.GameVoteMenu.Instance != null)
                Menu.GameVoteMenu.Instance.RefreshHost();

            if (Menu.GameUIManager.Instance == null) return;
            foreach (var pair in Menu.GameUIManager.Instance.PlayerList)
            {
                if (pair.Value == null
                    || !NetworkManager.ClientData.TryGetValue(pair.Key, out var data)
                    || !Multiplayer.Entity.Client.ClientPlayer.list.TryGetValue(pair.Key, out var player)) continue;

                string name = Chat.Instance.GetPlayerNameNetwork(data.Name, data.SteamId, player.DisplayTag, data.OwnedDlc) + " " + player.GetGroupName();
                pair.Value.playerName = name;
                pair.Value.playerNameText.SetText(name);
            }
        }

        #region Clients

        public bool TryMigrate(DisconnectedEventArgs e)
        {
            if (Migrating)
            {
                if (_pendingFinal != null)
                {
                    _promoteNext = _pendingFinal;
                    _pendingFinal = null;
                }
                return true;
            }
            if (_leaving)
            {
                _leaving = false;
                return false;
            }
            if (Server.IsRunning) return false;

            if (Riptide.Transports.Steam.SteamClient.LastEndReason == SteamPeer.KickedEndReason) return false;

            switch (e.Reason)
            {
                case DisconnectReason.ServerStopped:
                case DisconnectReason.TimedOut:
                case DisconnectReason.TransportError:
                case DisconnectReason.Disconnected:
                    break;
                default:
                    return false;
            }

            var lobby = Lobby;
            if (lobby == CSteamID.Nil) return false;

            _currentHost = ulong.TryParse(LobbyManager.Instance.hostAddress, out var host) ? host : 0;
            var candidates = Candidates(lobby);
            if (candidates.Count == 0) return false;

            Migrating = true;
            _disconnect = e;
            _ownerWatchUntil = Time.unscaledTime + 30f;
            Debug.Log($"[Migration] lost the host ({e.Reason}), trying {string.Join(", ", candidates)}");
            StartCoroutine(Migrate(candidates));
            return true;
        }

        private List<ulong> Candidates(CSteamID lobby)
        {
            var members = new HashSet<ulong>();
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            for (int i = 0; i < count; i++)
                members.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobby, i).m_SteamID);

            var ordered = new List<ulong>();
            if (_handoffTarget != 0) ordered.Add(_handoffTarget);
            ordered.AddRange(_successors);
            if (ordered.Count == 0)
            {
                foreach (var part in SteamMatchmaking.GetLobbyData(lobby, SuccessorsKey).Split(','))
                {
                    if (ulong.TryParse(part, out var id)) ordered.Add(id);
                }
            }

            return ordered
                .Distinct()
                .Where(id => id != _currentHost && members.Contains(id))
                .Where(id => id != Me || _lastSnapshot != null)
                .ToList();
        }

        private IEnumerator Migrate(List<ulong> candidates)
        {
            yield return null;
            float deadline = Time.unscaledTime + ReconnectWindow;
            foreach (var target in candidates)
            {
                if (Time.unscaledTime > deadline) break;

                if (target == Me)
                {
                    Promote(_lastSnapshot);
                    yield break;
                }

                float candidateDeadline = Mathf.Min(deadline, Time.unscaledTime + CandidateWindow);
                while (Time.unscaledTime < candidateDeadline)
                {
                    bool? result = null;
                    void Connected(object s, EventArgs a) => result = true;
                    void Failed(object s, ConnectionFailedEventArgs a) => result = false;

                    Resuming = true;
                    Client.Connected += Connected;
                    Client.ConnectionFailed += Failed;
                    bool started = Client.Connect(target.ToString(), 10, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId, ResumeMessage(_myId));
                    while (started && result == null && Time.unscaledTime < candidateDeadline)
                        yield return null;
                    Client.Connected -= Connected;
                    Client.ConnectionFailed -= Failed;

                    if (result == true)
                    {
                        _currentHost = target;
                        _newHost = target;
                        LobbyManager.Instance.hostAddress = target.ToString();
                        Migrating = false;
                        _handoffTarget = 0;
                        _oldHostStays = false;
                        Debug.Log($"[Migration] reconnected to the new host {target}");
                        yield break;
                    }

                    Resuming = false;
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }

            Debug.LogWarning("[Migration] no new host answered");
            Migrating = false;
            Resuming = false;
            NetworkManager.Instance.FinishDisconnect(_disconnect);
        }

        public bool CompleteResume()
        {
            if (!Resuming) return false;
            Resuming = false;
            Debug.Log("[Migration] authorized again");
            return true;
        }

        public bool OnOwnerLeft()
        {
            var lobby = Lobby;
            if (lobby == CSteamID.Nil) return false;
            if (Migrating || Resuming || _handoffTarget != 0) return true;
            return _successors.Count > 0 || !string.IsNullOrEmpty(SteamMatchmaking.GetLobbyData(lobby, SuccessorsKey));
        }

        private void WatchOwner(CSteamID lobby)
        {
            if (lobby == CSteamID.Nil || Time.unscaledTime > _ownerWatchUntil)
            {
                _newHost = 0;
                return;
            }

            if (_newHost == 0 || _newHost == Me) return;
            if (SteamMatchmaking.GetLobbyOwner(lobby).m_SteamID == Me)
                SteamMatchmaking.SetLobbyOwner(lobby, new CSteamID(_newHost));
        }

        [MessageHandler((ushort)ServerToClientId.MigrationSuccessors, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void OnSuccessors(Message message)
        {
            var ids = message.GetULongs();
            Instance._successors.Clear();
            Instance._successors.AddRange(ids);
            if (!ids.Contains(Me))
                Instance._lastSnapshot = null;
        }

        [MessageHandler((ushort)ServerToClientId.MigrationSnapshot, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void OnSnapshot(Message message)
        {
            uint session = message.GetUInt();
            uint seq = message.GetUInt();
            ushort index = message.GetUShort();
            ushort count = message.GetUShort();
            bool final = message.GetBool();
            bool oldHostStays = message.GetBool();
            byte[] chunk = message.GetBytes();

            var data = Instance._assembler.Add(session, seq, index, count, chunk);
            if (data == null) return;

            Instance._lastSnapshot = data;
            if (!final || Server.IsRunning || Instance.Migrating) return;

            Debug.Log($"[Migration] got the final snapshot ({data.Length} bytes), taking over");
            Instance.Migrating = true;
            Instance._oldHostStays = oldHostStays;
            Instance._ownerWatchUntil = Time.unscaledTime + 30f;
            Instance._disconnect = new DisconnectedEventArgs(DisconnectReason.ServerStopped, null);
            Instance._pendingFinal = data;
            Instance._pendingFinalDeadline = Time.unscaledTime + 2f;
            Client.Send(Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.MigrationAck));
        }

        [MessageHandler((ushort)ClientToServerId.MigrationAck, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void OnAck(ushort fromClient, Message message)
        {
            if (Instance._handingOff && fromClient == Instance._handoffClient)
                Instance._ackReceived = true;
        }

        [MessageHandler((ushort)ServerToClientId.MigrationHandoff, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void OnHandoff(Message message)
        {
            if (Server.IsRunning) return;
            Instance._handoffTarget = message.GetULong();
            Instance._newHost = Instance._handoffTarget;
            Instance._oldHostStays = message.GetBool();
            Instance._ownerWatchUntil = Time.unscaledTime + 30f;
        }

        [MessageHandler((ushort)ServerToClientId.MigrationResumed, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void OnResumed(Message message)
        {
            ulong host = message.GetULong();
            ushort[] dropped = message.GetUShorts();

            if (LobbyManager.Instance != null)
                LobbyManager.Instance.owner = new CSteamID(host);
            foreach (var id in dropped)
                NetworkManager.Instance.RemovePlayer(id);

            NetworkManager.Instance.ResumeAuthorize();
            RefreshHostUi();
        }

        [MessageHandler((ushort)ServerToClientId.MigrationDropped, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void OnDropped(Message message)
        {
            foreach (var id in message.GetUShorts())
                NetworkManager.Instance.RemovePlayer(id);
        }

        #endregion
    }
}
