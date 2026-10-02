using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Weapon.WeaponStats;

namespace Menu
{
    public class BuyWeaponItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText,priceText;
        [SerializeField] private Button btn;
        [SerializeField] private RawImage img;

        private WeaponStat _stat;

        public void SetValue(WeaponStat stat,int id)
        {
            _stat = stat;
            nameText.SetText(stat.weaponName);
            priceText.SetText($"{stat.price}$");
            btn.onClick.AddListener(delegate { BuyWeaponMenu.Instance.BuyWeapon(id); });
            img.texture = stat.texture;
        }

        public void Check(int cash)
        {
            btn.interactable = cash >= _stat.price;
        }
    }
}
