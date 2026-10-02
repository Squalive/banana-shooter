using UnityEngine.InputSystem;

namespace Weapon.Interface
{
    public interface IWeaponManager
    {
        void Update();

        /// <summary>
        /// Use for switch weapon (locally)
        /// </summary>
        /// <param name="index">The weapon you wanna switch</param>
        void SwitchWeapon(int index);

        void StartShoot();
        void StopShoot();
        void Shoot();

        bool StartAim(bool input);
        bool StopAim(bool input);

        bool Reload();

        bool TryToSwitchWeapon();

        int NextWeapon(InputAction.CallbackContext ctx,int currentWeaponIndex);
    }
}