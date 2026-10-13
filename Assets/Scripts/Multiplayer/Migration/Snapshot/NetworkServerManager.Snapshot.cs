using System;
using System.Collections.Generic;
using System.Linq;
using Manager;
using Multiplayer.Entity.Server;
using Multiplayer.Migration;
using Multiplayer.Server;
using Multiplayer.ServerTypes;
using Steamworks;

namespace Multiplayer
{
    public partial class ClientData
    {
        internal void WriteSnapshot(SnapshotWriter w)
        {
            w.UShort(Id);
            w.String(Name);
            w.ULong(SteamId);
            w.ULong(GroupId);
            w.String(Desc);
            w.Int(Exp);
            w.Bool(DisplayTag);
            w.Bool(Eliminated);
            w.Bool(OwnedDlc);
            w.Int(DesiredTeam);
            w.Shorts(Weapons);
            w.Int(Perks.Count);
            foreach (var perk in Perks)
                w.UShort((ushort)perk);
            w.Cosmetics(CosmeticIndex);
            w.Bool(SerializeInventoryInitialized);
            if (!SerializeInventoryInitialized)
            {
                w.Int(InventoryLength);
                w.Int(InventoryReceived);
                w.Bytes(SerializeInventory);
            }
        }

        internal static ClientData ReadSnapshot(SnapshotReader r)
        {
            ushort id = r.UShort();
            string name = r.String();
            ulong steamId = r.ULong();
            ulong groupId = r.ULong();
            string desc = r.String();
            int exp = r.Int();
            bool displayTag = r.Bool();
            bool eliminated = r.Bool();
            bool ownedDlc = r.Bool();
            int desiredTeam = r.Int();
            short[] weapons = r.Shorts();
            int perkCount = r.Int();
            var perks = new ushort[perkCount];
            for (int i = 0; i < perkCount; i++)
                perks[i] = r.UShort();

            var data = new ClientData(id, name, steamId, ownedDlc, groupId, desc, exp, displayTag, eliminated, weapons, perks)
            {
                DesiredTeam = desiredTeam,
                CosmeticIndex = r.Cosmetics() ?? new InventoryManager.CosmeticIndex()
            };

            data.SerializeInventoryInitialized = r.Bool();
            if (!data.SerializeInventoryInitialized)
            {
                data.InventoryLength = r.Int();
                data.InventoryReceived = r.Int();
                data.SerializeInventory = r.Bytes();
            }

            return data;
        }
    }

    public partial class NetworkServerManager
    {
        private bool _awaitingPlayers;

        void CancelServerTimers()
        {
            this.Unschedule(nameof(GotoReady));
            this.Unschedule(nameof(KickLoadFailedPlayer));
            _awaitingPlayers = false;
        }

        internal void ClearServerTimers()
        {
            StopAllCoroutines();
            CancelServerTimers();
        }

        internal void WriteCore(SnapshotWriter w)
        {
            w.UShort(Server.MaxClientCount);
            w.UInt(CurrentTick);
            w.UShort((ushort)GameState);
            w.UShort((ushort)ServerType);
            w.UShort((ushort)ServerGameMode);
            w.Bool(CheatsEnabled);
            w.Int(VoteCount);
            w.Bool(IsPlaying);
            w.Bool(ServerRandomMap);
            w.Bool(ServerRandomGameMode);
            w.Bool(ServerDisableSpecialWeapon);
            w.Bool(ServerEnableWorkshop);
            w.String(CurrentMap);
            w.Bool(IsWorkshopMap);
            w.ULong(WorkshopMap.m_PublishedFileId);
            w.UInt(Seed);
            w.Float(_votingTime);
            w.Bool(_awaitingPlayers);
            w.Int(CurrentServer is KnockoutServer knockout ? knockout.Round : 0);

            w.Int(BannedPlayer.Count);
            foreach (var id in BannedPlayer) w.ULong(id);

            w.Int(AllowedWeapon.Count);
            foreach (var allowed in AllowedWeapon) w.Bool(allowed);

            w.Int(EnabledWorkshopMaps.Count);
            foreach (var map in EnabledWorkshopMaps) w.ULong(map.m_PublishedFileId);

            w.Int(MapVote.Count);
            foreach (var pair in MapVote)
            {
                w.String(pair.Key);
                w.UShort(pair.Value);
            }

            w.Int(GameModeVote.Count);
            foreach (var pair in GameModeVote)
            {
                w.UShort(pair.Key);
                w.UShort(pair.Value);
            }

            w.Int(WorkshopMapVote.Count);
            foreach (var pair in WorkshopMapVote)
            {
                w.ULong(pair.Key);
                w.UShort(pair.Value);
            }

            w.Int(PlayerVoteList.Count);
            foreach (var pair in PlayerVoteList)
            {
                w.UShort(pair.Key);
                w.Short(pair.Value.Item1);
                w.String(pair.Value.Item2);
            }

            w.Int(ClientData.Count);
            foreach (var data in ClientData.Values)
                data.WriteSnapshot(w);

            w.Int(LobbyDataManager.datas.Count);
            foreach (var data in LobbyDataManager.datas.Values)
            {
                w.ULong(data.playerId);
                w.String(data.playerName);
                w.UInt(data.kills);
                w.UInt(data.deaths);
                w.UInt(data.exp);
            }

            w.Int(ServerGrenade.nextId);
            w.Int(ServerCollectable.nextId);

            w.WriteInvoke(this, nameof(GotoReady));
            w.WriteInvoke(this, nameof(KickLoadFailedPlayer));
        }

