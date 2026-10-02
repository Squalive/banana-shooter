using CodingDaniel.MapEditor.MEEditor.MESave;
using Menu;
using Multiplayer;
using Steamworks;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

namespace Mode
{
    public class KillConfirm : GameModes
    {
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.KillConfirm)
            {
                enabled = false;
                return;
            }
    
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap!=null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.KillConfirm.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();
        
            Tutorial.Instance.SetText("KillConfirmTip");
        }
    }
}
