using System;
using Newtonsoft.Json;

namespace SecureServer
{
    public class ReportPlayerCheatingItem
    {
        public ulong SteamId;
    
        public ulong SteamIdReporter;

        public ulong AppData;

        public bool Heuristic;

        public bool Detection;

        public bool PlayerReport;

        public string Match = String.Empty;

        public ReportPlayerCheatingItem(ulong steamId, ulong steamIdReporter, ulong appData, bool heuristic, bool detection,
            bool playerReport,string match)
        {
            SteamId = steamId;
            SteamIdReporter = steamIdReporter;
            AppData = appData;
            Heuristic = heuristic;
            Detection = detection;
            PlayerReport = playerReport;
            Match = match;
        }

        public ReportPlayerCheatingItem()
        {
        
        }
    }
}