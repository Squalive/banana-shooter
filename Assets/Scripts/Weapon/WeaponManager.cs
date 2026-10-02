using System;
using System.Collections.Generic;
using Audio;
using Manager;
using Menu;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using Riptide;
using Safe;
using UnityEngine;
using UnityEngine.InputSystem;
using Weapon.Interface;

namespace Weapon
{
    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance;
        
        public const int MaxStoredSize = 128;
        public bool[] ShootingBuffer { get; } = new bool[MaxStoredSize];
        
        private int _currentWeaponIndex=0;

        public int CurrentWeaponIndex
        {
            get => _currentWeaponIndex;
            set
            {
                _currentWeaponIndex = value;
                SetWeapon(value);
            }
        }

        public static readonly int WeaponIndex = Animator.StringToHash("WeaponIndex");

        public Firearms CurrentWeapon => _currentWeapon;
        public Firearms SpecialWeapon => _weapons[3];
        public List<MultiplayerWeapon> weapons = new List<MultiplayerWeapon>();

        [SerializeField] public ThrowableManager throwableManager;

        private IWeaponManager _weaponManager;
    
        private Firearms _currentWeapon;

        private Firearms[] _weapons = new Firearms[4];

        public List<Camera> cams = new();
        [HideInInspector]
        public float desiredFOV,defaultFOV,speedUpFOV;
        public SkinnedMeshRenderer arm;

        private PlayerState _currentPlayer;

        public PlayerState CurrentPlayer => _currentPlayer;

        public GameObject nightVission,flashLight;
        public Queue<Tuple<uint,int>> LastReloadTick { get; } = new();
        
        public Animator armAnimator;
        public LookedToObject leftHandTarget, rightHandTarget;

        [SerializeField] public LayerMask weaponCamDefaultLayer, weaponCamAimingLayer;
        
        public bool WeaponSelectAnimation { get; set; } = true;
        private void Awake()
        {
            Instance = this;

            ClientPlayer.SetWeaponManagerClass(this);
        }
        
        private void OnEnable()
        {
            GameManager.InputManager.Player.Shoot.started += StartShoot;
            GameManager.InputManager.Player.Shoot.canceled +=  StopShoot;
            GameManager.InputManager.Player.Shoot.performed += Shoot;
        
            GameManager.InputManager.Player.Reload.performed += Reload;
        
            GameManager.InputManager.Player.Weapon0.performed += Weapon0;
            GameManager.InputManager.Player.Weapon1.performed += Weapon1;
            GameManager.InputManager.Player.Weapon2.performed += Weapon2;
            GameManager.InputManager.Player.Power.performed += Weapon3;
        
            GameManager.InputManager.Player.NextWeapon.performed += NextWeapon;
        
            GameManager.InputManager.Player.Aim.started +=  StartAim;
            GameManager.InputManager.Player.Aim.canceled +=  StopAim;
        
            GameManager.InputManager.Player.FlashLight.performed += FlashLight;

            if (GameManager.SettingsLoaded)
            {
                LoadFov(GameManager.Instance.setting);
            }

            GameManager.OnSettingLoaded += LoadFov;
        }

    
        private void OnDisable()
        {
            GameManager.InputManager.Player.Shoot.started -= StartShoot;
            GameManager.InputManager.Player.Shoot.canceled -=  StopShoot;
            GameManager.InputManager.Player.Shoot.performed -= Shoot;
        
            GameManager.InputManager.Player.Reload.performed -= Reload;
        
            GameManager.InputManager.Player.Weapon0.performed -= Weapon0;
            GameManager.InputManager.Player.Weapon1.performed -= Weapon1;
            GameManager.InputManager.Player.Weapon2.performed -= Weapon2;
            GameManager.InputManager.Player.Power.performed -= Weapon3;
        
            GameManager.InputManager.Player.NextWeapon.performed -= NextWeapon;
        
            GameManager.InputManager.Player.Aim.started -=  StartAim;
            GameManager.InputManager.Player.Aim.canceled -=  StopAim;
        
            GameManager.InputManager.Player.FlashLight.performed-= FlashLight;
        
            GameManager.OnSettingLoaded -= LoadFov;
        }

        void LoadFov(Setting setting)
        {
            desiredFOV = setting.fov;
            defaultFOV = desiredFOV;
            speedUpFOV = defaultFOV + 6f;
        }
        
        public void InitializePlayer(PlayerState currentPlayer)
        {
            // Switching the controlled or spectated player must not keep the scope
            // opened by the previous player. Close it before swapping the manager.
            ResetAim();

            _currentPlayer = currentPlayer;
            _weaponManager = currentPlayer.selfControlled ? new LocalWeaponManager(this,_currentPlayer) : new SpectateWeaponManager(this);
            
            throwableManager.InitializePlayer(currentPlayer);
        }

        #region Input

