
using Multiplayer.Entity.Client.Enemy.Animation;
namespace Multiplayer.Entity.Client.Enemy
{
    public class ClientJack : ClientEnemy
    {
        private void Start()
        {
            EnemyAnimation = new JackAnimation(enemyState.anim);
        }

        private void Update()
        {
            EnemyAnimation.Update();
        }
    }
}
