using Manager;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class FriendDetailUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private LocalizeStringEvent stateText;

        [SerializeField] public Button inviteBtn;

        [SerializeField] private RawImage avatar;

        private Button _btn;

        public void Initialize(Friend friend,bool enableInvite=false)
        {
            nameText.SetText(friend.Name);
            string key = (int) friend.PersonalState <= 5 ? friend.PersonalState.ToString() : "k_EPersonaStateOffline";
            stateText.SetEntry(key);

            avatar.texture = friend.Avatar;

            _btn = GetComponent<Button>();

            _btn.interactable = friend.PersonalState == EPersonaState.k_EPersonaStateOnline;
            
            inviteBtn.gameObject.SetActive(enableInvite);
        }
    }
}
