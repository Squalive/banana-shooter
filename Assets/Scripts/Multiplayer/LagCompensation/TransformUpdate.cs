using UnityEngine;

namespace Multiplayer.LagCompensation
{
    public class TransformUpdate
    {
        public uint Tick { get; private set; }
        public bool IsTeleport { get; private set; }
        public Vector3 Position { get; private set; }

        public TransformUpdate(uint tick, bool isTeleport, Vector3 position)
        {
            Tick = tick;
            IsTeleport = isTeleport;
            Position = position;
        }
    }
}