using System;
using Multiplayer.Entity.Server.Enemy;
using Pool;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Client.Enemy.State
{
    public class KatState : EnemyState
    {
        public ServerKat.EKatState state = ServerKat.EKatState.Nonawake;
        
        [SerializeField] private Transform headTarget;
        private Vector3 _targetDesiredPos;
        [SerializeField] private Vector3 nonAwakePos;
        [HideInInspector]
        public Transform playerTarget;

        [SerializeField] public TwoBoneIKConstraint hand;
        [SerializeField] public Transform tip;
        [SerializeField] private AudioClip shootSound;
        
        [SerializeField] ParticleSystem muzzle;
        private bool _readyToShoot=false;

        protected override void Update()
        {
            base.Update();
            if (state == ServerKat.EKatState.Nonawake || state == ServerKat.EKatState.Awaking)
            {
                headTarget.localPosition = Vector3.Lerp(headTarget.localPosition, _targetDesiredPos, Time.deltaTime * 15f);
            }
            else
            {
                headTarget.position = Vector3.Lerp(headTarget.position,_targetDesiredPos,Time.deltaTime*15f);
                Vector3 rot = Quaternion.LookRotation(_targetDesiredPos-selfTransform.position).eulerAngles;
                headTarget.rotation = Quaternion.Euler(rot.x+95,rot.y,rot.z);
            }
        }

        private void FixedUpdate()
        {
            switch (state)
            {
                case ServerKat.EKatState.Nonawake:
                    _targetDesiredPos = nonAwakePos;
                
                    break;
                case ServerKat.EKatState.Chasing:
                    if (playerTarget != null)
                    {
                        _targetDesiredPos = playerTarget.position;
                    }
                    break;
                case ServerKat.EKatState.Shooting:
                    if (playerTarget != null)
                    {
                        _targetDesiredPos = playerTarget.position;
                        desiredRot = Quaternion.Euler(0,Quaternion.LookRotation(_targetDesiredPos-selfTransform.position).eulerAngles.y,0);
                        if (_readyToShoot)
                        {
                            _readyToShoot = false;
                            Invoke(nameof(ReadyToShoot),1f / 3);
                            Shoot();

                            Vector3 tipPos = tip.position;
                            Vector3 dir = tip.forward;
                            Vector3 offset = ((.5f / 60f) * Random.insideUnitCircle);
                            dir += offset;
                            Bullet bullet = ObjectPooler.Instance.SpawnFromPool("Bullet", tipPos,
                                Quaternion.LookRotation(dir)).GetComponent<Bullet>();
                            bullet.Initialization(dir, 800f, 0, LayerMask.NameToLayer("Bullet"), false, false);
                        }
                    }
                
                    break;
                case ServerKat.EKatState.Missile:
                    if (playerTarget != null)
                    {
                        _targetDesiredPos = playerTarget.position;
                        desiredRot = Quaternion.Euler(0,Quaternion.LookRotation(_targetDesiredPos-selfTransform.position).eulerAngles.y,0);
                    }

                    break;
            }
        }
        
        void Shoot()
        {
            source.PlayOneShot(shootSound);
        
            muzzle.Play();
        }
        
        public void ReadyToShoot()
        {
            _readyToShoot = true;
        }

        public void SetState(ServerKat.EKatState eKatState, Transform t)
        {
            state = eKatState;

            _readyToShoot = false;
            switch (eKatState)
            {
                case ServerKat.EKatState.Awaking:
                    break;
                case ServerKat.EKatState.Nonawake:
                    playerTarget = null;
                    hand.weight = 0;
                    break;
                case ServerKat.EKatState.Chasing:
                    hand.weight = 0;
                    playerTarget = t;
                    break;
                case ServerKat.EKatState.Shooting:
                    hand.weight = 1;
                    CancelInvoke(nameof(ReadyToShoot));
                    Invoke(nameof(ReadyToShoot),1f);
                    break;
                case ServerKat.EKatState.Missile:
                    hand.weight = 1;
                    break;
            }
        }
    }
}