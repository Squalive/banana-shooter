
using System;
using Multiplayer;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class LeaderBoardItem : MonoBehaviour
{
    public TextMeshProUGUI nameText, rank,scoreText;
    public RawImage avatar;

    private ulong playerSteamId;
    private bool set = false;
    public void SetValue(ulong steamId, int rank,int score)
    {
        playerSteamId = steamId;
        this.rank.SetText(rank.ToString());
        nameText.SetText(Chat.Instance.GetPlayerName(steamId));
        scoreText.SetText(score.ToString());
        
        GetPlayerAvatar();

        set = true;
    }

    private void Start()
    {
        if (!set && playerSteamId==0)
        {
            rank.SetText("-1");
            nameText.SetText(Chat.Instance.GetPlayerName(NetworkManager.Instance.steamId.m_SteamID));
            scoreText.SetText("-1");
            
            GetPlayerAvatar();
        }
    }

    void GetPlayerAvatar()
    {
        if (playerSteamId == NetworkManager.Instance.steamId.m_SteamID)
        {
            avatar.texture = NetworkManager.Instance.myAvatar;
            return;
        }
        int imageId = SteamFriends.GetSmallFriendAvatar((CSteamID) playerSteamId);
        if (imageId == -1) return;
        avatar.texture = SteamTextureUtils.GetSteamImageAsTexture(imageId);
    }

    private void OnEnable()
    {
        NetworkManager.OnAvatarImageLoaded += OnImageLoaded;
    }

    private void OnDisable()
    {
        NetworkManager.OnAvatarImageLoaded -= OnImageLoaded;
    }

    private void OnImageLoaded(CSteamID steamID, Texture2D avatarImage)
    {
        if (steamID.m_SteamID == playerSteamId)
        {
            avatar.texture = avatarImage;
            NetworkManager.OnAvatarImageLoaded -= OnImageLoaded;
        }
    }
}
