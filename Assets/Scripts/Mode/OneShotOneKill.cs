using CodingDaniel.MapEditor.MEEditor.MESave;
using Menu;
using Multiplayer;
using Steamworks;
using UnityEngine.SceneManagement;

namespace Mode
{
    public class OneShotOneKill : GameModes
    {
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.OneShotOneKill)
            {
                enabled = false;
                return;
            }
    
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap!=null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.OneShotOneKill.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();
        }

    }
}
