using Audio;
using UnityEngine;

namespace Weapon
{
    public class DoubleBarrel : Firearms
    {
        private float normalRate;
        public float rate = 20f;

        protected override void Start()
        {
            base.Start();
            normalRate = fireRate;
        }

        protected override void Shoot()
        {
            fireRate = rate;
            Invoke(nameof(ResetRate),0.2f);
            WeaponManager.Instance.CurrentPlayer.rb.AddForce(-PlayerCam.transform.forward.normalized*1000,ForceMode.Acceleration);
        }

        void ResetRate()
        {
            fireRate = normalRate;
        }

        protected override void Reload()
        {
            if(reload)
                AudioManager.Instance.PlayGunReload(reload,Mathf.Abs(reloadMultiplier - 1f) > 0.1f ? 1.6f : 1f);
        }

        protected override void Aim()
        {
        }
    }
}
