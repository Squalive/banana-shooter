using System.Collections.Generic;
using Manager;
using Multiplayer.Entity.Interface;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public class ServerObject : MonoBehaviour,IEntity
    {
        public static Dictionary<ushort, ServerObject> list = new Dictionary<ushort, ServerObject>();

        public ushort Id { get; private set; } = 0;
        private static ushort _maxAmount=150;
        private static ushort _nextId=1;

        private Transform _transform;
        private Rigidbody _rigidbody;

        private NetworkServerManager _networkManager;

        public  ObjectType type = ObjectType.None;

        [SerializeField] private bool continues = false;

        public void Awake()
        {
            _networkManager = NetworkServerManager.Instance; 
            
            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();
            Init();
        }
        public void Destroy()
        {
            Destroy(gameObject);
        }

        public bool IsEnemy()
        {
            return false;
        }

        public bool IsPlayer()
        {
            return false;
        }
        public bool IsVoid()
        {
            return false;
        }

        public bool IsThrowable()
        {
            return false;
        }
        private bool _init = false;
        void Init()
        {
            if (_init) return;
            _init = true;
            // if (list.Count >= _maxAmount)
            // {
            //     Destroy(gameObject);
            //     return;
            // }
            Id = _nextId;
            _nextId++;
            list.Add(Id,this);
            GameManager.Entities.Add(gameObject);

        
            SendSpawn();
        
            // 
            // InvokeRepeating(nameof(SendVel),0.2f,0.3f);
        
            if(!continues)
                Destroy(gameObject,8f);
        
            ready = continues;
        }
        void SendSpawn()
        {
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.ObjectSpawn);

            message.Add(Id);
            message.Add((int) type);
            message.Add(_transform.position);
        
            _networkManager.Server.SendToAll(message);
        }
        
        public void SendSpawn(ushort id)
        {
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.ObjectSpawn);

            message.Add(Id);
            message.Add((int) type);
            message.Add(_transform.position);
        
            _networkManager.Server.Send(message,id);
        }
        private void Update()
        {
            if(_transform.position.y<=-300f) Destroy(gameObject);
        }
        
        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
            {
                list.Remove(Id);
                Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.ObjectDestroy);

                message.Add(Id);
        
                _networkManager.Server.SendToAll(message);
            }
        }

        private static float lastTime = 0;
        private static float betweenTime = 0.2f;
        [MessageHandler((ushort) ClientToServerId.RequestSpawnObj, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void RequestSpawn(ushort fromClient, Message message)
        {
            if (Time.time-lastTime<betweenTime) return;
            lastTime = Time.time;
            ObjectType type = (ObjectType) message.GetInt();
            Vector3 pos = message.GetVector3();
            int times = message.GetInt();
            if (times > 50) times = 50;
            for (int i = 0; i < times; i++)
            {
                if (list.Count >= _maxAmount)
                {
                    break;
                }
                Rigidbody rb;
                switch (type)
                {
                    case ObjectType.SodaCan:
                        rb = Instantiate(PrefabManager.Instance.GetPrefab("ServerSodaCan"), pos, Quaternion.identity).GetComponent<Rigidbody>();
                
                        Vector3 dir = message.GetVector3();
                
                        rb.AddForce(dir*0.000001f,ForceMode.Impulse);
                        break;
                    case ObjectType.Book:
                        Instantiate(PrefabManager.Instance.GetPrefab("server_book"), pos, Quaternion.identity);
                        break;
                    case ObjectType.Crate:
                        Instantiate(PrefabManager.Instance.GetPrefab("ServerCrate"), pos, Quaternion.identity);

                        break;
                    case ObjectType.Package:
                        Instantiate(PrefabManager.Instance.GetPrefab("ServerPackage"), pos, Quaternion.identity);
                        break;
                }

                pos += Vector3.up * 2f;
            }
            
        }
        private void FixedUpdate()
        {
            if (!_rigidbody.IsSleeping())
            {
                SendMovement();
            }
        }

        void SendMovement()
        {
        
            Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ServerToClientId.ObjectMovement);

            message.Add(Id);
            message.Add(_transform.position);
            message.Add(_transform.rotation);
            message.Add(_rigidbody.velocity);
        
            _networkManager.Server.SendToAll(message);
        }

        private bool ready = true;
        private void OnCollisionEnter(Collision other)
        {
            if (ready && other.gameObject.layer!=LayerMask.NameToLayer("ServerPlayer"))
            {
                ready = false;
                Invoke(nameof(BeReady),0.1f);
                
                Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ServerToClientId.ObjHitWall);
                message.Add(other.contacts[0].point);
                _networkManager.Server.SendToAll(message);
            }
        }

        void BeReady()
        {
            ready = true;
        }
    }

    public enum ObjectType
    {
        None,
        SodaCan,
        Book,
        Crate,
        Package,
    }
}