using CodingDaniel.MapEditor.MEEditor.MESave;
using Menu;
using Multiplayer;
using UnityEngine.SceneManagement;

namespace Mode
{
    public class TeamDeathMatch : GameModes
    {
        public static TeamDeathMatch Instance;

    
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.TeamDeathMatch)
            {
                enabled = false;
                return;
            }
            Instance = this;
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap!=null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.TeamDeathMatch.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();
        
            Invoke(nameof(Voice),1.5f);
        }

        void Voice()
        {
            VoiceLine.Instance.PlayVoice(VoiceKey.team_deathMatch);
            VoiceLine.Instance.PlayVoice(VoiceKey.team_deathMatch_description);
        }
    }
}
