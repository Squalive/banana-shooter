using UnityEngine;

namespace Multiplayer.Entity.Client.Enemy.Animation
{
    public class KatAnimation : EnemyAnimation
    {
        private static readonly int X = Animator.StringToHash("x");
        private static readonly int Y = Animator.StringToHash("y");
        public KatAnimation(Animator animator) : base(animator)
        {
        }

        public override void Update()
        {
            base.Update();
            
            Animator.SetFloat(X, InputInterpolate.x);
            Animator.SetFloat(Y, InputInterpolate.z);
        }
    }
}