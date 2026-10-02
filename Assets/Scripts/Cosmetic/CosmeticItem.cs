
using System.Collections.Generic;

using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cosmetic
{
    [CreateAssetMenu(fileName = "New CosmeticItem", menuName = "Banana Shooter/CosmeticItem")]
    public class CosmeticItem : ScriptableObject
    {
        
        public enum Type
        {
            None = 0,
            Box = 1,
            Hair = 2,
            Hat = 3,
            Face = 4,
            Shoes= 5,
            Clothes=6,
            Pant=7,
            Rag=8,
            Particle,
            ShotGun=10,
            Al48=11,
            BWP=12,
            Knife=13,
            BananaKnife=14,
            BananaGun=15,
            DesertEagle=16,
            TaserGun=17,
            Broadsword=18,
            Vector=19,
            Boomer=20,
            Pistol=21,
            M4Carbine=22,
            DoubleBarrel=23,
            P90=24,
            BarrettM82=25,
            RocketLauncher=26,
            Bac20=27,
            Mp40=28,
            MDR=29,
            WRD4=30,
            Badge=31,
            MusicBox = 32,
            MenuScene = 33,
            Other=34,
        }

        
        public enum InventoryType
        {
            Cosmetics = 0,
            Weapon,
        }


        
        public enum Rarity
        {
            Common,
            Uncommon,
            Rare,
            Extraordinary,
            Legendary,
            Original,
            Unique,
            Epic
        }

        public int itemdefid;
        public string displayName;
        public string description, tag;
        public Type type;
        public InventoryType inventoryType = InventoryType.Cosmetics;
        public Rarity rarity;
        public Texture icon;
        public Mesh mesh;
        public Material[] materials;
        public int index;
        public float alpha = 1f;

        public string GetRarity()
        {
            switch (rarity)
            {
                case Rarity.Common:
                    return "<color=white>" + rarity + "</color>";
                case Rarity.Uncommon:
                    return "<color=#1e90ff>" + rarity + "</color>";
                case Rarity.Rare:
                    return "<color=red>" + rarity + "</color>";
                case Rarity.Extraordinary:
                    return "<color=green>" + rarity + "</color>";
                case Rarity.Legendary:
                    return "<color=yellow>" + rarity + "</color>";
                case Rarity.Original:
                    return "<color=#ff69b4>" + rarity + "</color>";
                case Rarity.Unique:
                    return "<color=#00ff00>" + rarity + "</color>";
                case Rarity.Epic:
                    return "<color=#ff4500>" + rarity + "</color>";
            }

            return rarity.ToString();
        }

        public string GetColorString()
        {
            string color = "white";
            switch (rarity)
            {
                case Rarity.Common:
                    color = "white";
                    break;
                case Rarity.Uncommon:
                    color = "#1e90ff";
                    break;
                case Rarity.Rare:
                    color = "red";
                    break;
                case Rarity.Extraordinary:
                    color = "#ff69B4";
                    break;
                case Rarity.Legendary:
                    color = "yellow";
                    break;
                case Rarity.Original:
                    color = "#ffd700";
                    break;
                case Rarity.Unique:
                    color = "#00ff00";
                    break;
                case Rarity.Epic:
                    color = "#ff4500";
                    break;
            }

            return color;
        }

        public Color GetColor()
        {
            Color color = Color.clear;
            switch (rarity)
            {
                case Rarity.Common:
                    color = Color.white;
                    break;
                case Rarity.Uncommon:
                    color = Color.cyan;
                    break;
                case Rarity.Rare:
                    color = Color.red;
                    break;
                case Rarity.Extraordinary:
                    color = new Color(1, 105 / 255f, 180 / 255f);
                    break;
                case Rarity.Legendary:
                    color = Color.yellow;
                    break;
                case Rarity.Original:
                    color = new Color(255 / 255f, 215 / 255f, 0);
                    break;
                case Rarity.Unique:
                    color = new Color(0f, 1f, 0f);
                    break;
                case Rarity.Epic:
                    color = new Color(255 / 255f, 69 / 255f, 0);
                    break;
            }

            return color;
        }

        public Vector3 size, offset=Vector3.zero, defaultRotation = new Vector3(-90, 0, 0);
        public float sizeMultiplier = 1f;

        public Vector3 GetSize()
        {
            return size * sizeMultiplier;
        }

        public List<CosmeticItem> relatedItem = new List<CosmeticItem>();

        public bool canApplyParticle=true;

        private void OnValidate()
        {
            if (SceneManager.GetActiveScene().name == "ItemShowcase")
            {
                ShowcaseManager showcaseManager = FindObjectOfType<ShowcaseManager>();
                
                if(showcaseManager && showcaseManager.IsThisItem(this))
                    showcaseManager.Showcase(this);
            }
        }
    }
}