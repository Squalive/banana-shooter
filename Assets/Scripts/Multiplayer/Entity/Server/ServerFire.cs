using Multiplayer.Entity.Interface;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public class ServerFire : MonoBehaviour
    {
        public ushort PlayerId { get;private set; } = 0;
    
        private static ushort nextId = 0;

        public void Initialize(ushort playerId)
        {
            PlayerId = playerId;
        
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.FireInit);

            message.Add(transform.position);
        
            NetworkServerManager.Instance.Server.SendToAll(message);
        
            Destroy(gameObject,3);
        }


        private bool ready = true;

        void GetReady()
        {
            ready = true;
        }

        private Collider[] cols = new Collider[5];
        private void FixedUpdate()
        {
            if (ready)
            {
                int cnt = Physics.OverlapSphereNonAlloc(transform.position, 2f,cols);

                for (int i = 0; i < cnt; i++)
                {
                    Transform root = cols[i].transform.root;
                    IDamageable damageable = root.GetComponent<IDamageable>();

                    if (damageable != null)
                    {
                        damageable.TakeDamage(5,PlayerId,NetworkServerManager.Instance.CurrentTick,false,false,1003);
                        ready = false;
                        Invoke(nameof(GetReady),0.5f);
                    }
                }
            }

        
        }
    }
}
