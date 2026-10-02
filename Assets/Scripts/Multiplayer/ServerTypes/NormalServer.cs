using Multiplayer.Interface;

namespace Multiplayer.ServerTypes
{
    public class NormalServer : IServerType
    {
        public float OverrideGameTime(float defaultTime)
        {
            return defaultTime;
        }

        public bool GetEliminated()
        {
            return false;
        }

        public ushort OverrideMinimalPlayerCount(ushort defaultMinimalPlayerCount)
        {
            return defaultMinimalPlayerCount;
        }
    }
}