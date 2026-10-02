
using Audio;
using UnityEngine;
using Weapon;

public class AssaultRifle : Firearms
{
    protected override void Shoot()
    {
        
    }

    protected override void Reload()
    {
        reloadTime = normalReloadTime;
        if(reload)
            AudioManager.Instance.PlayGunReload(reload,Mathf.Abs(reloadMultiplier - 1f) > 0.1f ? 1.6f : 1f);
    }

    protected override void Aim()
    {
        
    }
}
