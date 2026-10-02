using System;
using Manager;
using Steamworks;
using UnityEngine;

namespace Level
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance;

        public Action OnInitialize;

        public LevelSystem levelSystem=null;
        public LevelSystemAnimated levelSystemAnimated=null;

        public int expWaitToAdd=0;


        public Color[] colors = new Color[10];

        public static bool Initialized = false;
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                return;
            }
        }

        private void Start()
        {
            if (Instance != this) return;

            //Debug.Log(LevelUtils.GetExpFromLevel(44));
        }

        public void Refresh()
        {
            if (SteamUserStats.GetStat("EXPERIENCE", out int exp))
            {
                levelSystem = new LevelSystem(exp);
                levelSystemAnimated = new LevelSystemAnimated(levelSystem);

                Initialized = true;
                
                OnInitialize?.Invoke();
            }
        }


        public void StoreExp()
        {
            if (expWaitToAdd == 0) return;
            levelSystem.AddExperience(expWaitToAdd);
            expWaitToAdd = 0;
            AchievementManager.Instance.SetStat(AchievementManager.EStats.EXPERIENCE, AchievementManager.StatsType.Int, levelSystem.GetExp());
        }

        private void OnApplicationQuit()
        {
            StoreExp();
        }

        public Color GetColor(int level)
        {
            return colors[level / 10];
        }
    }
}
