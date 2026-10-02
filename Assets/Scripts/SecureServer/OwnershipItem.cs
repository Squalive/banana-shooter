using System;
using Newtonsoft.Json;

namespace SecureServer
{
    public class OwnershipItem
    {
        public bool OwnsApp;
        public bool Permanent;
        public string TimeStamp  = String.Empty;
        public ulong OwnerSteamId;
        public bool SiteLicense;
   
        [JsonConstructor] 
        public OwnershipItem(bool ownsApp,bool permanent,string timeStamp,ulong ownerSteamId,bool siteLicense)
        {
            OwnsApp = ownsApp;
            Permanent = permanent;
            TimeStamp = timeStamp;
            OwnerSteamId = ownerSteamId;
            SiteLicense = siteLicense;
        }

        public OwnershipItem()
        {
        
        }
    }
}