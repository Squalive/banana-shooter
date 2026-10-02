namespace Multiplayer.Entity.Interface
{
    public interface IEntity
    {
        ushort Id { get; }

        void Destroy();

        bool IsEnemy();

        bool IsPlayer();

        bool IsVoid();

        bool IsThrowable();
    }
}