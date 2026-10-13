using System.Collections.Generic;
using System.Linq;
using Multiplayer.Migration;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public partial class ServerJumpPad
    {
        private static readonly HashSet<ServerJumpPad> Active = new();

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        internal static void DestroyAll()
        {
            foreach (var p in Active.Where(p => p != null).ToList())
                DestroyImmediate(p.gameObject);
            Active.Clear();
        }

        internal static void WriteAll(SnapshotWriter w)
        {
            Active.RemoveWhere(p => p == null);
            w.Int(Active.Count);
            foreach (var p in Active)
            {
                w.String(SnapshotPrefabs.Key(p.gameObject));
                w.UShort(p.Id);
                w.Vector3(p.transform.position);
                w.Quaternion(p.transform.rotation);
                w.Float(Time.time - p._spawnedTime);
            }
        }

        internal static void ReadAll(SnapshotReader r)
        {
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                string key = r.String();
                ushort id = r.UShort();
                Vector3 pos = r.Vector3();
                Quaternion rot = r.Quaternion();
                float age = r.Float();

                var prefab = SnapshotPrefabs.Find(key);
                if (prefab == null) continue;

                var p = Instantiate(prefab, pos, rot).GetComponent<ServerJumpPad>();
                p._transform = p.transform;
                p.Id = id;
                p._spawnedTime = Time.time - age;
                NetworkServerManager.AddEntity(id, p);
                Destroy(p.gameObject, Mathf.Max(0f, 30f - age));
            }
        }
    }
}
