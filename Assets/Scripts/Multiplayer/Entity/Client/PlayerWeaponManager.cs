using System.Collections.Generic;
using DitzelGames.FastIK;
using Menu;
using Pool;
using UnityEngine;
using Weapon;

namespace Multiplayer.Entity.Client
{
    public class PlayerWeaponManager
    {
        private static readonly int Attack = Animator.StringToHash("Attack");
        private bool IsLocal { get; set; }

        private Transform _weaponHolder, _headTransform;

        private AudioSource _audioSource;

        private PlayerState _playerState;

        private Animator _clawKnifeAnimator;

        private List<MultiplayerWeapon> _weaponObjects;

        public MultiplayerWeapon CurrentWeapon;
        public MultiplayerWeapon[] Weapon = new MultiplayerWeapon[3];
        public MultiplayerWeapon SpecialWeapon;

        private LookedToObject _leftHandTarget, _rightHandTarget;
        private List<FastIKFabric> _iks;

        public short[] WeaponIndexes { get; set; } = { -1, -1, -1 };
        public int CurrentWeaponIndex { get; set; }
        public ushort[] WeaponSkinIndexes { get; set; }

        public PlayerWeaponManager(bool isLocal, List<MultiplayerWeapon> weaponObjects, LookedToObject leftHandTarget, LookedToObject rightHandTarget, List<FastIKFabric> iks, PlayerState playerState, Animator clawKnifeAnim, AudioSource audioSource, Transform weaponHolder, Transform headTransform)
        {
            IsLocal = isLocal;
            _weaponObjects = weaponObjects;
            _leftHandTarget = leftHandTarget;
            _rightHandTarget = rightHandTarget;
            _iks = iks;
            _playerState = playerState;
            _clawKnifeAnimator = clawKnifeAnim;
            _audioSource = audioSource;
            _weaponHolder = weaponHolder;
            _headTransform = headTransform;

            SetWeaponOwner();
        }

        public void SetLocal(bool flag)
        {
            IsLocal = flag;
        }

        public void SetWeaponObjects(List<MultiplayerWeapon> weaponObjects)
        {
            _weaponObjects = weaponObjects;

            SetWeaponOwner();
        }

        /// <summary>
        /// Gives every weapon model a reference to the player it belongs to so the
        /// remote/demo weapon bob can use that player's movement state.
        /// </summary>
        void SetWeaponOwner()
        {
            if (_weaponObjects == null) return;

            foreach (var weapon in _weaponObjects)
            {
                if (weapon != null)
                    weapon.SetPlayerState(_playerState);
            }
        }

        public void Update()
        {
            Vector3 weaponHolderLookPoint = _headTransform.position + _headTransform.forward * 5f;

            _weaponHolder.LookAt(weaponHolderLookPoint);
        }

        public void StartReloading(int index)
        {
            if (IsLocal)
            {
                if (!_playerState.selfControlled)
                {
                    if (CurrentWeaponIndex != index)
                        SwitchWeapon(index);
                    if (WeaponManager.Instance.CurrentWeapon != null)
                    {
                        switch (WeaponManager.Instance.CurrentWeapon.weaponType)
                        {
                            case Firearms.WeaponType.Gun:
                                WeaponManager.Instance.CurrentWeapon.DoReload(0, false);
                                break;
                        }
                    }
                }
            }
            else
            {
                if (CurrentWeapon != null)
                {
                    switch (CurrentWeapon.Type)
                    {
                        case Firearms.WeaponType.Gun:
                            CurrentWeapon.Reload(CurrentWeapon.stat.reloadTime, CurrentWeapon.stat.spinAmount);
                            break;
                    }
                }
            }
        }

