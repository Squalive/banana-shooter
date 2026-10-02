using System;
using Newtonsoft.Json;

namespace SecureServer
{
    public class RemovePlayerGameBan
    {
        public string Key = String.Empty;
        public ulong SteamId;

    
        [JsonConstructor]
        public RemovePlayerGameBan(string key, ulong steamId)
        {
            Key = key;
            SteamId = steamId;
        }

        public RemovePlayerGameBan()
        {
        
        }
    }
}