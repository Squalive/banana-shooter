using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using Steamworks;
using Steamworks.NET;
using UnityEngine;

namespace Multiplayer
{
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance;

        public static bool Initialized = false;
        public int targetHighScore,targetRank=-1;
        public float parkourTime = -1;

        private int _progress = 0; // 9
        private void Awake()
        {
            if(Instance==null)
                Instance = this;
        }
    
        private CallResult<LeaderboardFindResult_t> leaderBoardTargetFindResult = new CallResult<LeaderboardFindResult_t>();
        private CallResult<LeaderboardFindResult_t> leaderBoardParkourFindResult = new CallResult<LeaderboardFindResult_t>();
        private CallResult<LeaderboardFindResult_t> winFindResult = new CallResult<LeaderboardFindResult_t>();
        private CallResult<LeaderboardScoresDownloaded_t> leaderBoardScoresDownloaded = new CallResult<LeaderboardScoresDownloaded_t>();
        private CallResult<LeaderboardScoresDownloaded_t> leaderBoardScoresDownloadedRank = new CallResult<LeaderboardScoresDownloaded_t>();
        private CallResult<LeaderboardScoresDownloaded_t> leaderBoardTimeDownloadedRank = new CallResult<LeaderboardScoresDownloaded_t>();
        private CallResult<LeaderboardScoresDownloaded_t> leaderBoardTimeDownloaded = new CallResult<LeaderboardScoresDownloaded_t>();
        private CallResult<LeaderboardScoreUploaded_t> leaderBoardScoresUpload = new CallResult<LeaderboardScoreUploaded_t>();
    
        private CallResult<LeaderboardScoresDownloaded_t> winDownload = new CallResult<LeaderboardScoresDownloaded_t>();
        private CallResult<LeaderboardScoresDownloaded_t> winDownloadRank = new CallResult<LeaderboardScoresDownloaded_t>();

        public void Refresh()
        {
            if (!SteamManager.Initialized) return;

            StartCoroutine(Fresh());
        }

        IEnumerator Fresh()
        {
            while (!RolesManager.Initialized)
            {
                yield return null;
            }
            GetHighScore();
            leaderBoardTargetFindResult.Set(SteamUserStats.FindLeaderboard("Target Score"),OnResultTargetScoreFind);
            leaderBoardParkourFindResult.Set(SteamUserStats.FindLeaderboard("Parkour Time"),OnResultParkourTimeFind);
            winFindResult.Set(SteamUserStats.FindLeaderboard("Win"),OnResultWinFind);
            int seconds = 5;
            while (seconds>0)
            {
                yield return new WaitForSeconds(1f);
                seconds--;
            }

            Initialized = true;
        }

        void CheckProgress()
        {
            _progress++;
            Preload.Instance.NextStep();
            if (_progress >= 9)
            {
                Initialized = true;
            }
        }

