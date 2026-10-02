namespace Multiplayer.Interface
{
    public interface IServerType
    {
        public float OverrideGameTime(float defaultTime);

        public bool GetEliminated();

        public ushort OverrideMinimalPlayerCount(ushort defaultMinimalPlayerCount);
    }
}