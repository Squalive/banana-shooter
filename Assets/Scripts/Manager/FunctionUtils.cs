
using Console;
using Menu;
using Movement;
using Multiplayer;

namespace Manager
{
    public static class FunctionUtils
    {
        public static bool NoClip()
        {
            if (!NetworkManager.ClientCheatsEnabled && !RolesManager.Instance.CheckIsAdmin(NetworkManager.Instance.steamId.m_SteamID))
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("Admin permission required");
                return false;
            }

            if (PlayerMovement.Instance)
            {
                PlayerMovement.Instance.ChangeNoClip();
            }

            return true;
        }

        public static bool IsBlocked()
        {
            if (GameUIManager.Instance && GameUIManager.Instance.pause || NetworkManager.Instance.CantPlay(false))
            {
                return true;
            }

            return false;
        }
    }
}