        internal ushort ReadCore(SnapshotReader r)
        {
            ushort maxClients = r.UShort();
            r.UInt();
            CurrentTick = NetworkManager.Instance.ServerTick;
            GameState = (GameState)r.UShort();
            SetServerType((ServerType)r.UShort());
            ServerGameMode = (GameMode)r.UShort();
            CheatsEnabled = r.Bool();
            VoteCount = r.Int();
            IsPlaying = r.Bool();
            ServerRandomMap = r.Bool();
            ServerRandomGameMode = r.Bool();
            ServerDisableSpecialWeapon = r.Bool();
            ServerEnableWorkshop = r.Bool();
            CurrentMap = r.String();
            IsWorkshopMap = r.Bool();
            WorkshopMap = new PublishedFileId_t(r.ULong());
            Seed = r.UInt();
            _votingTime = r.Float();
            bool awaitingPlayers = r.Bool();
            int round = r.Int();
            if (CurrentServer is KnockoutServer knockout)
                knockout.SetRound(round);

            BannedPlayer.Clear();
            int count = r.Int();
            for (int i = 0; i < count; i++) BannedPlayer.Add(r.ULong());

            count = r.Int();
            for (int i = 0; i < count; i++)
            {
                bool allowed = r.Bool();
                if (i < AllowedWeapon.Count) AllowedWeapon[i] = allowed;
            }

            EnabledWorkshopMaps.Clear();
            count = r.Int();
            for (int i = 0; i < count; i++) EnabledWorkshopMaps.Add(new PublishedFileId_t(r.ULong()));

            count = r.Int();
            for (int i = 0; i < count; i++) MapVote[r.String()] = r.UShort();

            count = r.Int();
            for (int i = 0; i < count; i++) GameModeVote[r.UShort()] = r.UShort();

            WorkshopMapVote.Clear();
            count = r.Int();
            for (int i = 0; i < count; i++) WorkshopMapVote[r.ULong()] = r.UShort();

            PlayerVoteList.Clear();
            count = r.Int();
            for (int i = 0; i < count; i++)
            {
                ushort id = r.UShort();
                PlayerVoteList[id] = new Tuple<short, string>(r.Short(), r.String());
            }

            ClientData.Clear();
            count = r.Int();
            for (int i = 0; i < count; i++)
            {
                var data = Multiplayer.ClientData.ReadSnapshot(r);
                ClientData[data.Id] = data;
            }

            LobbyDataManager.datas.Clear();
            count = r.Int();
            for (int i = 0; i < count; i++)
            {
                var data = new LobbyDataManager.LobbyData(r.ULong(), r.String(), r.UInt(), r.UInt(), r.UInt());
                LobbyDataManager.datas[data.playerId] = data;
            }

            ServerGrenade.nextId = (ushort)r.Int();
            ServerCollectable.nextId = (ushort)r.Int();

            CancelServerTimers();
            if (awaitingPlayers)
                StartCoroutine(StartGame());
            r.ReadInvoke(this, nameof(GotoReady));
            r.ReadInvoke(this, nameof(KickLoadFailedPlayer));

            return maxClients;
        }

        internal void RebuildEntityIds()
        {
            _ids.Clear();
            for (ushort i = 41; i < ushort.MaxValue; i++)
            {
                if (!Entity.Entities.ContainsKey(i))
                    _ids.Enqueue(i);
            }
        }
    }
}
