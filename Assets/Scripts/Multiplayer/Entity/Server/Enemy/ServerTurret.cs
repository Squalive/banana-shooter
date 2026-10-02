using Multiplayer.Entity.Interface;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;

namespace Multiplayer.Entity.Server.Enemy
{
    public class ServerTurret : ServerEnemy
    {
        public override int MaxHealth { get; set; } = 300;

        public enum ETurretState
        {
            Idle,
            Recovering,
            Shooting,
        }

        private ETurretState _state = ETurretState.Idle;

        private Transform _transform;
        private IPlayerServer _targetPlayer;
        
        protected override void Awake()
        {
            base.Awake();
            _transform = transform;
            
            InvokeRepeating(nameof(TryToFindPlayer),0,2f);
        }

        private void OnEnable()
        {
            OnTakeDamaged += OnGetHit;
        }

        private void OnDisable()
        {
            OnTakeDamaged -= OnGetHit;
        }

        private Vector3 _desiredPos;
        [SerializeField] private Transform tip;
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            switch (_state)
            {
                case ETurretState.Shooting:
                    if (_targetPlayer != null)
                    {
                        _desiredPos = Vector3.Lerp(_desiredPos,_targetPlayer.PlayerRb.transform.position,Time.deltaTime*6f);
                        if (readyToShoot)
                        {
                            readyToShoot = false;
                            Invoke(nameof(ReadyToShoot),1f/fireRate);

                            if (Physics.Raycast(tip.position, (_desiredPos-tip.position).normalized, out var hit, 1000f,whatIsHittable))
                            {
                                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ServerPlayer"))
                                {
                                    IPlayerServer player = hit.transform.root.GetComponent<IPlayerServer>();
                        
                                    player.TakeDamage((int)(10*difficulty),Id,NetworkServerManager.Instance.CurrentTick,false,false,12);
                                }
                            }
                        }
                    }
                    
                    break;
            }
        }

        private bool readyToShoot=false;
        public float fireRate=20;
        private Collider[] _colliders = new Collider[2];

        [SerializeField] private LayerMask serverPlayer;
        void TryToFindPlayer()
        {
            if (_state == ETurretState.Idle)
            {
                int cnt = Physics.OverlapSphereNonAlloc(_transform.position, 40f, _colliders,
                    serverPlayer);

                if (cnt > 0)
                {
                    _targetPlayer = _colliders[0].transform.root.GetComponent<ServerPlayer>();
                                        
                    _state = ETurretState.Shooting;
                    Invoke(nameof(GotoRecover),5);
                    Invoke(nameof(ReadyToShoot),1.2f);
                    SendState();
                }
                else
                {
                    _targetPlayer = null;
                }
            }
        }

        void OnGetHit(ushort fromClient)
        {
            if (ServerPlayer.list.TryGetValue(fromClient, out var s))
            {
                _targetPlayer = s;
                    
                _state = ETurretState.Shooting;
                Invoke(nameof(GotoRecover),5);
                Invoke(nameof(ReadyToShoot),1.2f);
                SendState();
            }
           
        }
        void ReadyToShoot()
        {
            readyToShoot = true;
        }
        void GotoRecover()
        {
            readyToShoot = false;
            _state = ETurretState.Recovering;
            _targetPlayer = null;
            SendState();
            Invoke(nameof(GotoIdle),2.5f);
        }

        void GotoIdle()
        {
            _state = ETurretState.Idle;
            SendState();
        }
        
        void SendState()
        {
            Message message= Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.EnemyState);

            message.Add(Id);
                        
            message.Add((ushort) _state);
            if(_state == ETurretState.Shooting)
                message.Add(_targetPlayer.Id);
                        
            NetworkServerManager.Instance.Server.SendToAll(message);
        }
    }
}
