using UnityEngine;

namespace Multiplayer.Entity.Client.Enemy.Animation
{
    public abstract class EnemyAnimation
    {
        protected Animator Animator;

        protected Vector3 Input,InputInterpolate;

        protected EnemyAnimation(Animator animator)
        {
            Animator = animator;
        }

        public void SetInput(Vector3 input)
        {
            Input = input;
        }

        public virtual void Update()
        {
            InputInterpolate = Vector3.Slerp(InputInterpolate, Input, Time.deltaTime * 15f);
        }
    }
}