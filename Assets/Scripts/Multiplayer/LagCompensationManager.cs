
using System;
using System.Collections.Generic;
using Multiplayer.Entity.Interface;
using Multiplayer.Entity.Server;
using Riptide;
using UnityEngine;
using Weapon;

namespace Multiplayer
{
    public class LagCompensationManager : MonoBehaviour
    {
        public static LagCompensationManager Instance;
        
        private Queue<ShootLagCompensationData> _shootQueue = new();

        private void Awake()
        {
            Instance = this;
        }

        private void FixedUpdate()
        {
            if (_shootQueue.Count > 0)
            {
                ShootLagCompensationData data = _shootQueue.Dequeue();
                EShootingResult result = data.IsInfected ? EShootingResult.EResultOk : data.Weapon.DoAttack();
                
                // Debug.Log("Client: " + lookDir);
                // Debug.Log("Server: " + ((ServerPlayer)player).head.forward);

                if (result == EShootingResult.EResultOk)
                {
                    if(NetworkServerManager.Instance.Server.TryGetClient(data.PlayerServer.Id,out _))
                    {
                        // Debug.Log($"Before: {tick}");
                        // ushort tickLatency = (ushort)Math.Ceiling(connection.SmoothRTT / 30f);
                        // tick = (ushort)(NetworkServerManager.Instance.CurrentTick + tickLatency + 2);
                        // Debug.Log($"After: {tick}");
                        NetworkServerManager.Iinstance.ShootLagCompensation(data.Tick, data.LookDirection,
                            data.RaycastPos, data.PlayerServer.Id, data.Weapon);
                    }
                }

                Message msg = Message.Create(MessageSendMode.Unreliable,(ushort)ServerToClientId.Shoot);
                msg.Add(data.PlayerServer.Id);
                msg.AddInt((int)result);
                msg.Add(data.Weapon.Stat.bulletAmount);
                msg.Add(data.LookDirection);
                NetworkServerManager.Instance.Server.SendToAll(msg);
            }
        }
        
        [MessageHandler((ushort)ClientToServerId.Shoot, NetworkServerManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void Shoot(ushort fromClient, Message message)
        {
            if (ServerPlayer.list.TryGetValue(fromClient,out var player))
            {
                player.DisableInvincible();
                
                uint tick = message.GetUInt();

                Vector3 lookDir = message.GetVector3();
                Vector3 raycastPos = message.GetVector3();

                var weapon = player.GetCurrentWeapon();

                ShootLagCompensationData compensationData =
                    new ShootLagCompensationData(player, tick, weapon, player.IsInfected, lookDir, raycastPos);

                Instance._shootQueue.Enqueue(compensationData);
            }
        }

    }

    public class ShootLagCompensationData
    {
        public IPlayerServer PlayerServer;

        public uint Tick;

        public ActiveWeapon Weapon;

        public bool IsInfected;

        public Vector3 LookDirection, RaycastPos;

        public ShootLagCompensationData(IPlayerServer playerServer, uint tick, ActiveWeapon weapon, bool isInfected, Vector3 lookDirection,Vector3 raycastPos)
        {
            PlayerServer = playerServer;
            Tick = tick;
            Weapon = weapon;
            IsInfected = isInfected;
            RaycastPos = raycastPos;
            LookDirection = lookDirection;
        }
    }
}