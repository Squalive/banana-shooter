using System;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Utils;

namespace Menu
{
    public class AchivementItem : MonoBehaviour
    {
        public RawImage img;

        public TextMeshProUGUI title;

        public TextMeshProUGUI desc;
        public LocalizeStringEvent progressText;
        private bool avatarReceived;
        public string achievementName;

        public Slider slider;

        protected Callback<UserAchievementIconFetched_t> AchievementIconFetched;

        private void OnEnable()
        {
            AchievementIconFetched = Callback<UserAchievementIconFetched_t>.Create(OnAchievementIconFetched);
        }

        private void OnDisable()
        {
            AchievementIconFetched.Dispose();
        }

        private void OnAchievementIconFetched(UserAchievementIconFetched_t param)
        {
            if (param.m_nGameID == new CGameID(1949740))
            {
                if (param.m_nIconHandle != -1)
                {
                    img.texture = SteamTextureUtils.GetSteamImageAsTexture(param.m_nIconHandle);
                    avatarReceived = true;
                }
            }
        }

        private void OnAchievementProgressSet(GlobalAchievementPercentagesReady_t param, bool biofailure)
        {
            if (param.m_eResult == EResult.k_EResultOK && !biofailure && slider!=null && progressText!=null)
            {
                if (SteamUserStats.GetAchievementAchievedPercent(achievementName, out float percent))
                {
                    slider.value = percent;
                    progressText.StringReference.Arguments = new List<object>() {percent.ToString("F0")};
                    progressText.gameObject.SetActive(true);
                    slider.gameObject.SetActive(true);
                }
            }
        }
        
        void GetPlayerAvatar()
        {
            int ImageId = SteamUserStats.GetAchievementIcon(achievementName);
            if (ImageId == -1) return;
            img.texture = SteamTextureUtils.GetSteamImageAsTexture(ImageId);
            avatarReceived = true;
        }

        private CallResult<GlobalAchievementPercentagesReady_t> AchievementProgress = new CallResult<GlobalAchievementPercentagesReady_t>();
    
        public void SetAchievement(string name,string des,string localizeName)
        {
            achievementName = name;
            title.text =localizeName;
            desc.text = des;

            progressText.gameObject.SetActive(false);
            slider.gameObject.SetActive(false);

            if (SteamUserStats.GetAchievement(name, out bool achieved))
            {
                if (!achieved)
                {
                    title.color=Color.grey;
                    desc.color=Color.grey;
                    progressText.GetComponent<TextMeshProUGUI>().color = Color.grey;
                    img.color=Color.grey;
                }
            }
            if (SteamUserStats.RequestCurrentStats())
            {
                AchievementProgress.Set(SteamUserStats.RequestGlobalAchievementPercentages(),OnAchievementProgressSet);
            }
            if(!avatarReceived)GetPlayerAvatar();
        }
    }
}
