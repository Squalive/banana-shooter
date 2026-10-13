using System.Linq;
using Multiplayer.Migration;
using UnityEngine;

namespace Multiplayer.Server
{
    public partial class ServerCollectable
    {
        private float _spawnTime;

        internal static void WriteAll(SnapshotWriter w)
        {
            var items = list.Values.Where(c => c != null && !c._collected).ToList();
            w.Int(items.Count);
            foreach (var c in items)
            {
                w.String(Multiplayer.Migration.SnapshotPrefabs.Key(c.gameObject));
                w.UShort(c.Id);
                w.UShort((ushort)c.Type);
                w.UShort(c.deadPlayerId);
                w.UShort(c.killPlayerId);
                w.Vector3(c.transform.position);
                w.Float(Mathf.Max(0f, 20f - (Time.time - c._spawnTime)));
            }
        }

        internal static void ReadAll(SnapshotReader r)
        {
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                string key = r.String();
                ushort id = r.UShort();
                var type = (CollectableType)r.UShort();
                ushort dead = r.UShort();
                ushort killer = r.UShort();
                Vector3 pos = r.Vector3();
                float life = r.Float();

                var prefab = Multiplayer.Migration.SnapshotPrefabs.Find(key);
                if (prefab == null) continue;

                var c = Instantiate(prefab, pos, Quaternion.identity).GetComponent<ServerCollectable>();
                c.Id = id;
                c.Type = type;
                c.deadPlayerId = dead;
                c.killPlayerId = killer;
                c._spawnTime = Time.time - (20f - life);
                list[id] = c;
                Destroy(c.gameObject, life);
            }
        }
    }
}
