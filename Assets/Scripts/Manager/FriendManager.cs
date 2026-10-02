using System;
using System.Collections.Generic;
using Steamworks;
using Steamworks.NET;
using UnityEngine;
using Utils;

namespace Manager
{
    public class Friend
    {
        public CSteamID SteamID;
        public string Name;
        public EPersonaState PersonalState;
        public CSteamID CurrentServer;

        public Texture2D Avatar;
        public Friend(CSteamID steamID, string name, EPersonaState personalState)
        {
            SteamID = steamID;
            Name = name;
            PersonalState = personalState;
            RefreshRichPresence();
        }
        public void InitAvatar(Texture2D texture2D)
        {
            Avatar = texture2D;
        }

        public void RefreshState()
        {
            PersonalState = SteamFriends.GetFriendPersonaState(SteamID);
        }

        public void RefreshRichPresence()
        {
            if (ulong.TryParse(SteamFriends.GetFriendRichPresence(SteamID, "server"), out var id))
            {
                CurrentServer = (CSteamID)id;
            }
        }
        
    }
    public class FriendManager : MonoBehaviour
    {
        public static readonly Dictionary<CSteamID,Friend> Friends = new Dictionary<CSteamID,Friend>();

        public Action<Friend> FriendAvatarLoaded;
        
        private void Start()
        {
            if (!SteamManager.Initialized) return;
            int nFriends = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

            if (nFriends == -1)
            {
                nFriends = 0;
            }

            for (int i = 0; i < nFriends; i++)
            {
                CSteamID friendSteamID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

                string friendName = SteamFriends.GetFriendPersonaName(friendSteamID);
                EPersonaState personaState = SteamFriends.GetFriendPersonaState(friendSteamID);

                Friend friend = new Friend(friendSteamID, friendName, personaState);

                Friends.Add(friendSteamID,friend);

                // int iImage = SteamFriends.GetLargeFriendAvatar(friendSteamID);
                //
                // if (iImage == -1) continue;
                //
                // Texture2D texture2D = SteamTextureUtils.GetSteamImageAsTexture(iImage);
                //
                // friend.InitAvatar(texture2D);
                //
                // FriendAvatarLoaded?.Invoke(friend);
            }
        }
        
        // private void OnImageLoaded(AvatarImageLoaded_t param)
        // {
        //     foreach (var friend in Friends.Values)
        //     {
        //         if (friend.SteamID == param.m_steamID)
        //         {
        //             Texture2D texture2D = SteamTextureUtils.GetSteamImageAsTexture(param.m_iImage);
        //             
        //             friend.InitAvatar(texture2D);
        //             
        //             FriendAvatarLoaded?.Invoke(friend);
        //             break;
        //         }
        //     }
        // }
    }
}
