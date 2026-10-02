using UnityEngine;

namespace Multiplayer.Entity.Client.Enemy.Animation
{
    public class JackAnimation : EnemyAnimation
    {
        private static readonly int Y = Animator.StringToHash("y");
        private static readonly int Open = Animator.StringToHash("open");

        public JackAnimation(Animator animator) : base(animator)
        {
        }

        public override void Update()
        {
            base.Update();
            
            Animator.SetFloat(Y,Mathf.Abs(InputInterpolate.z+InputInterpolate.x));
        }

        public void SetDoor(bool flag)
        {
            Animator.SetBool(Open,flag);
        }
    }
}