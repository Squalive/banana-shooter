using Multiplayer;
using Steamworks;
using Steamworks.NET;
using UnityEngine;

namespace Manager
{
    public class AchievementManager : MonoBehaviour
    {

        public enum EAchievements
        {
            BANANA_GO_BRRRRRR = 0,
            FIRST_KILL=1,
            BECOME_DEV,
            I_AM_GOOT_AT_GRAPPLING,
            THIS_IS_A_BETRAY,
            SUCK_MY_BANANA,
            HEAD_SHOT,
            JACK,
            CRAZY_UPGRADE,
            I_AM_A_ROCK,
            BLOODDTHIRSTY,
            BAD_GUY,
            SKY_EYE,
            RED_LIGHT_GREEN_LIGHT,
            CANT_PLAY_UNTIL_RESIDUAL_BLOOD,
            ELECTRIC_EEL,
            BEING_EXPERT,
            GUN_GAME,
            GRAPPLE_KILLER,
            EXPLOSIVE_ENTRANCE,
            BANANA_GUN,
            MMM_SODA,
            ZOMBIE_SLAYER
        }
    
    
        public enum EStats
        {
            SNIPER_SHOT=0,
            START_GAME_1000 ,
            DEATHS ,
            AL48_SHOT ,
            LASER_SHOT ,
            KNIFE_KILL ,
            WINS,
            TARGET_SCORE,
            PARKOUR_TIME,
            EXPERIENCE,
            KILLS,
            BANANA_KILL,
            MAP_PLAY_AMOUNT,
            GET_BLIND_AMOUNT
        }

        public enum StatsType
        {
            Int,
            Float
        }
        public static AchievementManager Instance;

        protected Callback<UserAchievementStored_t> achievementStored;

        public uint appId;
        private void Awake()
        {
            if(Instance==null)
                Instance = this;
            else if (Instance != this)
                return;

        
        }

        private void Start()
        {
            if (Instance != this)
                return;
        
            if (!SteamManager.Initialized) return;
            appId = SteamUtils.GetAppID().m_AppId;
            Debug.Log(RequestStats() ? "Init stats complete" : "Init stats fail");

        
            achievementStored = Callback<UserAchievementStored_t>.Create(OnStatsStored);
        
            SteamUserStats.GetAchievement("BECOME_DEV", out bool achieved);
            achievedDev = achieved;
        }

        public void SetAchievement(EAchievements achievement)
        {
            SteamUserStats.SetAchievement(achievement.ToString());
            SteamUserStats.StoreStats();
        }

        public int SetStatsPlusOne(EStats stats)
        {
            SteamUserStats.GetStat(stats.ToString(), out int score);
            ++score;
            SteamUserStats.SetStat(stats.ToString(), score);
            SteamUserStats.StoreStats();
            return score;
        }

        public void SetStat(EStats stats, StatsType type, float score)
        {
            switch (type)
            {
                case StatsType.Int:
                    SteamUserStats.SetStat(stats.ToString(), (int)score);
                    break;
                case StatsType.Float:
                    SteamUserStats.SetStat(stats.ToString(),score);
                    break;
            }
            SteamUserStats.StoreStats();
        
        }

        public int GetStat(EStats stats)
        {
            SteamUserStats.GetStat(stats.ToString(), out int data);
            return data;
        }

        private void OnStatsStored(UserAchievementStored_t param)
        {
            if (param.m_nGameID == appId)
            {
                if (param.m_nCurProgress == param.m_nMaxProgress)
                {
                    if(NetworkManager.Instance.Client.IsConnected)
                        NetworkManager.Instance.SendMsg($"{NetworkManager.Instance.PersonalName} achieve the {param.m_rgchAchievementName} achievement",1);
                }
            }
        }


        bool RequestStats()
        {
            if (!SteamUser.BLoggedOn()) return false;

            return SteamUserStats.RequestCurrentStats();
        }

        public bool achievedDev;
        //CodingDaniel
        public int devLen = 0;
        private void Update()
        {
            if (achievedDev) return;

            if (Input.anyKeyDown&&
                !Input.GetKeyDown(KeyCode.C) &&
                !Input.GetKeyDown(KeyCode.O) &&
                !Input.GetKeyDown(KeyCode.D) &&
                !Input.GetKeyDown(KeyCode.I) &&
                !Input.GetKeyDown(KeyCode.N) &&
                !Input.GetKeyDown(KeyCode.G) &&
                !Input.GetKeyDown(KeyCode.D) &&
                !Input.GetKeyDown(KeyCode.A) &&
                !Input.GetKeyDown(KeyCode.N) &&
                !Input.GetKeyDown(KeyCode.I) &&
                !Input.GetKeyDown(KeyCode.E) &&
                !Input.GetKeyDown(KeyCode.L))
            {
                devLen = 0;
            }
        
            if (devLen==0 && Input.GetKeyDown(KeyCode.C))
            {
                devLen = 1;
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                if (devLen == 1 )
                {
                    devLen = 2;
                }
                else
                {
                    devLen = 0;
                }
            }
        

            if ( devLen == 2&&Input.GetKeyDown(KeyCode.D))
            {
                devLen = 3;
            }
        
            if (devLen==3&&Input.GetKeyDown(KeyCode.I))
            {
                devLen = 4;
            }
        
            if (devLen==4&& Input.GetKeyDown(KeyCode.N))
            {
                devLen = 5;
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (devLen == 5)
                    devLen = 6;
                else
                    devLen = 0;
            }
        
            if (devLen == 6&&Input.GetKeyDown(KeyCode.D))
            {
                devLen = 7;
            }
        
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (devLen == 7)
                    devLen = 8;
                else
                    devLen = 0;
            }
            if (devLen == 8 &&Input.GetKeyDown(KeyCode.N))
            {
                devLen = 9;
            }
            if (devLen == 9&& Input.GetKeyDown(KeyCode.I))
            {
                devLen = 10;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (devLen == 10)
                    devLen = 11;
                else
                    devLen = 0;
            }
            if ( Input.GetKeyDown(KeyCode.L))
            {
                if (devLen == 11)
                    devLen = 12;
                else
                    devLen = 0;
            }

            if (devLen == 12)
            {
                Debug.Log("achieve");
                SetAchievement(EAchievements.BECOME_DEV);
                achievedDev = true;
            }
        }
    }
}
