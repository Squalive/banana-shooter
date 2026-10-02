using System.Collections.Generic;
using Manager;
using Multiplayer.Entity.Server;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;

namespace Multiplayer.Server
{
    public class ServerCollectable : MonoBehaviour
    {
        public static Dictionary<ushort, ServerCollectable> list = new Dictionary<ushort, ServerCollectable>();

        public ushort Id { get; private set; }

        public CollectableType Type { get; private set; } = CollectableType.None;
        public enum CollectableType
        {
            None,
            KillConfirm,
            
        }

        public ushort deadPlayerId,killPlayerId;


        public static ushort nextId = 0;
        public void InitializeKillConfirm(ushort _deadPlayerId,ushort _killPlayerId)
        {
            Id = nextId++;

            deadPlayerId = _deadPlayerId;
            killPlayerId = _killPlayerId;

            Type = CollectableType.KillConfirm;
        
            list.Add(Id,this);
            
            Init();

        }
        
        void Init(){
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.CollectableInit);

            message.Add(Id);
            message.Add((ushort)Type);
            message.Add(transform.position);
            switch (Type)
            {
                case CollectableType.KillConfirm:
                    message.Add(deadPlayerId);
                    message.Add(killPlayerId);
                    break;
            }
        
            NetworkServerManager.Instance.Server.SendToAll(message);
            Destroy(gameObject,20f);
        }
    
        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
                list.Remove(Id);
        
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.GetConfirm);

            message.Add(Id);
            message.Add(41);
                    
            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        public static void RemoveItem(ushort playerId)
        {
            foreach (var killConfirm in list.Values)
            {
                if (killConfirm.deadPlayerId == playerId || killConfirm.killPlayerId == playerId)
                {
                    Destroy(list[killConfirm.Id].gameObject);
                }
            }
        }

        private void FixedUpdate()
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, 2f, GameManager.Instance.serverPlayer);

            foreach (var c in cols)
            {
                ServerPlayer player = c.transform.root.GetComponent<ServerPlayer>();

                if (player != null && !player.Dead)
                {
                    bool flag = false;
                    if (player.Id == killPlayerId)
                    {
                    
                        ServerPlayer.list[killPlayerId].Kills++;
                        ServerPlayer.list[killPlayerId].CurrentLifeKill++;
                        ServerPlayer.list[killPlayerId].Coin++;
                    
                        ServerPlayer.list[deadPlayerId].Deaths++;
                        if(LobbyDataManager.datas.ContainsKey(ServerPlayer.list[killPlayerId].SteamId))
                            LobbyDataManager.datas[ServerPlayer.list[killPlayerId].SteamId].UpdateKill();
                        if(LobbyDataManager.datas.ContainsKey(ServerPlayer.list[deadPlayerId].SteamId))
                            LobbyDataManager.datas[ServerPlayer.list[deadPlayerId].SteamId].UpdateDeaths();
                        Destroy(gameObject);

                        flag = true;
                    }
                    else if(player.Id == deadPlayerId)
                    {
                    
                        Destroy(gameObject);

                        flag = true;
                    }

                    if (flag)
                    {
                        Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.GetConfirm);

                        message.Add(Id);
                        message.Add(player.Id);
                    
                        NetworkServerManager.Instance.Server.SendToAll(message);
                    }
                
                }
            }
        }
    }
}
