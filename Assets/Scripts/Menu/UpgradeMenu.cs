using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Manager;
using Save;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class UpgradeMenu : MonoBehaviour
    {
        public UpgradeItemUI[] upgradeIndex = new UpgradeItemUI[3];

        public List<Button> btns = new List<Button>();

        public GameObject list;
    
        [Serializable]
        public class UpgradeItemUI
        {
            public Button btn;
            public RawImage[] images;
            public LocalizeStringEvent text;
            public string key;
        }

        private void Start()
        {
            for (int i = 0; i < GameManager.Instance.upgrades.Length; i++)
            {
                Texture2D texture2D = GameManager.Instance.GetUpgradeDetailedTexture2D(GameManager.Instance.upgrades[i]);
                for (int j = 0; j < upgradeIndex[i].images.Length; j++)
                {
                    upgradeIndex[i].images[j].texture = texture2D;
                }

                GameManager.UpgradeDetailed detailed = GameManager.Instance.GetUpgradeDetailed(GameManager.Instance.upgrades[i]);
                if (detailed != null)
                {
                    upgradeIndex[i].text.SetEntry(detailed.name);
                    upgradeIndex[i].text.RefreshString();
                }
            }
        
            for (int i = 0; i < upgradeIndex.Length; i++)
            {
                var i1 = i;
                if(upgradeIndex[i].btn!=null)
                    upgradeIndex[i].btn.onClick.AddListener(delegate { SelectIndex(i1); });
            }
            for (int i = 0; i < GameManager.Instance.upgradeDetaileds.Count; i++)
            {
                var i1 = i;
                btns[i].onClick.AddListener(delegate { SelectUpgrade(GameManager.Instance.upgradeDetaileds[i1].name); });
            }
        }   

        private string currentUpgradeKey;
        private int currentUpgradeIndex = 0;
        void SelectUpgrade(string upgrade_key)
        {
            AudioManager.Instance.PlayButton();
            currentUpgradeKey = upgrade_key;
            Apply();
        }

        void SelectIndex(int i)
        {
            AudioManager.Instance.PlayButton();
            currentUpgradeIndex = i;
            list.SetActive(true);

            if (PowerMenu.Instance)
                PowerMenu.Instance.powerScroll.SetActive(false);
        }

        void Apply()
        {
            list.SetActive(false);
            foreach (var upgrade in GameManager.Instance.upgrades)
            {
                if (upgrade == currentUpgradeKey) return;
            }

            if (GameUIManager.Instance)
            {
                string lastKey = GameManager.Instance.upgrades[currentUpgradeIndex];
                switch (lastKey)
                {
                    case "health":
                        UpgradeInGameMenu.Instance.DownHealth();
                        break;
                    case "moveSpeed":
                        UpgradeInGameMenu.Instance.DownMovementSpeed();
                        break;
                    case "dash":
                        UpgradeInGameMenu.Instance.ClearDash();
                        break;
                    case "doubleJump":
                        UpgradeInGameMenu.Instance.ClearDoubleJump();
                        break;
                }
            
            }
        
            GameManager.Instance.upgrades[currentUpgradeIndex] = currentUpgradeKey;

            Texture2D texture2D = GameManager.Instance.GetUpgradeDetailedTexture2D(currentUpgradeKey);
            foreach (var image in upgradeIndex[currentUpgradeIndex].images)
            {
                image.texture = texture2D;
            }

            GameManager.UpgradeDetailed detailed = GameManager.Instance.GetUpgradeDetailed(currentUpgradeKey);

            upgradeIndex[currentUpgradeIndex].text.SetEntry(detailed.name);
            upgradeIndex[currentUpgradeIndex].text.RefreshString();
            SaveSystem.SaveData("upgrades", GameManager.Instance.upgrades);
        
            if(UpgradeInGameMenu.Instance)UpgradeInGameMenu.Instance.upgradeItems[currentUpgradeIndex].Setup();
        
        }
    }
}
