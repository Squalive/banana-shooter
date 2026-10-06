using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using MapEditor;
using Menu;
using Mode;
using Multiplayer.Entity.Interface;
using Multiplayer.Entity.Server.Enemy;
using Multiplayer.LagCompensation;
using Multiplayer.Server;
using Riptide;
using Steamworks;
using UnityEngine;
using Weapon;
using Weapon.WeaponStats;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Server
{
    public enum Team
    {
        Rebel = 0,
        Alliance = 1
    }
    public class ServerPlayer : MonoBehaviour, IPlayerServer
    {
        public static Dictionary<ushort, IPlayerServer> list = new();

        public int Cash { get; set; } = 0;

        public ulong SteamId { get; set; }

        public ushort Id { get; private set; }
        public string Username { get; set; }

        public int Health { set; get; } = 100;
        public int MaxHealth { get; set; } = 100;
        public bool Dead { get; set; }

        public bool IsGrappling { get; set; }
        public bool DisplayTag { get; set; }
        public Vector3 GrapplePoint { get; set; }

        public Vector3 Velocity { set; get; }

        public Rigidbody player;

        public Rigidbody PlayerRb
        {
            get => player;
            set => player = value;
        }

        public ushort Kills { get; set; }
        public float StayTime { get; set; }
        public ushort Deaths { get; set; }
        public bool IsAiming { get; set; }
        public int CurrentWeaponIndex { set; get; } = 0;
        public short[] Weapons { get; } = { -1, -1, -1, -1 };
        public string Description { set; get; } = "Banana";
        public ushort CurrentLifeKill { set; get; } = 0;
        public bool IsCrazy { set; get; }
        public List<Perk> Perks { get; set; } = new() { Perk.None, Perk.None, Perk.None };
        public int WeaponLevel { get; set; }

        bool HasBanana { get; set; } = false;

        public short Coin { get; set; } = 0;

        public Team Team { get; set; } = Team.Rebel;
        public bool IsInfected { set; get; } = false;

        public bool Grounded { get; set; }
        public bool Crouch { get; set; }

        public const int MaxTickStore = 128;
        public bool Eliminated { get; set; }
        public TransformUpdate[] TransformBuffer { get; } = new TransformUpdate[MaxTickStore];
        public ActiveWeapon[] ActiveWeapons { get; set; }

        public float XRotation { get; set; }

        // public Transform head;

        #region Player Movement Action

        [MessageHandler((ushort)ClientToServerId.PlayerMovement, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerMovement(ushort fromClient, Message message)
        {
            Vector3 pos = message.GetVector3();
            float rot = message.GetFloat();
            Vector3 vel = message.GetVector3();

            if (list.TryGetValue(fromClient, out var player))
            {
                player.PlayerTransform.position = pos;
                player.PlayerTransform.rotation = Quaternion.Euler(0, rot, 0);

                player.Velocity = vel;
                player.XRotation = message.GetFloat();
                player.Grounded = message.GetBool();
            }
        }

        [MessageHandler((ushort)ClientToServerId.PlayerAim, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerAim(ushort fromClient, Message message)
        {
            bool aiming = message.GetBool();

            if (list.TryGetValue(fromClient, out var player))
            {
                player.IsAiming = aiming;

                Message msg = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.PlayerAim);

                msg.Add(fromClient);
                msg.Add(aiming);

                NetworkServerManager.Instance.Server.SendToAll(msg);
            }
        }

        [MessageHandler((ushort)ClientToServerId.PlayerStartCrouch, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerStartCrouch(ushort fromClient, Message message)
        {
            if (list.ContainsKey(fromClient) && list[fromClient] != null)
            {
                list[fromClient].DisableInvincible();
                list[fromClient].Crouch = true;
                // list[fromClient].col.height = CrouchSize;
                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.PlayerStartCrouch);
                msg.Add(fromClient);
                NetworkServerManager.Instance.Server.SendToAll(msg, fromClient);
            }
        }
        [MessageHandler((ushort)ClientToServerId.PlayerStopCrouch, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerStopCrouch(ushort fromClient, Message message)
        {
            if (list.ContainsKey(fromClient) && list[fromClient] != null)
            {
                list[fromClient].DisableInvincible();
                list[fromClient].Crouch = false;
                // list[fromClient].col.height = NormalSize;
                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.PlayerStopCrouch);
                msg.Add(fromClient);
                NetworkServerManager.Instance.Server.SendToAll(msg, fromClient);
            }
        }

        [MessageHandler((ushort)ClientToServerId.StartGrapple, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerStartGrapple(ushort fromClient, Message message)
        {
            if (list.ContainsKey(fromClient))
            {
                list[fromClient].GrapplePoint = message.GetVector3();
                list[fromClient].IsGrappling = true;

                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.StartGrapple);

                msg.Add(fromClient);
                msg.Add(list[fromClient].GrapplePoint);

                NetworkServerManager.Instance.Server.SendToAll(msg, fromClient);
            }
        }

        [MessageHandler((ushort)ClientToServerId.StopGrapple, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void StopGrappleServer(ushort fromClient, Message message)
        {
            if (list.TryGetValue(fromClient, out var player))
            {
                player.IsGrappling = false;

                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.StopGrapple);

                msg.Add(fromClient);

                NetworkServerManager.Instance.Server.SendToAll(msg, fromClient);
            }
        }

        #endregion

        #region Player Action

        [MessageHandler((ushort)ClientToServerId.UpdateWeaponIndex, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void UpdateWeaponIndex(ushort fromClient, Message message)
        {
            int index = message.GetInt();
            if (index < 0 || index > 3) return;

            if (list.TryGetValue(fromClient, out var player))
            {
                var currentWeapon = player.GetCurrentWeapon();
                if (currentWeapon != null)
                {
                    currentWeapon.Disable();
                }
                player.CurrentWeaponIndex = index;

                currentWeapon = player.GetCurrentWeapon();
                if (currentWeapon != null)
                {
                    currentWeapon.Enable();
                }


                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.UpdateWeaponIndex);

                msg.Add(fromClient);
                msg.Add(player.CurrentWeaponIndex);

                NetworkServerManager.Instance.Server.SendToAll(msg, fromClient);
            }
        }

        [MessageHandler((ushort)ClientToServerId.Upgrade, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void UpgradeHealth(ushort fromClient, Message message)
        {
            uint tick = message.GetUInt();
            uint upgrade = message.GetUInt();
            if (list.TryGetValue(fromClient, out var player))
            {
                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.Upgrade);
                bool flag = player.Coin - 1 >= 0;

                msg.Add(fromClient);
                msg.Add(tick);
                msg.Add(flag);
                msg.Add(upgrade);

                if (flag)
                {
                    player.Coin--;
                    switch (upgrade)
                    {
                        case 0:
                            player.MaxHealth += 20;
                            player.MaxHealth = Mathf.Clamp(player.MaxHealth, 100, 200);
                            if (player.HasPerk(Perk.Fat))
                                player.MaxHealth = (int)(player.MaxHealth * 1.25f);

                            ((ServerPlayer)player).CancelInvoke(nameof(Breathe));
                            ((ServerPlayer)player).Invoke(nameof(Breathe), 4);

                            msg.Add(player.MaxHealth);
                            break;
                    }
                }




                NetworkServerManager.Instance.Server.SendToAll(msg);
            }
        }

        [MessageHandler((ushort)ClientToServerId.RequestData, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void RequestData(ushort fromClient, Message message)
        {
            RequestDataType dataType = (RequestDataType)message.GetUShort();
            Message msg;
            switch (dataType)
            {
                case RequestDataType.PlayerSpawn:
                    if (NetworkServerManager.ClientData.TryGetValue(fromClient, out var data))
                    {
                        bool specter = false;
                        bool eliminated = false;
                        ushort index = data.GetIndex();
                        switch (NetworkServerManager.ServerType)
                        {
                            case ServerType.OneVsOne:
                                specter = index > 2;

                                if (NetworkServerManager.GameState == GameState.Voting)
                                {
                                    if (index == 1)
                                    {
                                        msg = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.Message);

                                        msg.Add((ushort)MessageType.OneVsOne0);

                                        NetworkServerManager.Instance.Server.SendToAll(msg);
                                    }
                                    else if (index == 2)
                                    {
                                        msg = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.Message);

                                        msg.Add((ushort)MessageType.OneVsOne1);

                                        NetworkServerManager.Instance.Server.SendToAll(msg);
                                    }
                                }
                                break;
                            case ServerType.KnockoutRound:
                                specter = data.Eliminated;
                                eliminated = data.Eliminated;
                                break;
                        }

                        Spawn(fromClient, data.Name, data.SteamId, data.Desc, data.Exp, data.GroupId, data.DisplayTag, specter, eliminated, data.Weapons, data.CosmeticIndex, data.Perks);
                    }
                    break;
                case RequestDataType.Init:
                    foreach (var enemy in ServerEnemy.list.Values)
                    {
                        NetworkServerManager.Instance.Server.Send(enemy.GetSpawnData(), fromClient);
                    }

                    foreach (var serverObject in ServerObject.list.Values)
                    {
                        serverObject.SendSpawn(fromClient);
                    }
                    break;
                case RequestDataType.InitRound:
                    if (NetworkServerManager.GameState == GameState.MidMatch)
                    {
                        NetworkServerManager.Instance.StartRound(fromClient);
                    }
                    break;
                case RequestDataType.DisplayTag:
                    bool t = message.GetBool();
                    if (NetworkServerManager.ClientData.ContainsKey(fromClient))
                    {
                        NetworkServerManager.ClientData[fromClient].SetDisplayTag(t);
                    }
                    break;
            }
        }

        [MessageHandler((ushort)ClientToServerId.VoiceChat, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void VoiceChat(ushort fromClient, Message message)
        {
            Message msg = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.VoiceChat);
            msg.Add(fromClient);
            msg.Add(message.GetBytes());
            msg.Add(message.GetUInt());
            NetworkServerManager.Instance.Server.SendToAll(msg, fromClient);
        }


        [MessageHandler((ushort)ClientToServerId.WeaponReload, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void WeaponReload(ushort fromClient, Message message)
        {
            if (list.TryGetValue(fromClient, out var player))
            {
                player.DisableInvincible();

                uint tick = message.GetUInt();

                var weapon = player.GetCurrentWeapon();

                if (weapon != null)
                {
                    weapon.DoReload(tick);

                    //TODO: Tell other players the fromclient is reloading
                    Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.WeaponStartReloading);
                    msg.Add(fromClient);
                    msg.Add(weapon.Index);
                    NetworkServerManager.Instance.Server.SendToAll(msg);
                }
            }
        }

        [MessageHandler((ushort)ClientToServerId.TurnLight, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void TurnLight(ushort fromClient, Message message)
        {
            if (list.ContainsKey(fromClient))
            {
                Message msg = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.TurnLight);
                msg.Add(fromClient);
                msg.Add(message.GetBool());
                NetworkServerManager.Instance.Server.SendToAll(msg, fromClient);
            }
        }


        [MessageHandler((ushort)ClientToServerId.ChangeCosmetic, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ChangeCosmetic(ushort fromClient, Message message)
        {
            if (list.ContainsKey(fromClient))
            {

                bool wear = message.GetBool();

                if (wear)
                {
                    byte[] serialize = message.GetBytes();

                    if (NetworkServerManager.ClientData.TryGetValue(fromClient, out var data))
                        InventoryManager.Instance.DeserializeNewItem(serialize, data);
                }
                else
                {
                    float type = message.GetUShort();

                    Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.ChangeCosmetic);
                    msg.Add(fromClient);
                    msg.Add((ushort)type);
                    msg.Add(-1);
                    msg.Add(Color.clear);
                    msg.Add(0);
                    msg.Add(-1);
                    NetworkServerManager.Instance.Server.SendToAll(msg);
                }
            }
        }

        [MessageHandler((ushort)ClientToServerId.SpecialWeapon, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SpecialWeapon(ushort fromClient, Message message)
        {
            if (NetworkServerManager.ServerDisableSpecialWeapon) return;
            if (NetworkServerManager.ServerGameMode == GameMode.Randomizer || NetworkServerManager.ServerGameMode == GameMode.GunGame ||
                NetworkServerManager.ServerGameMode == GameMode.RocketMode) return;

            if (list.TryGetValue(fromClient, out var player) && !player.IsInfected)
            {
                short weapon = message.GetShort();

                var weapons = NetworkServerManager.Instance.weaponInfo;
                if (weapon < 0 || weapon >= weapons.Count || !weapons[weapon].specialWeapon) return;

                player.CurrentWeaponIndex = 3;

                var w = player.GetCurrentWeapon();

                player.Weapons[3] = weapon;

                w.Init(NetworkServerManager.Instance.weaponInfo[weapon]);

                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.SpecialWeapon);
                msg.Add(fromClient);
                msg.Add(weapon);
                NetworkServerManager.Instance.Server.SendToAll(msg);

                player.StartSpecialWeaponTimer();
            }
        }

        public static VoteKicking Vote;
        [MessageHandler((ushort)ClientToServerId.ManageToKick, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ManageToKick(ushort fromClient, Message message)
        {

            if (Vote != null) return;
            ushort kickId = message.GetUShort();


            if (list.TryGetValue(kickId, out var player))
            {
                ulong SteamId = player.SteamId;

                if (RolesManager.Instance.CheckIsAdmin(SteamId)
                    || RolesManager.Instance.CheckIsHelper(SteamId) ||
                    RolesManager.Instance.CheckIsDiscordMan(SteamId)
                    || RolesManager.Instance.CheckIsBananaMan(SteamId))
                {
                    return;
                }
                VoteKicking vote = new VoteKicking(kickId, fromClient);

                Vote = vote;

                Vote.Players.Add(fromClient, true);
                if (fromClient == kickId)
                {
                    Vote = null;
                    return;
                }



                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.ManageToKick);

                msg.Add(kickId);
                msg.Add(fromClient);

                NetworkServerManager.Instance.Server.SendToAll(msg);

                ((ServerPlayer)player).clearKick = ((ServerPlayer)player).StartCoroutine(((ServerPlayer)player).ClearKick());
            }
        }

        private Coroutine clearKick;
        IEnumerator ClearKick()
        {
            yield return new WaitForSeconds(30f);
            Vote = null;
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.ClearKick);
            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        [MessageHandler((ushort)ClientToServerId.AgreeKicking, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void AgreeKicking(ushort fromClient, Message message)
        {
            if (Vote != null)
            {
                if (Vote.Players.ContainsKey(fromClient)) return;
            }
            else
            {
                return;
            }
            ushort playerId = message.GetUShort();
            if (list.ContainsKey(playerId) && list.ContainsKey(fromClient))
            {
                Vote.agreeCount++;
                Vote.Players.Add(fromClient, true);

                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.AgreeKicking);

                NetworkServerManager.Instance.Server.SendToAll(msg);

                if (Vote.agreeCount > (ushort)Mathf.CeilToInt(NetworkServerManager.Instance.Server.ClientCount * 1 / 3f))
                {
                    NetworkServerManager.BannedPlayer.Add(list[Vote.PlayerId].SteamId);
                    Message m = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.VoteToKickFinish);
                    m.Add(Vote.PlayerId);
                    NetworkServerManager.Instance.Server.SendToAll(m);

                    NetworkServerManager.Instance.Server.DisconnectClient(Vote.PlayerId);
                }
            }
        }
        [MessageHandler((ushort)ClientToServerId.DisAgreeKicking, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void DisAgreeKicking(ushort fromClient, Message message)
        {
            if (Vote != null)
            {
                if (Vote.Players.ContainsKey(fromClient)) return;
            }
            else
            {
                return;
            }
            ushort playerId = message.GetUShort();
            if (list.ContainsKey(playerId))
            {
                Vote.disAgreeCount++;
                Vote.Players.Add(fromClient, false);

                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.DisAgreeKicking);

                NetworkServerManager.Instance.Server.SendToAll(msg);
            }
        }

        [MessageHandler((ushort)ClientToServerId.Respawn, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerRespawn(ushort fromClient, Message message)
        {
            if (list.ContainsKey(fromClient))
            {
                if (list[fromClient].Dead)
                    list[fromClient].Respawn();
            }
        }

        [MessageHandler((ushort)ClientToServerId.SpecialWeaponDisable, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SpecialWeaponDisable(ushort fromClient, Message message)
        {
            if (list.TryGetValue(fromClient, out var player))
            {
                player.StopSpecialWeaponTimer();
            }
        }

        [MessageHandler((ushort)ClientToServerId.ThrowObj, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ThrowObj(ushort fromClient, Message message)
        {
            if (list.TryGetValue(fromClient, out var player))
            {
                if (player.Dead) return;
                if (player.TacticalThrowCount <= 0 && NetworkServerManager.ServerGameMode != GameMode.SpecialGameMode) return;
                if (NetworkServerManager.ServerGameMode != GameMode.SpecialGameMode)
                {
                    player.TacticalThrowCount--;
                }
                player.DisableInvincible();

                uint tick = message.GetUInt();
                ThrowObjectMenu.ThrowObjectType type = (ThrowObjectMenu.ThrowObjectType)message.GetInt();
                Vector3 dir = message.GetVector3();
                Vector3 pos = message.GetVector3();

                ServerGrenade obj;
                switch (type)
                {
                    case ThrowObjectMenu.ThrowObjectType.Grenade:
                        obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerGrenade"), pos, Quaternion.identity)
                            .GetComponent<ServerGrenade>();
                        break;
                    case ThrowObjectMenu.ThrowObjectType.Knife:
                        obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerKnife"), pos, Quaternion.identity)
                            .GetComponent<ServerGrenade>();
                        break;
                    case ThrowObjectMenu.ThrowObjectType.FlashBang:
                        obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerFlashBang"), pos, Quaternion.identity)
                            .GetComponent<ServerGrenade>();
                        break;

                    case ThrowObjectMenu.ThrowObjectType.MolotovCocktail:
                        obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerMolotovCocktail"), pos, Quaternion.identity)
                            .GetComponent<ServerGrenade>();
                        break;
                    case ThrowObjectMenu.ThrowObjectType.JumpPad:
                        obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerJumpPadThrowable"), pos, Quaternion.identity)
                            .GetComponent<ServerGrenade>();
                        break;
                    default:
                        obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerGrenade"), pos, Quaternion.identity)
                            .GetComponent<ServerGrenade>();
                        break;
                }

                Transform target = player.PlayerTransform;

                // if (type == ThrowObjectMenu.ThrowObjectType.Missile)
                // {
                //     bool flag = false;
                //     float dis = float.MaxValue;
                //     foreach (var otherTarget in list.Values)
                //     {
                //         if (player != otherTarget)
                //         {
                //             float d = Vector3.Distance(otherTarget.PlayerTransform.position, pos);
                //             if (d < dis)
                //             {
                //                 target = otherTarget.PlayerTransform;
                //                 dis = d;
                //                 flag = true;
                //             }
                //         }
                //     }
                //
                //     if (!flag)
                //     {
                //         foreach (var otherTarget in ServerEnemy.list.Values)
                //         {
                //             float d = Vector3.Distance(otherTarget.transform.position, pos);
                //             if (d < dis)
                //             {
                //                 target = otherTarget.transform;
                //                 dis = d;
                //                 flag = true;
                //             }
                //         }
                //     }
                // }

                obj.Initialize(dir, fromClient, type, DamageType.Player, target, tick);
            }
        }

        [MessageHandler((ushort)ClientToServerId.SpecterMode, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SpecterMode(ushort fromClient, Message message)
        {
            if (list.TryGetValue(fromClient, out var player))
            {
                if (fromClient == 1 || RolesManager.Instance.CheckIsAdmin(player.SteamId))
                {
                    bool flag = message.GetBool();
                    if (player.Dead && flag) return;
                    player.Spect(flag);

                }
            }
        }
        [MessageHandler((ushort)ClientToServerId.BuyWeapon, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        static void BuyWeapon(ushort fromClient, Message message)
        {
            if (list.TryGetValue(fromClient, out var player))
            {
                short weaponId = message.GetShort();
                int replaceIndex = message.GetInt();
                short[] w = player.Weapons;

                if (!ClientInputValidation.IsValidPurchase(weaponId, replaceIndex, NetworkServerManager.Instance.weaponInfo.Count, w.Length))
                {
                    return;
                }

                WeaponStat stat = NetworkServerManager.Instance.weaponInfo[weaponId];

                if (player.Cash < stat.price)
                {
                    return;
                }

                player.Cash -= stat.price;
                w[replaceIndex] = weaponId;
                player.CurrentWeaponIndex = replaceIndex;

                for (int i = 0; i < player.Weapons.Length; i++)
                {
                    if (player.Weapons[i] >= 0)
                        player.ActiveWeapons[i].Init(NetworkServerManager.Instance.weaponInfo[player.Weapons[i]]);
                    else
                        player.ActiveWeapons[i].Disable();
                }

                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.GetWeapon);
                msg.Add(player.Id);

                msg.Add(replaceIndex);

                msg.Add(player.Weapons);
                msg.Add(true);
                NetworkServerManager.Instance.Server.SendToAll(msg);
            }
        }

        public bool Specting { set; get; } = false;

        #endregion

        #region Receive Player Data

        [MessageHandler((ushort)ClientToServerId.Init, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ClientInit(ushort fromClient, Message message)
        {
            string name = message.GetString();

            if (NetworkServerManager.ServerType == ServerType.Endless)
            {
                if (Endless.Instance && Endless.Instance.serverStarted)
                {
                    NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                    return;
                }
            }
            ulong groupId = message.GetULong();
            string description = message.GetString();
            int exp = message.GetInt();
            bool displayTag = message.GetBool();
            short[] weapons = message.GetShorts();
            ushort[] perks = message.GetUShorts();

            int len = message.GetInt();
            byte[] serializedInventory = message.GetBytes();

            if (!NetworkServerManager.TryGetAuthorizedSteamId(fromClient, out ulong steamId))
            {
                NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                return;
            }

            if (NetworkServerManager.BannedPlayer.Contains(steamId))
            {
                NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                return;
            }
            if (!ClientInputValidation.IsValidLoadout(weapons, 3, NetworkServerManager.Instance.weaponInfo.Count)
                || !ClientInputValidation.IsValidPerks(perks, 3, (int)Perk.Bot)
                || !ClientInputValidation.TryStartInventory(len, serializedInventory, out var inventory, out var inventoryReceived))
            {
                Debug.LogWarning($"Client {fromClient} sent invalid init data");
                NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                return;
            }

            var dlcResult = NetworkServerManager.GetLicenseResult(steamId);

            //TODO:Set the init data
            ClientData data = new ClientData(fromClient, name, steamId, dlcResult == EUserHasLicenseForAppResult.k_EUserHasLicenseResultHasLicense, groupId, description, exp, displayTag, NetworkServerManager.CurrentServer.GetEliminated(), weapons, perks);
            if (NetworkServerManager.ClientData.ContainsKey(fromClient))
            {
                NetworkServerManager.ClientData.Remove(fromClient);
            }
            NetworkServerManager.ClientData.Add(fromClient, data);

            if (!LobbyDataManager.datas.ContainsKey(steamId))
            {
                LobbyDataManager.datas.Add(steamId, new LobbyDataManager.LobbyData(steamId, name, 0, 0, (uint)exp));
            }

            data.InventoryLength = len;

            data.SerializeInventory = inventory;
            data.InventoryReceived = inventoryReceived;

            if (len == inventoryReceived)
            {
                data.SerializeInventoryInitialized = true;
                InventoryManager.Instance.DeserializeInventory(inventory, data);
            }
            else
            {
                Debug.Log($"{steamId}'s inventory result is too long, need to be split");
            }

        }

        [MessageHandler((ushort)ClientToServerId.FragmentSerializeInventory, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void FragmentSerializeInventory(ushort fromClient, Message message)
        {
            if (NetworkServerManager.ClientData.TryGetValue(fromClient, out var data))
            {
                if (data.SerializeInventoryInitialized) return;

                byte[] newFragmentData = message.GetBytes();
                int received = data.InventoryReceived;

                if (!ClientInputValidation.TryAppendInventory(data.SerializeInventory, ref received, newFragmentData))
                {
                    Debug.LogWarning($"Client {fromClient} sent more inventory data than it declared");
                    NetworkServerManager.Instance.Server.DisconnectClient(fromClient);
                    return;
                }

                data.InventoryReceived = received;

                if (received == data.InventoryLength)
                {
                    data.SerializeInventoryInitialized = true;

                    InventoryManager.Instance.DeserializeInventory(data.SerializeInventory, data);
                }
            }
        }


        [MessageHandler((ushort)ClientToServerId.SendPerks, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ReceivePerks(ushort fromClient, Message message)
        {
            ushort[] p = message.GetUShorts();

            if (!ClientInputValidation.IsValidPerks(p, 3, (int)Perk.Bot)) return;

            if (list.TryGetValue(fromClient, out var player))
            {
                if (NetworkServerManager.ClientData.TryGetValue(fromClient, out var data))
                {
                    int i = 0;
                    foreach (var perk in p)
                    {
                        data.Perks[i++] = (Perk)perk;
                    }

                    player.Perks = data.Perks;
                }


                player.SetPerks();
            }

            Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.SendPerks);

            msg.Add(fromClient);
            msg.Add(p);

            NetworkServerManager.Instance.Server.SendToAll(msg);
        }

        [MessageHandler((ushort)ClientToServerId.GetWeapon, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ChangeWeapon(ushort fromClient, Message message)
        {
            short[] weapons = message.GetShorts();
            if (!ClientInputValidation.IsValidLoadout(weapons, 3, NetworkServerManager.Instance.weaponInfo.Count)) return;

            if (list.TryGetValue(fromClient, out var player))
            {
                player.PlayerGetWeapon(weapons);
            }
        }

        #endregion

        #region Set Player Data

        public void SetPerks()
        {
            if (HasPerk(Perk.Fat))
            {
                MaxHealth = (int)(100 * 1.25f);
                CancelInvoke(nameof(Breathe));
                Invoke(nameof(Breathe), 4);
            }
        }

        public void Spect(bool flag)
        {
            if (Eliminated) return;
            Specting = flag;

            player.gameObject.SetActive(!Specting);

            if (!flag)
                Respawn();
            else
            {
                Dead = true;
                Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.SpecterMode);

                message.Add(Id);
                message.Add(true);

                NetworkServerManager.Instance.Server.SendToAll(message);
            }
        }

        public void ProcessWeapon(short[] weapons)
        {
            switch (NetworkServerManager.ServerType)
            {
                case ServerType.Endless:
                    if (Endless.Instance.serverStarted)
                    {
                        Weapons[0] = 11;
                        Weapons[1] = 3;
                        Weapons[2] = -1;
                    }
                    else
                    {
                        Weapons[0] = Weapons[1] = Weapons[2] = -1;
                    }

                    break;
                default:
                    if (IsInfected) return;

                    switch (NetworkServerManager.ServerGameMode)
                    {
                        case GameMode.Randomizer:
                            short index = (short)Random.Range(0, NetworkServerManager.Instance.weaponInfo.Count);
                            while (index == 3 || NetworkServerManager.Instance.weaponInfo[index].specialWeapon)
                            {
                                index = (short)Random.Range(0, NetworkServerManager.Instance.weaponInfo.Count);
                            }

                            Weapons[0] = index;
                            Weapons[1] = 3;

                            Weapons[2] = -1;
                            CurrentWeaponIndex = 0;
                            break;
                        case GameMode.GunGame:
                            if (weapons.Length >= 3)
                            {
                                Weapons[0] = GunGame.WeaponIds[WeaponLevel];
                                Weapons[1] = 3;

                                Weapons[2] = -1;
                                CurrentWeaponIndex = 0;
                            }
                            break;
                        case GameMode.OneShotOneKill:
                            if (weapons.Length >= 3)
                            {
                                Weapons[0] = 6;
                                Weapons[1] = 3;

                                Weapons[2] = -1;
                                CurrentWeaponIndex = 0;
                            }
                            break;
                        case GameMode.RocketMode:
                            if (weapons.Length >= 3)
                            {
                                Weapons[0] = 0;
                                Weapons[1] = 20;

                                Weapons[2] = -1;
                                CurrentWeaponIndex = 0;
                            }
                            break;
                        default:
                            for (int i = 0; i < weapons.Length; i++)
                            {
                                var weaponIndex = weapons[i];

                                if (weaponIndex != -1 &&
                                    (!NetworkServerManager.AllowedWeapon[weapons[i]] || weaponIndex == 6 || NetworkServerManager.Instance.weaponInfo[weaponIndex].specialWeapon))
                                {
                                    weaponIndex = -1;
                                }

                                Weapons[i] = weaponIndex;
                            }

                            for (int i = CurrentWeaponIndex; i < weapons.Length; i++)
                            {
                                if (weapons[i] == -1) CurrentWeaponIndex++;
                            }
                            break;
                    }
                    break;
            }

            for (int i = 0; i < Weapons.Length; i++)
            {
                if (Weapons[i] >= 0)
                    ActiveWeapons[i].Init(NetworkServerManager.Instance.weaponInfo[Weapons[i]]);
                else
                    ActiveWeapons[i].Disable();
            }
        }

        public void SetWeapon()
        {
            if (IsInfected) return;
            ProcessWeapon(NetworkServerManager.ClientData[Id].Weapons);

            if (Dead) return;
            Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.GetWeapon);
            msg.Add(Id);

            msg.Add(CurrentWeaponIndex);

            msg.Add(Weapons);
            msg.Add(false);
            NetworkServerManager.Instance.Server.SendToAll(msg);
        }
        public void PlayerGetWeapon(short[] weapons)
        {
            if (IsInfected) return;
            ProcessWeapon(weapons);

            if (NetworkServerManager.ClientData.TryGetValue(Id, out var data))
            {
                data.Weapons = weapons;
            }

            // Debug.Log(weapons);
            if (Dead) return;
            Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.GetWeapon);
            msg.Add(Id);

            msg.Add(CurrentWeaponIndex);

            msg.Add(Weapons);
            msg.Add(false);
            NetworkServerManager.Instance.Server.SendToAll(msg);
        }

        public void SetCrazy(string str)
        {
            CancelInvoke(nameof(ClearCrazy));
            IsCrazy = true;
            Message crazyPlayerMessage = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.CrazyPlayer);
            crazyPlayerMessage.Add(str);
            crazyPlayerMessage.Add(Id);
            NetworkServerManager.Instance.Server.SendToAll(crazyPlayerMessage);
            Invoke(nameof(ClearCrazy), 20f);
        }

        void ClearCrazy()
        {
            IsCrazy = false;
        }

        public void SetHasBanana(bool flag)
        {
            if (NetworkServerManager.ServerGameMode != GameMode.CatchTheBanana) return;
            HasBanana = flag;

            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.PlayerHasBanana);

            message.Add(Id);
            message.Add(HasBanana);

            NetworkServerManager.Instance.Server.SendToAll(message);

        }

        void Breathe()
        {
            TakeHealth();

            if (Health < MaxHealth)
            {
                Invoke(nameof(Breathe), 0.4f);
            }
        }

        void FireGone()
        {
            IsFire = false;
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.PlayerFire);

            message.Add(Id);
            message.Add(IsFire);

            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        private float takeHealthTimer = 0;
        void TakeHealth()
        {
            if (Health <= 0 || Health >= MaxHealth || Dead) return;
            Health += 30;
            Health = Mathf.Clamp(Health, 0, MaxHealth);
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.TakeHealth);

            message.Add(Id);
            message.Add(Health);

            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        #endregion

        public void Destroy()
        {
            Destroy(gameObject);
            // Logger.LogError("You cant destroy player");
        }

        public bool IsEnemy()
        {
            return false;
        }

        public bool IsPlayer()
        {
            return true;
        }
        public bool IsVoid()
        {
            return false;
        }

        public bool IsThrowable()
        {
            return false;
        }

        public bool HasPerk(Perk perk)
        {
            return Perks.Contains(perk);
        }

        public ActiveWeapon GetCurrentWeapon()
        {
            if (CurrentWeaponIndex >= 0 && CurrentWeaponIndex < 4)
            {
                return ActiveWeapons[CurrentWeaponIndex];
            }
            return null;
        }

        private void OnDestroy()
        {
            NetworkServerManager.RemoveEntity(Id);

            if (list.ContainsKey(Id))
                list.Remove(Id);

            if (clearKick != null)
                StopCoroutine(clearKick);
            Vote = null;
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.ClearKick);
            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        public bool specting = false;
        public int TacticalThrowCount { get; set; }
        private ulong GroupId { get; set; }
        private InventoryManager.CosmeticIndex CosmeticIndex { set; get; } = new();
        static void Spawn(ushort id, string name, ulong SteamId, string description, int exp, ulong groupId, bool displayTag, bool specter, bool eliminated, short[] weapons, InventoryManager.CosmeticIndex cosmeticIndex, List<Perk> Perks)
        {
            // Debug.Log($"{id} wanted to spawn");
            foreach (var serverPlayer in list.Values)
            {
                serverPlayer.SendSpawn(id);
            }

            #region Find Spawn Pos

            Vector3 pos = GetSpawnPos();

            #endregion


            ServerPlayer player =
                Instantiate(NetworkServerManager.Instance.ServerPlayerPrefab, pos, Quaternion.identity)
                    .GetComponent<ServerPlayer>();

            player.Id = id;
            player.Username = name;
            player.SteamId = SteamId;
            player.Health = player.MaxHealth;
            player.Description = description;
            player.exp = exp;
            player.IsInfected = false;
            player.GroupId = groupId;
            player.DisplayTag = displayTag;
            player.Perks = Perks;
            player.Eliminated = eliminated;
            player.SetPerks();

            if (NetworkServerManager.ServerGameMode == GameMode.SpecialGameMode)
            {
                player.Coin = 1000;
            }

            player.ActiveWeapons = new[] { new ActiveWeapon(player, 0), new ActiveWeapon(player, 1), new ActiveWeapon(player, 2), new ActiveWeapon(player, 3) };

            player.ProcessWeapon(weapons);
            player.specting = specter;
            player.CosmeticIndex = cosmeticIndex;

            if (NetworkServerManager.ServerGameMode == GameMode.TeamDeathMatch)
            {
                if (NetworkServerManager.ClientData.TryGetValue(id, out var data))
                {
                    if (data.DesiredTeam == -1)
                    {
                        if (RebelPlayers > AlliancePlayers)
                        {
                            player.Team = Team.Alliance;
                            AlliancePlayers++;
                        }
                        else
                        {
                            player.Team = Team.Rebel;
                            RebelPlayers++;
                        }
                    }
                    else
                    {
                        Team t = (Team)data.DesiredTeam;

                        if (t == Team.Rebel)
                        {
                            if (RebelPlayers >= NetworkServerManager.Instance.Server.ClientCount / 2)
                            {
                                player.Team = Team.Alliance;
                                AlliancePlayers++;
                            }
                            else
                            {
                                player.Team = Team.Rebel;
                                RebelPlayers++;
                            }

                        }
                        else
                        {
                            if (AlliancePlayers >= NetworkServerManager.Instance.Server.ClientCount / 2)
                            {
                                player.Team = Team.Rebel;
                                RebelPlayers++;
                            }
                            else
                            {
                                player.Team = Team.Alliance;
                                AlliancePlayers++;
                            }

                        }
                    }
                }
            }


            list[id] = player;

            if (NetworkServerManager.Entity.GetEntity(id) != null)
            {
                NetworkServerManager.Entity.Entities.Remove(id);
            }

            NetworkServerManager.Entity.Entities.Add(id, player);

            player.SendSpawn();
        }

        public static int RebelPlayers = 0, AlliancePlayers = 0;
        public void SendSpawn(ushort toClient)
        {
            NetworkServerManager.Instance.Server.Send(GetSpawnData(Message.Create(MessageSendMode.Reliable, ServerToClientId.SpawnPlayer)), toClient);
        }
        /// <summary>Sends a player's info to all clients.</summary>
        void SendSpawn()
        {
            NetworkServerManager.Instance.Server.SendToAll(GetSpawnData(Message.Create(MessageSendMode.Reliable, ServerToClientId.SpawnPlayer)));
        }

        public int exp;

        private Message GetSpawnData(Message message)
        {
            message.Add(Id);
            message.Add(Username);
            message.Add(NetworkServerManager.Instance.Server.TryGetClient(Id, out var client));
            if (client != null)
                message.Add(client.RTT);
            message.Add(transform.position);
            message.Add(Kills);
            message.Add(Deaths);
            message.Add(CurrentWeaponIndex);
            message.Add(Weapons);
            message.Add(IsInfected);
            message.Add((int)Team);
            message.Add(WeaponLevel);
            message.AddInt((int)StayTime);
            message.Add(specting);
            message.Add(Cash);
            // message.Add(CosmeticIndex.weaponIndex);
            return message;
        }


        public enum DamageType
        {
            Player,
            Void,
            Enemy,
            Throwable
        }
        public void TakeDamage(int damage, ushort attacker, uint tick, bool headShot, bool wallbang, ushort weapon, params object[] param)
        {
            DamageType damageType = DamageType.Void;

            IEntity attackerEntity = NetworkServerManager.Entity.GetEntity(attacker);
            if (attackerEntity != null)
            {
                if (attackerEntity.IsPlayer())
                {
                    damageType = DamageType.Player;
                }
                else if (attackerEntity.IsEnemy())
                {
                    damageType = DamageType.Enemy;
                }
                else if (attackerEntity.IsThrowable())
                {
                    damageType = DamageType.Throwable;
                }
            }

            if (!CanDamage(attacker, damageType)) return;

            bool isAiming = false;

            if (param is { Length: > 0 })
            {
                isAiming = (bool)param[0];
            }

            // if (list.TryGetValue(attacker, out var attackerPlayer))
            //     damage = LuaManager.Hook.OnPlayerDamage(this, attackerPlayer, damage);
            //
            // damage = LuaManager.Hook.OnDamage(attacker, damage);

            Health -= damage;
            Health = Mathf.Clamp(Health, 0, MaxHealth);

            LastAttackBy = attacker;
            Message message;
            if (weapon == 1003 && !headShot)
            {
                if (Health > 0)
                {
                    CancelInvoke(nameof(FireGone));
                    IsFire = true;
                    Invoke(nameof(FireGone), 2f);

                    message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.PlayerFire);

                    message.Add(Id);
                    message.Add(IsFire);

                    NetworkServerManager.Instance.Server.SendToAll(message);
                }

            }
            takeHealthTimer = 0;
            if (Health <= 0 && !Dead)
            {
                Kill(attacker, headShot, wallbang, isAiming, weapon, damageType);
            }
            else
            {
                CancelInvoke(nameof(Breathe));
                Invoke(nameof(Breathe), 4);
                //Send Data
                message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.TakeDamage);

                message.Add(tick);
                message.Add(Id);
                message.Add(attacker);
                message.Add(headShot);
                message.Add(damage);

                NetworkServerManager.Instance.Server.SendToAll(message);
            }
        }

        public bool CanDamage(ushort attacker, DamageType damageType)
        {
            if (NetworkServerManager.ServerGameMode == GameMode.Infected &&
                (!NetworkServerManager.Instance.game.GetComponent<Infected>().infected ||
                (damageType == DamageType.Player && !((IPlayerServer)NetworkServerManager.Entity.Entities[attacker]).IsInfected && !IsInfected))) return false;
            if (NetworkServerManager.GameState == GameState.None) return false;
            if (invincible) return false;
            if (damageType == DamageType.Player && NetworkServerManager.ServerType == ServerType.Endless) return false;
            if (damageType != DamageType.Void && damageType != DamageType.Throwable && damageType != DamageType.Enemy)
            {
                switch (NetworkServerManager.ServerGameMode)
                {
                    case GameMode.SpecialGameMode:
                        if (attacker != Id)
                            return false;
                        break;
                }
            }

            return true;
        }

        public async void Kick(string reason = "")
        {
            await Task.Delay(500);
            NetworkServerManager.Instance.Server.DisconnectClient(Id, NetworkServerManager.GetDisconnectMessage(reason));
        }

        public async void Ban()
        {
            if (!NetworkServerManager.BannedPlayer.Contains(SteamId))
            {
                NetworkServerManager.BannedPlayer.Add(SteamId);
            }
            await Task.Delay(500);
            NetworkServerManager.Instance.Server.DisconnectClient(Id);
        }

        public void Kill(ushort attacker = 3000, bool headShot = false, bool wallbang = false, bool isAiming = false, ushort weapon = 1000, DamageType damageType = DamageType.Void)
        {
            CancelInvoke(nameof(FireGone));
            FireGone();
            CancelInvoke(nameof(Breathe));

            if (CurrentWeaponIndex == 3)
            {
                CurrentWeaponIndex = 0;
            }
            Cash = 0;

            if (NetworkServerManager.ServerGameMode != GameMode.KillConfirm)
            {
                Deaths++;
                if (LobbyDataManager.datas.TryGetValue(SteamId, out var data))
                    data.UpdateDeaths();
            }
            CurrentLifeKill = 0;
            Health = 0;
            if (NetworkServerManager.ServerGameMode == GameMode.Infected && !IsInfected)
            {
                MaxHealth = 200;

                if (HasPerk(Perk.Fat))
                    MaxHealth = (int)(MaxHealth * 1.25f);
                Health = MaxHealth;
                NetworkServerManager.Instance.SetInfectedPlayer(Id, attacker);
            }
            else
            {
                Dead = true;
                transform.GetChild(0).gameObject.SetActive(false);
                MaxHealth = 100;

                if (HasPerk(Perk.Fat))
                    MaxHealth = (int)(MaxHealth * 1.25f);
                //Send Dead
                Message deadMsg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.Dead);

                deadMsg.Add(Id);
                deadMsg.Add(attacker);
                deadMsg.Add(headShot);
                deadMsg.Add(wallbang);
                deadMsg.Add(isAiming);
                deadMsg.Add(weapon);
                deadMsg.Add((ushort)damageType);

                NetworkServerManager.Instance.Server.SendToAll(deadMsg);

                GameManager.Instance.ServerPlayerDead?.Invoke(this,
                    list.TryGetValue(attacker, out var p) ? p : null);
                Invoke(nameof(Respawn), 5f);
            }

            bool hasBanana = HasBanana;
            SetHasBanana(false);
            if (attacker != Id && list.TryGetValue(attacker, out var fromPlayer))
            {
                switch (NetworkServerManager.ServerGameMode)
                {
                    case GameMode.KillConfirm:
                        ServerCollectable item =
                            Instantiate(PrefabManager.Instance.GetPrefab("ServerKillConfirmItem"), player.transform.position,
                                Quaternion.identity).GetComponent<ServerCollectable>();

                        item.InitializeKillConfirm(Id, attacker);
                        break;
                    case GameMode.CatchTheBanana:
                        if (hasBanana)
                            fromPlayer.SetHasBanana(true);
                        break;
                    case GameMode.GunGame:
                        int weaponKill = (fromPlayer.Kills - GunGame.UpdateWeaponRequired * fromPlayer.WeaponLevel);
                        if (weaponKill >= GunGame.UpdateWeaponRequired)
                        {
                            fromPlayer.WeaponLevel++;

                            // fromPlayer.PlayerGetWeapon(fromPlayer.weapons);
                            if (fromPlayer.WeaponLevel >= GunGame.WeaponIds.Length)
                            {
                                NetworkServerManager.Instance.StopGame();
                            }
                            else
                            {
                                Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.PlayerWeaponLevel);
                                msg.Add(attacker);
                                msg.Add(fromPlayer.WeaponLevel);
                                NetworkServerManager.Instance.Server.SendToAll(msg);
                                fromPlayer.SetWeapon();
                            }
                        }

                        break;

                }

                if (NetworkServerManager.ServerGameMode != GameMode.KillConfirm)
                {
                    fromPlayer.Kills++;
                    fromPlayer.CurrentLifeKill++;
                    fromPlayer.Coin++;
                }

                if (!fromPlayer.IsCrazy)
                {
                    bool isFirstBlood = list[attacker].Kills == 1;
                    foreach (var player in list.Values)
                    {
                        if (player != list[attacker] && player.Kills > 0)
                        {
                            isFirstBlood = false;
                            break;
                        }
                    }

                    if (fromPlayer.CurrentLifeKill >= 10)
                    {
                        fromPlayer.SetCrazy($"Damn ({list[attacker].CurrentLifeKill} continues kills)");

                    }
                    else if (list[attacker].CurrentLifeKill >= 5)
                    {
                        fromPlayer.SetCrazy($"Damn ({list[attacker].CurrentLifeKill} continues kills)");
                    }
                    else if (isFirstBlood)
                    {
                        fromPlayer.SetCrazy("God Damn First blood");
                    }

                }
                if (LobbyDataManager.datas.TryGetValue(list[attacker].SteamId, out var data1))
                    data1.UpdateKill();
            }
            else if (damageType != DamageType.Player)
            {
                list.ElementAt(Random.Range(0, list.Count))
                    .Value.SetHasBanana(true);
            }
        }

        public Vector3 Position()
        {
            return PlayerTransform.position;
        }

        public string Nick()
        {
            return Username;
        }

        public bool IsFire { set; get; } = false;
        bool ReadyToFire { set; get; } = true;
        ushort LastAttackBy { set; get; } = 0;

        private float maxY, maxX, minX, maxZ, minZ;

        private MapBound _bound;

        public Transform PlayerTransform { get; set; }

        void SendLatency()
        {
            if (NetworkServerManager.Instance.Server.TryGetClient(Id, out var client))
            {
                Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.SendLatency);
                message.Add(Id);
                message.Add(client.RTT);
                NetworkServerManager.Instance.Server.SendToAll(message);
            }

        }

        private float latencyTimer = 0;
        private void Update()
        {

            latencyTimer += Time.deltaTime;
            if (latencyTimer >= 10)
            {
                latencyTimer -= 10;
                SendLatency();
            }

            if (leftTime > 0)
            {
                leftTime -= Time.deltaTime * leftTimeToDecrease;

                if (leftTime <= 0)
                {
                    StopSpecialWeaponTimer();
                }
            }

            if (IsFire && ReadyToFire)
            {
                ReadyToFire = false;
                TakeDamage(10, LastAttackBy, NetworkServerManager.Instance.CurrentTick, true, false, 1003);

                Invoke(nameof(GetReadyToFire), 1f);
            }

            if (!Dead)
            {
                var position = PlayerTransform.position;
                float x = position.x;
                float z = position.z;

                if (!NetworkServerManager.Instance.IsWorkshopMap)
                {
                    if (position.y >= maxY || x >= maxX || x <= minX || z >= maxZ || z <= minZ)
                    {
                        if (!alreadyWarn)
                        {
                            alreadyWarn = true;
                            warnTimer = WarningUI.time;
                        }

                        warnTimer -= Time.deltaTime;
                        if (warnTimer < 0)
                        {
                            alreadyWarn = false;
                            warnTimer = 0;
                            TakeDamage(300, Entity.MaxEntityAmount, NetworkServerManager.Instance.CurrentTick, false, false, 1004);//,DamageType.Void
                        }
                    }
                    else
                    {
                        if (alreadyWarn)
                        {
                            alreadyWarn = false;
                        }
                    }
                }

                if (TacticalThrowCount <= 0)
                {
                    throwablePower += ThrowableManager.ThrowableIncrease * Time.deltaTime;
                    if (throwablePower >= 1)
                    {
                        TacticalThrowCount++;
                        throwablePower = 0;
                    }
                }
                else
                {
                    if (throwablePower > 0)
                    {
                        throwablePower = 0;
                    }
                }
            }
            else
            {
                if (alreadyWarn)
                {
                    alreadyWarn = false;
                }
            }
            if (PlayerTransform.position.y < -300 && !Dead)
                TakeDamage(300, Entity.MaxEntityAmount, NetworkServerManager.Instance.CurrentTick, false, false, 1004);//,DamageType.Void
        }

        private float throwablePower;
        private float warnTimer;
        void GetReadyToFire()
        {
            ReadyToFire = true;
        }
        public bool invincible = false;

        public void DisableInvincible()
        {
            CancelInvoke(nameof(DisableInvincible));
            if (!invincible) return;
            invincible = false;

            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.DisableInvincible);
            message.Add(Id);
            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        public void Respawn()
        {
            if (!Dead || specting || Eliminated) return;
            CancelInvoke(nameof(Respawn));
            invincible = true;
            Invoke(nameof(DisableInvincible), 3f);
            Vector3 pos = GetSpawnPos();

            Health = MaxHealth;

            TacticalThrowCount = 1;

            PlayerTransform.position = pos;
            Dead = false;
            player.gameObject.SetActive(true);

            ProcessWeapon(NetworkServerManager.ClientData[Id].Weapons);
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.Respawn);

            message.Add(Id);
            message.Add(pos);
            message.Add(Weapons);
            message.Add(CurrentWeaponIndex);

            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        private float leftTime = 0;
        private float leftTimeToDecrease = 0.05f;
        public void StartSpecialWeaponTimer()
        {
            leftTime = 1f;
        }
        public void StopSpecialWeaponTimer()
        {
            //stop
            leftTime = 0;

            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.SpecialWeaponDisable);

            message.Add(Id);

            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        static Vector3 GetSpawnPos()
        {
            Vector3 pos;
            switch (NetworkServerManager.ServerType)
            {
                case ServerType.Endless:
                    pos = Endless.Instance.spawnPos[Random.Range(0, Endless.Instance.spawnPos.Count)].position;
                    break;
                default:
                    switch (NetworkServerManager.ServerGameMode)
                    {
                        case GameMode.SpecialGameMode:
                            pos = ShootingRange.Instance ? ShootingRange.Instance.spawnPos[Random.Range(0, ShootingRange.Instance.spawnPos.Count)].position : Vector3.zero;
                            break;
                        default:
                            pos = NetworkServerManager.Instance.IsWorkshopMap
                                ? MapSaver.CurrentMap != null && MapSaver.CurrentMap.spawnPos.Count > 0
                                    ? MapSaver.CurrentMap.spawnPos[Random.Range(0, MapSaver.CurrentMap.spawnPos.Count)].ToVector3()
                                    : Vector3.zero
                                : MapBound.Instance.spawnPos[Random.Range(0, MapBound.Instance.spawnPos.Count)]
                                    .position;

                            break;

                    }
                    break;
            }


            pos.y += 3f;

            if (Physics.Raycast(pos, Vector3.down, out var hit, 100f))
            {
                int i = 0;
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    pos = hit.point + Vector3.up * 2f;
                }
                else
                {
                    Vector3 newPos = pos + new Vector3(Random.Range(-30, 30f), 0f, Random.Range(-30, 30f));

                    while (i < 100)
                    {
                        if (Physics.Raycast(newPos, Vector3.down, out hit, 1000f))
                        {
                            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                            {
                                newPos = hit.point + Vector3.up * 2f;
                                break;
                            }
                        }
                        i++;
                        newPos = pos + new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
                    }

                    if (Physics.Raycast(newPos, Vector3.down, 100f))
                    {
                        pos = newPos;
                    }


                }
            }

            pos.y -= 1f;

            return pos;

        }

        private bool sendTime = true;

        void ReadyToSendTime()
        {
            sendTime = true;
        }

        public void AddTime()
        {
            StayTime += Time.fixedDeltaTime;
            SendTime();
        }

        public void AddCash(int c)
        {
            Cash += c;
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.GrindCash);

            message.Add(Id);
            message.Add(Cash);

            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        void SendTime()
        {
            if (sendTime)
            {
                sendTime = false;
                Invoke(nameof(ReadyToSendTime), 1f);

                Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.SetTime);
                message.Add(Id);
                message.Add((int)StayTime);
                NetworkServerManager.Instance.Server.SendToAll(message);
            }
        }
        private bool alreadyWarn = false;

        private void Start()
        {
            _bound = MapBound.Instance;
            maxY = _bound.maxY;

            maxX = _bound.maxX;
            maxZ = _bound.maxZ;

            minX = _bound.minX;
            minZ = _bound.minZ;

            PlayerTransform = player.transform;

            TacticalThrowCount = 1;
        }

        public static float CrouchSize = 2.1f;
        public static float NormalSize = 3f;
        [SerializeField] private CapsuleCollider col;

        private void FixedUpdate()
        {
            TransformUpdate transformUpdate = new TransformUpdate(NetworkServerManager.Instance.CurrentTick, false, PlayerTransform.position);

            TransformBuffer[NetworkServerManager.Instance.CurrentTick % MaxTickStore] = transformUpdate;

            if (!Dead)
                SendPlayerMovement();

            if (HasBanana && NetworkServerManager.ServerGameMode == GameMode.CatchTheBanana)
            {
                AddTime();
            }
        }

        public void SendPlayerMovement()
        {
            Message msg = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.PlayerMovement);
            msg.Add(Id);
            msg.AddUInt(NetworkServerManager.Instance.CurrentTick);
            msg.Add(PlayerTransform.position);
            msg.Add(Velocity);
            msg.Add(PlayerTransform.eulerAngles.y);
            msg.Add(XRotation);
            msg.Add(Grounded);
            NetworkServerManager.Instance.Server.SendToAll(msg, Id);
        }
    }
}
