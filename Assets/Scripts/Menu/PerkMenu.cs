using System;
using System.Collections.Generic;
using Audio;

using Manager;
using Movement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class PerkMenu : MonoBehaviour
    {
        public static PerkMenu Instance;
        [Serializable]
        public class PerkItemUI
        {
            public Button btn;
            public LocalizeStringEvent text;
            public RawImage icon,bg;
        }

        public List<PerkItemUI> items = new List<PerkItemUI>();

        private void Awake()
        {
            Instance = this;
            int i = 0;
            foreach (var item in items)
            {
                var i1 = i;
                item.btn.onClick.AddListener(delegate { SelectPerk(i1); });

                int index = (int) PerkManager.Instance.perks[i] - 1;
                item.icon.texture = PerkManager.Instance.perkItems[index].texture2D;
                item.text.SetEntry(PerkManager.Instance.perkItems[index].key);
                i++;
            }
        }

        public int currentIndex = -1;

        public GameObject selectObjMenu;
        void SelectPerk(int index)
        {
            AudioManager.Instance.PlayButton();
            
            currentIndex = index;
            selectObjMenu.SetActive(true);
        }

        
        public void ClearSelect()
        {
            foreach (var item in items)
            {
                item.btn.interactable = true;
            }
        }

        
        public void SetPerk(int index)
        {
            AudioManager.Instance.PlayButton();
            selectObjMenu.SetActive(false);
            foreach (var item in items)
            {
                item.btn.interactable = true;
            }
            if (currentIndex == -1) return;
            Perk perk = (Perk) index;
            PerkManager.Instance.perks[currentIndex] = perk;
            
            items[currentIndex].text.SetEntry(PerkManager.Instance.perkItems[index-1].key);
            items[currentIndex].icon.texture=(PerkManager.Instance.perkItems[index-1].texture2D);

            if (PlayerMovement.Instance)
            {
                PlayerMovement.Instance.SetPerks(true);
                Recoil.Instance.SetFactor(PerkManager.Instance.HasPerk(Perk.Bot) ? 0.3f : 1.25f);
                HitDetection.Instance.enabled=PerkManager.Instance.HasPerk(Perk.HitDetection);
            }
            CloseDescription();
            currentIndex = -1;

            GameManager.perkChanged = true;
        }

        [SerializeField] private LocalizeStringEvent description;

        [SerializeField] private GameObject descriptionPannel;
        public void SetDescription(string key)
        {
            descriptionPannel.SetActive(true);
            description.SetEntry(key);
            description.RefreshString();
        }

        public void CloseDescription()
        {
            descriptionPannel.SetActive(false);
        }
        
        

    }
}
