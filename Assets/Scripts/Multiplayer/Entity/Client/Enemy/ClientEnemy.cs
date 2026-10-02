
using System.Collections.Generic;
using Demo;
using Demo.Entity.Enemy;
using Menu;
using Mode;
using Multiplayer.Entity.Client.Enemy.Animation;
using Multiplayer.Entity.Client.Enemy.State;
using Multiplayer.Entity.Server.Enemy;
using Multiplayer.LagCompensation;
using Quest;
using Riptide;
using UnityEngine;
using Weapon;

namespace Multiplayer.Entity.Client.Enemy
{
    public abstract class ClientEnemy : MonoBehaviour
    {
        public static Dictionary<ushort, ClientEnemy> list = new Dictionary<ushort, ClientEnemy>();
        public ushort Id { private set; get; }

        protected EnemyAnimation EnemyAnimation;

        public DemoEnemy demoEnemy;
            
        [SerializeField] public Interpolator interpolator;

        [SerializeField] public EnemyState enemyState;

        [SerializeField] private Transform dot;

        [HideInInspector] public int index;

        protected virtual void Awake()
        {
            if (DemoManager.Replaying)
            {
                Destroy(this);
            }
        }
        void Initialize(ushort id, int health,ServerEnemy.EnemyType enemyType,int index)
        {
            Id = id;

            enemyState.enemyType = enemyType;

            enemyState.Health = health;
            this.index = index;

            if (NetworkManager.ClientServerType == ServerType.Endless)
            {
                GameUIManager.Instance.AddEnemyDot(dot,Id);
            }
            list.Add(Id,this);
            
            DemoManager.Instance.AddEnemySpawned(this);
        }

