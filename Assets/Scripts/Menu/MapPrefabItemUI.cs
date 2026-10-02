using System;
using System.IO;
using Multiplayer;
using Save;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class MapPrefabItemUI : MonoBehaviour
    {
        [SerializeField] public Toggle toggle;

        [SerializeField] private LocalizeStringEvent text;

        [SerializeField] private TextMeshProUGUI amountText;

        [SerializeField] private RawImage icon;

        [SerializeField] private CanvasGroup voteAnimation;

        private CallResult<SteamUGCQueryCompleted_t> _queryResult;
        private UGCQueryHandle_t _queryHandle;

        private void OnDestroy()
        {
            if(_queryResult!=null)
                _queryResult.Dispose();
        }

        public void Initialize(Map.Map map,ToggleGroup group)
        {
            text.SetEntry(map.name);
            
            amountText.SetText("0");

            icon.texture = map.texture;

            toggle.group = group;
        }
        
        public async void Initialize(ulong id,ToggleGroup group)
        {
            text.enabled = false;

            amountText.SetText("0");

            toggle.interactable = !NetworkManager.LocalClientData.Eliminated;

            toggle.group = group;
            
            _queryResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted);
            
            _queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(new[]{new PublishedFileId_t(id)},1);

            var call = SteamUGC.SendQueryUGCRequest(_queryHandle);
            _queryResult.Set(call);

            if (SteamUGC.GetItemInstallInfo(new PublishedFileId_t(id), out _, out var pchFolder, 1024,
                    out _))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(pchFolder);
                FileInfo[] info = directoryInfo.GetFiles("*.jpg");

                foreach (var file in info)
                {
                    var t = await SaveSystem.ReadByteFromFileAsync(file.FullName);
                    
                    Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
                    texture2D.LoadImage(t);

                    icon.texture = texture2D;

                    break;
                }
            }
        }
        
        private void OnUGCQueryCompleted(SteamUGCQueryCompleted_t result, bool biofailure)
        {
            if (biofailure || result.m_unNumResultsReturned == 0)
            {
                // Failed to retrieve workshop item info
                Debug.Log("Failed to retrieve workshop item info");
                return;
            }
            
            SteamUGCDetails_t itemDetails;
            if (!SteamUGC.GetQueryUGCResult(_queryHandle, 0, out itemDetails))
            {
                // Failed to get item details
                Debug.Log("Failed to get item details");
                return;
            }
            
            text.GetComponent<TextMeshProUGUI>().SetText(itemDetails.m_rgchTitle);
        }

        public void SetCount(ushort count)
        {
            amountText.SetText(count.ToString());
        }

        public void Animate()
        {
            voteAnimation.alpha = 1f;
        }

        private void Update()
        {
            voteAnimation.alpha = Mathf.Lerp(voteAnimation.alpha, 0f, Time.deltaTime * 10f);
        }
    }
}
