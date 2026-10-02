using System.Collections.Generic;
using Manager;
using Multiplayer;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Menu
{
    public class GroupMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropDown;
        [SerializeField] private Button btn;
        List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
        private List<CSteamID> groups = new List<CSteamID>();
        
        private NetworkManager network;
        private void Start()
        {
            network = NetworkManager.Instance;
        
            list.Add(new TMP_Dropdown.OptionData("No Group"));

            int cnt = SteamFriends.GetClanCount();

            int index = 0;
            for (int i = 0; i < cnt; i++)
            {
                CSteamID groupId = SteamFriends.GetClanByIndex(i);

                string groupName = SteamFriends.GetClanName(groupId);
                string groupTag = SteamFriends.GetClanTag(groupId);

                if (groupTag != "")
                {
                    groups.Add(groupId);
                    if (groupId == NetworkManager.Instance.currentGroup) index = groups.Count;
                    int ImageId = SteamFriends.GetSmallFriendAvatar( groupId);
                    if (ImageId == -1) return;
                    Texture2D texture = SteamTextureUtils.GetSteamImageAsTexture(ImageId);
                    Sprite sprite = Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(0.5f, 0.5f));
                    list.Add(new TMP_Dropdown.OptionData(groupName,sprite));
                }
            }
            dropDown.AddOptions(list);
            dropDown.value = index;
        
        
            dropDown.onValueChanged.AddListener(JoinGroup);
            btn.onClick.AddListener(OpenChat);
        }
    
        void JoinGroup(int index)
        {
            if(network.currentGroup.m_SteamID!= 0)
                SteamFriends.LeaveClanChatRoom(network.currentGroup);
            GameManager.groupChanged = true;
            if (index == 0)
            {
                network.currentGroup.m_SteamID = 0;
                return;
            }

            network.currentGroup = groups[index - 1];
            SteamFriends.JoinClanChatRoom(network.currentGroup);
        
        }

        void OpenChat()
        {
            SteamFriends.ActivateGameOverlayToUser("chat",network.currentGroup);
        }
    }
}
