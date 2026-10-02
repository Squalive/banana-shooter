
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using CodingDaniel.MapEditor.MEEditor.MESave;
using CodingDaniel.MapEditor.Utils;
using Steamworks;
using SteamWorkshop;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace CodingDaniel.MapEditor.UI
{
    public class PublishMenu : MonoBehaviour
    {
        public static PublishMenu Instance { private set; get; }

        private void Awake()
        {
            Instance = this;
            nameInput.interactable = false;

            SteamWorkshopManager.Instance.OnGetProgress += OnGetProgress;
            SteamWorkshopManager.Instance.OnUploadFinish += OnUploadFinish;
        }

        private void OnUploadFinish(bool obj,EResult result)
        {
            progressBar.value = obj? 1:0;
            progressText.SetText(obj ? "100%" : "--%");
            progressStatusText.SetEntry(obj?"Done" : "Failed");
            progressStatusText.StringReference.Arguments = !obj ? new List<object>() {result} : null;
            progressStatusText.RefreshString();
        }

        private void OnGetProgress(float arg1, EItemUpdateStatus arg2)
        {
            progressBar.value = arg1;
            progressText.SetText((arg1 * 100f).ToString("F2")+"%");
            progressStatusText.SetEntry(arg2.ToString());
            progressStatusText.RefreshString();
        }

        private void OnDestroy()
        {
            SteamWorkshopManager.Instance.OnGetProgress -= OnGetProgress;
            SteamWorkshopManager.Instance.OnUploadFinish -= OnUploadFinish;
        }

        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private LocalizeStringEvent progressStatusText;

        public TMP_InputField descriptionInput, nameInput,updateNoteInput;

        [SerializeField] private GameObject panel,window,processPanel;

        
        public void TryToPublish()
        {
            panel.SetActive(true);
            window.SetActive(true);
            processPanel.SetActive(false);
        }

        
        public void Publish()
        {
            window.SetActive(false);
            processPanel.SetActive(true);

            progressBar.value = 0;
            progressText.SetText("--%" );
            progressStatusText.SetEntry("EItemUpdateStatusPreparingConfig");
            progressStatusText.StringReference.Arguments =  null;
            progressStatusText.RefreshString();
            
            //Get the destinationPath
            string destinationPath = MapSaver.SteamTemp;
            //Create the directory if there isnt 
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            
            

            MapSaver.CurrentMap.description = descriptionInput.text;
            
            TabHolder.Instance.mapDescriptionInput.SetTextWithoutNotify(descriptionInput.text);
            
            SteamWorkshopManager.Instance.CreateOrUpdateWorkshopItem(MapSaver.CurrentMap.fileId,WorkshopItemType.Map,updateNoteInput.text);
        }

        
        public void ReadMore()
        {
            SteamWorkshopManager.Instance.ReadMore();
        }

        public bool IsUsingUI()
        {
            return panel.activeSelf;
        }
    }
}
