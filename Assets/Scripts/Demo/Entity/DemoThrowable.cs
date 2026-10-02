using Menu;
using Multiplayer.Entity.Client;
using UnityEngine;

namespace Demo.Entity
{
    public class DemoThrowable : DemoEntity
    {
        public int PlayerId { get; private set; }
        
        [SerializeField] private Throwable throwable;

        private Vector3 _lastVel;

        private bool _velSet;

        private int _tick = 0;
        public override void Spawn(int id, params object[] obj)
        {
            base.Spawn(id, obj);
            _tick = 0;

            if (DemoManager.Replaying)
            {
                throwable.Initialize((ThrowObjectMenu.ThrowObjectType)obj[0], (Vector3)obj[1], false, (Collider)obj[2]);
            }
        }

        public override bool IsThrowable()
        {
            return true;
        }

        public override void Destroy(params object[] objects)
        {
            bool visualEffect = (bool)objects[0];
            if (!IsDestroyed)
            {
                IsDestroyed = true;
                if(visualEffect)
                    throwable.Explode(GetTransform().position, false);
                gameObject.SetActive(false);
            }
        }

        public void TryToSetLastVelocity()
        {
            if (!_velSet)
            {
                _lastVel = rb.velocity;
                CancelInvoke(nameof(SetRbToKinematic));
                Invoke(nameof(SetRbToKinematic), Time.fixedDeltaTime * Time.timeScale);
                _velSet = true;
            }
        }

        public void TryToSetCurrentVelocity()
        {
            if (_velSet  && DemoManager.ReplayTick > _tick)
            {
                _velSet = false;
                rb.isKinematic = false;
                rb.velocity = _lastVel;
                _tick = DemoManager.ReplayTick;
            }
        }

        void SetRbToKinematic()
        {
            rb.isKinematic = true;
        }
    }
}