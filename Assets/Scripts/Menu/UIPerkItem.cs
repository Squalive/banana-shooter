using System;
using Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    public class UIPerkItem : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public Perk perk;

        private Button btn;

        private void Awake()
        {
            btn = GetComponent<Button>();
            init = true;
        }

        private bool init = false;

        private void OnEnable()
        {
           
            if (init)
            {
                btn.interactable = !PerkManager.Instance.HasPerk(perk);
            }
            else
            {
                btn = GetComponent<Button>();
                btn.interactable = false;
                init = true;
            }
        }

        static string[] key = new string[]
        {
            "WallRun_Description", "Quick Hand_Description", "Forg_Description", "Hit Indicator_Description",
            "Nerd_Description", "Fat_Description", "Bot_Description","Grapple Expert_Description"
        };
        public void OnPointerEnter(PointerEventData eventData)
        {
            PerkMenu.Instance.SetDescription(key[(int)(perk-1)]);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PerkMenu.Instance.CloseDescription();
        }
    }
}