
#define ONLINE
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Web
{
    public static class HttpClient
    {
        public static async Task<T> Get<T>(string endpoint,bool debug=false)
        {
            var getRequest = CreateRequest(endpoint);
            getRequest.timeout = 5;

            getRequest.SendWebRequest();


            while (!getRequest.isDone)
            {
                await Task.Delay(10);
            }

            if (getRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(getRequest.error);
            }
            
            string text = getRequest.downloadHandler.text;
            getRequest.Dispose();

            if (string.IsNullOrEmpty(text))
                return default(T);
            else if(debug)
            {
                Debug.Log(text);
            }

            return JsonConvert.DeserializeObject<T>(text, new JsonSerializerSettings(){ReferenceLoopHandling = ReferenceLoopHandling.Ignore});
        }
        
        public static async Task<string> Post(string endpoint,object payload)
        {
            var getRequest = CreateRequest(endpoint,RequestType.POST,payload);
            
            getRequest.SendWebRequest();

            while (!getRequest.isDone)
            {
                await Task.Delay(10);
            }
            string text =getRequest.downloadHandler.text;

            getRequest.Dispose();
            return text;
            // return JsonConvert.DeserializeObject<T>(getRequest.downloadHandler.text);
        }

        private static UnityWebRequest CreateRequest(string path, RequestType type = RequestType.GET,
            object data = null)
        {
            var request = new UnityWebRequest(path, type.ToString());

            if (data != null)
            {
                string json = JsonConvert.SerializeObject(data);
                var bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type","application/json");

            return request;
        }
    }

    public static class EndPoint
    {
        
#if ONLINE
        const string Base = "https://www.bsserver.uk/";
#else
        const string Base = "https://localhost:44395/";
#endif
        

        public static string GetCheatingReports = $"{Base}Steamworks/GetCheatingReports/";
        public static string GetPlayerSummaries = $"{Base}Steamworks/GetPlayerSummaries/";
        public static string GetPlayerBansSummaries = $"{Base}Steamworks/GetPlayerBans/";
        
        public static string GetRoles = $"{Base}Roles/GetRoles/";
        public static string ReportCheating = $"{Base}Steamworks/ReportPlayerCheating/";
        public static string RequestDailyReward = $"{Base}Steamworks/RequestDailyReward/";
        public static string AntiCheatDetection = $"{Base}Steamworks/AntiCheatDetection/";
        
        public static string GetNews = $"{Base}Steamworks/News/";
    }

    public enum RequestType
    {
        GET =0,
        POST,
    }
}
