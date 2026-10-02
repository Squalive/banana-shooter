
using System;
using System.Collections.Generic;
using Audio;

using Manager;
using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Weapon.WeaponStats;

namespace Menu
{
    public class BuyWeaponMenu : MonoBehaviour
    {
        public static BuyWeaponMenu Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void Init()
        {
            if (NetworkManager.ClientServerType == ServerType.Endless)
            {
                cashText.gameObject.SetActive(true);
                cashText.StringReference.Arguments = new List<object>() {0};
                cashText.RefreshString();


                foreach (var id in _weaponIds)
                {
                    BuyWeaponItemUI weaponItemUI = Instantiate(item, content);
                    
                    weaponItemUI.SetValue(NetworkManager.Instance.weaponInfo[id],id);
                    
                    _items.Add(weaponItemUI);
                }
            }
        }
        private List<BuyWeaponItemUI> _items = new List<BuyWeaponItemUI>();
        private void OnEnable()
        {
            GameManager.Instance.LocalPlayerSetWeapon += SetWeaponSlot;
        }

        private void OnDisable()
        {
            GameManager.Instance.LocalPlayerSetWeapon -= SetWeaponSlot;
        }

        void SetWeaponSlot(ClientPlayer player)
        {
            if(_myPlayer == null)
                _myPlayer = player;
            for (int i = 0; i < 3; i++)
            {
                short weapon =player.playerState.WeaponManager.WeaponIndexes[i];
                if (weapon >= 0 && weapon < NetworkManager.Instance.weaponInfo.Count)
                {
                    Texture2D texture2D = NetworkManager.Instance.GetWeaponTexture(NetworkManager.Instance.weaponInfo[weapon].weaponName);

                    slots[i].img.texture = texture2D;
                    slots[i].text.text = NetworkManager.Instance.weaponInfo[weapon].weaponName;
                }
            }
        }

        public bool Buying { get; private set; } = false;
        
        [SerializeField] private GameObject menu,replaceWeaponMenu,buyWeaponMenu;

        [SerializeField] private LocalizeStringEvent cashText;

        [SerializeField] private WeaponSlot[] slots = new WeaponSlot[3];

        [SerializeField] private WeaponSlot selectWeapon;

        [SerializeField] private BuyWeaponItemUI item;

        private int[] _weaponIds = { 11 , 3  , 4 , 5 , 7 , 12 , 1 , 18 , 9 , 14 , 20,15,2 };

        [SerializeField] private Transform content;
        [Serializable]
        public class WeaponSlot
        {
            public TextMeshProUGUI text;
            public RawImage img;
            public TextMeshProUGUI priceText;
        }

        private ClientPlayer _myPlayer;
        
        
        public void OpenMenu()
        {
            Buying = true;

            foreach (var i in _items)
            {
                i.Check(_myPlayer.Cash);
            }
            
            menu.SetActive(true);
            replaceWeaponMenu.SetActive(false);
            buyWeaponMenu.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        
        public void CloseMenu()
        {
            Buying = false;
            menu.SetActive(false);
            
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void SetCash(int c)
        {
            cashText.StringReference.Arguments = new List<object>() {c};
            cashText.RefreshString();
        }

        private int _currentSelectWeapon=0;
        public void BuyWeapon(int id)
        {
            _currentSelectWeapon = id;
            replaceWeaponMenu.SetActive(true);
            buyWeaponMenu.SetActive(false);

            WeaponStat stat = NetworkManager.Instance.weaponInfo[id];

            selectWeapon.img.texture = stat.texture;
            selectWeapon.text.SetText(stat.weaponName);
            selectWeapon.priceText.SetText($"{stat.price}$");

        }

        
        public void ReplaceWeapon(int index)
        {
            CloseMenu();

            if (_myPlayer == null) return;

            WeaponStat stat = NetworkManager.Instance.weaponInfo[_currentSelectWeapon];

            if (_myPlayer.Cash < stat.price)
            {
                return;
            }

            _myPlayer.SetCash(_myPlayer.Cash - stat.price);
            
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.BuyWeapon);

            message.Add((ushort)_currentSelectWeapon);
            message.Add(index);
            
            NetworkManager.Instance.Client.Send(message);
        }
    }
    
}
