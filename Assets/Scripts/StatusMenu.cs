
using System.Collections.Generic;
using Steamworks;
using Steamworks.NET;
using UnityEngine;
using UnityEngine.Localization.Components;

public class StatusMenu : MonoBehaviour
{
    public static StatusMenu Instance;

    private void Awake()
    {
        Instance = this;
    }

    public LocalizeStringEvent startGameCount,killCount,winText,scoreText,parkourTimeText,expText,deathsText;

    public LocalizeStringEvent statusDetailed, statusDetailedTrick;
    private void Start()
    {
        if (!SteamManager.Initialized) return;

        if (SteamUserStats.GetStat("START_GAME_1000", out startGame))
        {
            startGameCount.StringReference.Arguments = new List<object>() {startGame};
        }
        if (SteamUserStats.GetStat("KILLS", out kills))
        {
            killCount.StringReference.Arguments = new List<object>() {kills};
        }
        if (SteamUserStats.GetStat("WINS", out wins))
        {
            winText.StringReference.Arguments = new List<object>() {wins};
        }
        if (SteamUserStats.GetStat("TARGET_SCORE", out targetScore))
        {
            scoreText.StringReference.Arguments = new List<object>() {targetScore};
        }
        if (SteamUserStats.GetStat("PARKOUR_TIME", out parkourTime))
        {
            parkourTimeText.StringReference.Arguments = new List<object>() {parkourTime};
        }
        if (SteamUserStats.GetStat("EXPERIENCE", out exp))
        {
            expText.StringReference.Arguments = new List<object>() {exp};
        }
        if (SteamUserStats.GetStat("DEATHS", out death))
        {
            deathsText.StringReference.Arguments = new List<object>() {death};
        }
    }

    public int startGame, kills,wins,targetScore,exp,death;
    public float parkourTime;

    public void EnableDetailed()
    {
        statusDetailed.gameObject.SetActive(true);
    }

    public void DisableDetailed()
    {
        statusDetailed.gameObject.SetActive(false);
    }
    
}
