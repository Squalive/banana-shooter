
using Multiplayer.Entity.Client.Enemy.Animation;

namespace Multiplayer.Entity.Client.Enemy
{
    public class ClientKat : ClientEnemy
    {
        private void Start()
        {
            EnemyAnimation = new KatAnimation(enemyState.anim);
        }

        private void Update()
        {
            EnemyAnimation.Update();
        }
    }
}
