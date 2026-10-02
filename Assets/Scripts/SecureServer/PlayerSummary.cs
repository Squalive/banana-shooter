using System;
using Newtonsoft.Json;

namespace SecureServer
{
    public class PlayerSummary
    {
        public ulong SteamId;

        public string PersonaName = String.Empty;
    
        public string Avatar = String.Empty;
    
    
        [JsonConstructor]
        public PlayerSummary(ulong steamId,string personaName,string avatar)
        {
            SteamId = steamId;
            PersonaName = personaName;
            Avatar = avatar;
        }

        public PlayerSummary()
        {
        
        }
    }
}