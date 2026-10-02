using System.Collections.Generic;
using Multiplayer.Entity.Interface;
using Multiplayer.Entity.Server;

namespace Multiplayer.Entity
{
    public class Entity
    {
        public const ushort MaxEntityAmount = 3000;
        
        public readonly Dictionary<ushort, IEntity> Entities = new();

        public Entity()
        {
            Entities.Add(MaxEntityAmount,new ServerVoid());
        }

        public Dictionary<ushort, IEntity> GetAll()
        {
            return Entities;
        }

        public IEntity GetEntity(ushort id)
        {
            if (Entities.TryGetValue(id, out var entity))
            {
                return entity;
            }
            return null;
        }
    }
}