
using System.IO;
using Audio;
using CodingDaniel.MapEditor.MEEditor.MESave;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class MapItemUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI nameText;

        [SerializeField] private Button editBtn;

        [SerializeField] private LocalizeStringEvent isPublished;

        public FileInfo File;

        public MapMetadata Metadata = new();

        private CallResult<SteamUGCQueryCompleted_t> _queryResult;
        public void Init(FileInfo data)
        {
            File = data;

            if (MapSaver.EditingMapMetadatas.TryGetValue(File, out var md))
            {
                Metadata = md;

                nameText.SetText(Metadata.Name);
                isPublished.SetEntry(Metadata.IsPublished ? "Published" : "Local");

                if (Metadata.IsPublished)
                {
                    _queryResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUGCQueryCompleted);

                    var queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(new[] { Metadata.FileId }, 1);

                    var call = SteamUGC.SendQueryUGCRequest(queryHandle);
                    _queryResult.Set(call);
                }
            }


            editBtn.onClick.AddListener(delegate { Select(false); });
            editBtn.onClick.AddListener(Edit);

        }

        public uint per = 0;

        private void OnUGCQueryCompleted(SteamUGCQueryCompleted_t result, bool ioFailure)
        {
            if (ioFailure || result.m_unNumResultsReturned == 0 || result.m_eResult != EResult.k_EResultOK)
            {
                // Failed to retrieve workshop item info
                Debug.Log("Failed to retrieve workshop item info");
                SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
                return;
            }

            SteamUGCDetails_t itemDetails;
            if (!SteamUGC.GetQueryUGCResult(result.m_handle, 0, out itemDetails))
            {
                // Failed to get item details
                Debug.Log("Failed to get item details");
                SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
                return;
            }

            uint voteUp = itemDetails.m_unVotesUp;
            uint voteDown = itemDetails.m_unVotesDown;

            if (voteUp + voteDown < 10) per = 0;
            else per = (uint)(voteUp / (float)(voteUp + voteDown) * 5);


            SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
        }
        void Edit()
        {
            MapEditorMainMenu.Instance.editBar.position = Input.mousePosition;

            MapEditorMainMenu.Instance.editBar.gameObject.SetActive(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            bool flag = eventData.clickCount > 1;

            Select(flag);
        }

        void Select(bool flag)
        {
            MapEditorMainMenu.Instance.SelectMap(File, Metadata, gameObject, flag, per);

            AudioManager.Instance.PlayButton();
        }
    }
}
