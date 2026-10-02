
using System.Collections.Generic;
using Manager;
using UnityEngine;

namespace Menu
{
    public class PerkInGameMenu : MonoBehaviour
    {
        public static PerkInGameMenu Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        [SerializeField] private CanvasGroup canvasGroup;

        private float _desiredAlpha = 0f;

        public List<PerkMenu.PerkItemUI> items = new List<PerkMenu.PerkItemUI>();
        public void Enable()
        {
            int i = 0;
            foreach (var item in items)
            {
                int index = (int) PerkManager.Instance.perks[i] - 1;
                PerkManager.PerkItem perkItem = PerkManager.Instance.perkItems[index];
                item.icon.texture = perkItem.texture2D;
                item.text.SetEntry(perkItem.key);
                item.bg.color = perkItem.GetRarityColor();
                i++;
            }
            
            Invoke(nameof(Display),1f);
            Invoke(nameof(Clear),4.2f);
        }
        void Display()
        {
            _desiredAlpha = 1;
        }
        void Clear()
        {
            _desiredAlpha = 0;
        }

        private void Update()
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, _desiredAlpha, Time.deltaTime * 5f);
        }
    }
}
