using System;
using System.Collections;
using Manager;
using Menu;
using Multiplayer;
using Multiplayer.Interface;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace SteamWorkshop.UI
{
    public class WorkshopMapItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RawImage bg;
        [SerializeField] private GameObject checkMark;

        [SerializeField]
        private Toggle toggle;

        public PublishedFileId_t fileId;

        public void Init(string mapName, string url,PublishedFileId_t fileIdt,CustomMapMenu obj)
        {
            fileId = fileIdt;
            try
            {
                text.SetText(mapName);
            }
            catch (Exception e)
            {
                text.SetText($"Failed to load: {e.Message}");
            }
            
            toggle.onValueChanged.AddListener(OnToggle);

            bool flag = NetworkServerManager.EnabledWorkshopMaps.Contains(fileIdt);
            toggle.SetIsOnWithoutNotify(flag);
            
            checkMark.SetActive(!flag);

            bg.color = flag ? Color.white : Color.gray;
            
            bg.gameObject.SetActive(false);
            
            if (bg.texture != null && bg.texture != PrefabManager.Instance.errorTexture)
            {
                Destroy(bg.texture);
            }

            bg.texture = null;
            
            loading.SetActive(true);
            obj.StartCoroutine(LoadTextureToItem(url));
        }

        void SetTexture(Texture2D texture2D)
        {
            if (texture2D == null) return;
            
            bg.texture = texture2D;
            
            bg.gameObject.SetActive(true);
            
            loading.SetActive(false);
        }

        IEnumerator LoadTextureToItem(string url)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
                SetTexture(PrefabManager.Instance.errorTexture);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                SetTexture(texture);
            }
        }
        
        [SerializeField] private GameObject loading;

        void OnToggle(bool flag)
        {
            checkMark.SetActive(!flag);

            bg.color = flag ? Color.white : Color.gray;
            
            if (fileId == PublishedFileId_t.Invalid) return;

            bool contain = NetworkServerManager.EnabledWorkshopMaps.Contains(fileId);
            if (flag)
            {
                if (!contain) NetworkServerManager.EnabledWorkshopMaps.Add(fileId);
            }
            else
            {
                if (contain) NetworkServerManager.EnabledWorkshopMaps.Remove(fileId);
            }
            NetworkServerManager.OnEnabledMapsChanged?.Invoke();
        }
    }
}
