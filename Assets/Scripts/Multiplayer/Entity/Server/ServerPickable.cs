using System.Collections.Generic;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public enum PickableType
    {
        None=0,
        Weapon,
        
    }
    public class ServerPickable : MonoBehaviour
    {
        public static Dictionary<ushort, ServerPickable> list = new Dictionary<ushort, ServerPickable>();

        public PickableType pickableType = PickableType.None;
        public ushort Id { get; private set; }

        public static ushort MaxAmount = 300;

        private Transform _transform;

        [SerializeField] private int objectIndex=0;

        private Rigidbody _rb;
        private void Start()
        {
            if (!NetworkServerManager.Instance.Server.IsRunning)
            {
                Destroy(gameObject);
                return;
            }
            for (ushort i = 0; i < MaxAmount; i++)
            {
                if (!list.ContainsKey(i))
                {
                    Id = i;
                    break;
                }
            }

            _rb = GetComponent<Rigidbody>();
            _transform = transform;
            

            SendSpawn();
            
            list.Add(Id,this);
            InvokeRepeating(nameof(SendPos),0.2f,0.2f);
            InvokeRepeating(nameof(SendRot),0.2f,0.2f);
        }
        
        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
                list.Remove(Id);
            
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.PickableDestroy);

            message.Add(Id);
        
            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        private void FixedUpdate()
        {
            if(_transform.position.y<=-300f) Destroy(gameObject);
            
        }

        void SendPos()
        {
            if (_rb.IsSleeping()) return;
            Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ServerToClientId.PickablePos);
            message.Add(Id);
            message.Add(_transform.position);
            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        void SendRot()
        {
            if (_rb.IsSleeping()) return;
            Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ServerToClientId.PickableRot);
            message.Add(Id);
            message.Add(_transform.rotation);
            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        void SendSpawn()
        {
            Message message =
                GetSpawnData(Message.Create(MessageSendMode.Reliable, (ushort) ServerToClientId.PickableSpawned));

            
            NetworkServerManager.Instance.Server.SendToAll(message);
        }

        Message GetSpawnData(Message message)
        {
            message.Add(Id);
            message.Add((int) pickableType);
            message.Add(_transform.position);
            message.Add(objectIndex);
            // switch (pickableType)
            // {
            //     case PickableType.Weapon:
            //         break;
            // }
            //
            return message;
        }
        
        public void SendSpawn(ushort toClient)
        {
            Message message = GetSpawnData(Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.PickableSpawned));

            
        
            NetworkServerManager.Instance.Server.Send(message,toClient);
        }

        [MessageHandler((ushort) ClientToServerId.PickPickable, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        static void PickPickable(ushort fromClient, Message message)
        {
            ushort id = message.GetUShort();
            if (list.TryGetValue(id, out var serverPickable))
            {
                switch (serverPickable.pickableType)
                {
                    case PickableType.Weapon:
                        if (ServerPlayer.list.TryGetValue(fromClient, out var serverPlayer))
                        {
                            // short[] w = serverPlayer.Weapons;
                            // w[serverPlayer.CurrentWeaponIndex] =(short)serverPickable.objectIndex;
                            //
                            // serverPlayer.ProcessWeapon(w);
                            //
                            // Message msg = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.PickupWeapon);
                            // msg.Add(fromClient);
                            // msg.Add(serverPlayer.CurrentWeaponIndex);
                            // msg.Add(serverPickable.objectIndex);
                            // NetworkServerManager.Instance.Server.SendToAll(msg);
                        }
                        break;
                }
                
                Destroy(serverPickable.gameObject);
            }
        }
    }
}