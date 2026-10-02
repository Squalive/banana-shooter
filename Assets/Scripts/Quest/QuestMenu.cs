
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Localization.Components;

namespace Quest
{
    public class QuestMenu : MonoBehaviour
    {
       
        public static QuestMenu Instance { get; private set; }
        
        [SerializeField] private List<QuestItemUI> items = new List<QuestItemUI>();
        [SerializeField] private List<QuestItemUI> bonusItems = new List<QuestItemUI>();

        [SerializeField] private LocalizeStringEvent refreshTimer;
        public GameObject desc;
        public LocalizeStringEvent descText;
        private float timer;
        private void Start()
        {
            Refresh();
            RefreshQuestObj();
            RefreshBonusQuestObj();
        }

        private void OnEnable()
        {
            QuestManager.Instance.RefreshQuest += Refresh;
            QuestManager.Instance.OnClaimed += Refresh;
            QuestManager.Instance.OnProgressChanged += Refresh;
        }

        private void OnDisable()
        {
            QuestManager.Instance.RefreshQuest -= Refresh;
            QuestManager.Instance.OnClaimed -= Refresh;
            QuestManager.Instance.OnProgressChanged -= Refresh;
        }

        void Refresh()
        {
            //Check Bonus
            bool bns = true;
            for (int i = 0; i < items.Count; i++)
            {
                QuestItemUI itemUI = items[i];
                Quest quest = QuestManager.Instance._currentQuest[i];
                if (!quest.IsClaim) bns = false;
                itemUI.SetValue(quest);
            }

            if (bns)
            {
                bonusUI.SetActive(true);
                for (int i = 0; i < bonusItems.Count; i++)
                {
                    QuestItemUI itemUI = bonusItems[i];
                    Quest quest = QuestManager.Instance._currentQuest[i+3];
                    itemUI.SetValue(quest);
                }
            }
            

            timer = (float)(QuestManager.delay - (DateTime.Now - QuestManager.Instance.LastRecordTime).TotalSeconds);
            TimeSpan sp = TimeSpan.FromSeconds(timer);
            refreshTimer.StringReference.Arguments = new List<object>() {$"{(int) sp.TotalHours}:{sp.Minutes:00}:{sp.Seconds:00}"};
            refreshTimer.RefreshString();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            TimeSpan sp = TimeSpan.FromSeconds(timer);
            refreshTimer.StringReference.Arguments[0] = $"{(int) sp.TotalHours}:{sp.Minutes:00}:{sp.Seconds:00}";
            refreshTimer.RefreshString();
        }

        private void Awake()
        {
            Instance = this;
        }

        private static bool _displayQuest = true;
        private static bool _displayBonusQuest = true;

        [SerializeField] private GameObject quest;

        [SerializeField] private GameObject bonusBg, bonusObj,bonusUI;
        
        public void DisplayQuest()
        {
            _displayQuest = !_displayQuest;
            RefreshQuestObj();
        }

        void RefreshBonusQuestObj()
        {
            bonusBg.SetActive(_displayBonusQuest);
            bonusObj.SetActive(_displayBonusQuest);
        }
        void RefreshQuestObj()
        {
            quest.SetActive(_displayQuest);
            foreach (var item in items)
            {
                item.gameObject.SetActive(_displayQuest);
            }
        }
        
        public void DisplayBonusQuest()
        {
            _displayBonusQuest = !_displayBonusQuest;
            RefreshBonusQuestObj();
        }
    }
}
