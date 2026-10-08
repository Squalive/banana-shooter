
using System.Collections;
using System.Collections.Generic;
using Audio;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Manager;
using MapEditor;
using Menu;
using Multiplayer;
using Multiplayer.Entity.Server;
using Multiplayer.Interface;
using Multiplayer.Migration;
using Multiplayer.Server;
using Riptide;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mode
{
    public partial class KingOfTheHill : GameModes
    {
        public static KingOfTheHill Instance;

        public List<Transform> hills = new List<Transform>();
        private int index = -1;
        public int serverIndex = -1;
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.KingOfTheHill)
            {
                enabled = false;
                return;
            }
            hills = MapBound.Instance.hills;

            Instance = this;
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap != null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.KingOfTheHill.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();

            serverPlayer = GameManager.Instance.serverPlayer;

            gameStart += SetNewHillServer;
        }


        void SetNewHillServer()
        {
            if (!NetworkServerManager.Instance.Server.IsRunning) return;
            ++serverIndex;
            if (serverIndex >= hills.Count) serverIndex = 0;
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.SetHill);
            message.Add(serverIndex);
            NetworkServerManager.Instance.Server.SendToAll(message);
            _hillTime = 10;
            this.Schedule(nameof(HillCountdown), 20f);
        }

        private ushort _hillTime;

        void HillCountdown()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.Message);

            message.Add((ushort)MessageType.KingOfTheHill);
            message.Add(_hillTime);

            NetworkServerManager.Instance.Server.SendToAll(message);

            if (--_hillTime > 0)
                this.Schedule(nameof(HillCountdown), 1f);
            else
                this.Schedule(nameof(SetNewHillServer), 1f);
        }

        [MessageHandler((ushort)ServerToClientId.SetHill, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SetHillClient(Message message)
        {
            int index = message.GetInt();

            Instance.SetNewHillClient(index);
        }

        public void SetNewHillClient(int idx)
        {
            if (index != -1)
            {

                AudioManager.Instance.Play("hillchange");
                hills[index].gameObject.SetActive(false);
            }

            hills[idx].gameObject.SetActive(true);
            index = idx;

            GameStart.Instance.desiredPos = Instance.hills[idx].position;

            // AudioManager.Instance.SoundEffect3D("forcefield",Instance.hills[idx].position);

        }
        private LayerMask serverPlayer;

        private Collider[] col = new Collider[40];
        private readonly HashSet<ServerPlayer> _onHill = new();
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!NetworkServerManager.Instance.Server.IsRunning) return;
            if (started)
            {
                if (serverIndex > -1)
                {
                    Vector3 pos = hills[serverIndex].position;
                    int count = Physics.OverlapSphereNonAlloc(pos, 12, col, serverPlayer);
                    _onHill.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        ServerPlayer player = col[i].transform.root.GetComponent<ServerPlayer>();
                        if (player != null && !player.Dead && _onHill.Add(player))
                            player.AddTime();
                    }
                }

            }
        }
    }
}
