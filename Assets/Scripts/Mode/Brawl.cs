using CodingDaniel.MapEditor.MEEditor.MESave;
using Menu;
using Multiplayer;
using Steamworks;
using UnityEngine.SceneManagement;

namespace Mode
{
    public class Brawl : GameModes
    {
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.Brawl)
            {
                enabled = false;
                return;
            }
    
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap!=null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.Brawl.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();
            Invoke(nameof(Voice),1.5f);
        }

        void Voice()
        {
            VoiceLine.Instance.PlayVoice(VoiceKey.brawl);
            VoiceLine.Instance.PlayVoice(VoiceKey.brawl_description);
        } 

    }
}
