using System.Collections.Generic;
using Audio;
using Manager;
using Menu;
using Multiplayer.Entity.Client;
using Multiplayer.Server;
using Riptide;
using UnityEngine;

namespace Multiplayer.Client
{
    public class ClientCollectable : MonoBehaviour
    {
        public static Dictionary<ushort, ClientCollectable> list = new Dictionary<ushort, ClientCollectable>();

        public ushort Id { get; private set; }
        public ServerCollectable.CollectableType Type { get; private set; } = ServerCollectable.CollectableType.None;
    
        public ushort deadPlayerId,killPlayerId;

        public Outline outline;
        void InitializeKillConfirm(ushort id,ushort dead,ushort kill)
        {
            Id = id;

            deadPlayerId = dead;
            killPlayerId = kill;

            Type = ServerCollectable.CollectableType.KillConfirm;

            ushort myId = NetworkManager.Instance.Client.Id;
            if (deadPlayerId == myId)
            {
                outline.OutlineColor = Color.cyan;
            }
            else if (killPlayerId == myId)
            {
                outline.OutlineColor = Color.red;
            }
        
            Invoke(nameof(OutlineOpen),0.2f);

            if (list.TryGetValue(id, out var confirm))
            {
                Destroy(confirm.gameObject);
                list.Remove(id);
            }
            list.Add(id,this);
        }

        void OutlineOpen()
        {
            outline.enabled = true;
        }

        [MessageHandler((ushort) ServerToClientId.CollectableInit, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void KillConfirmInit(Message message)
        {
            ushort id = message.GetUShort();
            ServerCollectable.CollectableType type = (ServerCollectable.CollectableType) message.GetUShort();
            Vector3 pos = message.GetVector3();

            switch (type)
            {
                case ServerCollectable.CollectableType.KillConfirm:
                    
                    ushort dead = message.GetUShort();
                    ushort kill = message.GetUShort();

                    ClientCollectable item =
                        Instantiate(PrefabManager.Instance.GetPrefab("ClientKillConfirmItem"), pos, Quaternion.identity)
                            .GetComponent<ClientCollectable>();
                    item.InitializeKillConfirm(id,dead,kill);
                    break;
            }
        }

        [MessageHandler((ushort) ServerToClientId.GetConfirm, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void GetConfirm(Message message)
        {
            ushort id = message.GetUShort();

            ushort playerId = message.GetUShort();

            if (list.TryGetValue(id,out var killConfirm))
            {
                if (playerId == 41)
                {
                    Destroy(killConfirm.gameObject);
                    return;
                }
                killConfirm.GetConfirm(playerId);
            }
        }

        void GetConfirm(ushort playerId)
        {
            if (playerId == deadPlayerId)
            {
                if (playerId == NetworkManager.Instance.Client.Id)
                {
                    JuicyScore.Instance.UpdateScore(25,JuicyScore.ScoreType.Xp);
                    AudioManager.Instance.Play("ShootingTargetSuccess");
                }
            
                Instantiate(PrefabManager.Instance.GetPrefab("FX_Fireworks_Blue_Small"), transform.position,
                    Quaternion.identity);
            }
            else if (playerId == killPlayerId)
            {
                ClientPlayer.list[killPlayerId].Kills++;
                ClientPlayer.list[deadPlayerId].Deaths++;
                if (Mathf.Abs(ClientPlayer.list[killPlayerId].coins - ClientPlayer.list[killPlayerId].lastCoin) > 1) return;
                ClientPlayer.list[killPlayerId].lastCoin = ClientPlayer.list[killPlayerId].coins;
                ClientPlayer.list[killPlayerId].coins++;
                if(ClientPlayer.list[killPlayerId].IsLocal)
                    UpgradeInGameMenu.Instance.AutoUpgrade();
                ClientPlayer.list[killPlayerId].currentLifeKill++;
                
                if (GameUIManager.Instance)
                {
                    if (GameUIManager.Instance.PlayerList.ContainsKey(deadPlayerId))
                    {
                        GameUIManager.Instance.PlayerList[deadPlayerId].SetDeaths(ClientPlayer.list[deadPlayerId].Deaths);
                    }
                    if (GameUIManager.Instance.PlayerList.ContainsKey(killPlayerId))
                    {
                        GameUIManager.Instance.PlayerList[killPlayerId].SetKills(ClientPlayer.list[killPlayerId].Kills);
                    }
                }

                if (playerId == NetworkManager.Instance.Client.Id)
                {
                    JuicyScore.Instance.UpdateScore(25,JuicyScore.ScoreType.Xp);
                    AudioManager.Instance.Play("ShootingTargetSuccess");
                }
                Instantiate(PrefabManager.Instance.GetPrefab("FX_Fireworks_Red_Small"), transform.position,
                    Quaternion.identity);
            
                PowerInGameMenu.Instance.fillProgress += 0.1f;
            }
        
            Destroy(gameObject);
        }
        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
                list.Remove(Id);
            Instantiate(PrefabManager.Instance.GetPrefab("robotHIt2"), transform.position, Quaternion.identity);
        }

        private void Update()
        {
            float z = Mathf.PingPong(Time.time, 1f);
            Vector3 axis = new Vector3(0, z, 0);
            transform.Rotate(axis,1f);
        }
    }
}
