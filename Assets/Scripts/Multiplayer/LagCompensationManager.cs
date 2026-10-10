
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

        private const int MaxPendingShots = 1024;
        private const int MaxShotsPerPhysicsStep = 128;

        // 1 s at the 50 Hz fixed timestep: covers interpolation delay plus high ping.
        private const uint MaxRewindTicks = 50;

        // Camera sits 1.17 m above the player root (head y 0.78 * parent scale 1.5).
        private static readonly Vector3 HeadOffset = new(0, 1.17f, 0);

        // Slack for crouch, camera bob and the latest movement packet lagging the shot.
        private const float MaxShotOriginError = 3f;

        private Queue<ShootLagCompensationData> _shootQueue = new();

        private void Awake()
        {
            Instance = this;
        }

        private void FixedUpdate()
        {
            int shotsToProcess = Math.Min(_shootQueue.Count, MaxShotsPerPhysicsStep);
            for (int i = 0; i < shotsToProcess; i++)
            {
                ShootLagCompensationData data = _shootQueue.Dequeue();
                if (!ServerPlayer.list.TryGetValue(data.PlayerServer.Id, out var player) ||
                    !ReferenceEquals(player, data.PlayerServer) ||
                    !NetworkServerManager.Instance.Server.TryGetClient(data.PlayerServer.Id, out _) ||
                    data.Weapon?.Stat == null ||
                    !ReferenceEquals(player.GetCurrentWeapon(), data.Weapon))
                    continue;

                EShootingResult result = data.IsInfected ? EShootingResult.EResultOk : data.Weapon.DoAttack();

                // Debug.Log("Client: " + lookDir);
                // Debug.Log("Server: " + ((ServerPlayer)player).head.forward);

                if (result == EShootingResult.EResultOk)
                {
                    if (NetworkServerManager.Instance.Server.TryGetClient(data.PlayerServer.Id, out _))
                    {
                        // Debug.Log($"Before: {tick}");
                        // ushort tickLatency = (ushort)Math.Ceiling(connection.SmoothRTT / 30f);
                        // tick = (ushort)(NetworkServerManager.Instance.CurrentTick + tickLatency + 2);
                        // Debug.Log($"After: {tick}");
                        NetworkServerManager.Iinstance.ShootLagCompensation(data.Tick, data.LookDirection,
                            data.RaycastPos, data.PlayerServer.Id, data.Weapon);
                    }
                }

                Message msg = Message.Create(MessageSendMode.Unreliable, (ushort)ServerToClientId.Shoot);
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
            if (Instance == null || Instance._shootQueue.Count >= MaxPendingShots) return;

            if (ServerPlayer.list.TryGetValue(fromClient, out var player))
            {
                player.DisableInvincible();

                uint tick = message.GetUInt();

                Vector3 lookDir = message.GetVector3();
                Vector3 raycastPos = message.GetVector3();

                uint currentTick = NetworkServerManager.Instance.CurrentTick;
                uint oldestTick = currentTick > MaxRewindTicks ? currentTick - MaxRewindTicks : 0;
                tick = Math.Clamp(tick, oldestTick, currentTick);

                Vector3 serverHeadPos = player.PlayerTransform.position + HeadOffset;
                // Negated so NaN/Infinity origins also fail the check.
                if (!((raycastPos - serverHeadPos).sqrMagnitude <= MaxShotOriginError * MaxShotOriginError))
                    raycastPos = serverHeadPos;

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

        public ShootLagCompensationData(IPlayerServer playerServer, uint tick, ActiveWeapon weapon, bool isInfected, Vector3 lookDirection, Vector3 raycastPos)
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
