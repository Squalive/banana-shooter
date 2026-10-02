using CodingDaniel.MapEditor.MEEditor.MESave;
using Multiplayer;
using UnityEngine.SceneManagement;

namespace Mode
{
    public class Randomizer : GameModes
    {
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.Randomizer)
            {
                enabled = false;
                return;
            }
    
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap!=null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.Randomizer.ToString(), sceneName);
            
            LobbyManager.Instance.SetLobbyGameMode();
        }

    }
}
