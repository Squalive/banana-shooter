
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class PerkManager : MonoBehaviour
    {
        public static PerkManager Instance;

        public List<Perk> perks = new List<Perk>() {Perk.WallRun,Perk.Bot,Perk.HitDetection};
        public List<PerkItem> perkItems = new List<PerkItem>() {};

        private void Awake()
        {
            Instance = this;
        }

        public bool HasPerk( Perk perk)
        {
            // /return false;
            return perks.Contains(perk);
        }
        
        [Serializable]
        public class PerkItem
        {
            public string key;
            public Texture2D texture2D;
            public Rarity rarity = Rarity.Normal;

            public Color GetRarityColor()
            {
                switch (rarity)
                {
                    case Rarity.Normal:
                        return new Color(1, 241 / 255f, 0);
                    case Rarity.Medium:
                        return new Color(98/ 255f,1,0);
                    case Rarity.High:
                        return new Color(0,197/255f,1);
                    default:
                        return new Color(255 / 255f, 241 / 255f, 0);
                }
            }
            public enum Rarity
            {
                Normal,
                Medium,
                High,
            }
        }
        public static float FatMultiplier = 1.25f;
    }

    
    public enum Perk
    {
        None,
        WallRun,
        QuickHand,
        Forg,
        HitDetection,
        Nerd,
        Fat,
        Bot
    }
    
    
}