        void FlashLight(InputAction.CallbackContext ctx)
        {
            if (NetworkManager.Instance.CantPlay()) return;
        
            flashLight.SetActive(!flashLight.activeSelf);
            if (GameManager.Instance.setting.useNightVission)
            {
                nightVission.SetActive(!nightVission.activeSelf);
                if (nightVission.activeSelf)
                    AudioManager.Instance.Play("night");
            }
            else
            {
                AudioManager.Instance.Play("tip");
            }

            Message message = Message.Create(MessageSendMode.Unreliable,(ushort)ClientToServerId.TurnLight);
            message.Add(nightVission.activeSelf);
            NetworkManager.Instance.SendByte += message.WrittenLength;
            NetworkManager.Instance.Client.Send(message);
        }
        public void StartAim(InputAction.CallbackContext ctx)
        {
            if (_weaponManager == null || _currentWeapon == null) return;

            if (_weaponManager.StartAim(ctx.started))
            {
                _currentWeapon.DoAim();
                desiredFOV = 30;
                isAiming = true;
            }
        }

        public bool isAiming = false;

        public void StopAim(InputAction.CallbackContext ctx)
        {
            if (_weaponManager == null) return;

            if (_weaponManager.StopAim(ctx.canceled))
            {
                if(_currentWeapon)
                    _currentWeapon.DoNoAim();
                desiredFOV = defaultFOV;
                isAiming = false;
            }
        }

        /// <summary>
        /// Force-closes any active aim/scope view. Used when the controlled or
        /// spectated player changes so a scope opened by the previous player
        /// can't stay on screen for the new one.
        /// </summary>
        public void ResetAim()
        {
            bool wasAiming = isAiming || (_currentWeapon != null && _currentWeapon.isAiming);

            if (_weaponManager != null)
                _weaponManager.StopAim(false);

            if (wasAiming)
            {
                if (_currentWeapon)
                {
                    _currentWeapon.DoNoAim();
                }
                else if (cams.Count > 1 && cams[1])
                {
                    // Weapon already dropped, restore the weapon camera manually.
                    cams[1].cullingMask = weaponCamDefaultLayer;
                }

                desiredFOV = defaultFOV;
                isAiming = false;
            }

            if (GameUIManager.Instance)
                GameUIManager.Instance.scope.SetActive(false);
        }

        /// <summary>
        /// Mirrors an already known aim state after the spectated player changed.
        /// Aim changes normally arrive as events, so the current state has to be
        /// re-applied explicitly when the controlled player changes.
        /// </summary>
        public void SyncAim(bool aiming)
        {
            if (aiming)
                StartAim(new InputAction.CallbackContext());
            else
                StopAim(new InputAction.CallbackContext());
        }
        void StartShoot(InputAction.CallbackContext ctx)
        {
            _weaponManager.StartShoot();
        }
        void StopShoot(InputAction.CallbackContext ctx)
        {
            _weaponManager.StopShoot();
        }
        void Shoot(InputAction.CallbackContext ctx)
        {
            _weaponManager.Shoot();
        }
        void Reload(InputAction.CallbackContext ctx)
        {
            if (_weaponManager.Reload())
            {
                _currentWeapon.DoReload(_currentWeapon.currentAmmo.GetValue());
                StopAim(new InputAction.CallbackContext());
            }

        }
        void Weapon0(InputAction.CallbackContext ctx)
        {
            if (_weaponManager.TryToSwitchWeapon())
            {
                if (_weapons[0] != null && _weapons[0] == _currentWeapon) return;

                if (_currentWeapon)
                {
                }

                CurrentWeaponIndex = 0;
            }
        }
        void Weapon1(InputAction.CallbackContext ctx)
        {
            if (_weaponManager.TryToSwitchWeapon())
            {
                if (_weapons[1] != null && _weapons[1] == _currentWeapon) return;
        
                if(_currentWeapon)
                {
                }

                CurrentWeaponIndex = 1;
            }
        }
        void Weapon2(InputAction.CallbackContext ctx)
        {
            if (_weaponManager.TryToSwitchWeapon())
            {
                if (_weapons[2] != null && _weapons[2] == _currentWeapon) return;
        
                if(_currentWeapon)
                {
                }

                CurrentWeaponIndex = 2;
            }
        }
        void Weapon3(InputAction.CallbackContext ctx)
        {
            if (_weaponManager.TryToSwitchWeapon())
            {
                if (_weapons[3] != null && _weapons[3] == _currentWeapon) return;
                if(_currentWeapon)
                {
                }

                CurrentWeaponIndex = 3;
            }
        }
        void NextWeapon(InputAction.CallbackContext ctx)
        {
            if (_weaponManager.TryToSwitchWeapon())
            {
                int next = _weaponManager.NextWeapon(ctx, CurrentWeaponIndex);
                if (next != -1)
                {
                    CurrentWeaponIndex = next;
                }
            }
        }
        #endregion

