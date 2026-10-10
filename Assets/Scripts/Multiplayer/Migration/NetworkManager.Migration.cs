using Steamworks;

namespace Multiplayer
{
    public partial class NetworkManager
    {
        internal void ResumeAuthorize()
        {
            SteamUser.CancelAuthTicket(_currentTicket);
            ClientDataCount = 0;
            TryToAuthorize();
        }
    }
}
