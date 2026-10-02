
using Manager;
using Multiplayer.Entity.Client.Enemy.Animation;
using Multiplayer.Entity.Server.Enemy;
using Pool;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Client.Enemy.State
{
    public class JackState : EnemyState
    {
        public ServerJack.EJackState state = ServerJack.EJackState.Stop;
        
        [SerializeField] ParticleSystem muzzle;

        [SerializeField] Transform tip;
        [SerializeField] AudioClip shootSound;

        [SerializeField] public AudioClip walkSound,doorOpen,doorClose;
        private bool _grounded=false;
        private bool _readyToShoot=false;
        private float _distance  = 0f;
        
        private Collider[] _cols = new Collider[1];
        
        private float _height;

        private void Start()
        {
            _height = GetComponent<Collider>().bounds.size.y;
        }

        protected override void Update()
        {
            base.Update();
            FootSteps();
        }

        void FixedUpdate()
        {
            if (Health > 0)
            {
                int count = Physics.OverlapSphereNonAlloc(transform.position - Vector3.up* (_height / 2f), 0.5f,_cols, GameManager.Instance.whatIsGround);

                _grounded = count > 0;
            }

            switch (state)
            {
                case ServerJack.EJackState.Attacking:
                    if (!_readyToShoot)
                    {
                        break;
                    }

                    _readyToShoot = false;
                    Invoke(nameof(SetReadyToShoot),1f / 20);
                    Shoot();
                    Vector3 hitPoint = tip.position + tip.forward*1000f;
                    if (Physics.Raycast(tip.position, tip.forward, out var hit, 1000f))
                    {
                        hitPoint = hit.point;
                    }

                    Vector3 tipPos = tip.position;
                    Vector3 dir = (hitPoint - tipPos).normalized;
                    Vector3 offset = ((.5f / 60f) * Random.insideUnitCircle);
                    dir += offset;
                    Bullet bullet = ObjectPooler.Instance.SpawnFromPool("Bullet", tipPos,
                        Quaternion.LookRotation(dir)).GetComponent<Bullet>();
                    bullet.Initialization(dir, 800f, 0, LayerMask.NameToLayer("Bullet"), false, false);
                    break;
            
                case ServerJack.EJackState.Stop:
                    break;
                
            }
        }
        
        void Shoot()
        {
            source.PlayOneShot(shootSound);
        
            muzzle.Play();
        }
        public void SetReadyToShoot()
        {
            _readyToShoot = true;
        }
        
        private void FootSteps()
        {
            if (_grounded )
            {
                float num = 1.2f;
                float num2 = rb.velocity.magnitude;
                if (num2 > 20f)
                {
                    num2 = 20f;
                }
                _distance += num2;
                if (_distance > 600f / num)
                {
                    source.PlayOneShot(walkSound);
                    _distance = 0f;
                }
            }
        }

        public void SetState(ServerJack.EJackState eJackState, JackAnimation jackAnimation)
        {
            state = eJackState;
            _readyToShoot = false;
            switch (state)
            {
                case ServerJack.EJackState.Attacking:
                    Speed = 15f;
                    jackAnimation.SetDoor(true);
                    source.PlayOneShot(doorOpen);
                    Invoke(nameof(SetReadyToShoot),1.7f);
                    break;
                case ServerJack.EJackState.Stop:
                    Speed = 3f;
                    jackAnimation.SetDoor(false);
                    source.PlayOneShot(doorClose);
                    break;
            }
        }
    }
}