        [MessageHandler((ushort) ServerToClientId.EnemyDead, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void EnemyDead(Message message)
        {
            ushort id = message.GetUShort();
            if (list.TryGetValue(id, out var enemy))
            {
                ushort fromClient = message.GetUShort();
                Vector3 pos = message.GetVector3();

                if (enemy != null) enemy.EnemyDead(fromClient);
            }
        }
        [MessageHandler((ushort) ServerToClientId.HitEnemy, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void HitEnemy(Message message)
        {
            ushort id = message.GetUShort();
            uint tick = message.GetUInt();
            ushort playerId = message.GetUShort();
            if (list.TryGetValue(id, out var enemy))
            {
                int health = message.GetInt();
            
                enemy.TakeDamage(health,playerId,tick);
            }
        }

        [SerializeField] private Outline outline;

        void DisplayOutline()
        {
            if (!outline) return;
            outline.enabled = true;
            CancelInvoke(nameof(ClearOutline));
            Invoke(nameof(ClearOutline),0.35f);
        }

        void ClearOutline()
        {
            outline.enabled = false;
        }


        private static bool flag = false;
        void EnemyDead(ushort fromClient)
        {
            bool isLocal = fromClient == NetworkManager.Instance.Client.Id;

            GameUIManager.Instance.RemoveEnemyDot(Id);

            flag = !flag;
            if (ClientPlayer.list.TryGetValue(fromClient, out var player))
            {
                isLocal = player.playerState.IsLocal;
                demoEnemy.attackerId = player.demoPlayer.Id;
                
                enemyState.Dead(isLocal);
                if (flag)
                {
                    if (Mathf.Abs(player.coins - player.lastCoin) <= 1)
                    {
                        player.lastCoin = player.coins;
                        player.coins++;
                        if(player.IsLocal)
                            UpgradeInGameMenu.Instance.AutoUpgrade();
                    }
                }
            }

            if (isLocal)
            {
                PowerInGameMenu.Instance.fillProgress += 0.05f;

                if (Endless.Instance)
                {
                    Endless.Instance.RlKillCount++;
                    Endless.Instance.killCount = Endless.Instance.RlKillCount.GetValue().ToString();
                    
                    QuestManager.Instance.GetProgress(QuestType.EndlessKiller);
                }
            }

            Destroy(gameObject);
            
            
        }

        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
            {
                list.Remove(Id);
            }
        }

        void TakeDamage(int health,ushort playerId,uint tick)
        {
            bool local = NetworkManager.Instance.Client.Id == playerId;

            int damage = enemyState.SetHealth(health, !local);
            if (ClientPlayer.list.TryGetValue(playerId, out var localPlayer))
            {
                if (local)
                {
                    localPlayer.DamageTracker.DamageGiven(Id,damage);
                    DisplayOutline();

                    if (!WeaponManager.Instance.ShootingBuffer[tick % WeaponManager.MaxStoredSize])
                    {
                        HitMarker.Instance.StartHitMarkerRobot(Color.white);
                    }
                }
                DemoManager.Instance.AddEnemyTakeDamage(demoEnemy.Id,health, localPlayer.demoPlayer.Id);
            }
            
        }

        [MessageHandler((ushort) ServerToClientId.EnemyMovement, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void EnemyMovement(Message message)
        {
            ushort id = message.GetUShort();
            uint tick = message.GetUInt();
            if (list.TryGetValue(id,out var enemy))
            {
                Vector3 pos = message.GetVector3();
                enemy.enemyState.desiredRot = Quaternion.Euler(0,message.GetFloat(),0);

                if (enemy.EnemyAnimation != null)
                {
                    Vector3 dir = pos - enemy.enemyState.selfTransform.position;
                    enemy.EnemyAnimation.SetInput(dir.magnitude < 0.01f
                        ? Vector3.zero
                        : enemy.enemyState.selfTransform.InverseTransformDirection(dir) * 4f);
                }

                enemy.interpolator.NewUpdate(tick,false,pos);
            }
        }

        [MessageHandler((ushort) ServerToClientId.SpawnEnemy, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void SpawnEnemy(Message message)
        {
            ushort id = message.GetUShort();
            if (list.ContainsKey(id))
            {
                Destroy(list[id].gameObject);
                list.Remove(id);
            }
            int health = message.GetInt();
            ServerEnemy.EnemyType type = (ServerEnemy.EnemyType) message.GetUShort();
            int index = message.GetInt();
            Vector3 pos = message.GetVector3();
            ClientEnemy enemy = State.EnemyState.InstantiateEnemy(type,pos,index).GetComponent<ClientEnemy>();
            
            enemy.Initialize(id,health,type,index);
        }
    
        [MessageHandler((ushort) ServerToClientId.EnemyState, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void EnemyState(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var enemy))
            {
                Transform t;
                switch (enemy.enemyState.enemyType)
                {
                    case ServerEnemy.EnemyType.Jack:
                        ServerJack.EJackState state = (ServerJack.EJackState) message.GetUShort();

                        JackAnimation jackAnimation = (JackAnimation)enemy.EnemyAnimation;
                        JackState jackState = (JackState)enemy.enemyState;
                        
                        jackState.SetState(state, jackAnimation);
                        
                        DemoManager.Instance.AddEnemyState(enemy.demoEnemy.Id,(int)state);
                        break;
                    case ServerEnemy.EnemyType.Zombie:
                        ServerZombie.EZombieState eZombieState = (ServerZombie.EZombieState) message.GetUShort();
                        ZombieState zombieState = (ZombieState)enemy.enemyState;
                        
                        zombieState.SetState(eZombieState);
                        
                        DemoManager.Instance.AddEnemyState(enemy.demoEnemy.Id,(int)eZombieState);
                        break;
                    case ServerEnemy.EnemyType.Turret:
                        ServerTurret.ETurretState eTurretState = (ServerTurret.ETurretState) message.GetUShort();
                        TurretState turretState = (TurretState) enemy.enemyState;

                        t = null;
                        if (eTurretState == ServerTurret.ETurretState.Shooting)
                        {
                            ushort playerId = message.GetUShort();

                            if (ClientPlayer.list.TryGetValue(playerId, out var player))
                            {
                                t = player.player.transform;
                            }
                        }
                        
                        turretState.SetState(eTurretState,t);
                        break;
                    case ServerEnemy.EnemyType.Kat:
                        ServerKat.EKatState eKatState = (ServerKat.EKatState) message.GetUShort();
                        KatState katState = (KatState)enemy.enemyState;

                        t = null;
                        int targetId = -1;
                        if (eKatState == ServerKat.EKatState.Chasing)
                        {
                            ushort playerId = message.GetUShort();
                            if (ClientPlayer.list.TryGetValue(playerId, out var player))
                            {
                                t = player.player.transform;
                                targetId = player.demoPlayer.Id;
                            }
                        }
                        katState.SetState(eKatState,t);
                        
                        DemoManager.Instance.AddEnemyStateTarget(enemy.demoEnemy.Id,(int)eKatState,targetId);
                        break; 
                }
            }
        }

        public Vector3 headOffset;

    }
}
