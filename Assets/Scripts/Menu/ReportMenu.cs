
using System.Collections.Generic;

using Multiplayer;
using Multiplayer.Client;
using Multiplayer.Entity.Client;
using Riptide.Transports.Steam;
using SecureServer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using Web;

namespace Menu
{
    public class ReportMenu : MonoBehaviour
    {
        
        public static ReportMenu Instance { get; private set; }

        private static List<ulong> _alreadyReported = new List<ulong>();
        private void Awake()
        {
            Instance = this;
        }

        public enum ReportReasonType
        {
            None,
            WallHack,
            AimHack,
            FlyHack,
            OtherHacks
        }
        

        [SerializeField] private LocalizeStringEvent reportPlayer;
        private ReportReasonType _reasonType=ReportReasonType.None;
        [SerializeField] private GameObject page;

        private ClientPlayer _selectPlayer,_myPlayer=null;

        public void QuickReport(InputAction.CallbackContext obj)
        {
            OpenReportPage(true);
        }
        
        public void OpenReportPage(bool flag)
        {
            if (GameUIManager.Instance.currentSelectPlayer == null ) return;//GameUIManager.Instance.currentSelectPlayer.connectionId == NetworkManager.Instance.Client.Id
            if(flag)
                if (GameUIManager.Instance &&GameUIManager.Instance.pause || NetworkManager.Instance.CheckMultiplayerGameModeStarted() || NetworkManager.Instance.CantPlay()) return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            page.SetActive(true);
            PlayerListItem player = GameUIManager.Instance.currentSelectPlayer;
            if (ClientPlayer.list.TryGetValue(player.connectionId, out var p))
                _selectPlayer = p;
            reportPlayer.StringReference.Arguments = new List<object>() {$"<b>{player.playerName}</b>"};
            reportPlayer.RefreshString();

        }

        
        public void SendReport()
        {
            GameUIManager.Instance.gameScene.SetActive(true);
            page.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState =  CursorLockMode.Locked;
            if (_alreadyReported.Contains(_selectPlayer.playerState.SteamId))
            {
                return;
            }
            if (_myPlayer!=null || ClientPlayer.list.TryGetValue(NetworkManager.Instance.Client.Id, out _myPlayer))
            {
                _alreadyReported.Add(_selectPlayer.playerState.SteamId);
                // ReportMessage report = new ReportMessage(_selectPlayer.SteamId, _myPlayer.SteamId, _reasonType,
                //     _moreDetailed +"\n" + recentChat, DateTime.UtcNow,_selectPlayer.Username,_myPlayer.Username);

                // StartCoroutine(SetValue(json,_myPlayer));

                ReportPlayerCheatingItem cheating = new ReportPlayerCheatingItem(_selectPlayer.playerState.SteamId,
                    _myPlayer.playerState.SteamId, (ulong) _reasonType, false, false, true, NetworkManager.Instance.ConnectionString);
                
                SetValue(cheating);
            }
            
            
        }

        async void SetValue(ReportPlayerCheatingItem reportPlayerCheatingItem)
        {
            string result =await HttpClient.Post(EndPoint.ReportCheating, reportPlayerCheatingItem);
            
            Debug.Log(result);

            NotificationMenu.Instance.NewItem("nc_message","nc_report_sent_complete");
        }

        
        public void ChangeReason(int i)
        {
            _reasonType = (ReportReasonType) (i+1);
        }

        public bool IsReporting()
        {
            return page.activeSelf;
        }

       
    }
}
