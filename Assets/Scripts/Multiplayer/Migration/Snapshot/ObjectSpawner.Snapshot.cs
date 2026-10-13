using System.Linq;
using Multiplayer.Entity.Server;
using Multiplayer.Migration;
using UnityEngine;

namespace Multiplayer
{
    public partial class ObjectSpawner
    {
        internal static void WriteAll(SnapshotWriter w)
        {
            var spawners = FindObjectsOfType<ObjectSpawner>();
            w.Int(spawners.Length);
            foreach (var s in spawners)
            {
                w.String(SnapshotPrefabs.Path(s.transform));
                var obj = s._obj != null ? s._obj.GetComponent<ServerObject>() : null;
                w.UShort(obj != null ? obj.Id : (ushort)0);
                w.WriteInvoke(s, nameof(SpawnObj));
            }
        }

        internal static void ReadAll(SnapshotReader r)
        {
            var spawners = FindObjectsOfType<ObjectSpawner>().ToDictionary(s => SnapshotPrefabs.Path(s.transform));
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                string path = r.String();
                ushort objId = r.UShort();
                if (!spawners.TryGetValue(path, out var s))
                {
                    r.Float();
                    continue;
                }

                s._spawn = s.transform;
                s._obj = objId != 0 && ServerObject.list.TryGetValue(objId, out var obj) ? obj.gameObject : null;
                r.ReadInvoke(s, nameof(SpawnObj));
            }
        }
    }
}
