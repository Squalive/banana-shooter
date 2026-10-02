using System.Collections.Generic;
using Audio;
using Manager;
using Multiplayer.Entity.Server;
using Multiplayer.Server;
using Riptide;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Multiplayer.Client
{
    public class ClientObject : MonoBehaviour
    {
        public static Dictionary<ushort, ClientObject> list = new Dictionary<ushort, ClientObject>();

        public ushort Id;

        public ObjectType type;
    
        private Transform _transform;
        private Rigidbody _rigidbody;

        private Vector3 desiredPos;
        private Quaternion desiredRot;
        void Initialize(ushort id)
        {
            Id = id;
            list.Add(Id,this);

            _transform = transform;
            _rigidbody = GetComponent<Rigidbody>();

            var position = _transform.position;
            desiredPos = position;
            desiredRot = _transform.rotation;
        
            AudioManager.Instance.SoundEffect3D("tip",position);
        
        }

        private void Update()
        {
            if (Vector3.Distance(_transform.position, desiredPos) > 0.1f)
            {
                _transform.position = Vector3.Lerp(_transform.position,desiredPos,Time.deltaTime*10f);
            }
            _transform.rotation = Quaternion.Lerp(_transform.rotation,desiredRot,Time.deltaTime*10f);
        }

        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
                list.Remove(Id);
        }
        [MessageHandler((ushort) ServerToClientId.ObjectMovement, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ObjectMovement(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var clientObject))
            {
                Vector3 pos = message.GetVector3();

                clientObject.desiredPos = pos;
                
                Quaternion rot = message.GetQuaternion();

                clientObject.desiredRot = rot;

                Vector3 vel = message.GetVector3();

                clientObject._rigidbody.velocity = vel;
            }
        }
        [MessageHandler((ushort) ServerToClientId.ObjectDestroy, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ObjectDestroy(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var clientObject))
            {
                Destroy(clientObject.gameObject);
            }
        }
        [MessageHandler((ushort) ServerToClientId.ObjHitWall, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ObjHitWall(Message message)
        {
            Vector3 point = message.GetVector3();

            string n = "hit" + Random.Range(0, 5);
            AudioManager.Instance.SoundEffect3D(n,point);
        }
        [MessageHandler((ushort) ServerToClientId.ObjectSpawn, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ObjectSpawn(Message message)
        {
            ushort id = message.GetUShort();
            ObjectType type = (ObjectType)message.GetInt();
            Vector3 pos = message.GetVector3();
            ClientObject o;
            if (list.ContainsKey(id))
            {
                //
                Destroy(list[id].gameObject);
                list.Remove(id);
                return;
            }
            switch (type)
            {
                case ObjectType.SodaCan:
                    o = Instantiate(PrefabManager.Instance.GetPrefab("ClientSodaCan"), pos,
                        Quaternion.identity).GetComponent<ClientObject>();
                
                    o.Initialize(id);
                    break;
                case ObjectType.Book:
                    o = Instantiate(PrefabManager.Instance.GetPrefab("client_book"), pos,
                        Quaternion.identity).GetComponent<ClientObject>();
                
                    o.Initialize(id);
                    break;
                case ObjectType.Crate:
                    o = Instantiate(PrefabManager.Instance.GetPrefab("ClientCrate"), pos,
                        Quaternion.identity).GetComponent<ClientObject>();
                
                    o.Initialize(id);
                    break;
                case ObjectType.Package:
                    o = Instantiate(PrefabManager.Instance.GetPrefab("ClientPackage"), pos,
                        Quaternion.identity).GetComponent<ClientObject>();
                
                    o.Initialize(id);
                    break;
                default:
                    break;
            }
        }
    }
}
