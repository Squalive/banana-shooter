using System;
using System.Collections.Generic;
using System.Linq;

using Steamworks;
using SteamWorkshop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class WorkshopMenu : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private WorkshopItemUI prefab;
        [SerializeField] private Transform content;
        
        private List<List<Toggle>> _types = new();
        
        [Header("Subscribed")] 
        [SerializeField] private Toggle subscribedToggle;
        [SerializeField] private Toggle[] subscribedTypes = Array.Empty<Toggle>();
        [Space]
        [Header("Recent Popular")] 
        [SerializeField] private Toggle popularToggle;
        [SerializeField] private Toggle[] popularTypes = Array.Empty<Toggle>();
        
        [Space]
        [Header("Most Popular")] 
        [SerializeField] private Toggle mostPopularToggle;
        [SerializeField] private Toggle[] mostPopularTypes = Array.Empty<Toggle>();
        
        [Space]
        [Header("Latest")] 
        [SerializeField] private Toggle latestToggle;
        [SerializeField] private Toggle[] latestTypes = Array.Empty<Toggle>();
        
        [Space]
        [Header("Followed")] 
        [SerializeField] private Toggle followedToggle;
        [SerializeField] private Toggle[] followedTypes = Array.Empty<Toggle>();
        
        [Space]
        [Header("Favourite")] 
        [SerializeField] private Toggle favouriteToggle;
        [SerializeField] private Toggle[] favouriteTypes = Array.Empty<Toggle>();
        
        [Space]
        [Header("Friends")] 
        [SerializeField] private Toggle friendsToggle;
        [SerializeField] private Toggle[] friendsTypes = Array.Empty<Toggle>();
        
        [Space]
        [Header("my")] 
        [SerializeField] private Toggle myToggle;
        [SerializeField] private Toggle[] myTypes = Array.Empty<Toggle>();

        [SerializeField] private Button nextPage, previousPage;

        [SerializeField] private TextMeshProUGUI pageText;

        [SerializeField] private GameObject loading, pageSelector;

        [SerializeField] private Transform pageContent;

        [SerializeField] private Toggle pagePrefab;

        [SerializeField] private ToggleGroup pageGroup;
        public enum SearchType
        {
            Subscribed,
            Popular,
            MostPopular,
            Latest,
            Followed,
            Favourite,
            Friends,
            My
        }
        
        public enum ItemType
        {
            All,
            Map,
        }
        
        [Space]
        public ItemType itemType = ItemType.All;
        public SearchType searchType = SearchType.Subscribed;

        private WorkshopItemUI[] _items = new WorkshopItemUI[24];

        private UGCQueryHandle_t _queryHandleT;
        
        private CallResult<SteamUGCQueryCompleted_t> _queryResult;

        private Dictionary<uint, List<WorkshopItem>> _pages = new();

        private List<Toggle> _pageToggles = new();

        private const uint MaxPageAmount = 24;

        private uint _pageIndex = 1;

        private uint _maxPageIndex = 1;

        private uint _maxResult = 0;
        private void Start()
        {
            _queryResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted);
            
            _types.Add(subscribedTypes.ToList());
            _types.Add(popularTypes.ToList());
            _types.Add(mostPopularTypes.ToList());
            _types.Add(latestTypes.ToList());
            _types.Add(followedTypes.ToList());
            _types.Add(favouriteTypes.ToList());
            _types.Add(friendsTypes.ToList());
            _types.Add(myTypes.ToList());
            
            subscribedToggle.onValueChanged.AddListener(delegate(bool arg0) { Search(arg0,SearchType.Subscribed); });
            popularToggle.onValueChanged.AddListener(delegate(bool arg0) { Search(arg0,SearchType.Popular); });
            mostPopularToggle.onValueChanged.AddListener(delegate(bool arg0) { Search(arg0,SearchType.MostPopular); });
            latestToggle.onValueChanged.AddListener(delegate(bool arg0) { Search(arg0,SearchType.Latest); });
            favouriteToggle.onValueChanged.AddListener(delegate(bool arg0) { Search(arg0,SearchType.Favourite); });
            followedToggle.onValueChanged.AddListener(delegate(bool arg0) { Search(arg0,SearchType.Followed); });
            friendsToggle.onValueChanged.AddListener(delegate(bool arg0) { Search(arg0,SearchType.Friends); });
            myToggle.onValueChanged.AddListener(delegate(bool arg0) { Search(arg0,SearchType.My); });

            for (int i = 0; i < 24; i++)
            {
                _items[i] = Instantiate(prefab, content);
                
                _items[i].gameObject.SetActive(false);
            }
            
            nextPage.onClick.AddListener(delegate { NextPage(1); });
            previousPage.onClick.AddListener(delegate { NextPage(-1); });
        }
        void Search(bool flag,SearchType type)
        {
            if (flag)
            {
                _pages.Clear();
                searchType = type;
                _pageIndex = 1;
                pageText.SetText(_pageIndex.ToString());
                itemType = ItemType.All;

                foreach (var t in _types)
                {
                    foreach (var toggle in t)
                    {
                        toggle.SetIsOnWithoutNotify(false);
                    }
                }

                Refresh(_pageIndex);
                // var handle = SteamUGC.CreateQueryAllUGCRequest()
            }
        }

        
        public void OpenMenu()
        {
            subscribedToggle.isOn = true;
            
            Search(true,SearchType.Subscribed);
        }

        void Refresh(uint page)
        {
            loading.SetActive(true);
            pageSelector.SetActive(false);
            foreach (var item in _items)
            {
                item.gameObject.SetActive(false);
            }

            SteamUGC.ReleaseQueryUGCRequest(_queryHandleT);

            switch (searchType)
            {
                case SearchType.Subscribed:
                    _queryHandleT = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(),
                        EUserUGCList.k_EUserUGCList_Subscribed, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
                        EUserUGCListSortOrder.k_EUserUGCListSortOrder_SubscriptionDateDesc,
                        (AppId_t)1949740, (AppId_t)1949740, page);
                    break;
                case SearchType.Popular:
                    _queryHandleT = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByTrend,
                        EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
                        (AppId_t)1949740, (AppId_t)1949740, page);
                    break;
                case SearchType.MostPopular:
                    _queryHandleT = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByTotalUniqueSubscriptions,
                        EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
                        (AppId_t)1949740, (AppId_t)1949740, page);
                    break;
                case SearchType.Latest:
                    _queryHandleT = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByPublicationDate,
                        EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
                        (AppId_t)1949740, (AppId_t)1949740, page);
                    break;
                case SearchType.Followed:
                    _queryHandleT = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(),
                        EUserUGCList.k_EUserUGCList_Followed, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
                        EUserUGCListSortOrder.k_EUserUGCListSortOrder_SubscriptionDateDesc,
                        (AppId_t)1949740, (AppId_t)1949740, page);
                    break;
                case SearchType.Favourite:
                    _queryHandleT = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(),
                        EUserUGCList.k_EUserUGCList_Favorited, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
                        EUserUGCListSortOrder.k_EUserUGCListSortOrder_SubscriptionDateDesc,
                        (AppId_t)1949740, (AppId_t)1949740, page);
                    break;
                case SearchType.Friends:
                    _queryHandleT = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_CreatedByFriendsRankedByPublicationDate,
                        EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
                        (AppId_t)1949740, (AppId_t)1949740, page);
                    break;
                case SearchType.My:
                    _queryHandleT = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(),
                        EUserUGCList.k_EUserUGCList_Published, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
                        EUserUGCListSortOrder.k_EUserUGCListSortOrder_SubscriptionDateDesc,
                        (AppId_t)1949740, (AppId_t)1949740, page);
                    break;
            }

            var call = SteamUGC.SendQueryUGCRequest(_queryHandleT);

            _queryResult.Set(call);
        }
        void NextPage(int offset,int overridePage = -1)
        {
            _pageIndex = (uint)(_pageIndex+offset);

            if (overridePage != -1)
            {
                _pageIndex = (uint)overridePage;
            }

            if (_pageIndex < 1)
            {
                _pageIndex = 1;
                return;
            }

            if(_pageIndex > _maxPageIndex)
            {
                _pageIndex = _maxPageIndex;
                return;
            }

            if (_pageIndex <= _pageToggles.Count)
            {
                _pageToggles[(int)((_pageIndex - 1)%24)].SetIsOnWithoutNotify(true);
            }

            pageText.SetText(_pageIndex.ToString());

            int count = 0;

            foreach (var l in _pages.Values)
            {
                count += l.Count;
            }
            if (_pages.TryGetValue(_pageIndex, out var list))
            {
                if (list.Count >= MaxPageAmount || count>=_maxResult)
                {
                    foreach (var item in _items)
                    {
                        item.gameObject.SetActive(false);
                    }
                    for (int i = 0; i < list.Count; i++)
                    {
                        var detail = list[i];

                        WorkshopItemUI item = _items[i];
                
                        item.gameObject.SetActive(true);
                
                        item.Initialize(detail);
                    }
                }
                else
                {
                    Refresh((uint)(count / MaxPageAmount));
                }
            }
            else
            {
                Refresh((uint)(count / MaxPageAmount));
            }
            
            
        }

        void SetPage(bool flag,int page)
        {
            if(flag)
                NextPage(0,page);
        }
        
        private void OnUGCQueryCompleted(SteamUGCQueryCompleted_t result, bool biofailure)
        {
            _maxResult = result.m_unTotalMatchingResults;
            _maxPageIndex = result.m_unTotalMatchingResults / MaxPageAmount + (uint)(result.m_unTotalMatchingResults % 24 == 0 ? 0 : 1);

            _pageToggles.Clear();
            
            for (int i = 0; i < pageContent.childCount; i++)
            {
                Destroy(pageContent.GetChild(i).gameObject);
            }

            for (int i = 1; i <= (_maxPageIndex > 24 ? 24 : _maxPageIndex); i++)
            {
                Toggle toggle = Instantiate(pagePrefab, pageContent);

                toggle.group = pageGroup;

                toggle.isOn = i == _pageIndex;

                var i1 = i;
                toggle.onValueChanged.AddListener(delegate(bool arg0) { SetPage(arg0, i1); });
                
                _pageToggles.Add(toggle);
            }
            
            if (biofailure || result.m_unNumResultsReturned == 0)
            {
                loading.SetActive(false);
                // Failed to retrieve workshop item info
                Debug.Log("Failed to retrieve workshop item info");
                if (SteamUGC.ReleaseQueryUGCRequest(_queryHandleT))
                {
                    // Debug.Log("Release Query Request");
                }
                return;
            }

            uint offset = 0;
            
            for (uint i = 0; i < result.m_unNumResultsReturned; i++)
            {
                SteamUGCDetails_t itemDetails;
                if (!SteamUGC.GetQueryUGCResult(_queryHandleT, i, out itemDetails))
                {
                    // Failed to get item details
                    Debug.Log("Failed to get item details");
                    continue;
                }

                if (_pages.TryGetValue(_pageIndex + offset, out var list))
                {
                    while (list.Count >= MaxPageAmount)
                    {
                        offset++;
                        if (_pages.ContainsKey(_pageIndex + offset))
                        {
                            list = _pages[_pageIndex + offset];
                        }
                        else
                        {
                            list = new List<WorkshopItem>();
                            _pages.Add(_pageIndex + offset,list);
                        }
                    }
                }
                else
                {
                    list = new List<WorkshopItem>();
                    _pages.Add(_pageIndex + offset,list);
                }
                
                string url = String.Empty;

                if (SteamUGC.GetQueryUGCPreviewURL(_queryHandleT, i, out var pchURL, 1024))
                {
                    url = pchURL;
                }

                WorkshopItem item = new WorkshopItem(itemDetails.m_nPublishedFileId,(EItemState)SteamUGC.GetItemState(itemDetails.m_nPublishedFileId))
                {
                    Details = itemDetails,
                    previewUrl = url
                };
                list.Add(item);
            }
            loading.SetActive(false);
            pageSelector.SetActive(true);

            for (int i = 0; i < _pages[_pageIndex].Count; i++)
            {
                var detail = _pages[_pageIndex][i];

                WorkshopItemUI item = _items[i];
                
                item.gameObject.SetActive(true);
                
                item.Initialize(detail);
            }
            if (SteamUGC.ReleaseQueryUGCRequest(_queryHandleT))
            {
                // Debug.Log("Release Query Request");
            }
        }

        
        public void ActivateWorkshop(bool flag)
        {
            if(flag)
                SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/app/1949740/workshop/");
        }
    }
}
