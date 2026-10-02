
using Manager;
using Multiplayer.Entity.Server;
using Multiplayer.Interface;
using Multiplayer.Server;
using Pool;
using UnityEngine;

namespace Multiplayer
{
    public class ObjectSpawner : MonoBehaviour
    {
        
        private GameObject _obj;

        [SerializeField] private ObjectType objectType=ObjectType.None;
        private Transform _spawn;



        private void Start()
        {
            if(!NetworkServerManager.Instance.Server.IsRunning) Destroy(this);
            _spawn = transform;
            InvokeRepeating(nameof(SpawnObj),5f,15f);
        }

        void SpawnObj()
        {
            if (_obj != null) return;

            Rigidbody rb;
            switch (objectType)
            {
                case ObjectType.SodaCan:
                    rb = Instantiate(PrefabManager.Instance.GetPrefab("ServerSodaCan"), _spawn.position, Quaternion.identity).GetComponent<Rigidbody>();

                    Vector3 dir = _spawn.forward;
                
                    rb.AddForce(dir*8f,ForceMode.Impulse);
                    break;
                case ObjectType.Book:
                    Instantiate(PrefabManager.Instance.GetPrefab("server_book"),  _spawn.position, Quaternion.identity);
                    break;
                case ObjectType.Crate:
                    _obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerCrate"),  _spawn.position, Quaternion.identity);

                    
                    break;
                case ObjectType.Package:
                    _obj = Instantiate(PrefabManager.Instance.GetPrefab("ServerPackage"),  _spawn.position, Quaternion.identity);

                    
                    break;
                
                default:
                    break;
            }
        }
    }
}
