using Multiplayer.Entity.Server.Enemy;

namespace Multiplayer.Entity.Client.Enemy.State
{
    public class ZombieState : EnemyState
    {
        public ServerZombie.EZombieState state = ServerZombie.EZombieState.Idle;

        public void SetState(ServerZombie.EZombieState eZombieState)
        {
            state = eZombieState;
        }
    }
}