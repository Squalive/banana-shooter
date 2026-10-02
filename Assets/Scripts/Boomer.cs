

using Audio;
using Menu;
using Multiplayer;
using Riptide;
using UnityEngine;
using Weapon;

public class Boomer : Firearms
{
    protected override void Shoot()
    {
        if (!stat.specialWeapon)
            return;
        
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
        if(reload)
            AudioManager.Instance.PlayGunReload(reload,Mathf.Abs(reloadMultiplier - 1f) > 0.1f ? 1.6f : 1f);
    }

    protected override void Aim()
    {
    }

}
