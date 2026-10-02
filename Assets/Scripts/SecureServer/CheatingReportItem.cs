
using Newtonsoft.Json;

namespace SecureServer
{
    public class CheatingReportItem
    {
        public ulong ReportId = 0;
    
        public ulong SteamId= 0;
    
        public ulong SteamIdReporter = 0;
    
        public ulong AppData= 0;
    
        public bool Heuristic = false;

        public bool Detection = false;

        public bool PlayerReport= false;

        public uint TimeReport= 0;

        [JsonConstructor]
        public CheatingReportItem(ulong reportId,ulong steamId,ulong steamIdReporter,ulong appData,bool heuristic,bool detection,bool playerReport,uint timeReport)
        {
            ReportId = reportId;
            SteamId = steamId;
            SteamIdReporter = steamIdReporter;
            AppData = appData;
            Heuristic = heuristic;
            Detection = detection;
            PlayerReport = playerReport;
            TimeReport = timeReport;
        }
        public CheatingReportItem()
        {
        
        }
    }
}