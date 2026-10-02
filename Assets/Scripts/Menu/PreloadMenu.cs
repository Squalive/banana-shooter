
using System;

using Manager;
using Steamworks.NET;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class PreloadMenu : MonoBehaviour
    {
        [SerializeField] private LocalizeStringEvent loadingText;

        [SerializeField] private Slider progressBar;

        [SerializeField] private GameObject loadingPanel, steamNotInitPanel;

        [Serializable]
        public class PreloadTheme
        {
            [SerializeField] public Color backGroundColor = new(195 / 255f, 174 / 255f, 0);
            [SerializeField] public Color textColor = Color.black;
            [SerializeField] public Color sliderColor = new(0, 149 / 255f, 1f);
        }

        [SerializeField] private PreloadTheme[] themes;
        [SerializeField] [Range(0,10)] private int themeIndex = 0;
        
        [SerializeField] private RawImage backGround,slider;
        [SerializeField] private TextMeshProUGUI[] texts;

        private void OnValidate()
        {
            SetTheme();
        }

        void SetTheme()
        {
            if (themes.Length <= 0 || themes.Length <= themeIndex) return;
            Color backGroundColor = themes[themeIndex].backGroundColor;
            Color textColor = themes[themeIndex].textColor;
            Color sliderColor = themes[themeIndex].sliderColor;
            if (backGround)
            {
                backGround.color = backGroundColor;
            }

            if (texts != null)
            {
                foreach (var text in texts)
                {
                    text.color = textColor;
                }
            }
            
            if(slider){
                slider.color = sliderColor;
            }
        }

        private void Start()
        {
            CheckSteamInit();
            themeIndex = GameManager.Instance.introTheme;
            SetTheme();
        }

        void CheckSteamInit()
        {
            loadingPanel.SetActive(SteamManager.Initialized);
            steamNotInitPanel.SetActive(!SteamManager.Initialized);
        }

        
        public void Quit()
        {
            Application.Quit();
        }

        public void SetLoadingStateText(Preload.LoadingState state)
        {
            loadingText.SetEntry(state.ToString());
        }

        public void SetProgress(int currentStep,float offset)
        {
            progressBar.value = (float)currentStep / Preload.Step + offset;
        }
    }
}
