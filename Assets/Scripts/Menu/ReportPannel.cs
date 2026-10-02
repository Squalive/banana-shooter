
using System;
using System.Collections.Generic;

using Manager;
using Newtonsoft.Json;
using SecureServer;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Web;

namespace Menu
{
    public class ReportPannel : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private ReportItemUI itemPrefab;

        [SerializeField] private TMP_InputField searchBar;

        [SerializeField] private DateInputField timeBeginInput,timeEndInput;

        [SerializeField] private Button nextPage, previewPage;

        [SerializeField] private TextMeshProUGUI pageText;

        [SerializeField] private GameObject reportInfo, loading, reportView;

        private int _pageIndex=0;

        private const int MaxAmountInPage = 25;

        public List<List<CheatingReportItem>> Pages = new List<List<CheatingReportItem>>();

        private ReportItemUI[] _itemUis = new ReportItemUI[MaxAmountInPage];

        [SerializeField] private LocalizeStringEvent reportAmountText,reportIdText,
            steamIdText,
            steamIdReporterText,
            nameText,
            nameReporterText,
            reasonText,
            reportDateText;

        private void Awake()
        {
            searchBar.onEndEdit.AddListener(Search);

            for (int i = 0; i < MaxAmountInPage; i++)
            {
                ReportItemUI item = Instantiate(itemPrefab, content);

                _itemUis[i] = item;
                
                item.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            timeBeginInput.SetDateTime(DateTime.UtcNow.AddDays(-1));
            timeEndInput.SetDateTime(DateTime.UtcNow);
        }

        private void OnEnable()
        {
            timeBeginInput.OnDateSet += Refresh;
            timeEndInput.OnDateSet += Refresh;
            
            nextPage.onClick.AddListener(delegate { NextPage(1); });
            previewPage.onClick.AddListener(delegate { NextPage(-1); });
        }

        private void OnDisable()
        {
            timeBeginInput.OnDateSet -= Refresh;
            timeEndInput.OnDateSet -= Refresh;
            
            nextPage.onClick.RemoveAllListeners();
            previewPage.onClick.RemoveAllListeners();
        }

        void SetPage()
        {
            pageText.SetText(_pageIndex.ToString());
            if (_pageIndex < Pages.Count)
            {
                foreach (var t in _itemUis)
                {
                    t.gameObject.SetActive(false);
                }

                int i = 0;
                foreach (var cheatingReport in Pages[_pageIndex])
                {
                    _itemUis[i].gameObject.SetActive(true);
                    _itemUis[i].SetValue(cheatingReport,this);
                    
                    ++i;
                }
            }
        }

        void NextPage(int add)
        {
            _pageIndex += add;
            
            if(_pageIndex < 0) _pageIndex = Pages.Count-1;
            else if (_pageIndex >= Pages.Count) _pageIndex = 0;
            
            SetPage();
        }

        private void Search(string arg0)
        {
            
        }
        

        public async void DisplayInfo(CheatingReportItem item)
        {
            reportInfo.SetActive(true);
            
            reportView.SetActive(false);
            loading.SetActive(true);

            int reportAccount = 0;

            foreach (var page in Pages)
            {
                foreach (var cheatingReport in page)
                {
                    if (cheatingReport.SteamId == item.SteamId)
                    {
                        ++reportAccount;
                    }
                }
            }
            
            // Debug.Log(reportAccount);

            reportAmountText.StringReference.Arguments = new List<object>() {$"<color=green>{reportAccount}</color>"};
            reportIdText.StringReference.Arguments = new List<object>() {$"<color=green>{item.ReportId}</color>"};
            steamIdText.StringReference.Arguments = new List<object>() {$"<color=red>{item.SteamId}</color>"};
            steamIdReporterText.StringReference.Arguments = new List<object>() {$"<color=#00bde8>{item.SteamIdReporter}</color>"};
            reasonText.StringReference.Arguments = new List<object>() {(ReportMenu.ReportReasonType)item.AppData};
            reportDateText.StringReference.Arguments = new List<object>() {GameManager.JavaTimeStampToDateTime(item.TimeReport)};

            nameText.StringReference.Arguments = null;
            nameReporterText.StringReference.Arguments = null;
            
            PlayerSummaryResponse list = await HttpClient.Get<PlayerSummaryResponse>($"{EndPoint.GetPlayerSummaries}?steamids={item.SteamId},{item.SteamIdReporter}");

            if (list != null && list.response != null && list.response.players!= null)
            {
                foreach (var playerSummary in list.response.players)
                {
                    if (playerSummary.SteamId == item.SteamId)
                    {
                        nameText.StringReference.Arguments = new List<object>() {$"<color=red>{playerSummary.PersonaName}</color>"};
                    }
                    if (playerSummary.SteamId == item.SteamIdReporter)
                    {
                        nameReporterText.StringReference.Arguments = new List<object>() {$"<color=#00bde8>{playerSummary.PersonaName}</color>"};
                    }
                }
            }
            
            loading.SetActive(false);
            reportView.SetActive(true);
        }
        
        
        public class PlayerSummaryResponse
        {
            public class PlayerSummaryResult
            {
                public List<PlayerSummary> players{ get; set; }
            }

            public PlayerSummaryResult response;
        }

        
        public void Refresh()
        {
            _pageIndex = 0;
            Pages.Clear();
            reportInfo.SetActive(false);
            RefreshData();
        }

        public class CheatingReportResponse
        {
            public class CheatReportResult
            {
                public List<CheatingReportItem> results{ get; set; }
            }

            public CheatReportResult response;
        }
        async void RefreshData()
        {
            if (!GameManager.Instance.CheckAdminOrHelper()) return;

            uint timebegin = (uint)timeBeginInput.GetDateTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            uint timeend = (uint)timeEndInput.GetDateTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            
            
            CheatingReportResponse list = await HttpClient.Get<CheatingReportResponse>($"{EndPoint.GetCheatingReports}?timebegin={timebegin}&timeend={timeend}");

            if (list != null && list.response!=null && list.response.results!=null)
            {
                int index = 0;
                Pages.Add(new List<CheatingReportItem>());
                int totalIndex = 0;
                foreach (var cheatingReport in list.response.results)
                {
                    Pages[index].Add(cheatingReport);

                    if (Pages[index].Count >= MaxAmountInPage)
                    {
                        if (totalIndex + 1 < list.response.results.Count)
                        {
                            Pages.Add(new List<CheatingReportItem>());
                            ++index;
                        }
                    }

                    ++totalIndex;
                }
                
                // Pages.Add(new List<CheatingReportItem>(reports));
                        
                // reports.Clear();

                _pageIndex = 0;
                
                SetPage();
            }
            
            
            NotificationMenu.Instance.NewItem("nc_message","nc_reports_refresh");
        }

        [HideInInspector]
        public string reporterId,reportedId;


        public void CopyReporterId()
        {
            NotificationMenu.Instance.NewItem("nc_message","nc_copy_complete");
            GUIUtility.systemCopyBuffer = reporterId;
            EventSystem.current.SetSelectedGameObject(null);
        }
        public void CopyReportedId()
        {
            NotificationMenu.Instance.NewItem("nc_message","nc_copy_complete");
            GUIUtility.systemCopyBuffer = reportedId;
            EventSystem.current.SetSelectedGameObject(null);
        }

        private ReportItemUI _selectItem;

        // public void ManagePlayer(ReportItemUI itemUI)
        // {
        //     if (GameManager.Instance.CheckAdminOrHelper()) return;
        //     _selectItem = itemUI;
        //     managePlayerText.StringReference.Arguments = new List<object>() {_selectItem.reportedName};
        //     managePlayerText.RefreshString();
        //     descInputField.text = "";
        //     manageObj.SetActive(true);
        // }
    }
    
}