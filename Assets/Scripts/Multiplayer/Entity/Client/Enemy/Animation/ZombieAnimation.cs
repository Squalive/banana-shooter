using UnityEngine;

namespace Multiplayer.Entity.Client.Enemy.Animation
{
    public class ZombieAnimation : EnemyAnimation
    {
        private static readonly int Running = Animator.StringToHash("running");

        public ZombieAnimation(Animator animator) : base(animator)
        {
            
        }

        public override void Update()
        {
            base.Update();
            
            Animator.SetBool(Running,InputInterpolate.sqrMagnitude > 0.1f);
        }
    }
}