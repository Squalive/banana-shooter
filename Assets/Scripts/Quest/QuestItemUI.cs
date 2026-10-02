using System;
using System.Collections.Generic;
using Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Quest
{
    public class QuestItemUI : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField] LocalizeStringEvent title, desc;
        [SerializeField] Button claimBtn;
        [SerializeField] Slider progressBar;
        [SerializeField] TextMeshProUGUI progressText;
        [SerializeField] private GameObject complete;
        
        private Quest _quest;

        private void Start()
        {
            _menu = QuestMenu.Instance;
        }

        public void SetValue(Quest quest)
        {
            _quest = quest;
            title.SetEntry(quest.QuestObj.nameKey);
            desc.SetEntry(quest.QuestObj.descKey);
            progressBar.maxValue = quest.QuestObj.requiredAmount;
            progressBar.minValue = 0;
            progressBar.value = quest.Progress;
            claimBtn.interactable = !quest.IsClaim && quest.IsReached();
            claimBtn.onClick.RemoveAllListeners();
            claimBtn.onClick.AddListener(Claim);
            progressText.SetText($"{Mathf.Clamp(quest.Progress,0,quest.QuestObj.requiredAmount)} / {quest.QuestObj.requiredAmount}");
            
            complete.SetActive(quest.IsClaim);
        }
        void Claim()
        {
            AudioManager.Instance.PlayButton();
            _quest.Claim();
        }
        QuestMenu _menu;
        public void OnPointerEnter(PointerEventData eventData)
        {
            _menu.desc.SetActive(true);
            _menu.descText.SetEntry(_quest.QuestObj.prizeKey);
            _menu.descText.StringReference.Arguments = new List<object>() {_quest.QuestObj.expReward};
            _menu.descText.RefreshString();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            _menu.desc.SetActive(false);
            
        }
    }
}
