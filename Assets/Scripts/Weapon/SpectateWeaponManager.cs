using Menu;
using UnityEngine;
using UnityEngine.InputSystem;
using Weapon.Interface;

namespace Weapon
{
    public class SpectateWeaponManager : IWeaponManager
    {
        private WeaponManager _weaponManager;
        public SpectateWeaponManager(WeaponManager weaponManager)
        {
            _weaponManager = weaponManager;
        }
        
        public void Update()
        {
            if (!_weaponManager.CurrentWeapon || _weaponManager.CurrentWeapon.weaponType==Firearms.WeaponType.Knife || _weaponManager.CurrentWeapon.weaponType==Firearms.WeaponType.LaserGun)
            {
                if(GameUIManager.Instance) GameUIManager.Instance.bulletText.SetText("");
                return;
            }

            string color = "white";
            if (_weaponManager.CurrentWeapon.currentAmmo.GetValue() <= _weaponManager.CurrentWeapon.maxAmmo / 2) color = "yellow";
            if (_weaponManager.CurrentWeapon.currentAmmo.GetValue() <= _weaponManager.CurrentWeapon.maxAmmo / 6) color = "red";
            GameUIManager.Instance.bulletText.SetText($"<color={color}><size=40>{_weaponManager.CurrentWeapon.currentAmmo.GetValue()} </size></color>/ {_weaponManager.CurrentWeapon.maxAmmo}");
        }

        public void SwitchWeapon(int index)
        {
        }

        public void StartShoot()
        {
        }

        public void StopShoot()
        {
        }

        public void Shoot()
        {
        }

        public bool StartAim(bool input)
        {
            return !input;
        }

        public bool StopAim(bool input)
        {
            return !input;
        }

        public bool Reload()
        {
            return false;
        }

        public bool TryToSwitchWeapon()
        {
            return false;
        }

        public int NextWeapon(InputAction.CallbackContext ctx, int currentWeaponIndex)
        {
            return 0;
        }
    }
}