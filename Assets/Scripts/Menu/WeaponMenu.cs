
using System.Collections.Generic;
using Audio;

using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Save;
using UnityEngine;

namespace Menu
{
    public class WeaponMenu : MonoBehaviour
    {
        public static WeaponMenu Instance;

        private void Awake()
        {
            Instance = this;
        }
        private void Start()
        {
            for (int i = 0; i < 3; i++)
            {
                string n = NetworkManager.Instance.weaponInfo[NetworkManager.Instance.Weapons[i]].weaponName;
                weaponUI[i].text.SetText(n);
                weaponUI[i].weaponImage.texture =NetworkManager.Instance. GetWeaponTexture(n);
            }
        }

        public Camera weaponCam;
        public int selectWeaponIndex = 0;
        
        public void SetWeaponIndex(int index)
        {
            AudioManager.Instance.PlayButton();
            // weaponCam.enabled = true;
            // inspect.SetActive(true);
            selectWeaponIndex = index;
            // int i = 0;
            // foreach (var texture in NetworkManager.Instance.weaponInfo)
            // {
            //     if (texture.name == NetworkManager.Instance.weapons[selectWeaponIndex])
            //     {
            //         meshFilter.mesh = texture.skins[InventoryManager.Instance.cosmeticIndex.weaponIndex[i]].mesh;
            //         meshRenderer.materials = texture.skins[InventoryManager.Instance.cosmeticIndex.weaponIndex[i]].materials;
            //         meshRenderer.transform.localScale = texture.size;
            //         viewer.targetRot =  Quaternion.Euler(texture.defaultRotation);
            //         // meshRenderer.transform.rotation = Quaternion.Euler(texture.defaultRotation);
            //         break;
            //     }
            //     ++i;
            // }

        }

        public string currentWeaponName="";
        public List<UIManager.WeaponUI> weaponUI = new List<UIManager.WeaponUI>();
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        
        public void SelectWeapon(string weaponName)
        {
            // inspect.SetActive(true);
            // weaponCam.enabled = true;
            currentWeaponName = weaponName;
            AudioManager.Instance.PlayButton();
            // int index = -1;
            // foreach (var texture in NetworkManager.Instance.weaponInfo)
            // {
            //     ++index;
            //     if (texture.name == currentWeaponName)
            //     {
            //         meshFilter.mesh = texture.skins[InventoryManager.Instance.cosmeticIndex.weaponIndex[index]].mesh;
            //         meshRenderer.materials = texture.skins[InventoryManager.Instance.cosmeticIndex.weaponIndex[index]].materials;
            //         meshRenderer.transform.localScale = texture.size;
            //         viewer.targetRot =  Quaternion.Euler(texture.defaultRotation);
            //         // meshRenderer.transform.rotation = Quaternion.Euler(texture.defaultRotation);
            //         break;
            //     }
            // }
        
            ApplyWeapon();
        }

        public GameObject inspect;
        private void ApplyWeapon()
        {
            weaponCam.enabled = false;

            for (short i = 0; i < NetworkManager.Instance.weaponInfo.Count; i++)
            {
                if (i == 6) continue;
                
                var weapon = NetworkManager.Instance.weaponInfo[i];

                if (weapon.name == currentWeaponName)
                {
                    weaponUI[selectWeaponIndex].weaponImage.texture = weapon.texture;
                    weaponUI[selectWeaponIndex].weaponImage.gameObject.SetActive(true);
                    weaponUI[selectWeaponIndex].text.SetText(UIManager.IsItChinese() ? weapon.chineseName : weapon.name);

                    NetworkManager.Instance.Weapons[selectWeaponIndex] = i;
                }
            }

            SaveSystem.SaveData("weapons", NetworkManager.Instance.Weapons);

            if (NetworkManager.ClientGameMode == GameMode.Randomizer ||
                NetworkManager.ClientGameMode == GameMode.GunGame || 
                NetworkManager.ClientGameMode == GameMode.RocketMode) return;
            if (NetworkManager.Instance.Client.IsConnected && ClientPlayer.list.TryGetValue(NetworkManager.Instance.Client.Id, out var value))
            {
                value.GetWeapon();
            }
        }

        [SerializeField] private Item3DViewer viewer;

    }
}
