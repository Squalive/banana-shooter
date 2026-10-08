using System.Collections.Generic;
using System.Linq;
using Multiplayer.Migration;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public partial class ServerFire
    {
        private static readonly HashSet<ServerFire> Active = new();
        private float _spawnTime;

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        internal static void DestroyAll()
        {
            foreach (var f in Active.Where(f => f != null).ToList())
                DestroyImmediate(f.gameObject);
            Active.Clear();
        }

        internal static void WriteAll(SnapshotWriter w)
        {
            Active.RemoveWhere(f => f == null);
            w.Int(Active.Count);
            foreach (var f in Active)
            {
                w.String(SnapshotPrefabs.Key(f.gameObject));
                w.UShort(f.PlayerId);
                w.Vector3(f.transform.position);
                w.Bool(f.ready);
                w.Float(Mathf.Max(0f, 3f - (Time.time - f._spawnTime)));
                w.WriteInvoke(f, nameof(GetReady));
            }
        }

        internal static void ReadAll(SnapshotReader r)
        {
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                string key = r.String();
                ushort playerId = r.UShort();
                Vector3 pos = r.Vector3();
                bool ready = r.Bool();
                float life = r.Float();

                var prefab = SnapshotPrefabs.Find(key);
                if (prefab == null)
                {
                    r.Float();
                    continue;
                }

                var f = Instantiate(prefab, pos, Quaternion.identity).GetComponent<ServerFire>();
                f.PlayerId = playerId;
                f.ready = ready;
                f._spawnTime = Time.time - (3f - life);
                r.ReadInvoke(f, nameof(GetReady));
                Destroy(f.gameObject, life);
            }
        }
    }
}
