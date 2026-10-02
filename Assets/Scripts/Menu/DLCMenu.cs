
using System;
using Steamworks;
using Steamworks.NET;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class DLCMenu : MonoBehaviour
    {
        [SerializeField] private Button btn;
        
        [SerializeField] private RawImage thumbnail;
        [SerializeField] private TextMeshProUGUI text;

        [SerializeField] private CanvasGroup canvas;

        [SerializeField] private Texture2D thumbnailTexture;

        [SerializeField] private GameObject nextNew;

        private void Start()
        {
            if (!SteamApps.BIsDlcInstalled((AppId_t)2238100))
            {
                
                btn.onClick.AddListener(OpenDLCPage);
            
                text.SetText("<color=#05a1cf><b><size=24>Banana Shooter - Cyber Upgrade</size></b></color>\nBuy it now to get your new outfit and main menu, exclusive servers");

                thumbnail.texture = thumbnailTexture;

            } else{
                enabled = false;
                
                GetComponent<NewsMenu>().enabled = true;

                canvas.alpha = 0.0f;
                
                nextNew.SetActive(true);
            }

        }

        void OpenDLCPage()
        {
            SteamManager.OpenSteamBuiltInBrowser("https://store.steampowered.com/app/2238100");
        }

        private void Update()
        {
            canvas.alpha = math.abs(math.sin(Time.time));
        }
    }
}
