
using System.Linq;
using Audio;

using Manager;
using Multiplayer;
using Riptide.Transports.Steam;
using Steamworks;
using UnityEngine;
using SteamClient = Riptide.Transports.Steam.SteamClient;

namespace Menu
{
    public class InviteFriendMenu : MonoBehaviour
    {
        [SerializeField] private FriendDetailUI prefab;
        [SerializeField] private Transform content;

        [SerializeField] private GameObject window;
        private void Awake()
        {
            var list = FriendManager.Friends.Values.ToList();
            list.Sort((friend, friend1) => friend.PersonalState.CompareTo(friend1.PersonalState));
            list.Reverse();
            foreach (var friend in list)
            {
                // if (friend.PersonalState == EPersonaState.k_EPersonaStateAway || friend.PersonalState == EPersonaState.k_EPersonaStateSnooze) continue;
                FriendDetailUI ui = Instantiate(prefab, content);
                
                ui.Initialize(friend,true);
                
                ui.inviteBtn.onClick.AddListener(delegate { Invite(friend.SteamID); });
            }
        }

        void Invite(CSteamID id)
        {
            if (SteamFriends.InviteUserToGame(id, NetworkManager.Instance.ConnectionString))
            {
                NotificationMenu.Instance.NewItem("nc_message","nc_invite_sent");
            }
        }

        
        public void DisplayInviteWindow()
        {
            AudioManager.Instance.PlayButton();
            window.SetActive(true);
        }
    }
}
