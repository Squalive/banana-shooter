
using System.Collections.Generic;
using Multiplayer.Entity.Interface;
using Riptide;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Multiplayer.Entity.Server
{
    public class ServerJumpPad : MonoBehaviour, IEntity
    {
        public static Dictionary<ushort, ServerJumpPad> List = new();

        public ushort Id { get; private set; }

        private Transform _transform;

        private float _spawnedTime;

        public void Initialize()
        {
            _transform = transform;
            
            Id = NetworkServerManager.GetId();
            
            NetworkServerManager.AddEntity(Id,this);

            _spawnedTime = Time.time;
            
            Destroy(gameObject, 30f);

            SendSpawnToAll();
        }

        private void SendSpawn(ushort id)
        {
            NetworkServerManager.Instance.Server.Send(GetSpawnMessage(), id);
        }

        private void SendSpawnToAll()
        {
            NetworkServerManager.Instance.Server.SendToAll(GetSpawnMessage());
        }

        private Message GetSpawnMessage()
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.JumpPadSpawned);

            message.AddUShort(Id);
            message.AddVector3(_transform.position);
            message.AddQuaternion(_transform.rotation);
            message.AddFloat(30 - Time.time + _spawnedTime);
            
            return message;
        }

        private void OnDestroy()
        {
            NetworkServerManager.RemoveEntity(Id);
        }

        public void Destroy()
        {
            Object.Destroy(gameObject);
        }

        public bool IsEnemy() => false;
        
        public bool IsPlayer() => false;
        
        public bool IsVoid() => true;
        
        public bool IsThrowable() => false;
    }
}