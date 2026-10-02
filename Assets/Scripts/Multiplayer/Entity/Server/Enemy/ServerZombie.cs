using Manager;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;
using UnityEngine.AI;

namespace Multiplayer.Entity.Server.Enemy
{
    public class ServerZombie : ServerEnemy
    {
        /// <summary>
        /// Zombie should stand when there is no target in his view
        /// When there is a target zombie should roar and then track the target
        /// </summary>
        public enum EZombieState
        {
            Awake=0,
            Idle,
            Roam,
            Trace
        }
        public float moveSpeed = 3500f,maxSpeed=10;
        private EZombieState state = EZombieState.Idle;
        NavMeshAgent agent;
        public override int MaxHealth { get; set; } = 150;


        private Transform _transform, target;
        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            _transform = transform;
            InvokeRepeating(nameof(GetNavMeshPos),0.2f,.5f);
            InvokeRepeating(nameof(FindPlayer),0.2f,0.5f);
        }

        Quaternion rot;
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            switch (state)
            {
                case EZombieState.Trace:
                    //Rotate
                    Vector3 myPos = _transform.position;
                    //Check Disstance
                    bool flag = false;
                    float dis = Vector3.Distance(myPos, nextPos);
                    foreach (var corner in agent.path.corners)
                    {
                        if (Vector3.Distance(corner, myPos) < dis)
                        {
                            nextPos = corner;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag&&dis < 3f)
                    {
                        if (rb.velocity.sqrMagnitude < 1f)
                        {
                            rb.velocity = Vector3.zero;
                        }
                        break;
                    }
                    rot = Quaternion.LookRotation(nextPos - myPos);
                    _transform.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
                
                    //Move
                    Vector3 dir = moveSpeed * Time.fixedDeltaTime * _transform.forward;
                    rb.velocity = new Vector3(dir.x,rb.velocity.y,dir.z);
                    break;
            }
        }

        private Vector3 nextPos;
        private Collider[] cols = new Collider[1];

        void ChangeState(EZombieState s)
        {
            state = s;
            Message message= Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.EnemyState);

            message.Add(Id);
                        
            message.Add((ushort) state);
                        
            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        void ReadyToTrace()
        {
            ChangeState(EZombieState.Trace);
        }
        void FindPlayer()
        {
            if (state != EZombieState.Trace&&state != EZombieState.Awake)
            {
                int cnt = Physics.OverlapSphereNonAlloc(_transform.position, 120f,cols,GameManager.Instance.serverPlayer);

                if (cnt > 0)
                {
                    target = cols[0].transform;
                    ChangeState(EZombieState.Awake);
                    Invoke(nameof(ReadyToTrace),1f);
                }
            }
        }
        void GetNavMeshPos()
        {
            if (state == EZombieState.Idle)
            {
                return;
            }
            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                NavMeshPath path = new NavMeshPath();
                Vector3  targetPos = target!=null? target.position : Vector3.zero;
                if (Physics.Raycast(targetPos, Vector3.down, out var hit, 1000, whatIsGround))
                {
                    targetPos = hit.point;
                }
             
                Vector3 desiredPos = Vector3.zero;
                switch (state)
                {
                    case EZombieState.Roam:
                        break;
                    case EZombieState.Trace:
                        desiredPos = targetPos;
                        break;
                }
                if (desiredPos == nextPos) return;
                if (agent.CalculatePath(desiredPos, path))
                {
                    agent.path = path;
                
                    // for (int i = 1; i < path.corners.Length; i++)
                    // {
                    //     Debug.DrawLine(path.corners[i],path.corners[i-1],Color.red,1f);
                    // }
                }
            }

            nextPos = agent.steeringTarget;

            agent.enabled = false;

        }
    
    
    }
}
