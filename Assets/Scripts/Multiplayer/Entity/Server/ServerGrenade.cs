using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using Menu;
using Multiplayer.Entity.Interface;
using Multiplayer.Entity.Server.Enemy;
using Multiplayer.LagCompensation;
using Pool;
using Riptide;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Server
{
    public class ServerGrenade : MonoBehaviour, IEntity
    {
        public static Dictionary<ushort, ServerGrenade> list = new Dictionary<ushort, ServerGrenade>();

        public ushort Id { get; private set; }

        public ushort playerId = 0;

        public static float[] forces = new float[]
        {
            35f,
            150f,
            25f,
            25f,
            150f,
        };

        public static int[] gra = new int[]
        {
            1,
            0,
            1,
            1,
            0
        };

        public static ushort nextId = 0;

        private Rigidbody rigidbody;

        public bool exploded = false;

        private ThrowObjectMenu.ThrowObjectType type;

        private ServerPlayer.DamageType _damageType;
        private float _speed;

        private Transform _target;

        private int _offset;
        public void Initialize(Vector3 dir, ushort _playerId, ThrowObjectMenu.ThrowObjectType type, ServerPlayer.DamageType damageType, Transform target, uint tick)
        {
            _transform = transform;
            _offset = (int)(NetworkServerManager.Instance.CurrentTick - tick);
            Id = nextId++;

            playerId = _playerId;
            this.type = type;
            _damageType = damageType;
            _target = target;

            if (damageType == ServerPlayer.DamageType.Player)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), ServerPlayer.list[_playerId].PlayerRb.GetComponent<Collider>());
            }

            exploded = false;

            list.Add(Id, this);

            rigidbody = GetComponent<Rigidbody>();
            _speed = forces[(int)type];
            // if (type == ThrowObjectMenu.ThrowObjectType.Missile && target!=null)
            // {
            //     _transform.rotation = Quaternion.LookRotation(Vector3.up);
            //     _targetRb = target.GetComponent<Rigidbody>();
            //     rigidbody.velocity = transform.forward * _speed;
            // }
            rigidbody.velocity = dir * _speed + Vector3.up * _speed / 2f * gra[(int)type];
            // rigidbody.AddForce(dir*35f+Vector3.up*10f,ForceMode.Impulse);
            rigidbody.AddTorque(Vector3.right * 1000f, ForceMode.Impulse);

            Message msg = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.ThrowObj);
            msg.Add(Id);
            msg.Add((int)type);
            msg.Add(playerId);
            msg.Add(transform.position);
            msg.Add(dir);
            NetworkServerManager.Instance.Server.SendToAll(msg, playerId);
            Init();
        }
        public void Destroy()
        {
            Destroy(gameObject);
        }

        public bool IsEnemy()
        {
            return false;
        }

        public bool IsPlayer()
        {
            return false;
        }
        public bool IsVoid()
        {
            return false;
        }

        public bool IsThrowable()
        {
            return true;
        }
        void Init()
        {
            inited = true;
        }

        private bool inited = false;
        private void Start()
        {
            switch (type)
            {
                case ThrowObjectMenu.ThrowObjectType.Grenade:
                    StartCoroutine(Explode(1.2f, Vector3.zero));
                    break;
                case ThrowObjectMenu.ThrowObjectType.FlashBang:
                    StartCoroutine(Explode(1.5f, Vector3.zero));
                    break;
                case ThrowObjectMenu.ThrowObjectType.JumpPad:
                    Invoke(nameof(Destroy), 5f);
                    break;
                default:
                    StartCoroutine(Explode(10, Vector3.zero));
                    break;
            }
        }

        private Vector3 _standardPrediction;
        private Transform _transform;
        private float _maxDistancePredict = 100;
        private float _minDistancePredict = 5;
        private float _maxTimePrediction = 5;

        private Rigidbody _targetRb;

        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
                list.Remove(Id);
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!inited) return;

            if (type is ThrowObjectMenu.ThrowObjectType.MolotovCocktail or ThrowObjectMenu.ThrowObjectType.JumpPad)
            {
                Vector3 normal = other.contacts[0].normal;
                if (!exploded && Vector3.Angle(Vector3.up, normal) < 30)
                {
                    exploded = true;
                    StartCoroutine(Explode(0, other.contacts[0].point));
                }
            }
            else if (type == ThrowObjectMenu.ThrowObjectType.Knife)
            {
                if (!exploded)
                {
                    exploded = true;
                    StartCoroutine(Explode(0, other.contacts[0].point));
                }
            }
        }

        private Collider[] col = new Collider[15];
        private Collider[] _col = new Collider[15];

        List<ServerPlayer> _clients = new();
        List<ServerEnemy> _enemies = new();
        IEnumerator Explode(float time, Vector3 position)
        {
            yield return new WaitForSeconds(time);

            if (position == Vector3.zero)
                position = _transform.position;

            if (!ServerPlayer.list.TryGetValue(playerId, out var fromPlayer))
            {
                Destroy();
                yield break;
            }
            exploded = true;

            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.ThrowObjExplode);

            message.Add(Id);
            message.AddVector3(position);

            NetworkServerManager.Instance.Server.SendToAll(message, playerId);

            uint t = (uint)(NetworkServerManager.Instance.CurrentTick - _offset);

            List<LagCompensationHitbox> hitboxes = new List<LagCompensationHitbox>();

            //Player hitbox
            foreach (var serverPlayer in ServerPlayer.list.Values)
            {
                TransformUpdate transformUpdate = serverPlayer.TransformBuffer[t % ServerPlayer.MaxTickStore];

                if (transformUpdate != null && (NetworkServerManager.Instance.ShouldSpawnHitbox(serverPlayer, fromPlayer) || type == ThrowObjectMenu.ThrowObjectType.FlashBang))
                {
                    LagCompensationHitbox hitbox = ObjectPooler.Instance.SpawnFromPool("PlayerHitbox", transformUpdate.Position, Quaternion.identity).GetComponent<LagCompensationHitbox>();

                    hitbox.Initialize(serverPlayer.Id, t);

                    hitboxes.Add(hitbox);
                }
            }

            //Enemy Hitbox
            foreach (var serverEnemy in ServerEnemy.list.Values)
            {
                TransformUpdate transformUpdate = serverEnemy.TransformBuffer[t % ServerPlayer.MaxTickStore];

                if (transformUpdate != null)
                {
                    Vector3 predictPos = transformUpdate.Position;

                    string enemy;

                    switch (serverEnemy.enemyType)
                    {
                        case ServerEnemy.EnemyType.Jack:
                            enemy = "JackHitbox";
                            break;
                        case ServerEnemy.EnemyType.Kat:
                            enemy = "KatHitbox";
                            break;
                        case ServerEnemy.EnemyType.Turret:
                            enemy = "TurretHitbox";
                            break;
                        case ServerEnemy.EnemyType.Zombie:
                            enemy = "ZombieHitbox";
                            break;
                        default:
                            enemy = "JackHitbox";
                            break;
                    }

                    LagCompensationHitbox hitbox = ObjectPooler.Instance.SpawnFromPool(enemy, predictPos, Quaternion.identity).GetComponent<LagCompensationHitbox>();

                    hitbox.Initialize(serverEnemy.Id, t);

                    hitboxes.Add(hitbox);
                }
            }

            Physics.SyncTransforms();

            RaycastHit hit;
            int cnt;
            switch (type)
            {
                case ThrowObjectMenu.ThrowObjectType.FlashBang:
                    cnt = Physics.OverlapSphereNonAlloc(position, 40f, col, GameManager.Instance.lagCompensationHitboxLayer);

                    for (int i = 0; i < cnt; i++)
                    {
                        Collider c = col[i];
                        LagCompensationHitbox hitbox = c.transform.root.GetComponent<LagCompensationHitbox>();

                        if (hitbox == null) continue;

                        if (hitbox.type == LagCompensationHitbox.HitboxType.Player)
                        {
                            if (ServerPlayer.list.TryGetValue(hitbox.Id, out var player))
                            {
                                Vector3 pos = position;

                                var transform1 = hitbox.transform;
                                float angle = Vector3.Angle(player.PlayerTransform.forward,
                                    pos - (transform1.position));

                                if (Physics.Raycast(pos, (hitbox.transform.position - pos).normalized, out hit, 2000, GameManager.Instance.lagCompensationHitboxLayer))
                                {
                                    if (hitbox == hit.transform.root.GetComponent<LagCompensationHitbox>())
                                    {
                                        message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.FlashBang);

                                        message.Add(player.Id);
                                        message.Add(playerId);
                                        message.Add(angle);
                                        NetworkServerManager.Instance.Server.SendToAll(message);
                                    }
                                }
                            }
                        }


                    }
                    break;
                case ThrowObjectMenu.ThrowObjectType.MolotovCocktail:
                    Vector3 originalPos = position + Vector3.up * 2f;

                    int[] xx = { 1, -1, 0, 0, 1, -1, -1, 1, 2, -2, 0, 0, 2, 2, -2, -2, 1, -1, 1, -1 }, yy = { 0, 0, 1, -1, -1, 1, -1, 1, 0, 0, 2, -2, 1, -1, 1, -1, 2, 2, -2, -2 };
                    Ray ray = new Ray(originalPos, Vector3.down);
                    if (Physics.Raycast(ray, out hit, 100f, GameManager.Instance.whatIsGround))
                    {
                        ServerFire fire =
                            Instantiate(PrefabManager.Instance.GetPrefab("ServerFire"), hit.point, Quaternion.identity)
                                .GetComponent<ServerFire>();

                        fire.Initialize(playerId);
                    }
                    for (int i = 0; i < xx.Length; i++)
                    {
                        ray = new Ray(originalPos + new Vector3(xx[i], 0, yy[i]) * 2f, Vector3.down);
                        if (Physics.Raycast(ray, out hit, 100f, GameManager.Instance.whatIsGround))
                        {
                            ServerFire fire =
                                Instantiate(PrefabManager.Instance.GetPrefab("ServerFire"), hit.point, Quaternion.identity)
                                    .GetComponent<ServerFire>();

                            fire.Initialize(playerId);
                        }
                    }

                    xx = new int[] { CalculateDis(Random.Range(-1, 1)), CalculateDis(Random.Range(-1, 1)), CalculateDis(Random.Range(-1, 1)), CalculateDis(Random.Range(-1, 1)) };
                    yy = new int[] { CalculateDis(Random.Range(-1, 1)), CalculateDis(Random.Range(-1, 1)), CalculateDis(Random.Range(-1, 1)), CalculateDis(Random.Range(-1, 1)) };

                    for (int i = 0; i < xx.Length; i++)
                    {
                        ray = new Ray(originalPos + new Vector3(xx[i], 0, yy[i]) * 2f, Vector3.down);
                        if (Physics.Raycast(ray, out hit, 100f, GameManager.Instance.whatIsGround))
                        {
                            ServerFire fire =
                                Instantiate(PrefabManager.Instance.GetPrefab("ServerFire"), hit.point, Quaternion.identity)
                                    .GetComponent<ServerFire>();

                            fire.Initialize(playerId);
                        }
                    }
                    break;
                case ThrowObjectMenu.ThrowObjectType.Grenade:
                    cnt = Physics.OverlapSphereNonAlloc(position, 15f, _col, propLayer);

                    for (int i = 0; i < cnt; i++)
                    {
                        Rigidbody rb = _col[i].GetComponent<Rigidbody>();

                        if (rb != null)
                            rb.AddExplosionForce(.000005f, position, 25f, 1f, ForceMode.Impulse);
                    }
                    _clients.Clear();
                    _enemies.Clear();
                    cnt = Physics.OverlapSphereNonAlloc(position, 12f, _col, GameManager.Instance.lagCompensationHitboxLayer,
                        QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < cnt; i++)
                    {
                        if (_col[i].transform.root.TryGetComponent(out LagCompensationHitbox hitbox))
                        {
                            if (_damageType == ServerPlayer.DamageType.Player)
                            {
                                hitbox.TakeDamage(playerId, t, false, false, LagCompensationHitbox.HitboxType.Player, _col[i].bounds.ClosestPoint(_transform.position), false, 1002);
                            }
                        }
                    }
                    break;
                case ThrowObjectMenu.ThrowObjectType.Knife:
                    cnt = Physics.OverlapSphereNonAlloc(position, 4f, _col, propLayer);

                    for (int i = 0; i < cnt; i++)
                    {
                        Rigidbody rb = _col[i].GetComponent<Rigidbody>();

                        if (rb != null)
                            rb.AddExplosionForce(.000005f, position, 4f, 1f, ForceMode.Impulse);
                    }
                    cnt = Physics.OverlapSphereNonAlloc(position, 4f, _col, GameManager.Instance.lagCompensationHitboxLayer,
                        QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < cnt; i++)
                    {
                        Collider c = _col[i];
                        Transform root = c.transform.root;

                        LagCompensationHitbox hitbox = root.GetComponent<LagCompensationHitbox>();

                        if (hitbox != null)
                        {
                            if (_damageType == ServerPlayer.DamageType.Player)
                            {
                                hitbox.TakeDamage(playerId, t, false, false, LagCompensationHitbox.HitboxType.Player, c.bounds.ClosestPoint(_transform.position), false, 1000);
                            }
                        }
                    }
                    break;
                case ThrowObjectMenu.ThrowObjectType.JumpPad:
                    var jumpPadPrefab = PrefabManager.Instance.GetPrefab("ServerJumpPad").GetComponent<ServerJumpPad>();

                    var instance = Object.Instantiate(jumpPadPrefab, position, Quaternion.identity);

                    instance.Initialize();

                    break;
            }

            foreach (var hitbox in hitboxes)
            {
                hitbox.gameObject.SetActive(false);
            }

            Destroy(gameObject);
        }

        [SerializeField] private LayerMask propLayer, whatIsHittable;

        int CalculateDis(float a)
        {
            if (a < 0) return -3;
            if (a > 0) return 3;
            return 0;
        }
    }
}
