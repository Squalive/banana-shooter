using System.Linq;
using Manager;
using Multiplayer.Migration;
using UnityEngine;

namespace Multiplayer.Entity.Server.Enemy
{
    public abstract partial class ServerEnemy
    {
        internal static void WriteAll(SnapshotWriter w)
        {
            var enemies = list.Values.Where(e => e != null && !e.Dead).ToList();
            w.Int(enemies.Count);
            foreach (var e in enemies)
            {
                w.String(SnapshotPrefabs.Key(e.gameObject));
                w.UShort(e.Id);
                w.Int(e.enemyIndex);
                w.Int(e.MaxHealth);
                w.Int(e.Health);
                w.Bool(e.dead);
                w.Float(e.difficulty);
                w.Vector3(e._startPos);
                w.UShort(e.fromClient);
                w.Vector3(e.transform.position);
                w.Quaternion(e.transform.rotation);
                w.Vector3(e.rb.velocity);
                w.Vector3(e.rb.angularVelocity);
                w.WriteInvoke(e, nameof(ClearFrom));
                e.WriteAiSnapshot(w);
            }
        }

        internal static void ReadAll(SnapshotReader r)
        {
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                string key = r.String();
                var prefab = SnapshotPrefabs.Find(key);
                if (prefab == null)
                {
                    Debug.LogError($"[Migration] cant restore enemy {key}, the rest of the snapshot is skipped");
                    return;
                }

                ushort id = r.UShort();
                Vector3 pos;
                var e = Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<ServerEnemy>();
                e.Id = id;
                e.enemyIndex = r.Int();
                e.MaxHealth = r.Int();
                e.Health = r.Int();
                e.dead = r.Bool();
                e.difficulty = r.Float();
                e._startPos = r.Vector3();
                e.fromClient = r.UShort();
                pos = r.Vector3();
                e.transform.SetPositionAndRotation(pos, r.Quaternion());
                e.rb.velocity = r.Vector3();
                e.rb.angularVelocity = r.Vector3();
                r.ReadInvoke(e, nameof(ClearFrom));
                e.ReadAiSnapshot(r);

                NetworkServerManager.Entity.Entities[id] = e;
                GameManager.Entities.Add(e.gameObject);
                list[id] = e;
            }
        }

        protected virtual void WriteAiSnapshot(SnapshotWriter w)
        {
        }

        protected virtual void ReadAiSnapshot(SnapshotReader r)
        {
        }
    }

    public partial class ServerJack
    {
        protected override void WriteAiSnapshot(SnapshotWriter w)
        {
            w.Int((int)state);
            w.WritePlayerRef(target);
            w.Vector3(targetPos);
            w.Vector3(roamingPos);
            w.Float(meleeLeftTime);
            w.Float(traceTime);
            w.Bool(readyToShoot);
            w.Bool(flag);
            w.Bool(f);
            w.WritePlayerRef(_pendingTarget);
            w.Int((int)_pendingState);
            w.WriteInvoke(this, nameof(ApplyPendingState));
            w.WriteInvoke(this, nameof(GoAwake));
            w.WriteInvoke(this, nameof(ReadyToShoot));
            w.WriteInvoke(this, nameof(StopAttack));
            w.WriteInvoke(this, nameof(ReadyToAttack));
        }

        protected override void ReadAiSnapshot(SnapshotReader r)
        {
            state = (EJackState)r.Int();
            target = r.ReadPlayerRef();
            targetPos = r.Vector3();
            roamingPos = r.Vector3();
            meleeLeftTime = r.Float();
            traceTime = r.Float();
            readyToShoot = r.Bool();
            flag = r.Bool();
            f = r.Bool();
            _pendingTarget = r.ReadPlayerRef();
            _pendingState = (EJackState)r.Int();
            r.ReadInvoke(this, nameof(ApplyPendingState));
            r.ReadInvoke(this, nameof(GoAwake));
            r.ReadInvoke(this, nameof(ReadyToShoot));
            r.ReadInvoke(this, nameof(StopAttack));
            r.ReadInvoke(this, nameof(ReadyToAttack));
        }
    }

    public partial class ServerKat
    {
        protected override void WriteAiSnapshot(SnapshotWriter w)
        {
            w.Int((int)state);
            w.WritePlayerRef(_trackTarget);
            w.Vector3(targetPos);
            w.Bool(m);
            w.Bool(_readyToShoot);
            w.WriteInvoke(this, nameof(ReadyToShoot));
            w.WriteInvoke(this, nameof(BackToNormal));
            w.WriteInvoke(this, nameof(Chase));
        }

        protected override void ReadAiSnapshot(SnapshotReader r)
        {
            state = (EKatState)r.Int();
            _trackTarget = r.ReadPlayerRef();
            targetPos = r.Vector3();
            m = r.Bool();
            _readyToShoot = r.Bool();
            r.ReadInvoke(this, nameof(ReadyToShoot));
            r.ReadInvoke(this, nameof(BackToNormal));
            r.ReadInvoke(this, nameof(Chase));
        }
    }

    public partial class ServerTurret
    {
        protected override void WriteAiSnapshot(SnapshotWriter w)
        {
            w.Int((int)_state);
            w.UShort(_targetPlayer?.Id ?? 0);
            w.Vector3(_desiredPos);
            w.Bool(readyToShoot);
            w.WriteInvoke(this, nameof(ReadyToShoot));
            w.WriteInvoke(this, nameof(GotoRecover));
            w.WriteInvoke(this, nameof(GotoIdle));
        }

        protected override void ReadAiSnapshot(SnapshotReader r)
        {
            _state = (ETurretState)r.Int();
            ushort target = r.UShort();
            _targetPlayer = target != 0 && ServerPlayer.list.TryGetValue(target, out var p) ? p : null;
            _desiredPos = r.Vector3();
            readyToShoot = r.Bool();
            r.ReadInvoke(this, nameof(ReadyToShoot));
            r.ReadInvoke(this, nameof(GotoRecover));
            r.ReadInvoke(this, nameof(GotoIdle));
        }
    }

    public partial class ServerZombie
    {
        protected override void WriteAiSnapshot(SnapshotWriter w)
        {
            w.Int((int)state);
            w.WritePlayerRef(target);
            w.Vector3(nextPos);
            w.WriteInvoke(this, nameof(ReadyToTrace));
        }

        protected override void ReadAiSnapshot(SnapshotReader r)
        {
            state = (EZombieState)r.Int();
            target = r.ReadPlayerRef();
            nextPos = r.Vector3();
            r.ReadInvoke(this, nameof(ReadyToTrace));
        }
    }
}
