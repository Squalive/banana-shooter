
using System;
using System.Collections.Generic;
using Audio;

using Multiplayer;
using Multiplayer.Interface;
using Steamworks;
using SteamWorkshop;
using SteamWorkshop.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class CustomMapMenu : MonoBehaviour
    {
        [SerializeField] private WorkshopMapItemUI item;
        [SerializeField] private Transform content;

        private const uint MaxAmount = 16;

        private WorkshopMapItemUI[] _items = new WorkshopMapItemUI[MaxAmount];

        private List<List<WorkshopItem>> _pages = new();

        [SerializeField] private Toggle enableWorkshopToggle;
        [SerializeField] private TextMeshProUGUI pageText;

        [SerializeField] private Button createBtn;

        private int _currentPage = 0;

        [SerializeField] private Transform pageContent;

        private List<Toggle> _pageToggles = new();

        [SerializeField] private Toggle pagePrefab;
        [SerializeField] private ToggleGroup pageGroup;
        private void Awake()
        {
            for (int i = 0; i < MaxAmount; i++)
            {
                WorkshopMapItemUI itemUI = Instantiate(item, content);

                _items[i] = itemUI;

                itemUI.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (SteamWorkshopManager.QueryLoaded)
            {
                InitMaps();
            }
            else
            {
                SteamWorkshopManager.OnQueryLoaded += InitMaps;
            }

            SteamWorkshopManager.OnWorkshopItemInstalled += OnWorkshopItemInstalled;
            SteamWorkshopManager.OnUnSubscribeItem += OnUnSubscribeItem;
            NetworkServerManager.OnEnabledMapsChanged += CheckMaps;

            enableWorkshopToggle.isOn = NetworkServerManager.ServerEnableWorkshop;

            enableWorkshopToggle.onValueChanged.AddListener(SetEnableWorkshop);
        }

        private void OnDisable()
        {
            SteamWorkshopManager.OnQueryLoaded -= InitMaps;
            SteamWorkshopManager.OnWorkshopItemInstalled -= OnWorkshopItemInstalled;
            SteamWorkshopManager.OnUnSubscribeItem -= OnUnSubscribeItem;
            NetworkServerManager.OnEnabledMapsChanged -= CheckMaps;
            enableWorkshopToggle.onValueChanged.RemoveListener(SetEnableWorkshop);
        }

        void CheckMaps()
        {
            if (NetworkServerManager.ServerEnableWorkshop)
                createBtn.interactable = NetworkServerManager.EnabledWorkshopMaps.Count > 0;
            else
                createBtn.interactable = true;
        }

        private void OnWorkshopItemInstalled(WorkshopItem obj)
        {
            if (obj != null)
                AddWorkshopItem(obj);
        }
        private void OnUnSubscribeItem(PublishedFileId_t obj)
        {
            InitMaps();
        }
        void SetEnableWorkshop(bool flag)
        {
            AudioManager.Instance.PlayButton();
            NetworkServerManager.SetServerEnableWorkshop(flag);
            CheckMaps();
        }

        void InitMaps()
        {
            _pages.Clear();
            _currentPage = 0;
            pageText.SetText("1");
            foreach (var workshopItem in SteamWorkshopManager.WorkshopItems.Values)
            {
                AddWorkshopItem(workshopItem);
            }

            Refresh();
        }

        void AddWorkshopItem(WorkshopItem workshopItem)
        {
            bool newPage = false;
            List<WorkshopItem> list;

            if (_pages.Count > 0)
            {
                list = _pages[^1];
            }
            else
            {
                list = new List<WorkshopItem>();
                newPage = true;
            }

            if (list.Count >= MaxAmount)
            {
                list = new List<WorkshopItem>();
                newPage = true;
            }

            list.Add(workshopItem);

            if (newPage)
            {
                _pages.Add(list);
            }
        }

        void Refresh()
        {
            StopAllCoroutines();
            _pageToggles.Clear();

            for (int i = 0; i < pageContent.childCount; i++)
            {
                Destroy(pageContent.GetChild(i).gameObject);
            }
            for (int i = 0; i < _pages.Count; i++)
            {
                Toggle toggle = Instantiate(pagePrefab, pageContent);

                toggle.group = pageGroup;

                toggle.isOn = i == _currentPage;

                var i1 = i;
                toggle.onValueChanged.AddListener(delegate (bool arg0) { SetPage(arg0, i1); });

                _pageToggles.Add(toggle);
            }
            for (int i = 0; i < MaxAmount; i++)
            {
                _items[i].gameObject.SetActive(false);
            }
            if (_pages.Count > 0)
            {
                for (int i = 0; i < _pages[_currentPage].Count; i++)
                {
                    WorkshopItem workshopItem = _pages[_currentPage][i];

                    _items[i].gameObject.SetActive(true);

                    _items[i].Init(workshopItem.Details.m_rgchTitle, workshopItem.previewUrl, workshopItem.fileId, this);
                }
            }
        }

        void SetPage(bool flag, int page)
        {
            if (flag)
            {
                _currentPage = page;

                pageText.SetText((_currentPage + 1).ToString());

                Refresh();
            }
        }


        public void NextPage(int offset)
        {
            _currentPage += offset;

            if (_currentPage >= _pages.Count)
            {
                _currentPage = _pages.Count - 1;
                return;
            }
            if (_currentPage < 0)
            {
                _currentPage = 0;
                return;
            }

            pageText.SetText((_currentPage + 1).ToString());

            Refresh();
        }
    }
}
