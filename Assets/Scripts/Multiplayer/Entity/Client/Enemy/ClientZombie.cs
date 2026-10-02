
using Multiplayer.Entity.Client.Enemy.Animation;

namespace Multiplayer.Entity.Client.Enemy
{
    public class ClientZombie : ClientEnemy
    {

        private void Start()
        {
            EnemyAnimation = new ZombieAnimation(enemyState.anim);
        }

        private void Update()
        {
            EnemyAnimation.Update();
        }
    }
}
