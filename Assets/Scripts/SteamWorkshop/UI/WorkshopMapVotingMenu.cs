
using Menu;
using Multiplayer;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace SteamWorkshop.UI
{
    public class WorkshopMapVotingMenu : MonoBehaviour
    {
        [SerializeField] private GameObject mapVotingPanel, voteUpSelected, voteDownSelected;
        [SerializeField] private Button voteUpBtn, voteDownBtn;

        private PublishedFileId_t _currentMapId;

        protected CallResult<GetUserItemVoteResult_t> GetUserItemVoteResult;
        protected CallResult<SetUserItemVoteResult_t> SetUserItemVoteResult;

        private void Start()
        {
            _currentMapId = NetworkManager.Instance.GetCurrentWorkshopMap();
            if (_currentMapId != PublishedFileId_t.Invalid)
                GetUserItemVoteResult.Set(SteamUGC.GetUserItemVote(_currentMapId));
        }

        private void OnEnable()
        {
            GetUserItemVoteResult = CallResult<GetUserItemVoteResult_t>.Create(OnGetUserItemVoteResult);
            SetUserItemVoteResult = CallResult<SetUserItemVoteResult_t>.Create(OnSetUserItemVoteResult);

            voteUpBtn.onClick.AddListener(delegate { Vote(true); });
            voteDownBtn.onClick.AddListener(delegate { Vote(false); });
        }

        private void OnDisable()
        {
            voteUpBtn.onClick.RemoveAllListeners();
            voteDownBtn.onClick.RemoveAllListeners();
            GetUserItemVoteResult.Dispose();
            SetUserItemVoteResult.Dispose();
        }

        private void OnGetUserItemVoteResult(GetUserItemVoteResult_t param, bool fail)
        {
            if (fail || param.m_eResult != EResult.k_EResultOK)
            {
                Debug.Log("Failed to retrieve the map vote info");
                return;
            }
            Debug.Log($"{param.m_nPublishedFileId} - skipped: {param.m_bVoteSkipped}, vote up: {param.m_bVotedUp}, vote down: {param.m_bVotedDown}");

            if (_currentMapId == param.m_nPublishedFileId)
            {
                voteUpSelected.SetActive(param.m_bVotedUp);
                voteDownSelected.SetActive(param.m_bVotedDown);

                mapVotingPanel.SetActive(true);
            }
        }

        private void OnSetUserItemVoteResult(SetUserItemVoteResult_t param, bool biofailure)
        {
            if (biofailure || param.m_eResult != EResult.k_EResultOK)
            {
                Debug.Log("Failed to set the map vote");
                return;
            }

            if (_currentMapId == param.m_nPublishedFileId)
            {
                Debug.Log($"Set {param.m_bVoteUp} to {param.m_nPublishedFileId}");
                mapVotingPanel.SetActive(false);

                NotificationMenu.Instance.NewItem("nc_message", "nc_vote_set");
            }
        }

        void Vote(bool voteUp)
        {
            SetUserItemVoteResult.Set(SteamUGC.SetUserItemVote(_currentMapId, voteUp));
        }
    }
}