        public void Shoot(Vector3 dir, int bulletCount, EShootingResult result)
        {
            if (IsLocal)
            {
                if (result == EShootingResult.EResultOk)
                {
                    if (!_playerState.selfControlled)
                    {
                        if (_playerState.IsInfected)
                        {
                            InfectedHand.Instance.AttackVisual();
                        }
                        else if (WeaponManager.Instance.CurrentWeapon != null)
                        {
                            switch (WeaponManager.Instance.CurrentWeapon.weaponType)
                            {
                                case Firearms.WeaponType.Gun:
                                    WeaponManager.Instance.CurrentWeapon.GunShoot(true);
                                    break;
                                case Firearms.WeaponType.Knife:
                                    WeaponManager.Instance.CurrentWeapon.KnifeShoot(true);
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log($"Shooting  Failed: {result}");
                    if (WeaponManager.Instance.CurrentWeapon != null) WeaponManager.Instance.CurrentWeapon.ResetDynamic();
                }
            }
            else if (_playerState.IsInfected)
            {
                _clawKnifeAnimator.SetTrigger(Attack);
            }
            else if (result == EShootingResult.EResultOk && CurrentWeapon != null)
            {
                // ReplayManager.Instance.RecordWeaponShoot(id,tick,bulletCount,dir);
                switch (CurrentWeapon.Type)
                {
                    case Firearms.WeaponType.Knife:
                        if (CurrentWeapon.CompareTag("Broadsword"))
                        {
                            CurrentWeapon.animator.SetTrigger(Random.Range(0, 2) == 0 ? "Attack0" : "Attack1");
                        }
                        else CurrentWeapon.animator.SetTrigger("Attack");
                        if (CurrentWeapon.shoot != null)
                        {
                            _audioSource.PlayOneShot(CurrentWeapon.shoot);
                        }
                        break;
                    case Firearms.WeaponType.Gun:
                        MultiplayerWeapon weapon = CurrentWeapon;

                        if (weapon.dual)
                        {
                            weapon.tip = weapon.leftSide ? weapon.rTip : weapon.lTip;
                            weapon.particleSystem = weapon.leftSide ? weapon.rPart : weapon.lPart;
                        }

                        for (int i = 0; i < bulletCount; i++)
                        {
                            Vector3 offset = CurrentWeapon.spreadAngle / 60 * Random.insideUnitCircle;
                            dir += offset;
                            Bullet bullet = ObjectPooler.Instance.SpawnFromPool("Bullet", CurrentWeapon.tip.position,
                                Quaternion.LookRotation(dir)).GetComponent<Bullet>();
                            bullet.Initialization(dir, 1500f, 0, LayerMask.NameToLayer("Bullet"), false, CurrentWeapon.useGravity);
                            dir -= offset;
                        }
                        CurrentWeapon.particleSystem.Play();
                        CurrentWeapon.ShootAnim();
                        if (CurrentWeapon.shoot != null)
                        {
                            _audioSource.PlayOneShot(CurrentWeapon.shoot);
                        }
                        break;
                    case Firearms.WeaponType.LaserGun:
                        CurrentWeapon.particleSystem.Play();
                        CurrentWeapon.ShootAnim();
                        if (CurrentWeapon.shoot != null)
                        {
                            _audioSource.PlayOneShot(CurrentWeapon.shoot);
                        }
                        break;
                    case Firearms.WeaponType.Boomer:

                        for (int i = 0; i < bulletCount; i++)
                        {
                            Vector3 offset = CurrentWeapon.spreadAngle / 60 * Random.insideUnitCircle;
                            dir += offset;
                            Bullet bullet = ObjectPooler.Instance.SpawnFromPool("ExplosiveBullet", CurrentWeapon.tip.position,
                                Quaternion.LookRotation(dir)).GetComponent<Bullet>();

                            bullet.Initialization(dir, 100f, 0, LayerMask.NameToLayer("Bullet"), false, CurrentWeapon.useGravity);
                            dir -= offset;
                        }
                        CurrentWeapon.ShootAnim();
                        if (CurrentWeapon.shoot != null)
                        {
                            _audioSource.PlayOneShot(CurrentWeapon.shoot);
                        }
                        break;
                }
            }
        }

        public void UpdateWeapons(short[] weaponNames, int weaponIndex, ushort[] weaponIndexs)
        {
            WeaponIndexes = weaponNames;

            CurrentWeaponIndex = weaponIndex;

            WeaponSkinIndexes = weaponIndexs;

            foreach (var weapon in _weaponObjects)
            {
                weapon.gameObject.SetActive(false);
            }

            if (IsLocal)
            {
                foreach (var weaponUi in GameUIManager.Instance.weaponUis)
                {
                    weaponUi.NotDisplay();
                }
                WeaponManager.Instance.DisableAllWeapons();
            }

            for (int i = 0; i < Weapon.Length; i++)
            {
                Weapon[i] = null;
            }

            for (int i = 0; i < 3; i++)
            {
                if (weaponNames[i] >= 0 && weaponNames[i] < _weaponObjects.Count)
                {
                    var firearm = _weaponObjects[weaponNames[i]];

                    if (firearm.skins.Count > 1)
                    {
                        for (int k = 0; k < firearm.skins.Count; k++)
                        {
                            firearm.skins[k].SetActive(false);
                        }
                    }

                    if (weaponNames[i] < weaponIndexs.Length)
                    {
                        int a = weaponIndexs[weaponNames[i]] >= firearm.skins.Count ? 0 : weaponIndexs[weaponNames[i]];
                        firearm.skins[a].SetActive(true);
                    }

                    if (IsLocal)
                    {
                        WeaponManager.Instance.SetWeapons(weaponNames[i], i);
                    }
                    Weapon[i] = firearm;
                }
            }


            if (weaponIndex > 2) weaponIndex = 0;
            CurrentWeapon = Weapon[weaponIndex];

            if (CurrentWeapon != null && CurrentWeapon.gameObject != null)
            {
                CurrentWeapon.gameObject.SetActive(true);
                if (IsLocal)
                {
                    WeaponManager.Instance.CurrentWeaponIndex = weaponIndex;
                    GameUIManager.Instance.weaponUis[weaponIndex].Select();
                }
                else
                {
                    _leftHandTarget.parent = CurrentWeapon.leftHand;
                    _rightHandTarget.parent = CurrentWeapon.rightHand;
                    foreach (var ik in _iks)
                    {
                        ik.enabled = true;
                    }
                }
            }
        }

        public void SetSpecialWeapon(short w)
        {
            CurrentWeaponIndex = 3;
            if (CurrentWeapon != null && CurrentWeapon.gameObject != null)
            {
                if (!IsLocal)
                {
                    _leftHandTarget.parent = null;
                    _rightHandTarget.parent = null;
                    DisableIK();
                    CurrentWeapon.gameObject.SetActive(false);
                }

                CurrentWeapon = null;
            }

            SpecialWeapon = _weaponObjects[w];
            if (IsLocal)
            {
                WeaponManager.Instance.SetWeapons(w, 3);
                WeaponManager.Instance.CurrentWeaponIndex = 3;
                GameUIManager.Instance.weaponUis[3].Display();
                GameUIManager.Instance.weaponUis[3].Select();
                foreach (var weaponUi in GameUIManager.Instance.weaponUis)
                {
                    weaponUi.DeSelect();
                }
            }

            CurrentWeapon = SpecialWeapon;

            if (CurrentWeapon != null && CurrentWeapon.gameObject != null)
            {
                if (!IsLocal)
                {
                    CurrentWeapon.gameObject.SetActive(true);
                    _leftHandTarget.parent = CurrentWeapon.leftHand;
                    _rightHandTarget.parent = CurrentWeapon.rightHand;
                    foreach (var ik in _iks)
                    {
                        ik.enabled = true;
                    }
                    // fakeRig.enabled = true;
                    // fakeAnimator.enabled = true;
                }
            }
        }

        public void SwitchWeapon(int i)
        {
            CurrentWeaponIndex = i;

            if (CurrentWeapon != null && CurrentWeapon.gameObject != null)
            {
                if (_leftHandTarget) _leftHandTarget.parent = null;
                if (_rightHandTarget) _rightHandTarget.parent = null;
                CurrentWeapon.gameObject.SetActive(false);
                DisableIK();
                CurrentWeapon = null;
            }
            if (i < 3)
                CurrentWeapon = Weapon[i];
            else if (CurrentWeaponIndex == 3)
            {
                CurrentWeapon = SpecialWeapon;
            }

            if (IsLocal)
            {
                WeaponManager.Instance.CurrentWeaponIndex = i;
            }
            else
            {
                if (CurrentWeapon != null && CurrentWeapon.gameObject != null)
                {
                    CurrentWeapon.gameObject.SetActive(true);
                    _leftHandTarget.parent = CurrentWeapon.leftHand;
                    _rightHandTarget.parent = CurrentWeapon.rightHand;
                    foreach (var ik in _iks)
                    {
                        ik.enabled = true;
                    }
                }
            }


        }

        public void DisableAllWeapons()
        {
            foreach (var weapon in _weaponObjects)
            {
                weapon.gameObject.SetActive(false);
            }
            int i;
            for (i = 0; i < Weapon.Length; i++)
            {
                Weapon[i] = null;
            }
        }

        void DisableIK()
        {
            foreach (var ik in _iks)
            {
                ik.enabled = false;
            }
        }

        public bool IsUsingSpecialWeapon()
        {
            if (SpecialWeapon != null)
            {
                if (CurrentWeapon != null && SpecialWeapon == CurrentWeapon)
                {
                    CurrentWeapon = null;
                    CurrentWeaponIndex = 0;
                }
                SpecialWeapon = null;
                return true;
            }

            return false;
        }

        public void ClearCurrentWeapons()
        {
            Weapon = new MultiplayerWeapon[3];
        }

    }
}
