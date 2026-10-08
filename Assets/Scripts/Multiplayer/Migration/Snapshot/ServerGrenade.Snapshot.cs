using System.Linq;
using Menu;
using Multiplayer.Migration;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public partial class ServerGrenade
    {
        private bool _restored;
        private float _fuseEnd = -1f;

        void StartFuse(float time)
        {
            _fuseEnd = Time.time + time;
            StartCoroutine(Explode(time, Vector3.zero));
        }

        internal static void WriteAll(SnapshotWriter w)
        {
            var grenades = list.Values.Where(g => g != null && !g.exploded).ToList();
            w.UShort(nextId);
            w.Int(grenades.Count);
            foreach (var g in grenades)
            {
                w.String(SnapshotPrefabs.Key(g.gameObject));
                w.UShort(g.Id);
                w.UShort(g.playerId);
                w.Int((int)g.type);
                w.Int((int)g._damageType);
                w.Float(g._speed);
                w.Int(g._offset);
                w.Bool(g.inited);
                w.Vector3(g.transform.position);
                w.Quaternion(g.transform.rotation);
                w.Vector3(g.rigidbody.velocity);
                w.Vector3(g.rigidbody.angularVelocity);
                w.Float(g._fuseEnd >= 0f ? Mathf.Max(0f, g._fuseEnd - Time.time) : -1f);
                w.WriteInvoke(g, nameof(Destroy));
            }
        }

        internal static void ReadAll(SnapshotReader r)
        {
            nextId = r.UShort();
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                string key = r.String();
                ushort id = r.UShort();
                ushort owner = r.UShort();
                var type = (ThrowObjectMenu.ThrowObjectType)r.Int();
                var damageType = (ServerPlayer.DamageType)r.Int();
                float speed = r.Float();
                int offset = r.Int();
                bool inited = r.Bool();
                Vector3 pos = r.Vector3();
                Quaternion rot = r.Quaternion();
                Vector3 vel = r.Vector3();
                Vector3 angular = r.Vector3();
                float fuse = r.Float();

                var prefab = SnapshotPrefabs.Find(key);
                if (prefab == null)
                {
                    r.Float();
                    continue;
                }

                var g = Instantiate(prefab, pos, rot).GetComponent<ServerGrenade>();
                g._restored = true;
                g._transform = g.transform;
                g.rigidbody = g.GetComponent<Rigidbody>();
                g.Id = id;
                g.playerId = owner;
                g.type = type;
                g._damageType = damageType;
                g._speed = speed;
                g._offset = offset;
                g.inited = inited;
                g.exploded = false;
                g.rigidbody.velocity = vel;
                g.rigidbody.angularVelocity = angular;

                if (damageType == ServerPlayer.DamageType.Player && ServerPlayer.list.TryGetValue(owner, out var thrower))
                    Physics.IgnoreCollision(g.GetComponent<Collider>(), thrower.PlayerRb.GetComponent<Collider>());

                list[id] = g;
                if (fuse >= 0f)
                    g.StartFuse(fuse);
                r.ReadInvoke(g, nameof(Destroy));
            }
        }
    }
}
