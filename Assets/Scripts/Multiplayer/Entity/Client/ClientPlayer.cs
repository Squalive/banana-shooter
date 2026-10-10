
using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Cosmetic;
using Demo;
using Demo.Entity;
using DitzelGames.FastIK;
using Manager;
using Map;
using Menu;
using Movement;
using Multiplayer.Entity.Client.Enemy;
using Multiplayer.Entity.Server;
using Multiplayer.LagCompensation;
using PlayerCameraController;
using Pool;
using Quest;
using Riptide;
using Steamworks;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;
using Weapon;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Client
{
    public class ClientPlayer : MonoBehaviour
    {
        public static Dictionary<ushort, ClientPlayer> list = new Dictionary<ushort, ClientPlayer>();

        public static ClientPlayer LocalPlayer { get; private set; }

        private PlayerMovement playerMovement;

        PlayerMovement PlayerMovement
        {
            get
            {
                if (playerMovement == null)
                    return playerMovement = PlayerMovement.Instance;
                return playerMovement;
            }
        }

        public bool isLocal = false;
        public int Cash { get; private set; } = 0;
        public DemoPlayer demoPlayer;
        public PlayerState playerState;
        public ushort Id { get; private set; }

        public bool IsLocal { get; private set; }

        public bool Dead { get; set; }
        public ushort Kills { get; set; }
        public ushort Deaths { get; set; }
        public int StayTime { get; set; }
        public bool DisplayTag { get; private set; }

        public bool HasBanana { get; private set; } = false;

        [SerializeField] public Transform orientation;
        public enum OutlineType
        {
            None,
            Death,
            Invincible,
            Team,
            Infected,
            HasBanana,
        }

        [SerializeField] public Animator animator;

        public int Exp { get; private set; }

        public Rigidbody player;
        [SerializeField] public TextMeshProUGUI nameText;

        private Vector3 desiredPos = Vector3.zero;
        private Quaternion desiredRot = Quaternion.identity;

        // [Obsolete]
        // public List<MultiplayerWeapon> weapons = new List<MultiplayerWeapon>();

        [SerializeField] public LookedToObject leftHandTarget, rightHandTarget;

        //public List<GameObject> hatCosmetics = new List<GameObject>();
        //public List<GameObject> faceCosmetics = new List<GameObject>();
        //public List<GameObject> shoeLCosmetics = new List<GameObject>();
        //public List<GameObject> shoeRCosmetics = new List<GameObject>();
        //public List<GameObject> hairCosmetics = new List<GameObject>();
        //public List<GameObject> clothesCosmetics = new List<GameObject>();
        //public List<GameObject> pantCosmetics = new List<GameObject>();
        public SkinnedMeshRenderer daveHair, clothes, pant;

        public List<Transform> eyes = new List<Transform>();

        public SkinnedMeshRenderer[] models;

        public List<Perk> perks = new List<Perk>() { Perk.None, Perk.None, Perk.None };
        public int WeaponLevel { get; private set; }

        public SteamVoiceChatPeer Peer;

        public DamageTracker DamageTracker;

        private PlayerAnimation _playerAnimation;

        [SerializeField] public Transform spine;

        private void Start()
        {
            if (!isLocal)
            {
                Peer = new SteamVoiceChatPeer(voice);
                _playerAnimation = new PlayerAnimation(animator, spine);
            }
            DamageTracker = new DamageTracker();
            if (DemoManager.Replaying) Destroy(this);
        }

        private void OnDestroy()
        {
            if (_gameUi)
                _gameUi.RemovePlayerDot(Id);
            if (list.ContainsKey(Id))
                list.Remove(Id);
        }

        [MessageHandler((ushort)ServerToClientId.Voting, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void StartVoting(Message message)
        {
            if (NetworkManager.ClientEnableWorkshop && NetworkManager.LocalClientData == null) return;
            NetworkManager.GameState = GameState.Voting;

            float time = message.GetFloat();
            ushort minimalPlayerCount = message.GetUShort();
            ushort len = message.GetUShort();

            List<Tuple<ushort, LobbyDataManager.LobbyData>> datas = new List<Tuple<ushort, LobbyDataManager.LobbyData>>();

            for (int i = 0; i < len; i++)
            {
                ushort id = message.GetUShort();

                uint kills = message.GetUInt();

                if (NetworkManager.ClientData.TryGetValue(id, out var clientData))
                {
                    string n = Chat.Instance.GetPlayerNameNetwork(clientData.Name, clientData.SteamId, clientData.DisplayTag);
                    LobbyDataManager.LobbyData data =
                        new LobbyDataManager.LobbyData(clientData.SteamId, n, kills, 0, (uint)clientData.Exp);

                    datas.Add(new Tuple<ushort, LobbyDataManager.LobbyData>(id, data));
                }
            }

            LoadingManager.Instance.StartLoadVotingScene(time - NetworkManager.Instance.Client.RTT / 1000f, datas, minimalPlayerCount);
        }
        [MessageHandler((ushort)ServerToClientId.OtherMode, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void OtherMode(Message message)
        {
            string map = message.GetString();
            if (map == "ShootingRange")
            {
                NetworkManager.ClientGameMode = GameMode.SpecialGameMode;
            }
            else if (map == "Endless")
            {
                NetworkManager.ClientGameMode = GameMode.SpecialNormalGameMode;
            }
            NetworkManager.Instance.MapId = map;

            NetworkManager.GameState = GameState.MidMatch;

            NetworkManager.Instance.GameModeChanged?.Invoke(NetworkManager.ClientGameMode);
            LoadingManager.Iinstance.LoadGame(map, MapManager.Instance.GetMapTexture(map), "", -1);
        }

        [MessageHandler((ushort)ServerToClientId.StartGame, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void StartGameClient(Message message)
        {
            GameState roundStart = (GameState)message.GetUShort();

            NetworkManager.GameState = roundStart;

            bool isWorkshopMap = message.GetBool();

            GameMode gameMode = (GameMode)message.GetUShort();
            Debug.Log($"Game Mode is {gameMode}");
            string map = message.GetString();
            Debug.Log($"Map is {map}");

            uint seed = 0;

            int external = -1;
            NetworkManager.ClientGameMode = gameMode;
            NetworkManager.Instance.GameModeChanged?.Invoke(gameMode);

            NetworkManager.Instance.SetIsWorkshopMap(isWorkshopMap);
            NetworkManager.Instance.MapId = map;

            if (gameMode == GameMode.KingOfTheHill)
            {
                external = message.GetInt();
            }

            if (map == "ProcMap")
            {
                seed = message.GetUInt();
            }

            if (isWorkshopMap)
            {
                PublishedFileId_t mapId = (PublishedFileId_t)ulong.Parse(map);
                string workshopMapHash = message.GetString();

                Debug.Log($"{map} server hash: {workshopMapHash}");

                if (MapSaver.WorkshopMaps.TryGetValue(mapId, out var fileInfo))
                {
                    string localHash = fileInfo.GetMD5ChecksumForFile();
                    if (localHash != workshopMapHash)
                    {
                        Debug.Log($"{map} local hash: {localHash}");
                        LoadingManager.Instance.additionalMessage = "Custom map hash didnt match to the server";
                        NetworkManager.Instance.DisconnectClient();
                        return;
                    }
                }
                else
                {
                    LoadingManager.Instance.additionalMessage = "Custom map hash didnt match to the server: No local map found";
                    NetworkManager.Instance.DisconnectClient();
                    return;
                }
                LoadingManager.Iinstance.LoadWorkshopGame(mapId, external);
            }
            else
            {
                LoadingManager.Iinstance.LoadGame(map, MapManager.Instance.GetMapTexture(map), "", external, (int)seed);
            }
        }

        private ulong groupId;
        public static List<ulong> existGroup = new List<ulong>();
        public string GetGroupName()
        {
            string groupTag = SteamFriends.GetClanTag(new CSteamID(groupId));
            if (string.IsNullOrEmpty(groupTag)) return "";

            int index = Mathf.Max(0, existGroup.IndexOf(groupId)) % StringColor.Colors.Length;
            return $"<size=15><color={StringColor.Colors[index]}>{groupTag}</color></size>";
        }

        // void SetCosmeticLocal()
        // {
        //     Message message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.SetCosmetics);
        //     
        //     NetworkManager.Instance.SendByte += message.WrittenLength;
        //     
        //     NetworkManager.Instance.Client.Send(message);
        // }

        public void SentPerks()
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.SendPerks);
            ushort[] perks =
            {
                (ushort) PerkManager.Instance.perks[0], (ushort) PerkManager.Instance.perks[1],
                (ushort) PerkManager.Instance.perks[2]
            };
            message.Add(perks);
            NetworkManager.Instance.SendByte += message.WrittenLength;

            NetworkManager.Instance.Client.Send(message);
        }
        public int WeaponIndex { get; set; }
        [MessageHandler((ushort)ServerToClientId.SpawnPlayer, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SpawnPlayer(Message message)
        {
            ushort id = message.GetUShort();
            if (list.ContainsKey(id))
            {
                return;
            }
            string name = message.GetString();
            if (!NetworkManager.ClientData.TryGetValue(id, out var data))
            {
                data = new ClientData(id, name, 0, false, 0, String.Empty, 0, true, false, null, null);
            }
            short rtt = -1;
            if (message.GetBool())
                rtt = message.GetShort();
            Vector3 pos = message.GetVector3();
            ushort kills = message.GetUShort();
            ushort deaths = message.GetUShort();
            ClientPlayer player = null;
            if (!existGroup.Contains(data.GroupId))
            {
                existGroup.Add(data.GroupId);
            }
            if (id == NetworkManager.Instance.Client.Id)
            {
                //lcoal

                player = Instantiate(NetworkManager.Instance.LocalPlayerPrefab, pos, Quaternion.identity).GetComponent<ClientPlayer>();
                player.IsLocal = true;

                // player.SetCosmeticLocal();

                if (LoadingManager.Instance.isLoading)
                    LoadingManager.Instance.requestDone = true;

                LocalPlayer = player;
            }
            else
            {
                player = Instantiate(NetworkManager.Instance.PlayerPrefab, pos, Quaternion.identity)
                    .GetComponent<ClientPlayer>();
                player.IsLocal = false;
                player.nameText.SetText(name);
                GameMode gameMode = NetworkManager.ClientGameMode;
                player.nameText.color = gameMode == GameMode.SpecialGameMode
                    ? Color.white
                    : Color.red;

                if (!NetworkManager.Instance.IsTeamMode()) player.setNameCorrectly = true;
                else
                {
                    player.StartCoroutine(player.SetNameCorrectly());
                }
            }

            list.Add(id, player);
            player.Id = id;
            player.groupId = data.GroupId;
            player.Kills = kills;
            player.Deaths = deaths;
            player.playerState.Health = player.playerState.MaxHealth;
            player.WeaponIndex = message.GetInt();
            player.desiredPos = pos;

            if (player.interpolator) player.interpolator.NewUpdate(NetworkManager.Instance.ServerTick, false, pos);

            if (NetworkManager.Instance.testMode || NetworkManager.ClientGameMode == GameMode.SpecialGameMode)
            {
                player.coins = 1000;
                player.lastCoin = 1000;
                if (player.IsLocal)
                    UpgradeInGameMenu.Instance.AutoUpgrade();
                foreach (var item in UpgradeInGameMenu.Instance.upgradeItems)
                {
                    item.SetColor(player.coins);
                }
            }
            short[] weapons = message.GetShorts();

            bool isInfected = message.GetBool();
            player.playerState.IsInfected = isInfected;

            int team = message.GetInt();

            player.Exp = data.Exp;
            int weaponLevel = message.GetInt();
            player.WeaponLevel = weaponLevel;
            int stayTime = message.GetInt();
            player.StayTime = stayTime;

            player.DisplayTag = data.DisplayTag;
            bool specter = message.GetBool();

            player.Cash = message.GetInt();

            player.playerState.SetValues(player.isLocal, data.SteamId, data.Name, (Team)team, !specter);

            player.playerState.SetPlayerCosmetics(data.CosmeticIndex);

            player.InitializeWeaponManager();

            if (!player.IsLocal)
            {
                if (NetworkManager.ClientGameMode == GameMode.TeamDeathMatch)
                {
                    player.clothes.material.color = player.playerState.Team == Team.Rebel ? Color.red : Color.yellow;
                }
            }
            if (_gameUi != null)
            {
                _gameUi.AddPlayerToContent(name, id, data.SteamId, data.OwnedDlc, kills, deaths, player.IsReady, player.Exp, player.playerState.IsInfected, player.WeaponLevel, player.StayTime, rtt, player.DisplayTag, player.playerState);
            }
            player.SetWeapon(weapons, player.WeaponIndex, data.CosmeticIndex.weaponIndex, true);

            player.SetInfect();

            player.StartCoroutine(player.WaitToSpect(specter));

            DemoManager.Instance.AddPlayerSpawned(player);
        }

        void InitializeWeaponManager()
        {
            playerState.InitializeWeaponManager(audioSource);
        }

        IEnumerator SetNameCorrectly()
        {
            if (IsLocal) yield break;
            while (!list.ContainsKey(NetworkManager.Instance.Client.Id))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            if (_gameUi && NetworkManager.Instance.IsTeamMode(playerState))
            {
                //TODO: SPAWN ICON
                _gameUi.AddPlayerDot(head, Id, Vector3.up * 0.5f, playerState.Username, this);
            }
        }
        IEnumerator WaitToSpect(bool spect)
        {
            yield return new WaitForSeconds(0.1f);

            Spect(spect, false);
        }

        public Animator clawKnifeAnim;
        void SetInfect()
        {
            playerState.SetInfect();

            if (_gameUi && _gameUi.PlayerList.ContainsKey(Id))
            {
                _gameUi.PlayerList[Id].SetInfect(playerState.IsInfected);
            }

            if (!playerState.IsInfected)
            {
                if (NetworkManager.ClientGameMode == GameMode.TeamDeathMatch)
                {
                    if (_setTeamOutline != null)
                        StopCoroutine(_setTeamOutline);
                    _setTeamOutline = StartCoroutine(SetTeamOutline());
                }
            }
        }

        private Coroutine _setTeamOutline;
        IEnumerator SetTeamOutline()
        {
            while (!list.ContainsKey(NetworkManager.Instance.Client.Id))
            {
                yield return null;
            }

            if (NetworkManager.Instance.IsTeamMode(playerState))
            {
                playerState.SetOutlineType(OutlineType.Team);
                OutlineDisplay(Color.cyan);
            }
        }

        public void GetWeapon()
        {
            if (playerState.IsInfected) return;
            Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ClientToServerId.GetWeapon);

            msg.Add(NetworkManager.Instance.Weapons);

            NetworkManager.Instance.SendByte += msg.WrittenLength;

            NetworkManager.Instance.Client.Send(msg);
        }

        [MessageHandler((ushort)ServerToClientId.PlayerMovement, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void Movement(Message message)
        {
            ushort id = message.GetUShort();
            uint tick = message.GetUInt();
            Vector3 pos = message.GetVector3();
            Vector3 vel = message.GetVector3();
            float rot = message.GetFloat();
            float xRotation = message.GetFloat();
            bool ground = message.GetBool();
            if (list.TryGetValue(id, out var player))
            {
                float deltaY = AngleUtils.WrapAngle(player.head.eulerAngles.x) - xRotation;
                float deltaX = AngleUtils.WrapAngle(rot) - AngleUtils.WrapAngle(player.orientation.localEulerAngles.y);

                float deltaThreshold = 3.5f * Time.timeScale;

                deltaY = Mathf.Clamp(deltaY * 0.1f, -deltaThreshold, deltaThreshold);
                deltaX = Mathf.Clamp(deltaX * 0.1f, -deltaThreshold, deltaThreshold);

                Vector3 offset = player.Crouch ? new Vector3(0, 0.25f, 0) : Vector3.zero;
                Vector3 ogPos = player.desiredPos;
                player.desiredPos = pos + offset;
                player.desiredRot = Quaternion.Euler(0, rot, 0);
                player.xRotation = xRotation;
                if (!player.IsLocal)
                {
                    // player.player.velocity = vel;
                    // Head pivot keeps the vertical (pitch) look so the first
                    // person spectate camera (MoveCamera) can tilt up/down with
                    // the spectated player.
                    player.head.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                    player.playerState.SetVelocity(vel);

                    Vector3 dir = player.desiredPos - ogPos;
                    if (player._playerAnimation != null)
                    {
                        player._playerAnimation.SetInput(dir.magnitude < 0.01f
                            ? Vector3.zero
                            : player.orientation.InverseTransformDirection(dir) * 4f);
                        player._playerAnimation.SetGround(ground);
                        player._playerAnimation.SetXRotation(xRotation);
                    }

                    // Keep the recorded demo entity's animation in sync too so
                    // demos preserve the remote player's vertical look.
                    if (player.demoPlayer != null && player.demoPlayer.PlayerAnimation != null)
                        player.demoPlayer.PlayerAnimation.SetXRotation(xRotation);
                }

                player.Grounded = ground;

                player.playerState.SetDelta(deltaX, deltaY);

                if (player.interpolator) player.interpolator.NewUpdate(tick, false, player.desiredPos);
            }
        }

        [MessageHandler((ushort)ServerToClientId.PlayerAim, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerAim(Message message)
        {
            ushort id = message.GetUShort();

            bool aiming = message.GetBool();

            if (list.TryGetValue(id, out var player))
            {
                player.playerState.SetAiming(aiming);

                DemoManager.Instance.AddPlayerAiming(player.demoPlayer.Id, aiming);
            }
        }


        public Transform head;
        private float xRotation = 0;
        [MessageHandler((ushort)ServerToClientId.PlayerStartCrouch, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerStartCrouch(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var player))
            {
                player.Crouch = true;

                player.playerState.IsCrouching = true;

                if (player.Grounded)
                    AudioManager.Instance.SoundEffect3D("start_slide", player.player.transform.position, 0.8f);

                player.col.height = ServerPlayer.CrouchSize;

                player.demoPlayer.NewCrouch(true);

                player._playerAnimation.SetCrouch(true);
            }
        }
        [MessageHandler((ushort)ServerToClientId.PlayerStopCrouch, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerStopCrouch(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var player))
            {
                player.Crouch = false;

                player.playerState.IsCrouching = false;

                player.col.height = ServerPlayer.NormalSize;

                player.demoPlayer.NewCrouch(false);

                player._playerAnimation.SetCrouch(false);
            }
        }
        public bool IsReady { get; private set; }

        [MessageHandler((ushort)ServerToClientId.GetWeapon, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void GetWeapon(Message message)
        {
            ushort id = message.GetUShort();
            int weaponIndex = message.GetInt();
            if (list.TryGetValue(id, out var player) && !player.playerState.IsInfected && !player.specting && !player.Dead)
            {
                short[] weapons = message.GetShorts();
                // ushort[] weaponIndexs = message.GetUShorts();
                player.SetWeapon(weapons, weaponIndex, player.playerState.CosmeticIndex.weaponIndex);

                if (message.GetBool())
                {
                    AudioManager.Instance.PlayPitched("weapon_bought", .4f);
                }
                else
                {
                    AudioManager.Instance.SoundEffect3DPitch("weapon_bought", player.transform.position, .4f);
                }
            }
        }
        [MessageHandler((ushort)ServerToClientId.PickupWeapon, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PickupWeapon(Message message)
        {
            ushort id = message.GetUShort();
            int weaponIndex = message.GetInt();
            if (list.TryGetValue(id, out var player) && !player.playerState.IsInfected && !player.specting)
            {
                string weapon = message.GetString();

                // ushort[] weaponIndexs = message.GetUShorts();
                player.PickupWeapon(weapon, weaponIndex, player.playerState.CosmeticIndex.weaponIndex);
            }
        }
        public GameObject fire;

        [MessageHandler((ushort)ServerToClientId.TakeDamage, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void TakeDamage(Message message)
        {
            uint tick = message.GetUInt();
            ushort id = message.GetUShort();
            ushort fromClient = message.GetUShort();
            bool headShot = message.GetBool();
            if (list.TryGetValue(id, out var player))
            {
                int damage = message.GetInt();

                player.TakeDamage(damage, headShot, player.player.transform.position, Vector3.up, fromClient, tick);
            }
        }

        [MessageHandler((ushort)ServerToClientId.PlayerFire, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerFire(Message message)
        {
            ushort id = message.GetUShort();

            if (list.ContainsKey(id))
            {
                bool isFire = message.GetBool();
                if (list[id].IsLocal)
                {
                    if (_gameUi)
                    {
                        _gameUi.SetFire(isFire ? 1f : 0f);
                    }
                }
                else
                {
                    list[id].fire.SetActive(isFire);
                }
            }

        }
        [MessageHandler((ushort)ServerToClientId.Dead, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerDead(Message message)
        {
            ushort id = message.GetUShort();
            ushort fromClient = message.GetUShort();
            bool hitHead = message.GetBool();
            bool wall = message.GetBool();
            bool isAiming = message.GetBool();
            ushort weaponIndex = message.GetUShort();
            ServerPlayer.DamageType damageType = (ServerPlayer.DamageType)message.GetUShort();
            if (list.ContainsKey(id))
            {
                list[id].playerState.MaxHealth = 100;
                if (list[id].HasPerk(Perk.Fat))
                    list[id].playerState.MaxHealth = (int)(list[id].playerState.MaxHealth * PerkManager.FatMultiplier);
                list[id].PlayerDead(fromClient, hitHead, wall, isAiming, weaponIndex, damageType);
                if (id == NetworkManager.Instance.Client.Id)
                {
                    if (_gameUi)
                    {
                        _gameUi.Hurt(0, 100);
                    }
                }
            }
        }
        [MessageHandler((ushort)ServerToClientId.Respawn, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerRespawn(Message message)
        {
            ushort id = message.GetUShort();
            if (list.TryGetValue(id, out var player))
            {
                player.PlayerRespawn(message.GetVector3());
                short[] weapons = message.GetShorts();
                int weaponIndex = message.GetInt();
                player.SetWeapon(weapons, weaponIndex, player.playerState.CosmeticIndex.weaponIndex);
            }
        }

        public bool bloodThirsty = false;
        void TakeDamage(int damage, bool headShot, Vector3 point, Vector3 normal, ushort fromClient, uint tick)
        {
            if (!hitted)
                hitted = true;

            bool fromPlayerIsLocal = fromClient == NetworkManager.Instance.Client.Id;
            int attackerId = demoPlayer.Id;
            if (list.TryGetValue(fromClient, out var attacker))
            {
                attackerId = attacker.demoPlayer.Id;
                fromPlayerIsLocal = attacker.playerState.IsLocal;
            }

            playerState.TakeDamage(damage, headShot, point, normal, !fromPlayerIsLocal, fromPlayerIsLocal);
            if (IsLocal)
            {
                DamageTracker.DamageTaken(fromClient, damage);
                HitDetection.Instance.CreateIndicator(list.TryGetValue(fromClient, out var cp)
                    ? cp.player.transform.position
                    : Vector3.zero);
            }

            DemoManager.Instance.AddPlayerTakeDamage(demoPlayer.Id, attackerId, damage, playerState.Health, headShot, point, normal);

            if (fromClient == Id || fromClient == 41) return;

            if (fromPlayerIsLocal)
            {
                if (list.TryGetValue(fromClient, out var localPlayer))
                {
                    localPlayer.DamageTracker.DamageGiven(Id, damage);
                }
                if (!_weaponManager.ShootingBuffer[tick % WeaponManager.MaxStoredSize])
                {
                    HitMarker.Instance.StartHitMarker(headShot ? Color.yellow : Color.white);
                    if (GameManager.Instance.setting.enableGore && GameManager.Instance.setting.spawnParticle)
                    {
                        ObjectPooler.Instance.SpawnFromPool("Blood", point, Quaternion.LookRotation(normal));
                    }
                }
            }
        }
        [MessageHandler((ushort)ServerToClientId.UpdateWeaponIndex, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void UpdateWeaponIndex(Message message)
        {
            ushort id = message.GetUShort();
            if (list.ContainsKey(id) && !list[id].playerState.IsInfected)
            {
                int index = message.GetInt();

                list[id].SetWeapon(index);
            }
        }

        [MessageHandler((ushort)ServerToClientId.TakeHealth, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void TakeHealth(Message message)
        {
            ushort id = message.GetUShort();
            if (list.TryGetValue(id, out var player))
            {
                int health = message.GetInt();

                player.TakeHealth(health);
            }
        }

        void TakeHealth(int health)
        {
            DemoManager.Instance.AddPlayerTakeHealth(demoPlayer.Id, health, playerState.MaxHealth);
            playerState.TakeHealth(health, playerState.MaxHealth);
            bloodThirsty = true;
            Invoke(nameof(ClearBloodThirsty), 0.5f);
        }

        void ClearBloodThirsty()
        {
            bloodThirsty = false;
        }
        [MessageHandler((ushort)ServerToClientId.Upgrade, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void UpgradeHealth(Message message)
        {
            ushort id = message.GetUShort();
            uint tick = message.GetUInt();
            bool success = message.GetBool();
            if (list.TryGetValue(id, out var player))
            {
                uint upgradeIndex = message.GetUInt();

                if (success)
                {
                    switch (upgradeIndex)
                    {
                        case 0:
                            int maxHealth = message.GetInt();
                            player.playerState.MaxHealth = maxHealth;
                            player.playerState.Health = Mathf.Clamp(list[id].playerState.Health, 0, maxHealth);
                            if (player.IsLocal)
                            {
                                if (_gameUi)
                                {
                                    _gameUi.Health();
                                    _gameUi.healthSlider.maxValue = maxHealth;
                                }
                            }
                            break;
                    }
                }
                else
                {
                    if (player.IsLocal)
                    {
                        if (UpgradeInGameMenu.Instance.UpgradeBuffer[tick % UpgradeInGameMenu.MaxStoredTick])
                        {
                            switch (upgradeIndex)
                            {
                                case 0:
                                    UpgradeInGameMenu.Instance.DownHealth();
                                    break;
                                case 1:
                                    UpgradeInGameMenu.Instance.ClearDash();
                                    break;
                                case 2:
                                    UpgradeInGameMenu.Instance.ClearDoubleJump();
                                    break;
                                case 3:
                                    UpgradeInGameMenu.Instance.DownMovementSpeed();
                                    break;
                            }
                        }

                    }
                }



            }
        }
        public AudioSource audioSource, voice;

        [MessageHandler((ushort)ServerToClientId.Shoot, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void Shoot(Message message)
        {
            ushort id = message.GetUShort();
            EShootingResult result = (EShootingResult)message.GetInt();
            int bulletCount = message.GetInt();
            Vector3 dir = message.GetVector3();
            if (list.TryGetValue(id, out var player) && !player.Dead)
            {
                if (result == EShootingResult.EResultOk)
                {
                    DemoManager.Instance.AddPlayerWeaponShoot(player.demoPlayer.Id, player.playerState.WeaponManager.CurrentWeaponIndex, dir,
                        bulletCount);
                }
                player.playerState.WeaponManager.Shoot(dir, bulletCount, result);
            }
        }

        [MessageHandler((ushort)ServerToClientId.WeaponStartReloading, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void WeaponStartReloading(Message message)
        {
            ushort id = message.GetUShort();
            ushort index = message.GetUShort();

            if (list.TryGetValue(id, out var player))
            {
                player.playerState.WeaponManager.StartReloading(index);
                DemoManager.Instance.AddPlayerWeaponReload(player.demoPlayer.Id, player.playerState.WeaponManager.CurrentWeaponIndex, player.playerState.WeaponManager.CurrentWeapon != null ? 0 : 0);
            }
        }

        [MessageHandler((ushort)ServerToClientId.WeaponReloaded, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void WeaponReloaded(Message message)
        {
            ushort id = message.GetUShort();
            uint tick = message.GetUInt();
            ushort index = message.GetUShort();

            if (list.TryGetValue(id, out var player))
            {
                if (player.IsLocal)
                {
                    while (_weaponManager.LastReloadTick.Count > 0)
                    {
                        var t = _weaponManager.LastReloadTick.Dequeue();

                        if (t.Item1 > tick)
                        {
                            // Debug.LogError($"{index} reload failed, tick: {tick}, reason: time out");
                            return;
                        }

                        if (t.Item1 == tick && t.Item2 == index)
                        {
                            //TODO: Reload success
                            // Debug.Log($"{index} reload success, tick: {tick}");

                            if (player.IsLocal)
                            {

                            }
                            else
                            {

                            }

                            return;
                        }
                    }
                    if (_weaponManager.CurrentWeapon != null) _weaponManager.CurrentWeapon.ResetDynamic();
                }
                else
                {

                }

                // Debug.LogError($"{index} reload failed, tick: {tick}");
            }

        }

        public Light Light;

        [MessageHandler((ushort)ServerToClientId.TurnLight, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void TurnLight(Message message)
        {
            ushort id = message.GetUShort();
            if (list.ContainsKey(id))
            {
                bool turnLight = message.GetBool();

                list[id].Light.enabled = turnLight;
            }
        }

        [MessageHandler((ushort)ServerToClientId.PlayerInfect, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerInfect(Message message)
        {
            ushort id = message.GetUShort();
            ushort fromClient = message.GetUShort();

            if (list.TryGetValue(id, out var player))
            {
                player.playerState.IsInfected = true;

                if (id != fromClient)
                {
                    player.Kills = 0;
                    player.Deaths++;

                    if (list.TryGetValue(fromClient, out var attacker))
                    {
                        attacker.Kills++;
                        if (Mathf.Abs(attacker.coins - attacker.lastCoin) > 1) return;
                        attacker.lastCoin = attacker.coins;
                        attacker.coins++;
                        if (attacker.IsLocal)
                            UpgradeInGameMenu.Instance.AutoUpgrade();
                        attacker.currentLifeKill++;
                        if (attacker.IsLocal)
                        {
                            foreach (var item in UpgradeInGameMenu.Instance.upgradeItems)
                            {
                                item.SetColor(attacker.coins);
                            }
                        }
                    }
                    if (_gameUi)
                    {
                        if (_gameUi.PlayerList.ContainsKey(id))
                        {
                            _gameUi.PlayerList[id].SetDeaths(player.Deaths);
                        }
                        if (_gameUi.PlayerList.ContainsKey(fromClient))
                        {
                            _gameUi.PlayerList[fromClient].SetKills(list[fromClient].Kills);
                        }
                    }
                }

                DemoManager.Instance.AddPlayerState(player.demoPlayer.Id, player.Dead, player.playerState.IsInfected, -1, 0);

                player.SetInfect();
            }
        }

        public bool hitted = false;

        [MessageHandler((ushort)ServerToClientId.SpecialWeapon, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SpecialWeapon(Message message)
        {
            ushort fromClient = message.GetUShort();
            if (list.ContainsKey(fromClient))
            {
                if (list[fromClient].specting) return;
                short weapon = message.GetShort();
                list[fromClient].SpecialWeapon(weapon);
            }
        }

        void SpecialWeapon(short w)
        {
            WeaponIndex = 3;

            playerState.WeaponManager.SetSpecialWeapon(w);
        }
        [MessageHandler((ushort)ServerToClientId.ManageToKick, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ManageToKick(Message message)
        {
            ushort kickPlayerId = message.GetUShort();

            ushort fromClient = message.GetUShort();
            VoteKicking voteKicking = new VoteKicking(kickPlayerId, fromClient);

            if (_gameUi)
            {
                _gameUi.AddVote(voteKicking);
            }
        }
        [MessageHandler((ushort)ServerToClientId.AgreeKicking, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void AgreeKicking(Message message)
        {
            if (_gameUi)
            {
                _gameUi.AgreedVote();
            }
        }

        [MessageHandler((ushort)ServerToClientId.SetTime, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SetTime(Message message)
        {
            ushort id = message.GetUShort();

            int stayTime = message.GetInt();
            if (list.TryGetValue(id, out var player))
            {
                player.SetTime(stayTime);
            }
        }

        void SetTime(int time)
        {
            StayTime = time;
            if (_gameUi != null)
            {
                if (_gameUi.PlayerList.TryGetValue(Id, out var playerListItem))
                {
                    playerListItem.SetTime(time);
                }
            }

            if (IsLocal)
            {
                AudioManager.Instance.Play("reward_2");

                _gameUi.Ticking();
                _gameUi.Invoke(nameof(_gameUi.ClearTicking), 1.2f);
            }
        }
        [MessageHandler((ushort)ServerToClientId.DisAgreeKicking, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void DisAgreeKicking(Message message)
        {
            if (_gameUi)
            {
                _gameUi.DisAgreedVote();
            }
        }
        [MessageHandler((ushort)ServerToClientId.ClearKick, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ClearKick(Message message)
        {
            if (_gameUi)
            {
                _gameUi.ClearKick();
            }
        }
        [MessageHandler((ushort)ServerToClientId.VoteToKickFinish, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void VoteToKickFinish(Message message)
        {

            ushort kickPlayerId = message.GetUShort();


            if (kickPlayerId == NetworkManager.Instance.Client.Id)
            {
                if (NetworkServerManager.Instance.Server.IsRunning)
                {
                    if (_gameUi)
                    {
                        _gameUi.ClearKick();
                    }
                    return;
                }
                if (LoadingManager.Instance.menuType == LoadingManager.MenuType.None) LoadingManager.Instance.menuType = LoadingManager.MenuType.Kick;
                LobbyManager.Instance.LeaveLobby();
                return;
            }
            if (_gameUi)
            {
                _gameUi.KickFinish();
            }
        }

        [SerializeField] public GameObject bananaObj;

        [MessageHandler((ushort)ServerToClientId.PlayerHasBanana,
            NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerHasBanana(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var player))
            {
                player.SetHasBanana(message.GetBool());
            }

        }

        void SetHasBanana(bool flag)
        {
            HasBanana = flag;

            bananaObj.SetActive(flag);
            if (flag)
            {
                Tutorial.Instance.SetText("catchthebanana_hint_0", 5f);
            }

            playerState.SetOutlineType(OutlineType.HasBanana);
            OutlineDisplay(new Color(255 / 255f, 151 / 255f, 0), Outline.Mode.OutlineAll);
        }
        [MessageHandler((ushort)ServerToClientId.ChangeCosmetic, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ChangeCosmetic(Message message)
        {
            ushort fromClient = message.GetUShort();
            CosmeticItem.Type type = (CosmeticItem.Type)message.GetUShort();
            int index = message.GetInt();
            Color color = message.GetColor();
            float shiny = message.GetFloat();
            int particle = message.GetInt();
            if (list.TryGetValue(fromClient, out var player))
            {
                player.playerState.ChangeCosmetic(type, index, color, shiny, particle);

                player.playerState.CosmeticIndex = player.playerState.CosmeticIndex;
            }

        }

        [MessageHandler((ushort)ServerToClientId.GrindCash,
            NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void GrindCash(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var player))
            {
                int cash = message.GetInt();

                player.SetCash(cash);
            }
        }

        public void SetCash(int c)
        {
            int offset = c - Cash;
            Cash = c;
            if (IsLocal)
            {
                BuyWeaponMenu.Instance.SetCash(c);

                if (offset > 0)
                {
                    JuicyScore.Instance.UpdateScore(offset, JuicyScore.ScoreType.Usd);
                }
            }

            if (_gameUi)
            {
                _gameUi.SetPlayerDotName(Id, this);
            }
        }
        [MessageHandler((ushort)ServerToClientId.DisableInvincible, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void DisableInvincible(Message message)
        {
            ushort id = message.GetUShort();
            if (list.ContainsKey(id))
            {
                if (!list[id].playerState.IsInfected)
                {
                    if (!list[id].IsLocal && list[id].playerState.GetOutlineType() == OutlineType.Invincible)
                    {
                        list[id].ClearOutline();
                    }
                    else
                    {
                        if (_gameUi)
                        {
                            _gameUi.ClearInvincible();
                        }
                    }
                }
            }
        }


        public bool HasPerk(Perk perk)
        {
            return perks.Contains(perk);
        }
        [MessageHandler((ushort)ServerToClientId.SendPerks, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ReceivePerks(Message message)
        {
            ushort fromClient = message.GetUShort();
            ushort[] p = message.GetUShorts();

            if (list.ContainsKey(fromClient))
            {
                int i = 0;
                foreach (var perk in p)
                {
                    list[fromClient].perks[i++] = (Perk)perk;
                }

                if (list[fromClient].HasPerk(Perk.Fat))
                    list[fromClient].playerState.MaxHealth = (int)(100 * PerkManager.FatMultiplier);
                if (fromClient == NetworkManager.Instance.Client.Id)
                {
                    if (_gameUi)
                    {
                        _gameUi.Health();
                        _gameUi.healthSlider.maxValue = list[fromClient].playerState.MaxHealth;
                    }
                }
            }
        }

        [MessageHandler((ushort)ServerToClientId.PlayerWeaponLevel, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PlayerWeaponLevel(Message message)
        {
            ushort fromClient = message.GetUShort();
            if (list.TryGetValue(fromClient, out var player))
            {
                int weaponLevel = message.GetInt();
                player.WeaponLevel = weaponLevel;

                if (_gameUi)
                {
                    if (player.IsLocal)
                    {
                        GunGameWeapon.Instance.UpdateWeapon(weaponLevel);
                    }

                    if (_gameUi.PlayerList.TryGetValue(fromClient, out var playerListItem))
                        playerListItem.SetWeaponLevel(weaponLevel);
                }
            }


        }
        [MessageHandler((ushort)ServerToClientId.SendLatency, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SendLatency(Message message)
        {
            if (_gameUi == null) return;
            ushort id = message.GetUShort();
            ushort rtt = message.GetUShort();
            if (_gameUi.PlayerList.ContainsKey(id))
            {
                _gameUi.PlayerList[id].latencyText.SetText(rtt.ToString());
            }
        }

        [MessageHandler((ushort)ServerToClientId.SpecterMode, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SpecterMode(Message message)
        {
            ushort fromClient = message.GetUShort();

            if (list.TryGetValue(fromClient, out var player))
            {
                bool flag = message.GetBool();

                player.Spect(flag, true);
            }
        }

        public bool specting = false;

        void Spect(bool flag, bool f)
        {
            specting = flag;

            playerState.SetCanSpectate(!flag);

            if (_gameUi.PlayerList.TryGetValue(Id, out var item))
            {
                item.gameObject.SetActive(!flag);
            }

            if (!flag && !f) return;
            player.gameObject.SetActive(!flag);

            if (flag)
            {
                Dead = true;

                playerState.WeaponManager.IsUsingSpecialWeapon();

                if (IsLocal)
                {
                    DisableLocalWeapon();

                    SpectateMovement.Instance.StartSpect();
                    SpectateMovement.Instance.ChangePerspective(EPerspective.FirstPerson);
                    Tutorial.Instance.SetText("SpectateTip");

                    if (_gameUi)
                    {
                        _gameUi.Hurt(playerState.Health, playerState.MaxHealth);
                        _gameUi.Clear();
                        _gameUi.DeleteDeath();
                    }
                }


            }
        }

        void DisableLocalWeapon()
        {
            if (IsLocal)
            {
                _weaponManager.DisableAllWeapons();
            }
            else
            {
                foreach (var weapon in playerState.weapons)
                {
                    weapon.gameObject.gameObject.SetActive(false);
                }
            }
        }

        private static GameUIManager _gameUi;
        private static WeaponManager _weaponManager;

        public static void SetWeaponManagerClass(WeaponManager manager)
        {
            _weaponManager = manager;
        }

        public static void SetGameUIManagerClass(GameUIManager manager)
        {
            _gameUi = manager;
        }

        [SerializeField] public List<FastIKFabric> iks = new();
        public int lastCoin = 0;
        public bool moved = false;

        public ushort currentLifeKill = 0;
        void PlayerDead(ushort fromClient, bool hitHead, bool wall, bool isAiming, ushort weaponIndex, ServerPlayer.DamageType damageType)
        {
            if (Dead) return;
            Dead = true;
            if (NetworkManager.ClientGameMode != GameMode.KillConfirm)
                Deaths++;

            bool attackerIsLocal = false;
            if (list.TryGetValue(fromClient, out var attacker))
                attackerIsLocal = attacker.playerState.IsLocal;
            playerState.Dead(hitHead, wall, weaponIndex, attackerIsLocal);

            SetCash(0);

            if (_gameUi.PlayerList.TryGetValue(Id, out var playerListItem))
                playerListItem.SetState(PlayerListItem.PlayerItemState.Dead);
            switch (damageType)
            {
                case ServerPlayer.DamageType.Player:
                    if (list.TryGetValue(fromClient, out var fromPlayer))
                    {
                        if (NetworkManager.ClientGameMode != GameMode.KillConfirm && fromClient != Id)
                        {
                            fromPlayer.Kills++;
                            if (Mathf.Abs(list[fromClient].coins - list[fromClient].lastCoin) > 1) return;
                            fromPlayer.lastCoin = fromPlayer.coins;
                            fromPlayer.coins++;
                            if (fromPlayer.IsLocal)
                                UpgradeInGameMenu.Instance.AutoUpgrade();
                            fromPlayer.currentLifeKill++;

                            if (_gameUi)
                            {
                                if (_gameUi.PlayerList.TryGetValue(Id, out var playerUi))
                                {
                                    playerUi.SetDeaths(Deaths);
                                }
                                if (_gameUi.PlayerList.TryGetValue(fromClient, out playerUi))
                                {
                                    playerUi.SetKills(list[fromClient].Kills);
                                }
                            }

                            if (fromPlayer.playerState.IsLocal)
                                _gameUi.spectateCanvas.SetKd(Kills, Deaths);
                        }

                        if (fromPlayer.IsLocal)
                        {
                            foreach (var item in UpgradeInGameMenu.Instance.upgradeItems)
                            {
                                item.SetColor(list[fromClient].coins);
                            }
                        }
                        if (fromPlayer.playerState.WeaponManager.CurrentWeapon && fromPlayer.playerState.WeaponManager.CurrentWeapon.Type == Firearms.WeaponType.LaserGun)
                        {
                            if (IsLocal)
                                AudioManager.Instance.Play("Pew");
                            else audioSource.PlayOneShot(PrefabManager.Instance.pewClip);
                        }

                        bool noscope = false;

                        if (weaponIndex == 2 || weaponIndex == 15)
                        {
                            noscope = !isAiming;
                        }

                        KillMessage.Instance.AddKillMessage(Chat.Instance.GetPlayerNameNetwork(fromPlayer.playerState.Username, fromPlayer.playerState.SteamId, fromPlayer.DisplayTag) + " " + fromPlayer.GetGroupName()
                            , Chat.Instance.GetPlayerNameNetwork(playerState.Username, playerState.SteamId, DisplayTag) + " " + GetGroupName(), weaponIndex, hitHead, wall, noscope);

                        DemoManager.Instance.AddPlayerState(demoPlayer.Id, Dead, playerState.IsInfected, fromPlayer.demoPlayer.Id, weaponIndex);
                    }

                    break;
                case ServerPlayer.DamageType.Enemy:
                    if (ClientEnemy.list.TryGetValue(fromClient, out var enemy))
                    {
                        KillMessage.Instance.AddKillMessage(enemy.enemyState.enemyType.ToString(), Chat.Instance.GetPlayerNameNetwork(playerState.Username, playerState.SteamId, DisplayTag), 40, false, false, false);
                        DemoManager.Instance.AddPlayerState(demoPlayer.Id, Dead, playerState.IsInfected, enemy.demoEnemy.Id, weaponIndex);
                    }
                    else
                    {
                        KillMessage.Instance.AddKillMessage("ENEMY -" + fromClient, Chat.Instance.GetPlayerNameNetwork(playerState.Username, playerState.SteamId, DisplayTag), 40, false, false, false);
                        DemoManager.Instance.AddPlayerState(demoPlayer.Id, Dead, playerState.IsInfected, -1, weaponIndex);
                    }
                    break;
                case ServerPlayer.DamageType.Void:
                    KillMessage.Instance.AddKillMessage("Void", Chat.Instance.GetPlayerNameNetwork(playerState.Username, playerState.SteamId, DisplayTag), 40, false, false, false);
                    DemoManager.Instance.AddPlayerState(demoPlayer.Id, Dead, playerState.IsInfected, 3000, 0);
                    break;
            }

            DisableLocalWeapon();
            if (!IsLocal)
            {
                player.gameObject.SetActive(false);
                if (NetworkManager.Instance.IsTeamMode(playerState))
                {
                    if (_gameUi)
                    {
                        _gameUi.DisplayPlayerDot(Id, false);
                    }
                }
                if (damageType == ServerPlayer.DamageType.Player && fromClient == NetworkManager.Instance.Client.Id)
                {
                    #region Visual Effect

                    UpgradeInGameMenu.Instance.SetCoinText(list[fromClient].coins);

                    int score = 125;
                    if (hitHead) score += 25;
                    if (wall) score += 25;
                    JuicyScore.Instance.UpdateScore(score, JuicyScore.ScoreType.Xp);

                    #endregion

                    #region Achievement

                    AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.FIRST_KILL);

                    if (bloodThirsty)
                    {
                        AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.BLOODDTHIRSTY);
                    }
                    if (_weaponManager.CurrentWeapon && _weaponManager.CurrentWeapon.gameObject.CompareTag("Sniper"))
                    {
                        AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.SNIPER_SHOT);
                    }
                    if (_weaponManager.CurrentWeapon && _weaponManager.CurrentWeapon.gameObject.CompareTag("Banana"))
                    {
                        QuestManager.Instance.GetProgress(QuestType.BananaMan);
                        AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.BANANA_KILL);
                    }

                    if (list[fromClient].playerState.grappling.IsGrappling())
                    {
                        AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.GRAPPLE_KILLER);
                    }
                    if (!NetworkManager.Instance.game.complete)
                    {
                        int[] gunKills = NetworkManager.Instance.game.gunKills;

                        if (gunKills.Length > weaponIndex)
                            gunKills[weaponIndex]++;

                        // The achievement is earned by getting a kill with every weapon
                        // of the Gun Game rotation. Iterating every slot of gunKills is
                        // wrong: that array is sized after the whole weapon list, so it
                        // also holds weapon ids that are never handed out in Gun Game
                        // and the check could therefore never succeed.
                        bool flag = true;
                        foreach (short gunGameWeapon in Mode.GunGame.WeaponIds)
                        {
                            if (gunGameWeapon < 0 || gunGameWeapon >= gunKills.Length || gunKills[gunGameWeapon] <= 0)
                            {
                                flag = false;
                                break;
                            }
                        }

                        if (flag)
                        {
                            NetworkManager.Instance.game.complete = true;
                            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.GUN_GAME);
                        }
                    }

                    if (NetworkManager.Instance.game.gunKills[5] >= 20)
                    {
                        AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.BANANA_GUN);
                    }

                    {
                        bool firstKill = true;
                        ClientPlayer localPlayer = list[fromClient];
                        foreach (var clientPlayer in list.Values)
                        {
                            if (clientPlayer != localPlayer)
                            {
                                if (clientPlayer.Kills != 0)
                                {
                                    firstKill = false;
                                }
                                break;
                            }
                        }



                        if (firstKill)
                        {
                            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.BAD_GUY);

                            if (weaponIndex == 1002)
                            {
                                AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.EXPLOSIVE_ENTRANCE);
                            }
                        }

                        if (list.ContainsKey(fromClient) && Vector3.Distance(list[fromClient].player.transform.position, player.transform.position) > 100)
                        {
                            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.SKY_EYE);
                        }

                        if (list.ContainsKey(fromClient) && list[fromClient].playerState.Health <= 10)
                        {
                            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.CANT_PLAY_UNTIL_RESIDUAL_BLOOD);
                        }

                        if (_weaponManager.CurrentWeapon &&
                            _weaponManager.CurrentWeapon.gameObject.CompareTag("AL48"))
                        {
                            //AL48_SHOT
                            AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.AL48_SHOT);
                        }
                        if (_weaponManager.CurrentWeapon &&
                            _weaponManager.CurrentWeapon.weaponType == Firearms.WeaponType.LaserGun)
                        {
                            AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.LASER_SHOT);
                            if (PlayerMovement.inWater)
                            {
                                AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.ELECTRIC_EEL);
                            }
                        }
                        if (_weaponManager.CurrentWeapon &&
                            _weaponManager.CurrentWeapon.weaponType == Firearms.WeaponType.Knife)
                        {
                            AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.KNIFE_KILL);
                            QuestManager.Instance.GetProgress(QuestType.KnifeMan);
                        }
                        if (SteamFriends.HasFriend((CSteamID)playerState.SteamId, EFriendFlags.k_EFriendFlagAll))
                        {
                            if (SteamUserStats.RequestCurrentStats())
                            {
                                AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.THIS_IS_A_BETRAY);
                            }
                        }

                        if (playerState.SteamId == 76561198983573782)
                        {
                            if (SteamUserStats.RequestCurrentStats())
                            {
                                AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.BECOME_DEV);
                            }
                        }
                    }

                    #endregion

                    #region Sound

                    bool tiedOrWin = false;
                    if (Kills > 10)
                    {
                        foreach (var player in list.Values)
                        {
                            if (player.Kills > 10 && Kills >= player.Kills)
                            {
                                tiedOrWin = true;
                                break;
                            }
                        }
                    }

                    if (currentLifeKill >= 8 && Random.Range(0, 100) <= 20)
                    {
                        //don’t kill too many my boy!
                        VoiceLine.Instance.PlayVoice(VoiceKey.kill_E_01);
                        VoiceLine.Instance.PlayVoice(VoiceKey.kill_E_02);
                    }
                    else if (currentLifeKill >= 5 && Random.Range(0, 100) <= 20)
                    {
                        VoiceLine.Instance.PlayVoice(Random.Range(0, 10) <= 5 ? VoiceKey.kill_A : VoiceKey.kill_C);
                    }
                    else if (tiedOrWin && Random.Range(0, 100) <= 10)
                    {
                        VoiceLine.Instance.PlayVoice(Random.Range(0, 2) < 1 ? VoiceKey.kill_G : VoiceKey.funny_A);
                    }
                    else if (currentLifeKill == 1 && Random.Range(0, 100) <= 5)
                    {
                        if (Random.Range(0, 10) < 5)
                        {
                            //you receive fun,I receive sjkhjky
                            VoiceLine.Instance.PlayVoice(VoiceKey.kill_F);
                        }
                        else
                        {
                            //Banana banana banana
                            VoiceLine.Instance.PlayVoice(VoiceKey.funny_E);

                        }

                    }
                    else if (currentLifeKill == 0)
                    {
                        int a = Random.Range(0, 10);
                        if (a <= 2)
                        {
                            VoiceLine.Instance.PlayVoice(VoiceKey.kill_D_01);
                            VoiceLine.Instance.PlayVoice(VoiceKey.kill_D_02);
                        }
                        else if (a >= 7)
                        {
                            VoiceLine.Instance.PlayVoice(VoiceKey.funny_C);
                        }

                    }

                    #endregion

                    #region Quest

                    bool rifle = _weaponManager.CurrentWeapon && _weaponManager.CurrentWeapon.CompareTag("AL48");
                    if (rifle)
                    {
                        QuestManager.Instance.GetProgress(QuestType.RifleMan);
                    }

                    if (list[fromClient].Kills >= 30)
                    {
                        QuestManager.Instance.GetProgress(QuestType.Killer);
                    }
                    GameManager.lastKill = list[fromClient].Kills;

                    if (weaponIndex == 1002)
                    {
                        QuestManager.Instance.GetProgress(QuestType.Grenadier);
                    }

                    #endregion

                    AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.KILLS);
                    // if (weaponManager.currentWeapon&&weaponManager.currentWeapon.weaponType == Firearms.WeaponType.Knife||Random.Range(0, 10) > 8)
                    // {
                    //     weaponManager.TacticalThrowCount++;
                    //     gameUi.throwObjCount.SetText(NetworkManager.ClientGameMode == GameMode.SpecialGameMode
                    //         ? "∞"
                    //         : weaponManager.TacticalThrowCount.ToString());
                    // }

                    PowerInGameMenu.Instance.fillProgress += 0.05f;

                }

                Crouch = false;
                col.height = ServerPlayer.NormalSize;
            }
            else
            {
                DamageTracker.DamageTaken(fromClient, playerState.Health);

                #region Input

                PlayerMovement.DeInitialize();

                playerState.grappling.StopGrapple(new InputAction.CallbackContext());

                #endregion

                #region Visual Effect

                PlayerMovement.gameObject.SetActive(false);

                if (playerState.IsInfected && InfectedHand.Instance)
                {
                    InfectedHand.Instance.clawKnife.gameObject.SetActive(false);
                }

                PowerInGameMenu.Instance.ClearSpecialWeaponForLocal();
                UpgradeInGameMenu.Instance.ClearDash();
                UpgradeInGameMenu.Instance.ClearDoubleJump();
                _gameUi.micIcon.SetActive(false);
                _gameUi.ClearSpeedUp();
                _gameUi.bulletText.SetText("");
                UpgradeInGameMenu.Instance.DownHealth();
                UpgradeInGameMenu.Instance.DownMovementSpeed();
                _gameUi.healthSlider.maxValue = playerState.MaxHealth;

                switch (damageType)
                {
                    case ServerPlayer.DamageType.Player:
                        if (list.ContainsKey(fromClient))
                        {
                            Texture2D texture2D = weaponIndex < NetworkManager.Instance.weaponInfo.Count ? NetworkManager.Instance.weaponInfo[weaponIndex].texture : null;
                            if (weaponIndex == 1003)
                            {
                                texture2D = PrefabManager.Instance.throwObjTexture[3];
                            }
                            else if (weaponIndex == 1000)
                            {
                                texture2D = PrefabManager.Instance.throwObjTexture[1];
                            }
                            else if (weaponIndex == 1002)
                            {
                                texture2D = PrefabManager.Instance.throwObjTexture[0];
                            }
                            else if (weaponIndex == 1004)
                            {
                                texture2D = PrefabManager.Instance.throwObjTexture[4];
                            }
                            _gameUi.SetDeath(fromClient, texture2D);
                            DeadCamera.GetInstance().Dead(list[fromClient].head.position, Vector3.zero, list[fromClient].head);
                            if (fromClient != Id)
                            {
                                list[fromClient].OutlineDisplay(Color.red, 5f);
                            }
                        }
                        break;
                    case ServerPlayer.DamageType.Enemy:
                        if (ClientEnemy.list.TryGetValue(fromClient, out var enemy))
                        {
                            _gameUi.SetDeath(fromClient, enemy.enemyState.enemyType.ToString(), PrefabManager.Instance.jackTexture, NetworkManager.Instance.GetWeaponTexture("Vector"));
                            var transform1 = enemy.transform;
                            DeadCamera.GetInstance().Dead(transform1.position, enemy.headOffset, transform1);
                        }
                        else
                        {
                            _gameUi.SetDeath(fromClient, "Jack", PrefabManager.Instance.jackTexture, NetworkManager.Instance.GetWeaponTexture("Vector"));
                        }
                        break;
                    case ServerPlayer.DamageType.Void:
                        _gameUi.SetDeath(fromClient, null);
                        DeadCamera.GetInstance().Dead(Vector3.zero, Vector3.zero);
                        break;
                }

                #endregion

                #region Achievement

                AchievementManager.Instance.SetStatsPlusOne(AchievementManager.EStats.DEATHS);

                #endregion

                #region Quest

                QuestManager.Instance.GetProgress(QuestType.Noob);

                #endregion

                #region Other

                GameManager.lastDie = Deaths;

                #endregion

                #region Sound

                AudioManager.Instance.Play("Died");

                if (UpgradeInGameMenu.Instance.buyUpgradeRecently)
                {
                    //death_B
                    VoiceLine.Instance.PlayVoice(VoiceKey.death_B_01);
                    VoiceLine.Instance.PlayVoice(VoiceKey.death_B_02);
                }

                else if (GameManager.hitRecently)
                {
                    if (Random.Range(0, 10) <= 5)
                    {
                        //So close,you must be angry
                        VoiceLine.Instance.PlayVoice(VoiceKey.death_C);
                    }
                    else
                    {
                        //yes fight each other
                        VoiceLine.Instance.PlayVoice(VoiceKey.death_F);
                    }

                }
                else if (currentLifeKill == 0 && Random.Range(0, 100) <= 20)
                {
                    //no kill only death
                    VoiceLine.Instance.PlayVoice(VoiceKey.death_G);
                }
                else if (GameManager.Instance.knewDead && Random.Range(0, 100) <= 20)
                {
                    //haha I knew you will die
                    VoiceLine.Instance.PlayVoice(VoiceKey.death_E);
                }
                // else if (Random.Range(0, 100) < 3)
                // {
                //     //This is the democracy aftermath
                //     VoiceLine.Instance.PlayVoice(VoiceKey.death_D);
                // }
                else if (Random.Range(0, 10) >= 7)
                {
                    //Death is your destination after all
                    //but except me
                    VoiceLine.Instance.PlayVoice(VoiceKey.death_A_01);
                    VoiceLine.Instance.PlayVoice(VoiceKey.death_A_02);
                }

                #endregion

                if (list.TryGetValue(fromClient, out var from))
                    GameManager.Instance.ClientLocalPlayerDead?.Invoke(this, from);

                foreach (var otherPlayer in list.Values)
                {
                    otherPlayer.DamageTracker.Clear();
                }
            }

            playerState.SpawnRagdoll(hitHead && list.ContainsKey(fromClient) && list[fromClient].currentLifeKill >= 3);

            playerState.Health = 0;
            currentLifeKill = 0;

            if (playerState.IsLocal)
                _gameUi.spectateCanvas.SetKd(Kills, Deaths);
        }

        void OutlineDisplay(Color color, float duration)
        {
            playerState.OutlineDisplay(color, duration);
        }
        void OutlineDisplay(Color color, Outline.Mode mode = Outline.Mode.OutlineAll)
        {
            playerState.OutlineDisplay(color, mode);
        }
        public void ClearOutline()
        {
            playerState.ClearOutline();
        }
        public int coins;

        void PlayerRespawn(Vector3 spawnPos)
        {
            DemoManager.Instance.AddPlayerState(demoPlayer.Id, false, playerState.IsInfected, -1, 0);
            if (IsLocal)
            {
                foreach (var c in list.Values)
                {
                    if (!c.IsLocal && c.playerState.GetOutlineType() == OutlineType.Death)
                        c.ClearOutline();
                }
                DeadCamera.GetInstance().Respawn();

                UpgradeInGameMenu.Instance.AutoUpgrade();
                foreach (var item in UpgradeInGameMenu.Instance.upgradeItems)
                {
                    item.SetColor(coins);
                }
            }

            if (_gameUi != null)
            {
                if (NetworkManager.Instance.IsTeamMode(playerState))
                {
                    _gameUi.DisplayPlayerDot(Id, true);
                }
                if (_gameUi.PlayerList.TryGetValue(Id, out var item))
                    item.SetState(PlayerListItem.PlayerItemState.Alive);
            }

            if (!playerState.IsInfected)
            {
                if (!IsLocal)
                {
                    playerState.SetOutlineType(OutlineType.Invincible);
                    OutlineDisplay(Color.yellow, Outline.Mode.OutlineVisible);
                }
                else
                {
                    _gameUi.Invincible();
                }
            }

            player.velocity = Vector3.zero;
            player.transform.position = spawnPos;
            desiredPos = spawnPos;
            if (interpolator) interpolator.NewUpdate(NetworkManager.Instance.ServerTick, true, desiredPos);

            Dead = false;
            if (specting)
            {
                if (_gameUi.PlayerList.TryGetValue(Id, out var item))
                {
                    item.gameObject.SetActive(true);
                }
                if (IsLocal)
                    SpectateMovement.Instance.StopSpect(true);
            }
            specting = false;
            playerState.Health = playerState.MaxHealth;

            playerState.Respawn();
            if (IsLocal)
            {
                if (_gameUi)
                {
                    _gameUi.DeleteDeath();
                }

                _weaponManager.throwableManager.TacticalThrowCount = 1;

                if (NetworkManager.ClientGameMode == GameMode.SpecialGameMode)
                    _gameUi.throwObjCount.SetText("∞");
                else _gameUi.throwObjCount.SetText(_weaponManager.throwableManager.TacticalThrowCount.ToString());
            }
            if (!IsLocal)
            {
                _playerAnimation.Reset();
                head.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            player.gameObject.SetActive(true);
            SetInfect();
        }

        void SetWeapon(int weaponIndex)
        {
            DemoManager.Instance.AddPlayerWeaponSwitched(demoPlayer.Id, weaponIndex);

            playerState.WeaponManager.SwitchWeapon(weaponIndex);
        }


        void SetWeapon(short[] weaponNames, int weaponIndex, ushort[] weaponIndexs, bool spawn = false)
        {
            if (playerState.IsInfected) return;

            if (IsLocal)
            {
                GameManager.Instance.LocalPlayerSetWeapon?.Invoke(this);
            }

            if (!spawn)
            {
                DemoManager.Instance.AddPlayerWeaponUpdated(demoPlayer.Id, weaponIndex, weaponNames, weaponIndexs);
            }

            playerState.WeaponManager.UpdateWeapons(weaponNames, weaponIndex, weaponIndexs);
        }
        void PickupWeapon(string weaponNames, int weaponIndex, ushort[] weaponIndexs)
        {
            // if (IsLocal)
            // {
            //     if (_weaponManager.currentWeapon)
            //     {
            //         _weaponManager.currentWeapon.DeSelect();
            //     }
            //     _weaponManager.currentWeapon = null;
            //
            //     _gameUi.weaponUis[weaponIndex].NotDisplay();
            // }
            //
            // var i = -1;
            // foreach (var firearm in weapons)
            // {
            //     ++i;
            //     if (firearm.skins.Count > 1)
            //     {
            //         for (int j = 0; j < firearm.skins.Count; j++)
            //         {
            //             firearm.skins[j].SetActive(false);
            //         }
            //     }
            //
            //     if (i < weaponIndexs.Length)
            //     {
            //         int a = weaponIndexs[i] >= firearm.skins.Count ? 0 : weaponIndexs[i];
            //         firearm.skins[a].SetActive(true);
            //     }
            //
            //     if (firearm.name == weaponNames)
            //     {
            //         if (IsLocal)
            //         {
            //             Firearms f = firearm.gameObject.GetComponent<Firearms>();
            //             switch (weaponIndex)
            //             {
            //                 case 0:
            //                     _weaponManager.mainWeapon = f;
            //                     break;
            //                 case 1:
            //                     _weaponManager.secondaryWeapon = f;
            //                     break;
            //                 case 2:
            //                     _weaponManager.thirdWeapon = f;
            //                     break;
            //             }
            //             _gameUi.weaponUis[weaponIndex].SetText(UIManager.IsItChinese()? NetworkManager.Instance.GetWeaponChineseName(firearm.name) : firearm.name);
            //             _gameUi.weaponUis[weaponIndex].SetTexture(NetworkManager.Instance.GetWeaponTexture(firearm.name));
            //             _gameUi.weaponUis[weaponIndex].DeSelect();
            //             _gameUi.weaponUis[weaponIndex].Display();
            //         }
            //     
            //         weapon[weaponIndex] = firearm;
            //         break;
            //     }
            // }
            //
            // if (weaponIndex > 2) weaponIndex = 0;
            // currentWeapon = weapon[weaponIndex];
            //
            // if (currentWeapon != null  && currentWeapon.gameObject!=null)
            // {
            //     currentWeapon.gameObject.SetActive(true);
            //     if (IsLocal)
            //     {
            //         _weaponManager.CurrentWeaponIndex = weaponIndex;
            //         _gameUi.weaponUis[weaponIndex].Select();
            //     }
            //     else
            //     {
            //         leftHandTarget.parent = currentWeapon.leftHand;
            //         rightHandTarget.parent = currentWeapon.rightHand;
            //         foreach (var ik in iks)
            //         {
            //             ik.enabled=true;
            //         }
            //     }
            // }
        }
        private void FixedUpdate()
        {
            if (!IsLocal && !Dead)
            {
                FootSteps();
            }
        }

        private bool setNameCorrectly = false;

        [SerializeField] private Interpolator interpolator;
        private void Update()
        {
            if (!IsLocal && !Dead)
            {
                player.transform.rotation = Quaternion.Lerp(player.transform.rotation, desiredRot, Time.deltaTime * 15f);

                _playerAnimation.Update();

                // var position = head.position;
                nameTrans.position = head.position + Vector3.up * .5f;
                // aimTarget.position = Vector3.Lerp(aimTarget.position, head.forward * 5f+position+Vector3.down*0.5f, Time.deltaTime * 15f);

                Vector3 seePos = Vector3.zero;

                float dis = float.MaxValue;
                foreach (var player in list.Values)
                {
                    if (this != player)
                    {
                        float d = Vector3.Distance(player.head.position, this.head.position);
                        if (d < dis)
                        {
                            dis = d;
                            seePos = player.head.position;
                        }
                    }

                }
                if (seePos == Vector3.zero || dis > 15f || Vector3.Dot(player.transform.forward, seePos - player.transform.position) < 0f)
                {
                    for (int i = 0; i < eyes.Count; i++)
                    {
                        eyes[i].rotation = Quaternion.identity;
                    }
                }
                else
                {
                    for (int i = 0; i < eyes.Count; i++)
                    {
                        eyes[i].rotation = Quaternion.LookRotation(seePos - eyes[i].position);
                    }
                }

                playerState.Grounded = Grounded;
            }
        }

        private void LateUpdate()
        {
            if (!IsLocal && !Dead)
            {
                _playerAnimation.LateUpdate();
            }
        }

        public void SendMovement(float xRot)
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ClientToServerId.PlayerMovement);
            message.Add(PlayerMovement.transform.position);
            message.Add(PlayerMovement.desiredX);
            message.Add(player.velocity);
            message.Add(xRot);
            message.Add(PlayerMovement.IsGrounded());
            NetworkManager.Instance.Client.Send(message);
        }

        private bool Grounded { get; set; }
        private bool Crouch { get; set; }
        // public void SendInput(Vector2 vel,float xRot)
        // {
        //     Message message= Message.Create(MessageSendMode.Unreliable,(ushort)ClientToServerId.PlayerInput);
        //     NetworkManager.Instance.SendByte += message.WrittenLength;
        //     NetworkManager.Instance.Client.Send(message);
        // }

        [SerializeField] public CapsuleCollider col;
        public static readonly int Speaking = Animator.StringToHash("Speaking");

        public void SendStartCrouch()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ClientToServerId.PlayerStartCrouch);
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);
        }

        public void SendStopCrouch()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ClientToServerId.PlayerStopCrouch);
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);
        }

        public Transform nameTrans;
        private float distance;
        private void FootSteps()
        {
            if (!Crouch && Grounded)
            {
                float num = 1.2f;
                float num2 = player.velocity.magnitude;
                if (num2 > 20f)
                {
                    num2 = 20f;
                }
                distance += num2;
                if (distance > 300f / num)
                {
                    int range = Random.Range(0, PrefabManager.Instance.walksSound.Length - 1);
                    audioSource.PlayOneShot(PrefabManager.Instance.walksSound[range]);
                    if (Physics.Raycast(player.position, Vector3.down, out var hit, 1000, GameManager.Instance.whatIsGround))
                    {
                        if (GameManager.Instance.setting.spawnParticle)
                        {
                            ObjectPooler.Instance.SpawnFromPool("PlayerWalkSmokeFx", hit.point,
                                Quaternion.Euler(-90, 0, 0));
                        }
                    }


                    distance = 0f;
                    if (!moved) moved = true;
                }
            }
        }
        public List<Transform> bones = new List<Transform>();

        public GameObject nameCanvas;
        private static readonly int Infecting = Animator.StringToHash("Infecting");
    }
}