        private void OnTargetRankDownloaded(LeaderboardScoresDownloaded_t param,bool fail)
        {
            CheckProgress();
            if (fail) return;
            for (int i = 0; i < param.m_cEntryCount; i++)
            {
                LeaderboardEntry_t leaderboardEntryT;
                SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, i, out leaderboardEntryT,
                    null, 0);
            
                targetRank=leaderboardEntryT.m_nGlobalRank;
            
                myScore = new LeaderBoardIndex(leaderboardEntryT.m_steamIDUser.m_SteamID, parkourRank,leaderboardEntryT.m_nScore);

                if (LeaderBoardMenu.Instance)
                {
                    LeaderBoardMenu.Instance.scoreItem.SetValue(myScore.steamId,myScore.rank,myScore.score);
                }
            }
            if (ShootingTarget2.Instance)
            {
                ShootingTarget2.Instance.highScoreText.SetText($"Highest Score: {targetHighScore}\nRank: {targetRank}");
            }
        }
        private void OnTargetScoresDownloaded(LeaderboardScoresDownloaded_t param,bool fail)
        {
            CheckProgress();
            if (fail) return;
            for (int i = 0; i < param.m_cEntryCount; i++)
            {
                LeaderboardEntry_t leaderboardEntryT;
                SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, i, out leaderboardEntryT,
                    null, 0);
                if(i<8)target2LeaderBoard +=
                    $"  {leaderboardEntryT.m_nGlobalRank}.{Chat.Instance.GetPlayerName(leaderboardEntryT.m_steamIDUser.m_SteamID)} Score: {leaderboardEntryT.m_nScore}\n";
            
                LeaderBoardIndex index = new LeaderBoardIndex(leaderboardEntryT.m_steamIDUser.m_SteamID,
                    leaderboardEntryT.m_nGlobalRank,leaderboardEntryT.m_nScore);
            
                scoreIndex.Add(index);

                if (LeaderBoardMenu.Instance)
                {
                    LeaderBoardItem item =
                        Instantiate(PrefabManager.Instance.GetPrefab("LeaderBoardItem"),
                            LeaderBoardMenu.Instance.targetScoreContent).GetComponent<LeaderBoardItem>();
                
                    item.SetValue(index.steamId,index.rank,index.score);
                }
            }

            if (ShootingTarget2.Instance)
            {
                ShootingTarget2.Instance.leaderBoard.SetText(target2LeaderBoard);
            }
        }
        private void OnParkourRankDownloaded(LeaderboardScoresDownloaded_t param,bool fail)
        {
            CheckProgress();
            if (fail) return;
            for (int i = 0; i < param.m_cEntryCount; i++)
            {
                LeaderboardEntry_t leaderboardEntryT;
                SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, i, out leaderboardEntryT,
                    null, 0);
            
                parkourRank = leaderboardEntryT.m_nGlobalRank;

                myParkour = new LeaderBoardIndex(leaderboardEntryT.m_steamIDUser.m_SteamID, parkourRank,leaderboardEntryT.m_nScore);

                if (LeaderBoardMenu.Instance)
                {
                    LeaderBoardMenu.Instance.parkourItem.SetValue(myParkour.steamId,myParkour.rank,myParkour.score);
                }
            }

            // if (ShootingRange.Instance)
            // {
            //     ShootingRange.Instance.parkourRank.SetText(
            //         $"{parkourTime.ToString()}\nRank: {parkourRank.ToString()}");
            // }
        }
        private void OnParkourScoresDownloaded(LeaderboardScoresDownloaded_t param,bool fail)
        {
            CheckProgress();
            if (fail) return;
            for (int i = 0; i < param.m_cEntryCount; i++)
            {
                LeaderboardEntry_t leaderboardEntryT;
                SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, i, out leaderboardEntryT,
                    null, 0);
                if(i<8)
                    leaderBoardParkour+=$" {leaderboardEntryT.m_nGlobalRank}.{Chat.Instance.GetPlayerName(leaderboardEntryT.m_steamIDUser.m_SteamID)} Time: {leaderboardEntryT.m_nScore}\n";
            
                LeaderBoardIndex index = new LeaderBoardIndex(leaderboardEntryT.m_steamIDUser.m_SteamID,
                    leaderboardEntryT.m_nGlobalRank,leaderboardEntryT.m_nScore);
            
                parkourIndex.Add(index);

                if (LeaderBoardMenu.Instance)
                {
                    LeaderBoardItem item =
                        Instantiate(PrefabManager.Instance.GetPrefab("LeaderBoardItem"),
                            LeaderBoardMenu.Instance.parkourContent).GetComponent<LeaderBoardItem>();
                
                    item.SetValue(index.steamId,index.rank,index.score);
                }
            }

            if (ShootingRange.Instance)
            {
                ShootingRange.Instance.leaderBoardParkour.SetText(leaderBoardParkour);
            }
        }

        private SteamLeaderboard_t targetScoreLB, parkourTimeLB,winLB;

        private void OnResultTargetScoreFind(LeaderboardFindResult_t param, bool fail)
        {
            CheckProgress();
            if (fail) return;
            targetScoreLB = param.m_hSteamLeaderboard;
            GetRank();
            GetLeaderBoard();
        }
        private void OnResultParkourTimeFind(LeaderboardFindResult_t param,bool fail)
        {
            CheckProgress();
            if (fail) return;
            parkourTimeLB = param.m_hSteamLeaderboard;
            GetParkourTimeRank();
            GetLeaderBoardParkourTime();
        
        }
        private void OnResultWinFind(LeaderboardFindResult_t param,bool fail)
        {
            CheckProgress();
            if (fail) return;
            winLB = param.m_hSteamLeaderboard;
            winIndex.Clear();
            winDownload.Set(SteamUserStats.DownloadLeaderboardEntries(winLB, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,0,100),OnWinDownload);

            myWin = null;
            winRank = 0;
            winDownloadRank.Set(SteamUserStats.DownloadLeaderboardEntries(winLB, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,0,0),OnWinRankDownload);
        }

        public int winRank;
        private void OnWinRankDownload(LeaderboardScoresDownloaded_t param, bool biofailure)
        {
            CheckProgress();
            if (biofailure) return;
            for (int i = 0; i < param.m_cEntryCount; i++)
            {
                LeaderboardEntry_t leaderboardEntryT;
                SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, i, out leaderboardEntryT,
                    null, 0);
            
                winRank = leaderboardEntryT.m_nGlobalRank;

                myWin = new LeaderBoardIndex(leaderboardEntryT.m_steamIDUser.m_SteamID,winRank,leaderboardEntryT.m_nScore);

                if (LeaderBoardMenu.Instance)
                {
                    LeaderBoardMenu.Instance.winItem.SetValue(myWin.steamId,myWin.rank,myWin.score);
                }
            }
        }

        [Serializable]
        public class LeaderBoardIndex
        {
            public ulong steamId;
            public int rank,score;

            public LeaderBoardIndex(ulong id, int r,int score)
            {
                rank = r;
                steamId = id;
                this.score = score;
            }
        }

        public List<LeaderBoardIndex> winIndex = new List<LeaderBoardIndex>();
        public List<LeaderBoardIndex> parkourIndex = new List<LeaderBoardIndex>();
        public List<LeaderBoardIndex> scoreIndex = new List<LeaderBoardIndex>();

        public LeaderBoardIndex myWin,myParkour,myScore;
        private void OnWinDownload(LeaderboardScoresDownloaded_t param, bool biofailure)
        {
            CheckProgress();
            if (biofailure) return;
            for (int i = 0; i < param.m_cEntryCount; i++)
            {
                LeaderboardEntry_t leaderboardEntryT;
                SteamUserStats.GetDownloadedLeaderboardEntry(param.m_hSteamLeaderboardEntries, i, out leaderboardEntryT,
                    null, 0);

                LeaderBoardIndex index = new LeaderBoardIndex(leaderboardEntryT.m_steamIDUser.m_SteamID,
                    leaderboardEntryT.m_nGlobalRank,leaderboardEntryT.m_nScore);
            
                winIndex.Add(index);

                if (LeaderBoardMenu.Instance)
                {
                    LeaderBoardItem item =
                        Instantiate(PrefabManager.Instance.GetPrefab("LeaderBoardItem"),
                            LeaderBoardMenu.Instance.winContent).GetComponent<LeaderBoardItem>();
                
                    item.SetValue(index.steamId,index.rank,index.score);
                }
            }

        }

        private void OnUploadScore(LeaderboardScoreUploaded_t param, bool failure)
        {
            Debug.Log(failure || !Convert.ToBoolean(param.m_bSuccess) ? "Upload score failure" : "Upload score success");
            if (param.m_hSteamLeaderboard == parkourTimeLB)
            {
                GetParkourTimeRank();
                GetLeaderBoardParkourTime();
            }
            else if (param.m_hSteamLeaderboard == targetScoreLB)
            {
                GetRank();
                GetLeaderBoard();
            }
        }

        void GetHighScore()
        {
            if (SteamUserStats.RequestCurrentStats())
            {
                SteamUserStats.GetStat("TARGET_SCORE", out targetHighScore);
            }
        }

        public void GetRank()
        {
            myScore = null;
            leaderBoardScoresDownloadedRank.Set(SteamUserStats.DownloadLeaderboardEntries(targetScoreLB, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,0,0),OnTargetRankDownloaded);
        }
    
        public void UploadTargetScore()
        {
            leaderBoardScoresUpload.Set(SteamUserStats.UploadLeaderboardScore(targetScoreLB,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, targetHighScore, null, 0),OnUploadScore);
        }
        public void UploadParkourTime()
        {
            leaderBoardScoresUpload.Set(SteamUserStats.UploadLeaderboardScore(parkourTimeLB,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, (int)parkourTime, null, 0),OnUploadScore);
        }
        public void UploadWin(int win)
        {
            leaderBoardScoresUpload.Set(SteamUserStats.UploadLeaderboardScore(winLB,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, win, null, 0),OnUploadScore);
        }
        public string target2LeaderBoard;
        void GetLeaderBoard()
        {
            target2LeaderBoard = "\n";
            leaderBoardScoresDownloaded.Set(
                SteamUserStats.DownloadLeaderboardEntries(targetScoreLB,
                    ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal, 0, 100), OnTargetScoresDownloaded);
        }

        public int parkourRank;
        void GetParkourTimeRank()
        {
            if (SteamUserStats.RequestCurrentStats())
            {
                SteamUserStats.GetStat("PARKOUR_TIME", out parkourTime);
            }

            myParkour = null;
            leaderBoardTimeDownloadedRank.Set(SteamUserStats.DownloadLeaderboardEntries(parkourTimeLB, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,0,0),OnParkourRankDownloaded);
        }

    
        public string leaderBoardParkour,parkourTimeRankText;
        void GetLeaderBoardParkourTime()
        {
            leaderBoardParkour = "";
            parkourIndex.Clear();
            leaderBoardTimeDownloaded.Set(SteamUserStats.DownloadLeaderboardEntries(parkourTimeLB, ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,0,100),OnParkourScoresDownloaded);
        }
    }
}
