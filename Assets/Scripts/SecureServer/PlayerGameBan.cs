
using System;

namespace SecureServer
{
    public class PlayerGameBan
    {
        public string Key = String.Empty;
        public ulong SteamId;
        public ulong ReportId;
        public string CheatDescription = String.Empty;
        public uint Duration = 0;
        public uint Flags = 0;

        public PlayerGameBan(string key,ulong steamId, ulong reportId, string cheatDescription)
        {
            Key = key;
            SteamId = steamId;
            ReportId = reportId;
            CheatDescription = cheatDescription;
        }

        public PlayerGameBan()
        {
        }
    }
}