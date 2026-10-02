using CodingDaniel.MapEditor.MEEditor.MESave;
using Multiplayer;
using UnityEngine.SceneManagement;

namespace Mode
{
    public class RocketMode : GameModes
    {
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.RocketMode)
            {
                enabled = false;
                return;
            }
    
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap!=null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.RocketMode.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();
        }
    }
}