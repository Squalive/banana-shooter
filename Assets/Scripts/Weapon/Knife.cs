using System.Collections;
using System.Collections.Generic;
using EZCameraShake;
using UnityEngine;
using Weapon;

public class Knife : Firearms
{
    protected override void Shoot()
    {
        CameraShaker.Instance.ShakeOnce(2, 1f, 0.1f, 0.5f);
        ClearTrail();
    }

    protected override void Reload()
    {
    }

    protected override void Aim()
    {
    }


    public TrailRenderer trailRenderer;

    void ClearTrail()
    {
        if(trailRenderer!=null)
            trailRenderer.Clear();
    }
}
