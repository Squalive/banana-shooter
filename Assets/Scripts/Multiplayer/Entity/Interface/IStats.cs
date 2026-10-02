namespace Multiplayer.Entity.Interface
{
    public interface IStats
    {
        ushort Kills { get; set; }
        
        ushort Deaths { get; set; }
    }
}