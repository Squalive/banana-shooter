using System;
using System.Collections.Generic;
using Manager;
using Mode;
using Multiplayer.Entity.Interface;
using Multiplayer.Interface;
using Multiplayer.LagCompensation;
using Riptide;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Server.Enemy
{
    public abstract class ServerEnemy : MonoBehaviour,IEntity,IDamageable
    {
        public enum EnemyType
        {
            None=0,
            Jack,
            Zombie,
            Turret,
            Kat
        }
    
        public static Dictionary<ushort, ServerEnemy> list = new Dictionary<ushort, ServerEnemy>();

        public ushort Id { get;private set; }

        public EnemyType enemyType;
        public bool dead = false;

        protected Rigidbody rb;

        static int MaxEnemyCount = 256;

        public LayerMask whatIsGround;

        [SerializeField]protected float difficulty = 1;
        
        public const int MaxTickStore = 128;

        public readonly TransformUpdate[] TransformBuffer = new TransformUpdate[MaxTickStore];
        
        public abstract int MaxHealth { get; set; }
        public int Health { get; set; }
        public bool Dead { get; set; }
        
        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private Vector3 _startPos;
        public void Initialize()
        {
            float d = 1;
            if (Endless.Instance)
            {
                difficulty = Endless.Instance.difficulty*2f;
                difficulty = Mathf.Clamp(difficulty,0.2f,2f);
                d = Mathf.Clamp(difficulty, 0.2f, 1f);
            }
            else
            {
                difficulty = 0.5f;
            }
            Id = NetworkServerManager.Instance.GetAvailableEntityId();

            if (Id == 0)
            {
                Destroy(gameObject);
                return;
            }
            
            NetworkServerManager.Entity.Entities.Add(Id,this);

            _startPos = transform.position;
        
            GameManager.Entities.Add(gameObject);

            MaxHealth = (int)(MaxHealth * d);
            Health = MaxHealth;
        
            list.Add(Id,this);

            NetworkServerManager.Instance.Server.SendToAll(GetSpawnData());
        
        }

        public Message GetSpawnData()
        {
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.SpawnEnemy);

            message.AddUShort(Id);
            message.Add(Health);
            message.Add((ushort)enemyType);
            message.Add(enemyIndex);
            message.Add(transform.position);

            return message;
        }

        // Update is called once per frame
        protected virtual void FixedUpdate()
        {
            if (!dead)
            {
                if (transform.position.y < -20)
                    transform.position = _startPos;
                SendMovement();
                
                TransformUpdate transformUpdate = new TransformUpdate(NetworkServerManager.Instance.CurrentTick,false,transform.position);
 
                TransformBuffer[NetworkServerManager.Instance.CurrentTick % MaxTickStore] = transformUpdate;
            }
        }

        public int enemyIndex = 1;
        void SendMovement()
        {
            Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ServerToClientId.EnemyMovement);

            message.Add(Id);
            message.Add(NetworkServerManager.Instance.CurrentTick);
            message.Add(transform.position);
            message.Add(transform.rotation.eulerAngles.y);
            Vector3 vel = transform.InverseTransformDirection(rb.velocity);
            message.Add(vel);
        
            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        [MessageHandler((ushort) ClientToServerId.SpawnEnemy, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SpawnEnemy(ushort fromClient, Message message)
        {
            if (list.Count >= MaxEnemyCount) return;
            EnemyType type = (EnemyType) message.GetUShort();

            Vector3 pos = message.GetVector3();
        
            SpawnEnemy(type,pos);
        }
        public LayerMask whatIsHittable;

        void ClearFrom()
        {
            fromClient = 0;
        }
        public void TakeDamage(int damage, ushort attacker, uint tick, bool headShot, bool wallbang, ushort weapon, params object[] param)
        {
            // damage = LuaManager.Hook.OnDamage(attacker,damage);
            
            Health -= damage;
            Health = Mathf.Clamp(Health, 0, MaxHealth);
            this.fromClient = attacker;
            Invoke(nameof(ClearFrom),0.5f);
            OnTakeDamaged?.Invoke(fromClient);
            if (Health <= 0 && !Dead)
            {
                Dead = true;

                int addCash = 150;
                if (NetworkServerManager.ServerType == ServerType.Endless)
                {
                    addCash = (int) (addCash * Endless.Instance.difficulty);

                    if (addCash < 150) addCash = 150;

                    addCash = Random.Range(addCash - 30, addCash + 30);
                }

                if (ServerPlayer.list.TryGetValue(fromClient, out var player))
                {
                    player.AddCash(addCash);
                }
                
                
            
                Destroy(gameObject);
            }
            else
            {
                Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ServerToClientId.HitEnemy);

                message.Add(Id);
                message.Add(tick);
                message.Add(fromClient);
                message.Add(Health);
            
                NetworkServerManager.Instance.Server.SendToAll(message);
            }
        }

        public Action<ushort> OnTakeDamaged;

        private ushort fromClient;

        private void OnDestroy()
        {
            Message msg = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.EnemyDead);

            msg.Add(Id);
            msg.Add(fromClient);
            msg.Add(transform.position);
            
            NetworkServerManager.Instance.Server.SendToAll(msg);
            GameManager.Entities.Remove(gameObject);
            if (list.ContainsKey(Id))
            {
                list.Remove(Id);
            }
            
            if (NetworkServerManager.Entity.Entities.ContainsKey(Id))
            {
                NetworkServerManager.Entity.Entities.Remove(Id);
            }
        }

        public static void SpawnEnemy(EnemyType type,Vector3 pos)
        {
            ServerEnemy enemy=null;
            switch (type)
            {
                case EnemyType.Jack:
                    enemy = Instantiate(PrefabManager.Instance.GetPrefab("ServerJack"), pos, Quaternion.identity)
                        .GetComponent<ServerEnemy>();
                    break;
                // case EnemyType.Zombie:
                //     int index = Random.Range(1, 2);
                //     enemy = Instantiate(PrefabManager.Instance.GetPrefab("ServerZombie"+index), pos, Quaternion.identity)
                //         .GetComponent<ServerEnemy>();
                //     break;
                case EnemyType.Turret:
                    enemy = Instantiate(PrefabManager.Instance.GetPrefab("ServerTurret"), pos, Quaternion.identity)
                        .GetComponent<ServerEnemy>();
                    break;
                case EnemyType.Kat:
                    enemy = Instantiate(PrefabManager.Instance.GetPrefab("ServerKat"), pos, Quaternion.identity)
                        .GetComponent<ServerEnemy>();
                    break;
                default:
                    enemy = Instantiate(PrefabManager.Instance.GetPrefab("ServerJack"), pos, Quaternion.identity)
                        .GetComponent<ServerEnemy>();
                    break;
            }

            if (enemy != null)
            {
                enemy.Initialize();
            }
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public bool IsEnemy()
        {
            return true;
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
            return false;
        }

        public bool CanDamage(ushort attacker, ServerPlayer.DamageType damageType)
        {
            return true;
        }
    }
}
