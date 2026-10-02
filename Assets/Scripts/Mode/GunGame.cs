
using CodingDaniel.MapEditor.MEEditor.MESave;
using Multiplayer;
using UnityEngine.SceneManagement;

namespace Mode
{
    public class GunGame : GameModes
    {
        public static GunGame Instance;

        public static readonly short[] WeaponIds = new short[] { 5, 15, 2, 1, 19, 12, 14, 9, 18, 17, 16, 10, 11, 6, 0, 13, 3, 4, 8, 7};

        public static readonly int UpdateWeaponRequired = 2;
        private void Awake()
        {
            if (NetworkManager.ClientGameMode != GameMode.GunGame)
            {
                enabled = false;
                return;
            }
    
            Instance = this;
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "CustomMap" && MapSaver.CurrentMap!=null) sceneName = MapSaver.CurrentMap.name;
            NetworkManager.Instance.SetRichPreference(GameMode.GunGame.ToString(), sceneName);
            LobbyManager.Instance.SetLobbyGameMode();
        }
    }
}