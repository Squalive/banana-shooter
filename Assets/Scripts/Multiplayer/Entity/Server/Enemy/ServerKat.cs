using Multiplayer.Interface;
using Riptide;
using UnityEngine;
using UnityEngine.AI;

namespace Multiplayer.Entity.Server.Enemy
{
    public class ServerKat : ServerEnemy
    {
        public override int MaxHealth { get; set; } = 100;

        public EKatState state = EKatState.Nonawake;
        public enum EKatState
        {
            Nonawake,
            Awaking,
            Idle,
            Chasing,
            Shooting,
            Missile,
        }

        protected override void Awake()
        {
            base.Awake();
            _transform = transform;
            agent = GetComponent<NavMeshAgent>();
        
            InvokeRepeating(nameof(TryToAwake),1f,1f);
            InvokeRepeating(nameof(GetNavMeshPos),0.2f,1f);
        }

        private Transform _transform;
        private Collider[] _cols = new Collider[5];
        [SerializeField] private LayerMask whatIsPlayer;
    
    

        private Transform _trackTarget;

        [SerializeField] [Range(0, 3000f)] private float moveSpeed = 1500f;
        [SerializeField] [Range(0, 20f)] private float chaseMinDistance = 10;
        [SerializeField] [Range(0, 50f)] private float maxSpeed = 20f;

        private bool m = false;
        [Header("Shoot")]
        [SerializeField] private Transform tip;
        [SerializeField] [Range(0, 15f)] private float fireRate = 5;
        private float offset = 0.1f;
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            var position = _transform.position;
            Vector3 dir = targetPos - position;
        
            switch (state)
            {
                case EKatState.Chasing:
                    if (_trackTarget != null)
                    {
                    
                        var position1 = _trackTarget.position;
                        float dis = Vector3.Distance(position, position1);
                        bool flag = false;
                        foreach (var corner in agent.path.corners)
                        {
                            if (Vector3.Distance(corner, position) < dis)
                            {
                                position1 = corner;
                                flag = true;
                                break;
                            }
                        }
                        if (!flag&&dis < 3f)
                        {
                        
                            break;
                        }
                        dir = position1 - position;
                        if (dis > chaseMinDistance)
                        {
                            // if(rb.velocity.sqrMagnitude <= maxSpeed)
                            //     rb.AddForce(moveSpeed*Time.deltaTime*_transform.forward,ForceMode.Force);
                            // else
                            //     rb.AddForce(moveSpeed*Time.deltaTime*-rb.velocity.normalized,ForceMode.Force);

                            Vector3 move = moveSpeed * Time.deltaTime * _transform.forward;
                            rb.velocity = new Vector3(move.x,rb.velocity.y,move.z);
                            m = true;
                        }
                        else if(!flag)
                        {
                            if (rb.velocity.sqrMagnitude < 1f)
                            {
                                rb.velocity = Vector3.zero;
                            }
                            if (m)
                            {
                                m = false;
                            }

                            state = Random.Range(0, 10) < 5*difficulty ? EKatState.Missile : EKatState.Shooting;
                            _readyToShoot = false;
                            Invoke(nameof(ReadyToShoot),1f);
                            Invoke(nameof(BackToNormal),4f);
                            SendState();
                        }

                    }
                    break;
                case EKatState.Shooting:
                
                    if (_readyToShoot)
                    {
                        _readyToShoot = false;
                        Invoke(nameof(ReadyToShoot),1f / fireRate);
                        tip.LookAt(_trackTarget);
                        if (Physics.Raycast(tip.position, tip.forward + new Vector3(Random.Range(-offset,offset),Random.Range(-offset,offset)), out var hit, 1000f,whatIsHittable))
                        {
                            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ServerPlayer"))
                            {
                                ServerPlayer player = hit.transform.root.GetComponent<ServerPlayer>();
                        
                                player.TakeDamage((int)((15 + Random.Range(-10,10))*difficulty),Id,NetworkServerManager.Instance.CurrentTick,false,false,12);
                            }
                        }
                    }
                    break;
                case EKatState.Missile:
                    if (_readyToShoot)
                    {
                        _readyToShoot = false;
                        Invoke(nameof(ReadyToShoot), 1f);
                        tip.LookAt(_trackTarget);
                        // Vector3 pos = tip.position;
                        // var obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerMissile"),pos,Quaternion.identity)
                        //     .GetComponent<ServerGrenade>();
                        // Transform target = _transform;
                        //
                        // float dis = float.MaxValue;
                        // foreach (var otherTarget in ServerPlayer.list.Values)
                        // {
                        //     
                        //     float d = Vector3.Distance(otherTarget.PlayerTransform.position, pos);
                        //     if (d < dis)
                        //     {
                        //         target = otherTarget.PlayerTransform;
                        //         dis = d;
                        //     }
                        // }
                        //
                    }
                    

                    break;
                
            }
            if(dir!=Vector3.zero)
                _transform.rotation = Quaternion.Euler(0,Quaternion.LookRotation(dir).eulerAngles.y,0);
        } 

        void ReadyToShoot()
        {
            _readyToShoot = true;
        }

        void BackToNormal()
        {
            _readyToShoot = false;
            state = EKatState.Nonawake;
            _trackTarget = null;
            SendState();
        }

        private bool _readyToShoot = false;
        void TryToAwake()
        {
            if (state == EKatState.Nonawake && _trackTarget==null)
            {
                int cnt = Physics.OverlapSphereNonAlloc(_transform.position, 120f , _cols, whatIsPlayer);

                if (cnt > 0)
                {
                    _trackTarget = _cols[0].transform;
                    state = EKatState.Awaking;
                    Invoke(nameof(Chase),1.3f);
                    SendState();
                }
            }
        }

        void SendState()
        {
            Message message= Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.EnemyState);

            message.Add(Id);
                        
            message.Add((ushort) state);

            if (state == EKatState.Chasing && _trackTarget)
            {
                message.Add(_trackTarget.root.GetComponent<ServerPlayer>().Id);
            }
                        
            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        void Chase()
        {
            state = EKatState.Chasing;
            SendState();
        }

        private Vector3 targetPos;
    
        NavMeshAgent agent;

        void GetNavMeshPos()
        {
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                NavMeshPath path = new NavMeshPath();
                Vector3  targetPos = _trackTarget!=null? _trackTarget.position : Vector3.zero;
                if (Physics.Raycast(targetPos, Vector3.down, out var hit, 1000, whatIsGround))
                {
                    targetPos = hit.point;
                }
             
                Vector3 desiredPos = targetPos;

                if (desiredPos == this.targetPos) return;
                if (agent.CalculatePath(desiredPos, path))
                {
                    agent.path = path;
                }
            }

            this.targetPos = agent.steeringTarget;

            agent.enabled = false;

        }
    }
}
