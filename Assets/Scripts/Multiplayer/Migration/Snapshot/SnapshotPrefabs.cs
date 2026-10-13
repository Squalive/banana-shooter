using Manager;
using Mode;
using Multiplayer.Entity.Server;
using UnityEngine;

namespace Multiplayer.Migration
{
    public static class SnapshotPrefabs
    {
        public static string Key(GameObject go)
        {
            string name = go.name;
            return name.EndsWith("(Clone)") ? name.Substring(0, name.Length - 7).TrimEnd() : name;
        }

        public static GameObject Find(string key)
        {
            var prefab = PrefabManager.Instance.GetPrefab(key);
            if (prefab != null) return prefab;

            if (Endless.Instance != null)
            {
                foreach (var enemy in Endless.Instance.enemyPrefabs)
                {
                    if (enemy != null && enemy.name == key)
                        return enemy.gameObject;
                }
            }

            Debug.LogWarning($"[Migration] no prefab for {key}");
            return null;
        }

        public static string Path(Transform t)
        {
            string path = $"{t.GetSiblingIndex()}:{t.name}";
            while (t.parent != null)
            {
                t = t.parent;
                path = $"{t.GetSiblingIndex()}:{t.name}/{path}";
            }
            return $"{t.gameObject.scene.name}/{path}";
        }

        public static void WritePlayerRef(this SnapshotWriter w, Transform t)
        {
            var player = t != null ? t.GetComponentInParent<ServerPlayer>() : null;
            w.UShort(player != null ? player.Id : (ushort)0);
            if (player == null) return;

            string path = string.Empty;
            for (var c = t; c != null && c != player.transform; c = c.parent)
                path = path.Length == 0 ? c.name : $"{c.name}/{path}";
            w.String(path);
        }

        public static Transform ReadPlayerRef(this SnapshotReader r)
        {
            ushort id = r.UShort();
            if (id == 0) return null;
            string path = r.String();
            if (!ServerPlayer.list.TryGetValue(id, out var p) || p is not ServerPlayer player) return null;
            return path.Length == 0 ? player.transform : player.transform.Find(path) ?? player.PlayerTransform;
        }
    }
}
