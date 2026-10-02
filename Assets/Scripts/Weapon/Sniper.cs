using Audio;
using Menu;
using UnityEngine;

namespace Weapon
{
    public class Sniper : Firearms
    {
        private static readonly int Reset = Animator.StringToHash("Reset");

        public override void Select(bool doAnimation)
        {
            base.Select(doAnimation);

            if (GameUIManager.Instance)
            {
                GameUIManager.Instance.crossHair.SetActive(false);
            }
        }
        
        

        protected override void Shoot()
        {
            Invoke(nameof(ResetState),1 / fireRate /2f);
        }

        protected override void Reload()
        {
            reloadTime = normalReloadTime;
            if(reload)
                AudioManager.Instance.PlayGunReload(reload,Mathf.Abs(reloadMultiplier - 1f) > 0.1f ? 1.6f : 1f);
        }

        protected override void Aim()
        {
            WeaponManager.Instance.cams[1].cullingMask = WeaponManager.Instance.weaponCamAimingLayer;
            GameUIManager.Instance.gameScene.SetActive(false);
            GameUIManager.Instance.scope.SetActive(true);
        
            if(trail)
                Destroy(trail.gameObject);
        }

        void ResetState()
        {
            animator.SetTrigger(Reset);
            if(reset!=null && !semiAuto)
                AudioManager.Instance.PlayGunReset(reset);
        }
    }
}
