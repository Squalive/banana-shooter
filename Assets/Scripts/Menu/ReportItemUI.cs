
using System.Globalization;
using Manager;
using SecureServer;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class ReportItemUI : MonoBehaviour
    {
        private ReportPannel _pannel;
        
        CheatingReportItem _report;

        [SerializeField] private LocalizeStringEvent reasonText;

        [SerializeField] private TextMeshProUGUI reportIdText,
            steamIdText,
            steamIdReporterText,
            heuristicText,
            detectionText,
            playerReportText,
            timeText,
            reportTimesText;

        public void SetValue(CheatingReportItem reportMessage,ReportPannel pannel)
        {
            _pannel = pannel;
            _report = reportMessage;
            
            reportIdText.SetText(reportMessage.ReportId.ToString());
            steamIdText.SetText(reportMessage.SteamId.ToString());
            steamIdReporterText.SetText(reportMessage.SteamIdReporter.ToString());
            heuristicText.SetText(reportMessage.Heuristic ? "True" : "False");
            detectionText.SetText(reportMessage.Detection ? "True" : "False");
            if(reportMessage.Detection)
                detectionText.color = Color.red;
            if(reportMessage.Heuristic)
                detectionText.color = Color.yellow;
            if(reportMessage.PlayerReport)
                detectionText.color = Color.cyan;
            playerReportText.SetText(reportMessage.PlayerReport ? "True" : "False");
            timeText.SetText(GameManager.JavaTimeStampToDateTime(reportMessage.TimeReport).ToString(CultureInfo.InvariantCulture));
            reasonText.SetEntry($"rs_{(ReportMenu.ReportReasonType)(reportMessage.AppData)}");
            
            GetComponent<Button>().onClick.AddListener(DisplayInfo);

            int reportAmount = 0;
            foreach (var page in pannel.Pages)
            {
                foreach (var cheatingReport in page)
                {
                    if (cheatingReport.SteamId == reportMessage.SteamId)
                    {
                        ++reportAmount;
                    }
                }
            }
            
            reportTimesText.SetText(reportAmount.ToString());

            if (reportAmount > 5)
            {
                reportTimesText.color = Color.red;
            }
        }

        void DisplayInfo()
        {
            _pannel.DisplayInfo(_report);
        }

        // void More()
        // {
        //     AudioManager.Instance.PlayButton();
        //     _pannel.RefreshInfo(Report);
        // }
        // void Confirm()
        // {
        //     AudioManager.Instance.PlayButton();
        //     
        //     // confirmBtn.interactable = false;
        //     _pannel.ManagePlayer(this);
        // }
        
        
    }
}
