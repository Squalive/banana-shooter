
using Menu;
using Steamworks;
using UnityEngine;

public class AchievementMenu : MonoBehaviour
{
    public GameObject achievementPrefab;

    public Transform achievementParent;

    
    private void Start()
    {
       uint len= SteamUserStats.GetNumAchievements();
       for (uint i = 0; i < len; i++)
       { 
          string achievementName= SteamUserStats.GetAchievementName(i);
          bool hidden =  SteamUserStats.GetAchievementDisplayAttribute(achievementName, "hidden") == "1" ;
          if(hidden) continue;
          
          string desc = SteamUserStats.GetAchievementDisplayAttribute(achievementName, "desc");
          string name = SteamUserStats.GetAchievementDisplayAttribute(achievementName, "name");
          
          Instantiate(achievementPrefab,achievementParent).GetComponent<AchivementItem>().SetAchievement(achievementName,desc,name);
       }
    }
}
