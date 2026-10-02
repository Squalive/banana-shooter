using System;
using System.Collections;
using Manager;
using Movement;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Multiplayer.Entity.Server.Enemy
{
    public class ServerJack : ServerEnemy
    {
        public override int MaxHealth { get; set; } = 100;
        
        public float moveSpeed = 4000f;
    
        [Header("Roaming")]

        [Range(0,50f)]public float maxRange = 10;
        private Vector3 roamingPos=Vector3.zero;
        [Header("Track")]
        private Transform target;
        [Range(0,20f)]public float trackTime = 8f;
        private float leftTimeToTrack;
        [Header("Melee")]
    
        [Range(0,2f)]public float meleeWaitTime = 0.34f;
        [Range(0,200f)]public float meleeAttackForce = 100f;
        [Range(0,200f)]public float jumpForce = 50f;
        [Range(0,100)]public int meleeAttackDamage = 50;
        // [Range(0,2f)]public float stopTime = 1.2f;
    
        public enum EJackState
        {
            Idle=0,
            Roaming,
            Tracing,
            Awake,
            Stop,
            Attacking,
        }

        private EJackState state = EJackState.Awake;
    
        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            playerHeight = GetComponent<Collider>().bounds.size.y;

            _transform = transform;
        
            InvokeRepeating(nameof(FindPlayer),0.2f,1f);
            InvokeRepeating(nameof(GetNavMeshPos),0.2f,1f);
            InvokeRepeating(nameof(FindRoamingPos),0.2f,1f);
        }
        private Transform _transform;

        private Collider[] cols = new Collider[10];
        Quaternion rot;
        private bool f = false;
        void FindPlayer()
        {
            if (state != EJackState.Stop && state != EJackState.Attacking)
            {
                int cnt = Physics.OverlapSphereNonAlloc(transform.position, 60,cols,GameManager.Instance.serverPlayer);
                if (target == null)
                {
                    if (cnt > 0)
                    {
                        target = cols[0].transform;
                        state = EJackState.Tracing;
                        if(_coroutine!=null) StopCoroutine(_coroutine);
                    }
                }
                else
                {
                    if (cnt > 0)
                    {
                        if (target != cols[0].transform)
                        {
                            state = EJackState.Stop;
                            if(_coroutine!=null) StopCoroutine(_coroutine);
                            _coroutine = StartCoroutine(ChangeStateAndTarget(8, cols[0].transform, EJackState.Tracing));
                            // leftTimeToTrack -= Time.fixedDeltaTime;
                            // if (leftTimeToTrack <= 0)
                            // {
                            //     target = cols[0].transform;
                            //     state = JackState.Tracing;
                            //     leftTimeToTrack = trackTime;
                        }
                    }
                    else
                    {
                        state = EJackState.Stop;
                        if(_coroutine!=null) StopCoroutine(_coroutine);
                        _coroutine = StartCoroutine(ChangeStateAndTarget(8, null, EJackState.Awake));
                        // leftTimeToTrack -= Time.fixedDeltaTime;
                        // if (leftTimeToTrack <= 0)
                        // {
                        //     target = null;
                        //     state = JackState.Awake;
                        // }
                    }
                }
            }
        }

        private Coroutine _coroutine;

        IEnumerator ChangeStateAndTarget(float t,Transform tr, EJackState s)
        {
            yield return new WaitForSeconds(t);

            target = tr;
            state = s;
        }
    

        private Vector3 targetPos;
        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        
            lastMoveSpeed = PlayerMovement.XZVector(rb.velocity);
            Vector3 pos = _transform.position;

            float dis;
        
            switch (state)
            {
                case EJackState.Idle:
                    if (target)
                    {
                        if (targetPos - pos != Vector3.zero)
                        {
                            rot = Quaternion.LookRotation(target.position - pos);
                            // _transform.rotation = Quaternion.Lerp(_transform.rotation,
                            //     Quaternion.Euler(0f, rot.eulerAngles.y, 0f), Time.deltaTime * 5f);
                    
                            _transform.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
                        }

                        meleeLeftTime -= Time.fixedDeltaTime;
                        if (meleeLeftTime <= 0)
                        {
                        
                            Vector3 dir = (targetPos - pos).normalized;
                            rb.AddForce(dir*meleeAttackForce+Vector3.up*jumpForce,ForceMode.Impulse);
                            meleeLeftTime = meleeWaitTime;
                            target = null;
                            state = EJackState.Stop;
                            StartCoroutine(GoAwake(0.3f));
                        
                        }
                    }
                
                
                    break;
                case EJackState.Roaming:
                    dis = Vector3.Distance(pos, targetPos);
                    if (dis > 2.5f)
                    {
                        rb.AddForce(moveSpeed * Time.fixedDeltaTime*transform.forward );
                        if (targetPos - pos != Vector3.zero)
                        {
                            rot = Quaternion.LookRotation(targetPos - pos);
                            // transform.rotation = Quaternion.Lerp(transform.rotation,
                            //     Quaternion.Euler(0f, rot.eulerAngles.y, 0f), Time.fixedDeltaTime * 3f);
                            _transform.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
                        }
                    }
                    else
                    {
                        state = EJackState.Stop;
                        StartCoroutine(GoAwake(0.7f));
                    }
                    break;
                case EJackState.Tracing:
                    dis = Vector3.Distance(pos, targetPos);
                    if (dis > 10f)
                    {
                        rb.AddForce(moveSpeed*1.25f * Time.fixedDeltaTime*transform.forward  );
                        traceTime += Time.fixedDeltaTime;

                        if (traceTime >= 3&&target && Mathf.Abs(_transform.position.y - target.position.y) < 1f)
                        {
                            traceTime = 0;
                            state = EJackState.Attacking;
                            readyToShoot=false;
                            Invoke(nameof(ReadyToShoot),1.7f);
                            SendState();
                        
                            Invoke(nameof(StopAttack),4f);
                        }
                    }
                    else if(!f)
                    {
                        state = EJackState.Idle;
                        meleeLeftTime = meleeWaitTime;
                    }

                    if (targetPos - pos != Vector3.zero)
                    {
                        rot = Quaternion.LookRotation(targetPos - pos);
                        // _transform.rotation = Quaternion.Lerp(_transform.rotation,
                        //     Quaternion.Euler(0f, rot.eulerAngles.y, 0f), Time.fixedDeltaTime * 3f);
                        _transform.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);
                    }

                    break;
                case EJackState.Stop:
                    // stopTimer -= Time.fixedDeltaTime;
                    // if (stopTimer <= 0)
                    // {
                    //     target = null;
                    //     stopTimer = stopTime;
                    //     state = JackState.Awake;
                    // }
                    break;
                case EJackState.Attacking:
                    if (target != null)
                    {
                        if (target.position - pos != Vector3.zero)
                        {
                            // _transform.rotation = Quaternion.Lerp(_transform.rotation,
                            //     Quaternion.Euler(0f,Quaternion.LookRotation(target.position-pos).eulerAngles.y,0f), Time.fixedDeltaTime * 3f);
                            _transform.rotation = Quaternion.Slerp(_transform.rotation, Quaternion.Euler(0f,Quaternion.LookRotation(target.position-pos).eulerAngles.y,0f),Time.fixedDeltaTime*10f);
                        }
                    

                    
                    }
                    if (!readyToShoot)
                    {
                        break;
                    }

                    readyToShoot = false;
                    Invoke(nameof(ReadyToShoot),1f / fireRate);
                    // Message msg = Message.Create(MessageSendMode.unreliable,(ushort)ServerToClientId.EnemyShoot);
                    // msg.Add(Id);
                    // msg.Add(1);
                    if (Physics.Raycast(tip.position, tip.forward, out var hit, 1000f,whatIsHittable))
                    {
                        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("ServerPlayer"))
                        {
                            ServerPlayer player = hit.transform.root.GetComponent<ServerPlayer>();
                        
                            player.TakeDamage((int) (10 * difficulty),Id,NetworkServerManager.Instance.CurrentTick,false,false,12); 
                        }
                    }
                    // msg.Add(hitPoint);
                    // NetworkManager.Instance.Server.SendToAll(msg);
                    break;
            
            }
        }


        IEnumerator GoAwake(float t)
        {
            yield return new WaitForSeconds(t);
            state = EJackState.Awake;
        }
    
        void ReadyToShoot()
        {
            readyToShoot = true;
        }
        private bool readyToShoot=false;
        public float fireRate=20;
        public Transform tip;

        void SendState()
        {
            Message message= Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.EnemyState);

            message.Add(Id);
                        
            message.Add((ushort) state);
                        
            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        void StopAttack()
        {
            readyToShoot = false;
            state = EJackState.Stop;
            target = null;
            StartCoroutine(GoAwake(1.2f));
        
            SendState();
        }

        // private float stopTimer = 1.2f;

        private float meleeLeftTime = 0f;

        private bool flag = true;

        private float traceTime = 0;
        void FindRoamingPos()
        {
            // Debug.Log(state);
            if (state != EJackState.Awake) return;
            state = EJackState.Roaming;
            Vector3 pos = _transform.position;
            roamingPos = new Vector3(Random.Range(pos.x - maxRange, pos.x + maxRange), pos.y,Random.Range(pos.z - maxRange, pos.z + maxRange));
        }
    
        NavMeshAgent agent;

        void GetNavMeshPos()
        {
            if ( target != null )
                targetPos = target.position;
            // agent.enabled = true;
            //
            // if (agent.isOnNavMesh)
            // {
            //     NavMeshPath path = new NavMeshPath();
            //     Vector3  targetPos = target!=null? target.position : Vector3.zero;
            //     if (Physics.Raycast(targetPos, Vector3.down, out var hit, 1000, whatIsGround))
            //     {
            //         targetPos = hit.point;
            //     }
            //  
            //     Vector3 desiredPos = Vector3.zero;
            //     switch (state)
            //     {
            //         case EJackState.Idle:
            //             desiredPos = _transform.position;
            //             break;
            //         case EJackState.Roaming:
            //             desiredPos = roamingPos;
            //             break;
            //         case EJackState.Tracing:
            //             desiredPos = targetPos;
            //             break;
            //     }
            //
            //     if (desiredPos == this.targetPos) return;
            //     if (agent.CalculatePath(desiredPos, path))
            //     {
            //         agent.path = path;
            //     }
            // }
            //
            // this.targetPos = agent.steeringTarget;
            //
            // agent.enabled = false;

        }

        private float playerHeight;
        private Vector3 lastMoveSpeed;

        void ReadyToAttack()
        {
            flag = true;
        }
    
        private void OnCollisionEnter(Collision other)
        {
        
        
            int layer = other.gameObject.layer;

            if (flag&&layer == LayerMask.NameToLayer("ServerPlayer"))
            {
                ServerPlayer player = other.transform.root.GetComponent<ServerPlayer>();
            
                player.TakeDamage((int)(meleeAttackDamage*difficulty),Id,NetworkServerManager.Instance.CurrentTick,false,false,12);
                flag = false;
                Invoke(nameof(ReadyToAttack),0.7f);
                return;
            }
        
            if (whatIsGround != (whatIsGround | (1 << layer)))
            {
                return;
            }

            for (int i = 0; i < other.contactCount; i++)
            {
                Vector3 normal = other.contacts[i].normal;
                if (IsWall(normal) )
                {
                    // Vector3 normalized = lastMoveSpeed.normalized;
                    // Vector3 vector = _transform.position + Vector3.up * 1f;
                    // // Debug.DrawLine(vector, vector + normalized * num, Color.blue, 10f);
                    // if (!Physics.Raycast(vector, normalized, num, whatIsGround) && Physics.Raycast(vector + normalized * num, Vector3.down, out var hitInfo, 3f, whatIsGround))
                    // {
                    //     Vector3 vector2 = hitInfo.point + Vector3.up * playerHeight * 0.5f;
                    //     _transform.position = vector2;
                    //     rb.velocity = lastMoveSpeed * 0.4f;
                    // }
                    
                    rb.AddForce(Vector3.up*jumpForce,ForceMode.Force);
                }
            }
        
        }
        private bool IsWall(Vector3 v)
        {
            return Math.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.1f;
        }
    }
}
