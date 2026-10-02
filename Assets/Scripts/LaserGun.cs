
using Audio;
using UnityEngine;
using Weapon;

public class LaserGun : Firearms
{

    protected override void Shoot()
    {
        AudioManager.Instance.StopGunReload();
        AudioManager.Instance.PlayGunReload(hitClip);
        Invoke("StopGunSound",0.2f);
    }

    void StopGunSound()
    {AudioManager.Instance.StopGunReload();
        
    }
    protected override void Reload()
    {
    }

    protected override void Aim()
    {
    }

    public AudioClip hitClip;
}
