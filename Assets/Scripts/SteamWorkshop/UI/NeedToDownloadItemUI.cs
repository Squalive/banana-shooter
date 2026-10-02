
using System.Collections;
using Manager;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace SteamWorkshop.UI
{
    public class NeedToDownloadItemUI : MonoBehaviour
    {
        [SerializeField] private RawImage img,previewImg;

        [SerializeField] private GameObject downloaded, downloading, notInstalled;

        [SerializeField] private Color downloadedColor=Color.green, downloadingColor = Color.grey, notInstalledColor = Color.red,needsUpdatedColor = Color.yellow;

        [SerializeField] private TextMeshProUGUI text,progressText;

        [SerializeField] private Slider progressSlider;

        public EItemState itemState;

        public PublishedFileId_t fileId;

        private CallResult<SteamUGCQueryCompleted_t> _queryResult;
        private UGCQueryHandle_t _queryHandle;

        private CanvasGroup _canvas;

        private float desiredAlpha = 0f;

        [SerializeField] private GameObject loading;
        public void Init(PublishedFileId_t fileIdT)
        {
            _canvas = gameObject.AddComponent<CanvasGroup>();
            _canvas.alpha = 0f;
            
            this.fileId = fileIdT;

            InitState((EItemState)SteamUGC.GetItemState(fileId));
            
            _queryResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted);
            
            _queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(new[]{fileIdT},1);

            var call = SteamUGC.SendQueryUGCRequest(_queryHandle);
            _queryResult.Set(call);
        }

        private void Update()
        {
            _canvas.alpha = Mathf.Lerp(_canvas.alpha, desiredAlpha, Time.deltaTime * 15f);

            progressSlider.value = Mathf.Lerp(progressSlider.value, desiredProgress, Time.deltaTime * 18f);
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
            
            text.SetText(itemDetails.m_rgchTitle);

            if (SteamUGC.GetQueryUGCPreviewURL(result.m_handle, 0, out var url, 1024))
            {
                StartCoroutine(LoadTextureToItem(url));
            }

            desiredAlpha = 1f;
        }
        IEnumerator LoadTextureToItem(string url)
        {
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                previewImg.texture = texture;
                previewImg.gameObject.SetActive(true);
                loading.SetActive(false);
            }
        }
        public void InitState(EItemState state)
        {
            itemState = state;
            if ((state & EItemState.k_EItemStateNeedsUpdate) == EItemState.k_EItemStateNeedsUpdate)
            {
                img.color = needsUpdatedColor;
                notInstalled.SetActive(true);
                downloading.SetActive(false);
                downloaded.SetActive(false);
            }
            else if ((state & EItemState.k_EItemStateInstalled) == EItemState.k_EItemStateInstalled)
            {
                img.color = downloadedColor;
                notInstalled.SetActive(false);
                downloading.SetActive(false);
                downloaded.SetActive(true);
                desiredProgress = 1f;
            }
            else if ((state & EItemState.k_EItemStateDownloading) == EItemState.k_EItemStateDownloading || 
                     (state & EItemState.k_EItemStateDownloadPending) == EItemState.k_EItemStateDownloadPending)
            {
                img.color = downloadingColor;
                notInstalled.SetActive(false);
                downloading.SetActive(true);
                downloaded.SetActive(false);
                
            }
            else
            {
                img.color = notInstalledColor;
                notInstalled.SetActive(true);
                downloading.SetActive(false);
                downloaded.SetActive(false);
                
            }
        }

        public void StartGetDownloadProgress()
        {
            desiredProgress = 0;
            progressText.SetText("0%");
            StartCoroutine(LoopGetDownloadProgress());
        }

        private float desiredProgress = 0f;

        IEnumerator LoopGetDownloadProgress()
        {
            void GetDownloadProgress()
            {
                if (SteamUGC.GetItemDownloadInfo(fileId, out var down, out var total))
                {
                    if (total > 0)
                    {
                        desiredProgress = down / (float) total;
                        
                        DownloadItemMenu.Instance.AddBytes(fileId,down,total);
                    
                        progressText.SetText((desiredProgress*100f).ToString("F0" )+ "%");
                    }
                }
            }

            while (itemState==EItemState.k_EItemStateDownloading)
            {
                yield return new WaitForSeconds(1f);
                GetDownloadProgress();
            }
        }
        
    }
}
