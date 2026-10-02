
using Steamworks.NET;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Menu
{
    public class BanMenu : MonoBehaviour
    {
        private void Awake()
        {
            if (SteamManager.Initialized)
            {
                if (SteamManager.CurrentUserBanSummary.VacBanned)
                {
                    foreach (var gameBan in SteamManager.CurrentUserBanSummary.Bans)
                    {
                        if (gameBan.AppIdMin == gameBan.AppIdMax && gameBan.AppIdMax == 1949740)
                        {
                            ban.SetActive(true);
                            foreach (var btn in btns)
                            {
                                btn.interactable = false;
                            }
                            Debug.Log("User got game banned");
                            banText.SetEntry("ban");
                            return;
                        }
                    }

                    // if (SteamManager.CurrentUserBanSummary.NumberOfVacBans - SteamManager.CurrentUserBanSummary.NumberOfGameBans > 0)
                    // {
                    //     ban.SetActive(true);
                    //     banText.SetEntry("vac_ban");
                    //     Debug.Log("User got VAC banned");
                    // }
                }
            }
        }

        [SerializeField] private LocalizeStringEvent banText;

        public Button[] btns;

        public GameObject ban;

    }
}
