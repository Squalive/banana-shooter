using System;
using System.Linq;
using Mode;
using Multiplayer.Entity.Server;
using Multiplayer.Entity.Server.Enemy;
using Multiplayer.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Multiplayer.Migration
{
    public readonly struct RestoreInfo
    {
        public readonly ushort MaxClients;
        public readonly ushort HostClientId;

        public RestoreInfo(ushort maxClients, ushort hostClientId)
        {
            MaxClients = maxClients;
            HostClientId = hostClientId;
        }
    }

    public static class ServerSnapshot
    {
        private const byte Version = 1;

        public static byte[] Capture()
        {
            using var w = new SnapshotWriter();
            w.Byte(Version);
            w.UShort(NetworkManager.Instance.Client.Id);
            w.String(SceneManager.GetActiveScene().name);
            Section(w, NetworkServerManager.Instance.WriteCore);
            Section(w, ServerPlayer.WriteAll);
            Section(w, s =>
            {
                var game = NetworkServerManager.Instance.game;
                s.Bool(game != null);
                if (game != null) game.WriteSnapshot(s);
                s.Bool(Endless.Instance != null);
                if (Endless.Instance != null) Endless.Instance.WriteSnapshot(s);
            });
            Section(w, ServerObject.WriteAll);
            Section(w, ObjectSpawner.WriteAll);
            Section(w, ServerCollectable.WriteAll);
            Section(w, ServerGrenade.WriteAll);
            Section(w, ServerFire.WriteAll);
            Section(w, ServerJumpPad.WriteAll);
            Section(w, ServerEnemy.WriteAll);
            return w.ToArray();
        }

        public static RestoreInfo Restore(byte[] data)
        {
            var r = new SnapshotReader(data);
            byte version = r.Byte();
            if (version != Version)
                throw new InvalidOperationException($"snapshot version {version}, expected {Version}");

            ushort hostClientId = r.UShort();
            string scene = r.String();
            ClearServerState();
            ushort maxClients = 0;
            Read(r, s => maxClients = NetworkServerManager.Instance.ReadCore(s));

            if (scene != SceneManager.GetActiveScene().name)
            {
                Debug.LogWarning($"[Migration] host was in {scene}, here is {SceneManager.GetActiveScene().name}, only core state restored");
                NetworkServerManager.Instance.RebuildEntityIds();
                return new RestoreInfo(maxClients, hostClientId);
            }

            Read(r, ServerPlayer.ReadAll);
            Read(r, s =>
            {
                var game = NetworkServerManager.Instance.game;
                if (s.Bool())
                {
                    if (game != null) game.ReadSnapshot(s);
                    else Debug.LogWarning("[Migration] host had a game mode, this client doesnt");
                }
                if (s.Bool() && Endless.Instance != null)
                    Endless.Instance.ReadSnapshot(s);
            });
            Read(r, ServerObject.ReadAll);
            Read(r, ObjectSpawner.ReadAll);
            Read(r, ServerCollectable.ReadAll);
            Read(r, ServerGrenade.ReadAll);
            Read(r, ServerFire.ReadAll);
            Read(r, ServerJumpPad.ReadAll);
            Read(r, ServerEnemy.ReadAll);

            NetworkServerManager.Instance.RebuildEntityIds();
            return new RestoreInfo(maxClients, hostClientId);
        }

        public static string Describe()
        {
            var players = ServerPlayer.list.Values.OfType<ServerPlayer>().OrderBy(p => p.Id).Select(p =>
            {
                var weapon = p.GetCurrentWeapon();
                var pos = p.PlayerTransform != null ? p.PlayerTransform.position : Vector3.zero;
                return $"{p.Id}:hp{p.Health}/{p.MaxHealth} k{p.Kills} d{p.Deaths} w{p.CurrentWeaponIndex} ammo{weapon?.CurrentAmmo} dead{(p.Dead ? 1 : 0)} pos({pos.x:F1},{pos.y:F1},{pos.z:F1})";
            });
            var enemies = ServerEnemy.list.Values.Where(e => e != null).OrderBy(e => e.Id).Select(e =>
            {
                var pos = e.transform.position;
                return $"e{e.Id}:{e.enemyType} hp{e.Health} pos({pos.x:F1},{pos.y:F1},{pos.z:F1})";
            });
            var game = NetworkServerManager.Instance.game;
            return $"state {NetworkServerManager.GameState} tick {NetworkServerManager.Instance.CurrentTick} end {(game != null ? game.EndTick : 0)} objects {ServerObject.list.Count} | {string.Join(" | ", players.Concat(enemies))}";
        }

        public static void ClearServerState()
        {
            foreach (var p in ServerPlayer.list.Values.OfType<ServerPlayer>().ToList())
                UnityEngine.Object.DestroyImmediate(p.gameObject);
            ServerPlayer.list.Clear();
            ServerPlayer.Vote = null;

            foreach (var e in ServerEnemy.list.Values.Where(e => e != null).ToList())
                UnityEngine.Object.DestroyImmediate(e.gameObject);
            ServerEnemy.list.Clear();

            foreach (var o in ServerObject.list.Values.Where(o => o != null).ToList())
                UnityEngine.Object.DestroyImmediate(o.gameObject);
            ServerObject.list.Clear();

            foreach (var g in ServerGrenade.list.Values.Where(g => g != null).ToList())
                UnityEngine.Object.DestroyImmediate(g.gameObject);
            ServerGrenade.list.Clear();

            foreach (var c in ServerCollectable.list.Values.Where(c => c != null).ToList())
                UnityEngine.Object.DestroyImmediate(c.gameObject);
            ServerCollectable.list.Clear();

            ServerFire.DestroyAll();
            ServerJumpPad.DestroyAll();

            var entities = NetworkServerManager.Entity.Entities;
            foreach (var id in entities.Keys.Where(id => id != Multiplayer.Entity.Entity.MaxEntityAmount).ToList())
                entities.Remove(id);

            var game = NetworkServerManager.Instance.game;
            if (game != null) game.ClearServerSide();
            if (Endless.Instance != null) Endless.Instance.ClearServerSide();

            NetworkServerManager.Instance.ClearServerTimers();
        }

        private static void Section(SnapshotWriter w, Action<SnapshotWriter> write)
        {
            using var section = new SnapshotWriter();
            write(section);
            w.Bytes(section.ToArray());
        }

        private static void Read(SnapshotReader r, Action<SnapshotReader> read)
        {
            byte[] bytes = r.Bytes();
            try
            {
                read(new SnapshotReader(bytes));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
