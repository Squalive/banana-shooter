
namespace Pool
{
    public interface IPooledObject
    {
        void OnObjectSpawn();
        void OnObjectInit();
    }
}