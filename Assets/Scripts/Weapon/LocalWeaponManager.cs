
using CodingDaniel.MapEditor.UI;
using Demo;
using Manager;
using Menu;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using Riptide;
using UnityEngine;
using UnityEngine.InputSystem;
using Weapon.Interface;

namespace Weapon
{
    public class LocalWeaponManager : IWeaponManager
    {
        private WeaponManager _weaponManager;
        
        private PlayerState _currentPlayer;
        
        private bool _shooting;
        private float _lastSwitchTime = 0;

        private bool _aiming = false;

        public LocalWeaponManager(WeaponManager weaponManager,PlayerState currentPlayer)
        {
            _weaponManager = weaponManager;
            _currentPlayer = currentPlayer;
        }

        public void Update()
        {
            if (NetworkManager.Instance.CantPlay())
            {
                return;
            }
            
            if (!_weaponManager.CurrentWeapon || _weaponManager.CurrentWeapon.weaponType==Firearms.WeaponType.Knife || _weaponManager.CurrentWeapon.weaponType==Firearms.WeaponType.LaserGun)
            {
                if(GameUIManager.Instance) GameUIManager.Instance.bulletText.SetText("");
                return;
            }

            if (_weaponManager.CurrentWeapon.allowHolding && _shooting)
            {
                _weaponManager.CurrentWeapon.DoAttack();
            }

            string color = "white";
            if (_weaponManager.CurrentWeapon.currentAmmo.GetValue() <= _weaponManager.CurrentWeapon.maxAmmo / 2) color = "yellow";
            if (_weaponManager.CurrentWeapon.currentAmmo.GetValue() <= _weaponManager.CurrentWeapon.maxAmmo / 6) color = "red";
            GameUIManager.Instance.bulletText.SetText($"<color={color}><size=40>{_weaponManager.CurrentWeapon.currentAmmo.GetValue()} </size></color>/ {_weaponManager.CurrentWeapon.maxAmmo}");
        }
        
        public void SwitchWeapon(int index)
        {
            _shooting = false;

            //TODO: Set the client player variable
            if(ClientPlayer.LocalPlayer)
                ClientPlayer.LocalPlayer.playerState.WeaponManager.CurrentWeaponIndex = index;
            //Record if needed
            if (NetworkManager.Instance.Client.Connection != null)
            {
                DemoManager.Instance.AddPlayerWeaponSwitched(ClientPlayer.LocalPlayer.demoPlayer.Id,index);
            }

            if (_weaponManager.CurrentWeapon != null)
            {
                ClientPlayer.LocalPlayer.playerState.WeaponManager.CurrentWeapon = index < 3 ? ClientPlayer.LocalPlayer.playerState.WeaponManager.Weapon[index] : ClientPlayer.LocalPlayer.playerState.WeaponManager.SpecialWeapon;
            }
            else
            {
                ClientPlayer.LocalPlayer.playerState.WeaponManager.CurrentWeapon = null;
            }
        
            Message message = Message.Create(MessageSendMode.Reliable,(ushort)ClientToServerId.UpdateWeaponIndex);

            message.Add(index);
            NetworkManager.Instance.SendByte += message.WrittenLength;
        
            NetworkManager.Instance.Client.Send(message);
        }

        #region Input

        #region Shoot

        public void StartShoot()
        {
            if (_weaponManager.CurrentWeapon == null) return;
            if (!_weaponManager.CurrentWeapon.allowHolding) return;
            if (NetworkManager.Instance.CantPlay()) return;
            _shooting = true;
        }

        public void StopShoot()
        {
            if (_weaponManager.CurrentWeapon == null) return;
            if (!_weaponManager.CurrentWeapon.allowHolding) return;
            if (NetworkManager.Instance.CantPlay()) return;
            _shooting = false;
        }

        public void Shoot()
        {
            if (_weaponManager.CurrentWeapon == null) return;
            if (_weaponManager.CurrentWeapon.allowHolding) return;
            if (NetworkManager.Instance.CantPlay()) return;
            _weaponManager.CurrentWeapon.DoAttack();
        }

        #endregion

        #region Aim

        public bool StartAim(bool input)
        {
            if (TabHolder.Instance != null) return false;
            if (NetworkManager.Instance.CantPlay()) return false;
            if (_aiming) return false;
            if (ClientPlayer.LocalPlayer.Dead) return false;
            if (GameUIManager.Instance && GameUIManager.Instance.scoreBoard.activeSelf) return false;
            if (_weaponManager.CurrentWeapon == null || _currentPlayer.grappling.hitGrapplePoint) return false;
            if (!_weaponManager.CurrentWeapon.allowToAim || _weaponManager.CurrentWeapon.isReload) return false;
            
            PlayerMovement.Instance.sensMultiplier = GameManager.Instance.setting.aimSensitivityMultiplier;
            
            _aiming = true;
            
            SendAim(true);

            return true;
        }

        public bool StopAim(bool input)
        {
            if (!_aiming) return false;
            // Don't bail out while the game can't be played (e.g. the pause menu is
            // open): releasing RMB (or un-scoping on weapon switch) must always
            // reset the aim/scope state, otherwise the scope is left on after the
            // menu is closed and the weapon has changed.
            if (_weaponManager.CurrentWeapon == null || _currentPlayer.grappling.hitGrapplePoint) return false;

            if (PlayerMovement.Instance)
                PlayerMovement.Instance.sensMultiplier = 1f;

            _aiming = false;
            SendAim(false);

            return true;
        }

        void SendAim(bool flag)
        {
            Message message = Message.Create(MessageSendMode.Unreliable, (ushort) ClientToServerId.PlayerAim);

            message.Add(flag);
            
            NetworkManager.Instance.Client.Send(message);
        }

        #endregion

        #region Reload

        public bool Reload()
        {
            if (NetworkManager.Instance.CantPlay()) return false;
            if (_weaponManager.CurrentWeapon == null) return false;
            if (_weaponManager.CurrentWeapon.weaponType==Firearms.WeaponType.Knife || _weaponManager.CurrentWeapon.weaponType==Firearms.WeaponType.LaserGun) return false;

            return true;
        }

        #endregion

        #region Switch
        
        public bool TryToSwitchWeapon()
        {
            if (NetworkManager.Instance.CantPlay()) return false;
            if (TabHolder.Instance != null || Chat.Instance.IsChat()) return false;
            return true;
        }

        public int NextWeapon(InputAction.CallbackContext ctx, int currentWeaponIndex)
        {
            if (TabHolder.Instance != null) return -1;
            if (NetworkManager.Instance.CantPlay() ) return -1;
            float y = ctx.ReadValue<Vector2>().y;
            if (_lastSwitchTime + 0.001f > Time.time) return -1;
            _lastSwitchTime = Time.time;
            if(_weaponManager.CurrentWeapon)
            {
            }

            int temp= 0;
            if (y > 0)
            {
                temp= currentWeaponIndex-1;
                if (temp <0)
                    temp = 2;
            }
            else if(y<0)
            {
                temp= currentWeaponIndex+1;

                if (temp > 2)
                    temp = 0;
            }

            return temp;
        }

        #endregion

        #endregion

        
    }
}