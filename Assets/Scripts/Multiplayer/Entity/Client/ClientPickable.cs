using System.Collections.Generic;
using Manager;
using Multiplayer.Entity.Server;
using Multiplayer.Server;
using Riptide;
using UnityEngine;

namespace Multiplayer.Client
{
    public class ClientPickable : MonoBehaviour
    {
        public static Dictionary<ushort, ClientPickable> list = new Dictionary<ushort, ClientPickable>();
        public ushort Id { get; private set; }
        public PickableType pickableType = PickableType.None;

        private Transform _transform;
        [HideInInspector]
        public int ObjectIndex { get; private set; }
        void Initialize(ushort id,int index)
        {
            Id = id;
            ObjectIndex = index;

            _transform = transform;
        
            list.Add(Id,this);
        }
        
        private void OnDestroy()
        {
            if (list.ContainsKey(Id))
                list.Remove(Id);
        }
        private void FixedUpdate()
        {
            if(_transform.position.y<=-300f) Destroy(gameObject);
        }
        
        [MessageHandler((ushort) ServerToClientId.PickableSpawned, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void ObjectSpawn(Message message)
        {
            ushort id = message.GetUShort();
            PickableType type = (PickableType)message.GetInt();
            Vector3 pos = message.GetVector3();
            ClientPickable o;
            int objectIndex = message.GetInt();
            switch (type)
            {
                case PickableType.Weapon:
                    string weaponName = NetworkManager.Instance.weaponInfo[objectIndex].weaponName;
                    o = Instantiate(PrefabManager.Instance.GetPrefab("client_pick_" + weaponName), pos,
                        Quaternion.identity).GetComponent<ClientPickable>();
                    
                    o.Initialize(id,objectIndex);
                    break;
                default: break;
            }
        }
        
        [MessageHandler((ushort) ServerToClientId.PickablePos, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PickablePos(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var clientObject))
            {
                Vector3 pos = message.GetVector3();

                clientObject._transform.position = pos;
            }
        }
        [MessageHandler((ushort) ServerToClientId.PickableRot, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PickableRot(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var clientObject))
            {
                Quaternion rot = message.GetQuaternion();

                clientObject._transform.rotation = rot;
            }
        }
        [MessageHandler((ushort) ServerToClientId.PickableDestroy, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
        private static void PickableDestroy(Message message)
        {
            ushort id = message.GetUShort();

            if (list.TryGetValue(id, out var clientObject))
            {
                Destroy(clientObject.gameObject);
            }
        }
    }
}