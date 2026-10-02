using System;
using Multiplayer.Entity.Server.Enemy;
using Pool;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Client.Enemy.State
{
    public class TurretState : EnemyState
    {
        public ServerTurret.ETurretState state = ServerTurret.ETurretState.Idle;
        
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 recoverPos;
        [SerializeField] private float fireRate = 10f;
        private Vector3 _desiredPos,_defaultPos;
        
        public ParticleSystem muzzle;

        public Transform tip;
        public AudioClip shootSound;
        
        [HideInInspector] public Transform targetPlayer;
        private bool _readyToShoot=false;
        private void Start()
        {
            _defaultPos = target.localPosition;
            _desiredPos = _defaultPos; 
        }
        
        void FixedUpdate()
        {
            target.localPosition = Vector3.Lerp(target.localPosition, _desiredPos, Time.deltaTime * 6f);
            switch (state)
            {
                case ServerTurret.ETurretState.Shooting:

                    if (targetPlayer != null)
                    {
                        target.rotation = Quaternion.Lerp(target.rotation,Quaternion.LookRotation(targetPlayer.position-target.position),Time.deltaTime*6f );
                    }
                    
                    if (!_readyToShoot)
                    {
                        break;
                    }

                    _readyToShoot = false;
                    Invoke(nameof(ReadyToShoot),1f / fireRate);
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
                case ServerTurret.ETurretState.Recovering:
                    target.rotation = Quaternion.Lerp(target.rotation,Quaternion.Euler(60,0,0), Time.deltaTime*6f );
                    break;
            }
        }
        
        public void ReadyToShoot()
        {
            _readyToShoot = true;
        }

        public void GotoShootPos()
        {
            _desiredPos = _defaultPos; 
        }

        public void GotoRecoverPos()
        {
            _desiredPos = recoverPos; 
            
        }
        
        void Shoot()
        {
            source.PlayOneShot(shootSound);
        
            muzzle.Play();
        }

        public void SetState(ServerTurret.ETurretState turretState, Transform t = null)
        {
            state = turretState;
            _readyToShoot = false;

            switch (state)
            {
                case ServerTurret.ETurretState.Idle:
                    break;
                case ServerTurret.ETurretState.Shooting:
                    targetPlayer = t;
                    GotoShootPos();
                    Invoke(nameof(ReadyToShoot),1.2f);
                    break;
                case ServerTurret.ETurretState.Recovering:
                    GotoRecoverPos();
                    break;
            }
        }
    }
}