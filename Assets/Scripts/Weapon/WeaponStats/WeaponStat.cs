using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weapon.WeaponStats
{
    [CreateAssetMenu(fileName = "New Weapon Stat", menuName = "Banana Shooter/Weapon Stat")]
    public class WeaponStat : ScriptableObject
    {
        public string weaponName, chineseName;
        public int price = 0;
        public Texture2D texture;
        public List<Skin> skins = new List<Skin>();
        public Vector3 size,defaultRotation=new Vector3(-90,0,0);

        public int damage = 0;
        [HideInInspector]
        public int damageOffset = 10;
        public bool specialWeapon=false;
        public int spinAmount;
        
        public WeaponType weaponType = WeaponType.None;
        [Serializable]
        public class Skin
        {
            public Mesh mesh;
            public Material[] materials;
        }
        
        public enum WeaponType
        {
            None,
            AssaultRifle,
            ShotGun,
            SubMachineGun,
            LightMachineGun,
            Pistol,
            Knife,
            Sniper,
            Boomer,
            Taser
        }

        public bool bUseGravity = false;
        public uint maxAmmo=30;
        public float fireRate = 10;
        public uint bulletAmount = 1;
        public float spreadAngle = 0f;
        public bool cantReload = false;
        public float reloadTime = 0;
        public bool explosiveAmmo = false;
    }
}
