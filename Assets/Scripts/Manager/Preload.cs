
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using CodingDaniel.MapEditor.MEEditor.MESave;
using Level;
using Menu;
using Multiplayer;
using Quest;
using SecureServer;
using Steamworks;
using Steamworks.NET;
using SteamWorkshop;
using UnityEngine;
using UnityEngine.SceneManagement;
using Web;
using Debug = UnityEngine.Debug;

namespace Manager
{
    [DefaultExecutionOrder(-110)]
    public class Preload : MonoBehaviour
    {
        public static Preload Instance { private set; get; }

        public static int Step = 0;

        private int _currentStep = 0;

        
        
        public enum LoadingState
        {
            Loading_Main_Game,
            Loading_Quests,
            Loading_Replay,
            Loading_Inventory,
            Loading_Steam_Workshop,
            Loading_Map_Editor,
            Loading_Roles,
            Loading_Level,
            Loading_Leaderboard,
            Authenticating,
        }

        [SerializeField] private PreloadMenu menu;
        public static bool Initialized = false;

        private void Awake()
        {
                
            Instance = this;
                
            //GameManager -- 8
            //QuestManager -- 7
            //Load Level -- 1
            //LeaderBoard -- 9
            //Authenticating -- 1
            Step = 8 + 7 + 1 + 9 + 1;
        }

        public static void IncreaseTotalStep(int s)
        {
            Step += s;
        }

        public void NextStep(int step = 1)
        {
            _currentStep += step;

            menu.SetProgress(_currentStep, 0);
        }

        private IEnumerator Start()
        {
            Authenticate();
            RolesManager.Instance.TryToInitialize();
            LeaderboardManager.Instance.Refresh();
            
            Stopwatch stopwatch = Stopwatch.StartNew();

            menu.SetLoadingStateText(LoadingState.Loading_Main_Game);

            while (!GameManager.Initialized)
            {
                yield return null;
            }

            menu.SetLoadingStateText(LoadingState.Loading_Quests);

            while (!QuestManager.Initialized)
            {
                yield return null;
            }

            menu.SetLoadingStateText(LoadingState.Loading_Map_Editor);

            while (!MapSaver.Initialized)
            {
                yield return null;
            }

            // menu.SetLoadingStateText(LoadingState.Loading_Inventory);
            //
            // while (!InventoryManager.Initialized)
            // {
            //     yield return null;
            // }

            menu.SetLoadingStateText(LoadingState.Loading_Steam_Workshop);

            while (!SteamWorkshopManager.Initialized)
            {
                yield return null;
            }

            //TODO: Loading Level
            menu.SetLoadingStateText(LoadingState.Loading_Level);

            LevelManager.Instance.Refresh();

            while (!LevelManager.Initialized)
            {
                yield return null;
            }


            //TODO: Requesting Roles
            menu.SetLoadingStateText(LoadingState.Loading_Roles);

            while (!RolesManager.Initialized)
            {
                yield return null;
            }

            //TODO: Loading Leaderboard
            menu.SetLoadingStateText(LoadingState.Loading_Leaderboard);

            while (!LeaderboardManager.Initialized)
            {
                yield return null;
            }

            #region Authenticate

            menu.SetLoadingStateText(LoadingState.Authenticating);

            while (!Initialized)
            {
                yield return null;
            }

            #endregion

            #region Load

            stopwatch.Stop();

            Debug.Log($"Load The Required Datas in {stopwatch.ElapsedMilliseconds / 1000f}s");
            
            TransitionUI.Instance.StartTransition();

            AsyncOperation operation = SceneManager.LoadSceneAsync("Menu",LoadSceneMode.Single);

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                menu.SetProgress(_currentStep, operation.progress / Step);
                yield return null;
            }

            NextStep();

            yield return new WaitForSeconds(0.15f);

            #endregion

            operation.allowSceneActivation = true;

            TransitionUI.Instance.ClearTransition();
        }
        
        [Serializable]
        public class PlayerBansResponse
        {
            public List<PlayerBanSummary> players = new List<PlayerBanSummary>();

            PlayerBansResponse(){}
        }
        
        async void Authenticate()
        {
            PlayerBansResponse response = await HttpClient.Get<PlayerBansResponse>(EndPoint.GetPlayerBansSummaries + SteamUser.GetSteamID(),false);
            
            Initialized = true;
            
            if (response is { players: { Count: > 0 } })
            {
                SteamManager.CurrentUserBanSummary = response.players[0];
                Debug.Log("Authenticated! ");
            }
            else
            {
                //TODO: Failed to authenticate
                SteamManager.CurrentUserBanSummary = new PlayerBanSummary();
                Debug.Log("Failed to authenticate");
            }
            
            NextStep();
        }
    }
}
