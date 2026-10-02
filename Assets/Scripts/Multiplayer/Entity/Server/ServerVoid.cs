using Multiplayer.Entity.Interface;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public class ServerVoid : IEntity
    {
        public ushort Id { get; } = Entity.MaxEntityAmount;
        
        public void Destroy()
        {
            Debug.LogError("You cant destroy void");
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
            return true;
        }

        public bool IsThrowable()
        {
            return false;
        }
    }
}