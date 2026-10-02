
using Audio;
using Demo;
using Demo.Entity;
using Manager;
using Multiplayer.Entity.Server.Enemy;
using Pool;
using UnityEngine;

namespace Multiplayer.Entity.Client.Enemy.State
{
    public abstract class EnemyState : MonoBehaviour
    {
        [HideInInspector] public ServerEnemy.EnemyType enemyType = ServerEnemy.EnemyType.None;
        
        [HideInInspector] public Transform selfTransform;

        [HideInInspector] public Rigidbody rb;
        
        [HideInInspector] public AudioSource source;
        
        [HideInInspector] public Quaternion desiredRot;

        [SerializeField] public Animator anim;

        public int Health { get; set; } = 100;
        protected float Speed { get; set; } = 15f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            anim = GetComponent<Animator>();
            source = gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = UnderWaterSfx.Instance.@group;
            source.loop = false;
            source.playOnAwake = false;
            
            source.spatialBlend = 1f;

            selfTransform = transform;
        }

        protected virtual void Update()
        {
            if (Health > 0)
            {
                selfTransform.rotation = Quaternion.Lerp(selfTransform.rotation,desiredRot,Time.deltaTime*Speed);
            }
        }

        public void Dead(bool isLocal)
        {
            var transform1 = selfTransform;
            Vector3 position = transform1.position;
            Quaternion rotation = transform1.rotation;
            if (isLocal)
            {
                AudioManager.Instance.Play("bulletimpact_robot2");
                HitMarker.Instance.StartHitMarker(Color.red);
            }
            else
            {
                AudioManager.Instance.SoundEffect3D("bulletimpact_robot2",position, 25f);
            }
            switch (enemyType)
            {
                case ServerEnemy.EnemyType.Jack:
                
                    Destroy(Instantiate(PrefabManager.Instance.GetPrefab("JackRagdoll"),position,rotation),5f);
                    Instantiate(PrefabManager.Instance.GetPrefab("SmallExplode"), position, rotation);
                
                    if(isLocal)
                        AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.JACK);
                    break;
                case ServerEnemy.EnemyType.Turret:
                    Destroy(Instantiate(PrefabManager.Instance.GetPrefab("TurretRagdoll"),position,rotation),5f);
                    Instantiate(PrefabManager.Instance.GetPrefab("BigExplode"), position, rotation);

                    AudioManager.Instance.SoundEffect3D("Explosion",position);
                    if (GameManager.Instance.setting.cameraShake)
                    {
                        GameManager.Instance.CameraShake3D(5f, 5f, 0.1f, 1 ,position);
                    }
                    break;
            }
        }

        public int SetHealth(int health, bool visual)
        {
            int damage = Health - health;
            Health = health;
            if(visual)
                TakeDamageVisual(damage);
            return damage;
        }
        
        public int SetHealth(int health, int attackerId, bool visual)
        {
            DemoEntity entity = DemoManager.Instance.GetEntity(attackerId);
            if (entity != null && entity.IsPlayer() && ((DemoPlayer)entity).playerState.IsLocal)
            {
                HitMarker.Instance.StartHitMarker(Color.white);
            }
            return SetHealth(health,visual);
        }

        void TakeDamageVisual(int damage)
        {
            var position = transform.position;
            HitMarker3D h = ObjectPooler.Instance.SpawnFromPool("HitMarker", position,
                    Quaternion.LookRotation(ListenerManager.Instance.cameraTransform.position - position))
                .GetComponent<HitMarker3D>();
            h.text.SetText(damage.ToString());
            
            if (GameManager.Instance.setting.spawnParticle)
            {
                // Instantiate(PrefabManager.Instance.GetPrefab("robotHit"), pos, rot);
                Quaternion rot = Quaternion.LookRotation(Vector3.up);
                ObjectPooler.Instance.SpawnFromPool("BulletHit",position,rot);
                Instantiate(PrefabManager.Instance.GetPrefab("robotHIt2"), position, rot);
                AudioManager.Instance.SoundEffect3D("bulletimpact_robot", position);
            }
        }

        public static GameObject InstantiateEnemy(ServerEnemy.EnemyType type, Vector3 pos, int index)
        {
            GameObject enemy;
            switch (type)
            {
                case ServerEnemy.EnemyType.Jack:
                    enemy =
                        Instantiate(PrefabManager.Instance.GetPrefab("ClientJack"), pos, Quaternion.identity);
                    break;
                case ServerEnemy.EnemyType.Zombie:
                    enemy =
                        Instantiate(PrefabManager.Instance.GetPrefab("ClientZombie"+index), pos, Quaternion.identity);
                    break;
                case ServerEnemy.EnemyType.Kat:
                    enemy =
                        Instantiate(PrefabManager.Instance.GetPrefab("ClientKat"), pos, Quaternion.identity);
                    break;
                case ServerEnemy.EnemyType.Turret:
                    enemy =
                        Instantiate(PrefabManager.Instance.GetPrefab("ClientTurret"), pos, Quaternion.identity);
                    break;
                default:
                    enemy =
                        Instantiate(PrefabManager.Instance.GetPrefab("ClientJack"), pos, Quaternion.identity);
                
                    break;
            }

            return enemy;
        }
    }
}