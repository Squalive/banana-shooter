using System.Linq;
using Manager;
using Multiplayer.Migration;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public partial class ServerObject
    {
        private static bool _restoring;
        private float _spawnTime;

        internal static void WriteAll(SnapshotWriter w)
        {
            var objects = list.Values.Where(o => o != null).ToList();
            w.UShort(_nextId);
            w.Int(objects.Count);
            foreach (var o in objects)
            {
                w.String(SnapshotPrefabs.Key(o.gameObject));
                w.UShort(o.Id);
                w.Int((int)o.type);
                w.Bool(o.ready);
                w.Float(o.continues ? -1f : Mathf.Max(0f, 8f - (Time.time - o._spawnTime)));
                w.Vector3(o._transform.position);
                w.Quaternion(o._transform.rotation);
                w.Vector3(o._rigidbody.velocity);
                w.Vector3(o._rigidbody.angularVelocity);
                w.WriteInvoke(o, nameof(BeReady));
            }
        }

        internal static void ReadAll(SnapshotReader r)
        {
            _nextId = r.UShort();
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                string key = r.String();
                ushort id = r.UShort();
                var type = (ObjectType)r.Int();
                bool ready = r.Bool();
                float life = r.Float();
                Vector3 pos = r.Vector3();
                Quaternion rot = r.Quaternion();
                Vector3 vel = r.Vector3();
                Vector3 angular = r.Vector3();

                var prefab = SnapshotPrefabs.Find(key);
                if (prefab == null)
                {
                    r.Float();
                    continue;
                }

                _restoring = true;
                ServerObject o;
                try
                {
                    o = Instantiate(prefab, pos, rot).GetComponent<ServerObject>();
                }
                finally
                {
                    _restoring = false;
                }

                o.Id = id;
                o.type = type;
                o.ready = ready;
                o._init = true;
                o._spawnTime = Time.time - (8f - Mathf.Max(life, 0f));
                o._rigidbody.velocity = vel;
                o._rigidbody.angularVelocity = angular;
                r.ReadInvoke(o, nameof(BeReady));

                list[id] = o;
                GameManager.Entities.Add(o.gameObject);
                if (life >= 0f)
                    Destroy(o.gameObject, life);
            }
        }
    }
}
