
using Multiplayer.Entity.Client.Enemy.Animation;
using Multiplayer.Entity.Client.Enemy.State;
using Multiplayer.Entity.Server.Enemy;
using UnityEngine;

namespace Demo.Entity.Enemy
{
    public abstract class DemoEnemy : DemoEntity
    {
        [SerializeField] public EnemyState enemyState;
        
        public EnemyAnimation Animation;
        
        private readonly float _movementThreshold = 0.02f;
        private float _squareMovementThreshold;
        
        private Vector3 _to, _previous;

        [HideInInspector]
        public int attackerId;

        protected virtual void Awake()
        {
            _squareMovementThreshold = _movementThreshold * _movementThreshold;
        }

        protected virtual void Update()
        {
            if (DemoManager.Replaying)
            {
                float lerp = Time.deltaTime / (0.02f / Time.timeScale);
                
                lerp = Mathf.Max(0.02f, lerp);
                
                InterpolatePosition(lerp);

                if (Animation != null)
                {
                    Animation.Update();
                }
            }
        }

        private void InterpolatePosition(float lerpAmount)
        {
            Vector3 from = GetTransform().position;

            if (_to != from)
            {
                selfTrans.position = Vector3.Lerp(from, _to, lerpAmount);
            }
        }
        
        public void NewPosition(Vector3 position)
        {
            _previous = _to;
            _to = position;

            Vector3 from = GetTransform().position;

            Vector3 dir = _to - from;
            if (Animation != null)
            {
                Animation.SetInput(dir.magnitude < 0.01f
                    ? Vector3.zero
                    : orientation.InverseTransformDirection(dir) * 4f);
            }
        }

        public void NewRotation(float rot)
        {
            enemyState.desiredRot = Quaternion.Euler(0,rot,0);
        }

        public override void Destroy(params object[] objects)
        {
            bool visualEffect = (bool)objects[0];
            int attacker = (int)objects[1];
            if (!IsDestroyed)
            {
                IsDestroyed = true;
                if (visualEffect)
                {
                    bool isLocal = ((DemoPlayer)DemoManager.Instance.GetEntity(attacker)).playerState.IsLocal;
                    enemyState.Dead(isLocal);
                }
                gameObject.SetActive(false);
            }
        }

        public override void Spawn(int id, params object[] obj)
        {
            base.Spawn(id, obj);

            ServerEnemy.EnemyType type = (ServerEnemy.EnemyType)obj[0];

            if (DemoManager.Replaying)
            {
                Vector3 position = GetTransform().position;

                _to = position;
                _previous = position;
                enemyState.enemyType = type;
                switch (type)
                {
                    case ServerEnemy.EnemyType.Jack:
                        Animation = new JackAnimation(enemyState.anim);
                        break;
                    case ServerEnemy.EnemyType.Zombie:
                        Animation = new ZombieAnimation(enemyState.anim);
                        break;
                    case ServerEnemy.EnemyType.Kat:
                        Animation = new KatAnimation(enemyState.anim);
                        break;
                }
            }
        }

        public override bool IsEnemy()
        {
            return true;
        }
    }
}