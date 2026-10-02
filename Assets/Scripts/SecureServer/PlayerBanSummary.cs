using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecureServer
{
    public class PlayerBanSummary
    {
        public ulong SteamId=0;
        public bool CommunityBanned=false;
        public bool VacBanned=false;
        public uint NumberOfVacBans=0;
        public uint DaysSinceLastBan=0;
        public uint NumberOfGameBans=0;

        public List<GameBan> Bans = new List<GameBan>();

        public string EconomyBan = String.Empty;
    
        [JsonConstructor]
        public PlayerBanSummary(ulong steamId,bool communityBanned,bool vacBanned,uint numberOfGameBans,uint numberOfVacBans,uint daysSinceLastBan)
        {
            SteamId = steamId;
            CommunityBanned = communityBanned;
            VacBanned = vacBanned;
            NumberOfVacBans = numberOfVacBans;
            DaysSinceLastBan = daysSinceLastBan;
            NumberOfGameBans = numberOfGameBans;
        }

        public PlayerBanSummary()
        {
            
        }
    }

    public class GameBan
    {
        public uint AppIdMin;
        public uint AppIdMax;
        public uint BanStartTime;
        public int BanType;
        public ulong BanProviderId;

        [JsonConstructor]
        public GameBan(uint appIdMin, uint appIdMax, uint banStartTime, int banType, ulong banProviderId)
        {
            AppIdMin = appIdMin;
            AppIdMax = appIdMax;
            BanStartTime = banStartTime;
            BanType = banType;
            BanProviderId = banProviderId;
        }

        public GameBan()
        {
        
        }
    }
}