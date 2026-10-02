
using System.Collections;
using Audio;
using UnityEngine;
using Weapon;

public class ShotGun : Firearms
{
    protected override void Shoot()
    {
        WeaponManager.Instance.CurrentPlayer.rb.AddForce(-PlayerCam.transform.forward.normalized*1000,ForceMode.Acceleration);
    }

    protected override void Reload()
    {
        spinAmount = maxAmmo - currentAmmo.GetValue();
        reloadTime = spinAmount * normalReloadTime;
        StartCoroutine(ReloadSound());
    }

    protected override void Aim()
    {
        
    }

    IEnumerator ReloadSound()
    {
        // One shell per spin pass. Firearms.DoReload scales the whole spin with
        // reloadMultiplier (and spinReloadPercantage), so the sound has to use the
        // same interval or it keeps clicking after the gun is already reloaded.
        float shellInterval = normalReloadTime * reloadMultiplier * spinReloadPercantage;

        // A zero interval would spin the loop at frame rate.
        if (shellInterval <= 0.01f) shellInterval = 0.01f;

        for (int i = 0; i < spinAmount; i++)
        {
            AudioManager.Instance.PlayGunReload(reload,Mathf.Abs(reloadMultiplier - 1f) > 0.1f ? 1.6f : 1f);
            yield return new WaitForSeconds(shellInterval);
        }
    }
}