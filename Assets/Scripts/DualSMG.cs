

using Menu;
using Multiplayer;
using Riptide;
using UnityEngine;
using Weapon;

public class DualSMG : Firearms
{
    public Transform lTip, rTip;
    public ParticleSystem lPart, rPart;

    private bool left = false;
    protected override void Shoot()
    {
        muzzlePoint = left ? rTip : lTip;
        muzzleParticle = left ? rPart : lPart;

        left = !left;
        
        if (currentAmmo.GetValue() <= 0)
        {
            Invoke(nameof(Disable),0.4f);
        }
    }
    
    void Disable()
    {
        WeaponManager.Instance.CurrentWeaponIndex = 0;

        WeaponManager.Instance.DisableSpecialWeapon();
        
        Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.SpecialWeaponDisable);
        
        NetworkManager.Instance.SendByte += message.WrittenLength;
        NetworkManager.Instance.Client.Send(message);
    }

    protected override void Reload()
    {
    }

    protected override void Aim()
    {
    }

}