        private void Update()
        {
            if(!isAiming)
                desiredFOV = Mathf.Lerp(desiredFOV,_currentPlayer.GetVelocity().magnitude > 20f ? speedUpFOV : defaultFOV,Time.deltaTime*10f);
            
            // float offset = 1;
            // int index = 0;
            foreach (var cam in cams)
            {
                // if(index>0)
                //     camTransform.fieldOfView = Mathf.Lerp(camTransform.fieldOfView, Mathf.Clamp(desiredFOV*offset,Mathf.Min(GameManager.Instance.setting.fov,90),105), Time.deltaTime * 20f);
                // else
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desiredFOV, Time.deltaTime * 20f);
                // index++;
            }
            
            _weaponManager.Update();
        }

        void SetWeapon(int index)
        {
            // Stop throwing
            throwableManager.IsThrowing = false;
            
            // Stop The Current Weapon Audio
            AudioManager.Instance.StopGunReload();
            AudioManager.Instance.StopGunReset();
            
            if (_currentWeapon != null)
            {
                if (_currentWeapon.isAiming)
                {
                    _currentWeapon.DoNoAim();
                    StopAim(new InputAction.CallbackContext());
                }
                _currentWeapon.DeSelect();
                _currentWeapon = null;
            }
            // Stops when the player is dead or infected
            if (_currentPlayer.Health <= 0 || _currentPlayer.IsInfected) return;
            
            // Deselect every weapon ui
            foreach (var weapon in GameUIManager.Instance.weaponUis)
            {
                weapon.DeSelect();
            }
            GameUIManager.Instance.weaponUis[3].DeSelect();
            
            // Disable arms when switching
            if (GameManager.Instance.setting.useArm)
            {
                leftHandTarget.parent = null;
                rightHandTarget.parent = null;
                arm.enabled = false;
            }
            
            // Execute the command
            _weaponManager.SwitchWeapon(index);

            _currentWeapon = _weapons[index];
            
            //TODO: Set the ui back
            if (_currentWeapon != null)
            {
                if (index < 3)
                {
                    GameUIManager.Instance.weaponUis[index].Select();
                }
                else
                {
                    GameUIManager.Instance.weaponUis[3].Select();
                }
                _currentWeapon.Select(WeaponSelectAnimation);
                if (GameManager.Instance.setting.useArm && _currentWeapon.useArm)
                {
                    leftHandTarget.parent = _currentWeapon.leftHandIK;
                    rightHandTarget.parent = _currentWeapon.rightHandIK;
                    arm.enabled = true;
                }
            
                armAnimator.SetInteger(WeaponIndex,_currentWeapon.weaponIndex);
            }

            WeaponSelectAnimation = true;
        }
        public void SetArm(bool flag)
        {
            if (!flag)
            {
                leftHandTarget.parent = null;
                rightHandTarget.parent = null;
                arm.enabled = false;
                return;
            }

            if (InfectedHand.Instance.isInfected)
            {
                arm.enabled = true;
                return;
            }
            if (!_currentWeapon || !_currentWeapon.useArm) return;
            leftHandTarget.parent = _currentWeapon.leftHandIK;
            rightHandTarget.parent = _currentWeapon.rightHandIK;
            arm.enabled = true;
        }
        public void DisableSpecialWeapon()
        {
            _weapons[3] = null;
            GameUIManager.Instance.weaponUis[3].NotDisplay();
            GameUIManager.Instance.weaponUis[3].DeSelect();
        }

        public void DisableAllWeapons()
        {
            // Dropping the weapon references below would otherwise leave the scope
            // of the previous player on screen (e.g. when switching spectate targets).
            ResetAim();

            foreach (var weapon in weapons)
            {
                weapon.gameObject.gameObject.SetActive(false);
            }

            arm.enabled = false;
            
            if (_currentWeapon)
            {
                _currentWeapon.DeSelect();
                _currentWeapon = null;
            }

            for (int i = 0; i < 4; i++)
            {
                if(_weapons[i]!=null)
                    _weapons[i].DeSelect();
                _weapons[i] = null;
            }
        }

        public void SetWeapons(int weaponIndex, int index)
        {
            MultiplayerWeapon multiplayerWeapon = weapons[weaponIndex];
            _weapons[index] = multiplayerWeapon.GetComponent<Firearms>();
            _weapons[index].currentAmmo = new SafeInt(_weapons[index].maxAmmo);
            GameUIManager.Instance.weaponUis[index].SetText(UIManager.IsItChinese()? NetworkManager.Instance.GetWeaponChineseName(multiplayerWeapon.name) : multiplayerWeapon.name);
            GameUIManager.Instance.weaponUis[index].SetTexture(NetworkManager.Instance.GetWeaponTexture(multiplayerWeapon.name));
            GameUIManager.Instance.weaponUis[index].DeSelect();
            GameUIManager.Instance.weaponUis[index].Display();
        }

        
    }
}
